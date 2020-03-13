using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.Common.Constants;
using net.jancerveny.sofaking.Common.Models;
using net.jancerveny.sofaking.DataLayer;
using net.jancerveny.sofaking.DataLayer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.client.console
{
    class Program
    {
        private static ServiceProvider _serviceProvider;
        static async Task Main(string[] args)
        {
            IVerifiedMovie selectedMovie = null;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true) // TODO: Change the Production with Enviroment
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var proxies = new List<string>();
            configuration.GetSection("TPBProxies").Bind(proxies);
            if(proxies.Count == 0)
            {
                throw new Exception("TPB Proxies configuration missing");
            }
            TPBProxies.SetProxies(proxies.ToArray());

            var transmissionConfiguration = new TransmissionConfiguration();
            configuration.GetSection("Transmission").Bind(transmissionConfiguration);
            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddHttpClient()
                .AddSingleton(transmissionConfiguration)
                .AddSingleton(new SoFakingContextFactory())
                .AddSingleton<MovieService>()
                .AddSingleton<TPBParserService>()
                .AddSingleton<ITorrentClientService, TransmissionService>()
                .AddSingleton<IVerifiedMovieSearchService, ImdbService>()
                .BuildServiceProvider();

            while (true)
            {
                var movieService = _serviceProvider.GetService<MovieService>();
                Console.Clear();
                Console.ResetColor();
                Console.WriteLine("Enter a movie name in English to look for: (CTRL+C to quit)");
                var query = Console.ReadLine();
                var verifiedMovieSearch = _serviceProvider.GetService<IVerifiedMovieSearchService>();
                var verifiedMovies = await verifiedMovieSearch.Search(query);
                if (verifiedMovies.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No results found.");
                    Console.ResetColor();
                    continue;
                }

                if (verifiedMovies.Count > 1)
                {
                    Console.WriteLine("There are several matches:");
                    var movieJobs = movieService.GetMovies();

                    for (var i = 0; i < verifiedMovies.Count(); i++)
                    {
                        var vm = verifiedMovies.ElementAt(i);
                        var status = string.Empty;
                        var movieJob = movieJobs.Where(x => x.ImdbId == vm.Id).FirstOrDefault();
                        if (movieJob != null)
                        {
                            switch (movieJob.Status)
                            {
                                case MovieStatusEnum.Downloaded:
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    status = "Dlded";
                                    break;
                                case MovieStatusEnum.Downloading:
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    status = "Dlding";
                                    break;
                                case MovieStatusEnum.Finished:
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    status = "Fnishd";
                                    break;
                                case MovieStatusEnum.Queued:
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    status = "Queued";
                                    break;
                                case MovieStatusEnum.Transcoding:
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    status = "Transc";
                                    break;
                                case MovieStatusEnum.WatchingFor:
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    status = "Wtchng";
                                    break;
                            }

                            if(movieJob.Deleted != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                status = "\u2713";
                            }
                        }

                        Console.WriteLine($"[{i}]\t{vm.Score}/10\t{vm.ScoreMetacritic} Metacritic\t{status}\t{vm.Title} ({vm.ReleaseYear})");

                        if (movieJob != null)
                            Console.ResetColor();
                    }
                    Console.WriteLine("[n] for new search");

                    bool restartFlag = false;
                    while (true)
                    {
                        var key1 = Console.ReadKey();
                        if (key1.KeyChar == 'n')
                        {
                            restartFlag = true;
                            break;
                        }

                        if(int.TryParse(key1.KeyChar.ToString(), out int selectedMovieIndex))
                        {
                            selectedMovie = verifiedMovies.ElementAt(selectedMovieIndex);
                            break;
                        }
                    }

                    if(restartFlag)
                    {
                        continue;
                    }
                }

                var torrentSearchService = _serviceProvider.GetService<TPBParserService>();
                var results = await torrentSearchService.Search($"{selectedMovie.Title} {selectedMovie.ReleaseYear}");
                if(results.Count == 0)
                {
                    Console.Write("\r\n");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No torrents found.");
                    Console.ResetColor();

                    await AddToWatchlistPrompt(new Movie
                    {
                        Title = selectedMovie.Title,
                        ImdbId = selectedMovie.Id,
                        ImdbScore = selectedMovie.Score,
                        MetacriticScore = selectedMovie.ScoreMetacritic,
                        TorrentName = query,
                        Status = MovieStatusEnum.WatchingFor
                    });
                    continue;
                }

                foreach (var t in results)
                {
                    Console.WriteLine($"{t.Seeders}\t{t.SizeGb}Gb\t{t.Name}");
                }

                var bestTorrent = TorrentRating.GetBestTorrent(results);
                if (bestTorrent == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"None of the torrents above passed the limits (Seeders > 0, Size Gb > {TorrentScoring.FileSizeGbMin} < {TorrentScoring.FileSizeGbMax}), or contained a banned word: {string.Join(", ", TorrentScoring.Tags.Where(x => x.Value == TorrentScoring.BannedTag).Select(x => x.Key))}");
                    Console.ResetColor();

                    await AddToWatchlistPrompt(new Movie
                    {
                        Title = selectedMovie.Title,
                        ImdbId = selectedMovie.Id,
                        ImdbScore = selectedMovie.Score,
                        MetacriticScore = selectedMovie.ScoreMetacritic,
                        TorrentName = query,
                        Status = MovieStatusEnum.WatchingFor
                    });
                    continue;
                }
                Console.WriteLine("\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Best torrent: ({bestTorrent.Seeders} Seeders), {bestTorrent.SizeGb}Gb {bestTorrent.Name}");
                Console.ResetColor();
                Console.WriteLine("\n");
                Console.WriteLine("Download? [y/n], CTRL+C = quit)");
                var key = Console.ReadKey();
                if (key.KeyChar == 'y')
                {
                    var transmission = _serviceProvider.GetService<ITorrentClientService>();
                    try
                    {
                        var result = await transmission.AddTorrentAsync(bestTorrent.MagnetLink);
                        try
                        {
                            var m = new Movie
                            {
                                Title = selectedMovie.Title,
                                ImdbId  = selectedMovie.Id,
                                ImdbScore = selectedMovie.Score,
                                MetacriticScore = selectedMovie.ScoreMetacritic,
                                TorrentName = bestTorrent.Name,
                                TorrentClientTorrentId = result.TorrentId,
                                TorrentHash = result.Hash,
                                SizeGb = bestTorrent.SizeGb
                            };
                            if (!await movieService.AddMovie(m))
                            {
                                throw new Exception("db failed");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not add Movie to the catalog: {ex.Message}");
                            Console.ReadKey();
                        }
                    } catch(Exception ex)
                    {
                        Console.WriteLine($"Could not add torrent to download: {ex.Message}");
                        Console.ReadKey();
                    }
                }
            }
        }

        private static async Task AddToWatchlistPrompt(Movie movie)
        {
            Console.Write("\r\n");
            Console.WriteLine("[y/n] Add to watchlist?");
            var key = Console.ReadKey();
            if (key.KeyChar == 'y')
            {
                var movieService = _serviceProvider.GetService<MovieService>();
                try
                {
                    await movieService.AddMovie(movie);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not add Movie to the watchlist: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }
    }
}
