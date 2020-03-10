using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class TPBCrawlerService
    {
        private int _pagingDepthLimit = 3;
        private int _maxAttempts = 10;
        private readonly IHttpClientFactory _clientFactory;
        private static Regex isSeries = new Regex(@"\ss\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public TPBCrawlerService(IHttpClientFactory clientFactory)
        {
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _clientFactory = clientFactory;
        }

        /// <summary>
        /// Search for a Movie torrent
        /// </summary>
        /// <param name="query">The movie name to search for</param>
        /// <returns>A list of torrents ordered by Seeders</returns>
        public async Task<List<TorrentSearchResult>> Search(string query)
        {
            // TODO: Refactor this to make own TPB-crawler-specific client
            using (var client = _clientFactory.CreateClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                var i = 0;

                while (true)
                {
                    if (i == _maxAttempts)
                    {
                        throw new Exception($"Reached the limit of max attempts {_maxAttempts}");
                    }
                    i++;

                    var host = TPBProxies.GetProxy();

                    try
                    {
                        return await FetchSearchResultPageAsync(client, host, query, (isSeries.Match(query).Success ? TPBCategoriesEnum.HDShows : TPBCategoriesEnum.HDMovies));
                    }
                    catch (Exception ex)
                    {
                        if (ex is HttpRequestException || ex is ParsingErrorException)
                        {
                            TPBProxies.ProxyInvalid();
                            continue;
                        }

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Fetch results page-by-page
        /// </summary>
        /// <param name="client">HTTP Client</param>
        /// <param name="host">The TPB Proxy Host</param>
        /// <param name="query">The search query</param>
        /// <param name="pageNumber">Page number</param>
        /// <returns></returns>
        private async Task<List<TorrentSearchResult>> FetchSearchResultPageAsync(HttpClient client, string host, string query, TPBCategoriesEnum category = TPBCategoriesEnum.HDMovies, int pageNumber = 1)
        {
            var result = new List<TorrentSearchResult>();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{host}search/{HttpUtility.UrlEncode(query)}/{pageNumber}/{(int)TPBOrderByEnum.SeedersDesc}/{(int)category}");
            HttpResponseMessage response;

            response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new ParsingErrorException($"Unexpected HTTP response code: {response.StatusCode}");
            }

            var htmlSource = await response.Content.ReadAsStringAsync();
            var searchResultsHtml = Regexes.SearchResults.Match(htmlSource);

            if (!searchResultsHtml.Success)
            {
                throw new ParsingErrorException("No search results found");
            }
            else
            {
                var rows = Regexes.Row.Matches(searchResultsHtml.Value);
                foreach (Match m in rows)
                {
                    var name = Regexes.RowName.Match(m.Value).Groups[2].Value;
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    int.TryParse(Regexes.RowSeedersLeeches.Match(m.Value).Groups[1].Value, out int seeders);
                    int.TryParse(Regexes.RowSeedersLeeches.Match(m.Value).Groups[2].Value, out int leeches);
                    // TPB automatically converts file size to Meegabytes, if the file size is less than a Gb.
                    if (!double.TryParse(Regexes.RowSizeGiB.Match(m.Value).Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double size))
                    {
                        double.TryParse(Regexes.RowSizeMiB.Match(m.Value).Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out size);
                        size = Math.Ceiling((size/1024)*1000)/1000;
                    }

                    result.Add(new TorrentSearchResult
                    {
                        Name = Regexes.HTMLTags.Replace(name, string.Empty),
                        DetailsLink = Regexes.HTMLTags.Replace(Regexes.RowName.Match(m.Value).Groups[1].Value, string.Empty),
                        MagnetLink = Regexes.HTMLTags.Replace(Regexes.RowMagnetLink.Match(m.Value).Groups[1].Value, string.Empty),
                        Seeders = seeders,
                        Leeches = leeches,
                        SizeGb = size
                    });
                }
            }

            // Load next page of results if there is one, and if the last item had any seeders (we're ordering by seeders desc)
            var pagingHtml = Regexes.PagingRow.Match(searchResultsHtml.Value);
            if (pagingHtml.Success)
            {
                var availablePages = new List<int>();
                foreach(Match p in Regexes.PagingRowPage.Matches(pagingHtml.Value))
                {
                    if (int.TryParse(p.Groups[1].Value, out int pnr))
                    {
                        availablePages.Add(pnr);
                    }
                }

                var nextPage = pageNumber + 1;
                if (result.Count() > 0 && nextPage <= _pagingDepthLimit && result.Last().Seeders > 0 && availablePages.Contains(nextPage))
                {
                    result.AddRange(await FetchSearchResultPageAsync(client, host, query, category, nextPage));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Compiled Regexes for better performance
    /// </summary>
    public static class Regexes
    {
        public static Regex SearchResults => new Regex(@"<table id=""searchResult"">(.+)<\/table>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex Row => new Regex(@"<tr(?:\sclass=""alt"")?>(.+?)<\/tr>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex RowName => new Regex(@"<div class=""detName"">\s*<a href=""(.+?)""(?:.+?)?title=""Details\sfor (.+?)"">", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex RowMagnetLink => new Regex(@"<a href=""(magnet:.+?)"">", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex RowSeedersLeeches => new Regex(@"<td(?:.*?)?>(\d+)<\/td>\s*<td(?:.*?)?>(\d+)<\/td>\s*<\/tr>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex RowSizeGiB => new Regex(@"Size\s([\d\.]+)\sGiB", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex RowSizeMiB => new Regex(@"Size\s([\d\.]+)\sMiB", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex PagingRow => new Regex(@"<tr>\s*<td colspan=""9""(?:.+?)?>(.+)<\/td>\s*<\/tr>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex PagingRowPage => new Regex(@"<a(?:.+?)>(\d+)<\/a>\s", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex HTMLTags => new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
