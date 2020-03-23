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
            public static Regex LdJson = new Regex(@"<script type=""application/ld\+json"">(.+?)</script>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
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
            if(string.IsNullOrWhiteSpace(queryRaw))
            {
                return null;
            }

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
            if(imdbResponse.Matches == null)
            {
                return null;
            }

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
                        var ldJson = Regexes.LdJson.Match(imdbMovieDetailResponse);
                        if (!ldJson.Success)
                        {
                            return null;
                        }

                        var rawJson = ldJson.Groups[1].Value;
                        IMDBStructuredData structuredData;
                        try
                        {
                            structuredData = JsonSerializer.Deserialize<IMDBStructuredData>(rawJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                        } catch(Exception ex)
                        {
                            return null;
                        }

                        double.TryParse(structuredData.AggregateRating.RatingValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double imdbScore);
                        int.TryParse(Regexes.MetacriticScore.Match(imdbMovieDetailResponse).Groups[1].Value, out int metacriticScore);
                        GenreFlags genres = GenreFlags.Other;
                        foreach (string genre in structuredData.Genre)
                        {
                            foreach (GenreFlags enumVal in Enum.GetValues(typeof(GenreFlags)))
                            {
                                if(enumVal.ToString().ToLower() == genre.ToLower().Replace("-", string.Empty).Replace(" ", string.Empty))
                                {
                                    genres |= enumVal;
                                }
                            }
                        }

                        return new MovieSearchResult
                        {
                            Id = imdbMatch.Id,
                            Title = imdbMatch.Title,
                            ReleaseYear = imdbMatch.Year,
                            Score = imdbScore,
                            ScoreMetacritic = metacriticScore,
                            ImageUrl = imdbMatch.Image?.ImageUrl,
                            Genres = genres,
                            Description = structuredData.Description,
                            Director = structuredData.Director?.Name
                        };
                    }
                });

            return (await Task.WhenAll(moviesWithDetails))
                .Where(x => x != null)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.ScoreMetacritic)
                .ToList();
        }
    }
}
