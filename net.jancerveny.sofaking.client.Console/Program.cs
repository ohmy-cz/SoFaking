using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        //private static ServiceProvider _serviceProvider;
        static async Task Main(string[] args)
        {
            IVerifiedMovie selectedMovie = null;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // TODO: Change the Production with Enviroment
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var proxies = new List<string>();
            configuration.GetSection("TPBProxies").Bind(proxies);
            var sofakingConfiguration = new SoFakingConfiguration();
            configuration.GetSection("Sofaking").Bind(sofakingConfiguration);
            if (proxies.Count == 0)
            {
                throw new Exception("TPB Proxies configuration missing");
            }
            TPBProxies.SetProxies(proxies.ToArray());


            var transmissionConfiguration = new TransmissionConfiguration();
            configuration.GetSection("Transmission").Bind(transmissionConfiguration);

            //TransmissionHttpClient.Configure(transmissionConfiguration)

            var builder1 = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddHttpClient()
                        .AddSingleton(transmissionConfiguration)
                        .AddSingleton<TransmissionHttpClientFactory>()
                        .AddSingleton(new SoFakingContextFactory())
                        .AddSingleton<MovieService>()
                        .AddSingleton<TPBParserService>()
                        .AddSingleton<ITorrentClientService, TransmissionService>()
                        .AddSingleton<IVerifiedMovieSearchService, ImdbService>();
                }).UseConsoleLifetime();

            var host = builder1.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var serviceProvider = serviceScope.ServiceProvider;









                while (true)
                {
                    var movieService = serviceProvider.GetService<MovieService>();
                    Console.Clear();
                    Console.ResetColor();
                    Console.WriteLine("Enter a movie name in English to look for: (CTRL+C to quit)");
                    var query = Console.ReadLine();
                    var verifiedMovieSearch = serviceProvider.GetService<IVerifiedMovieSearchService>();
                    var verifiedMovies = await verifiedMovieSearch.Search(query);
                    var movieJobs = await movieService.GetMoviesAsync();
                    if (verifiedMovies == null || verifiedMovies.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No results found.");
                        Console.ResetColor();
                        Console.ReadKey();
                        continue;
                    }

                    if (verifiedMovies.Count == 1)
                    {
                        selectedMovie = verifiedMovies.ElementAt(0);
                    }

                    if (verifiedMovies.Count > 1)
                    {
                        Console.WriteLine("There are several matches:");

                        for (var i = 0; i < verifiedMovies.Count(); i++)
                        {
                            var vm = verifiedMovies.ElementAt(i);
                            var status = string.Empty;
                            var movieJob = movieJobs.Where(x => x.ImdbId == vm.Id).FirstOrDefault();
                            if (movieJob != null)
                            {
                                status = SetMovieConsoleStatus(movieJob);
                            }

                            Console.WriteLine($"[{(i+1)}]\t{vm.Score}/10\t{vm.ScoreMetacritic} Metacritic\t{status}\t{vm.Title.Utf8ToAscii()} ({vm.ReleaseYear})");

                            if (movieJob != null)
                                Console.ResetColor();
                        }
                        Console.WriteLine("[n] for new search");

                        bool restartFlag1 = false;
                        while (true)
                        {
                            var key1 = Console.ReadKey();
                            if (key1.KeyChar == 'n')
                            {
                                restartFlag1 = true;
                                break;
                            }

                            if (int.TryParse(key1.KeyChar.ToString(), out int selectedMovieIndex))
                            {
                                selectedMovie = verifiedMovies.ElementAt(selectedMovieIndex-1);
                                break;
                            }
                        }

                        if (restartFlag1)
                        {
                            continue;
                        }
                    }

                    var torrentSearchService = serviceProvider.GetService<TPBParserService>();
                    var foundTorrents = await torrentSearchService.Search($"{selectedMovie.Title} {selectedMovie.ReleaseYear}");
                    if (foundTorrents.Count == 0)
                    {
                        Console.Write("\r\n");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No torrents found.");
                        Console.ResetColor();

                        var m = MergeMovie(selectedMovie);
                        m.TorrentName = query;
                        m.Status = MovieStatusEnum.WatchingFor;
                        await AddToWatchlistPrompt(m, movieService);
                        continue;
                    }













                    Console.Clear();
                    for (var i = 0; i < foundTorrents.Count(); i++)
                    {
                        var t = foundTorrents.ElementAt(i);
                        var status = string.Empty;
                        var movieJob = movieJobs.Where(x => x.TorrentName == t.Name).FirstOrDefault();
                        if (movieJob != null)
                        {
                            status = SetMovieConsoleStatus(movieJob);
                        }

                        Console.WriteLine($"[{(i+1)}]\t{t.Seeders}\t{t.SizeGb}Gb\t{status}\t{t.Name}");

                        if (movieJob != null)
                            Console.ResetColor();
                    }

                    Console.WriteLine("\n");
                    var bestTorrent = TorrentRating.GetBestTorrent(foundTorrents, sofakingConfiguration.AudioLanguages);
                    if (bestTorrent == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"None of the torrents above passed the limits (Seeders > 0, Size Gb > {TorrentScoring.FileSizeGbMin} < {TorrentScoring.FileSizeGbMax}), or contained a banned word: {string.Join(", ", TorrentScoring.Tags.Where(x => x.Value == TorrentScoring.BannedTag).Select(x => x.Key))}");
                        Console.ResetColor();
                        Console.WriteLine("\n");
                        Console.WriteLine($"Cancel? [n], [1-{(foundTorrents.Count() + 1)}] for manual selection, CTRL+C = quit)");

                        var m = MergeMovie(selectedMovie);
                        m.TorrentName = query;
                        m.Status = MovieStatusEnum.WatchingFor;
                        await AddToWatchlistPrompt(m, movieService);
                        continue;
                    } else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Best torrent: ({bestTorrent.Seeders} Seeders), {bestTorrent.SizeGb}Gb {bestTorrent.Name}");
                        Console.ResetColor();
                        Console.WriteLine("\n");
                        Console.WriteLine($"Download the best torrent? [y/n], [1-{(foundTorrents.Count() + 1)}] for manual selection, CTRL+C = quit)");
                    }

                    bool restartFlag2 = false;
                    while (true)
                    {
                        var selectedTorrent = bestTorrent;
                        string input = Console.ReadLine();
                        
                        if (input == "n")
                        {
                            restartFlag2 = true;
                            break;
                        }

                        if (input == "y" && bestTorrent == null)
                        {
                            continue;
                        }

                        if (input != "y" && int.TryParse(input, out int selectedTorrentIndex))
                        {
                            selectedTorrent = foundTorrents.ElementAt(selectedTorrentIndex - 1);
                        }

                        var transmission = serviceProvider.GetService<ITorrentClientService>();
                        try
                        {
                            var result = await transmission.AddTorrentAsync(selectedTorrent.MagnetLink);
                            if(result == null)
                            {
                                throw new Exception("Could not add torrent to the torrent client.");
                            }
                            try
                            {
                                var m = MergeMovie(selectedMovie, selectedTorrent, result);

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
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not add torrent to download: {ex.Message}");
                            Console.ReadKey();
                        }

                        break;
                    }

                    if (restartFlag2)
                    {
                        continue;
                    }
                }








            }


                //_serviceProvider = new ServiceCollection()
                //    .AddLogging()
                //    .AddHttpClient()
                //    .AddSingleton(transmissionConfiguration)
                //    .AddSingleton(new SoFakingContextFactory())
                //    .AddSingleton<MovieService>()
                //    .AddSingleton<TPBParserService>()
                //    .AddSingleton<ITorrentClientService, TransmissionService>()
                //    .AddSingleton<IVerifiedMovieSearchService, ImdbService>()
                //    .BuildServiceProvider();

        }

        private static async Task AddToWatchlistPrompt(Movie movie, MovieService ms)
        {
            Console.Write("\r\n");
            Console.WriteLine("[y/n] Add to watchlist?");
            var key = Console.ReadKey();
            if (key.KeyChar == 'y')
            {
                try
                {
                    await ms.AddMovie(movie);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not add Movie to the watchlist: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }

        private static string SetMovieConsoleStatus(Movie movieJob)
        {
            switch (movieJob.Status)
            {
                case MovieStatusEnum.WatchingFor:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    return "Watching";
                case MovieStatusEnum.DownloadQueued:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    return "Queued";
                case MovieStatusEnum.Downloading:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return "Downloading";
                case MovieStatusEnum.DownloadingPaused:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return "Paused";
                case MovieStatusEnum.Downloaded:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    return "Downloaded";
                case MovieStatusEnum.NoVideoFilesError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "No Video Files";
                case MovieStatusEnum.TranscodingQueued:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    return "Transcoding Queued";
                case MovieStatusEnum.AnalysisStarted:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return "Analysis Started";
                case MovieStatusEnum.TranscodingStarted:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return "Transcoding Started";
                case MovieStatusEnum.TranscodingFinished:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return "Transcoding Finished";
                case MovieStatusEnum.TranscodingError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Transcoding Error";
                case MovieStatusEnum.Finished:
                    Console.ForegroundColor = ConsoleColor.Green;
                    return "Finished";
                case MovieStatusEnum.FileInUse:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "File in use";
                case MovieStatusEnum.FileNotFound:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "File not found";
                case MovieStatusEnum.CouldNotDeleteDownloadDirectory:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Download not deleted";
                case MovieStatusEnum.TranscodingIncomplete:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Transcoding Incomplete";
                case MovieStatusEnum.TranscodingRunningTooLong:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Transcoding for too long";
                case MovieStatusEnum.AnalysisAudioFailed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Audio analysis failed";
                case MovieStatusEnum.AnalysisVideoFailed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Video analysis failed";
                case MovieStatusEnum.TorrentNotFound:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return "Torrent not found";
            }

            if (movieJob.Deleted != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                return "\u2713";
            }

            Console.ForegroundColor = ConsoleColor.Red;
            return "?";
        }

        private static Movie MergeMovie(IVerifiedMovie selectedMovie, TorrentSearchResult selectedTorrent = null, ITorrentAddedResponse result = null)
        {
            return new Movie
            {
                Title = selectedMovie.Title,
                ImdbId = selectedMovie.Id,
                ImdbScore = selectedMovie.Score,
                MetacriticScore = selectedMovie.ScoreMetacritic,
                TorrentName = selectedTorrent?.Name,
                TorrentClientTorrentId = result?.TorrentId ?? -1,
                TorrentHash = result?.Hash,
                ImageUrl = selectedMovie.ImageUrl,
                SizeGb = selectedTorrent?.SizeGb ?? -1,
                Genres = selectedMovie.Genres,
                Year = selectedMovie.ReleaseYear,
                Director = selectedMovie.Director,
                Creators = selectedMovie.Creators,
                Actors = selectedMovie.Actors,
                Description = selectedMovie.Description
            };
        }
    }
}
