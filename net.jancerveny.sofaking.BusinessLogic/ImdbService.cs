using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Collections.Concurrent;
using System.Linq;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class ImdbService : IVerifiedMovieSearchService
    {
        private static class Regexes
        {
            public static Regex ImdbScore => new Regex(@"class=""imdbRating""(?:.+?)<span itemprop=""ratingValue"">([\d.,]+?)<\/span>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            public static Regex MetacriticScore => new Regex(@"<(?:[a-z]+?)\sclass=""metacriticScore(?:.*?)?"">\s*<span>(.+?)<\/span>\s*<\/div>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            public static Regex ValidImdbObjectId = new Regex(@"tt\d+", RegexOptions.Compiled);
        }

        private readonly IHttpClientFactory _clientFactory;
        private static string endpoint = "https://v2.sg.media-imdb.com/suggestion";
        private static string movieDetailEndpoint = "https://www.imdb.com/title";
        private static Regex imdbUrlSafe = new Regex(@"[^a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public ImdbService(IHttpClientFactory clientFactory)
        {
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _clientFactory = clientFactory;
        }

        public async Task<IReadOnlyCollection<IVerifiedMovie>> Search(string queryRaw)
        {
            string jsonResponse;

            // Get all movie suggestions first
            using (var client = _clientFactory.CreateClient())
            {
                var query = imdbUrlSafe.Replace(queryRaw.Replace(" ", "_"), string.Empty).ToLower();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/{query.Substring(0, 1)}/{HttpUtility.HtmlEncode(query)}.json");

                HttpResponseMessage response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ParsingErrorException($"Unexpected IMDB HTTP response code: {response.StatusCode}");
                }

                jsonResponse = await response.Content.ReadAsStringAsync();
            }

            var imdbResponse = JsonSerializer.Deserialize<ImdbResponse>(jsonResponse);

            // search for movie details in parallel, as every movie needs to fetch na URL with its details.
            // sometimes, an actor (or other object type) can be returned, so we're looking for presence of the release year.
            var moviesWithDetails = imdbResponse.Matches
                .Where(x => Regexes.ValidImdbObjectId.Match(x.Id).Success)
                .Select(async imdbMatch => {
                    using (var client = _clientFactory.CreateClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"{movieDetailEndpoint}/{imdbUrlSafe.Replace(imdbMatch.Id, string.Empty)}");

                        HttpResponseMessage movieDetailResponse = await client.SendAsync(request);
                        if (!movieDetailResponse.IsSuccessStatusCode)
                        {
                            return null;
                        }

                        var imdbMovieDetailResponse = await movieDetailResponse.Content.ReadAsStringAsync();
                        double.TryParse(Regexes.ImdbScore.Match(imdbMovieDetailResponse).Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double imdbScore);
                        int.TryParse(Regexes.MetacriticScore.Match(imdbMovieDetailResponse).Groups[1].Value, out int metacriticScore);

                        return new MovieSearchResult
                        {
                            Id = imdbMatch.Id,
                            Title = imdbMatch.Title,
                            ReleaseYear = imdbMatch.Year,
                            Score = imdbScore,
                            ScoreMetacritic = metacriticScore,
                            ImageUrl = imdbMatch.Image?.ImageUrl
                        };
                    }
                });

            return ((await Task.WhenAll(moviesWithDetails)).Where(x => x != null).ToList());
        }
    }
}
