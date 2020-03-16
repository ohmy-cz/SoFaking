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
                    var movieJobs = movieService.GetMovies();
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
                                    case MovieStatusEnum.DownloadQueued:
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

                                if (movieJob.Deleted != null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    status = "\u2713";
                                }
                            }

                            Console.WriteLine($"[{(i+1)}]\t{vm.Score}/10\t{vm.ScoreMetacritic} Metacritic\t{status}\t{vm.Title} ({vm.ReleaseYear})");

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

                        await AddToWatchlistPrompt(new Movie
                        {
                            Title = selectedMovie.Title,
                            ImdbId = selectedMovie.Id,
                            ImdbScore = selectedMovie.Score,
                            MetacriticScore = selectedMovie.ScoreMetacritic,
                            TorrentName = query,
                            ImageUrl = selectedMovie.ImageUrl,
                            Status = MovieStatusEnum.WatchingFor
                        }, movieService);
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
                                case MovieStatusEnum.DownloadQueued:
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

                            if (movieJob.Deleted != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                status = "\u2713";
                            }
                        }

                        Console.WriteLine($"[{(i+1)}]\t{t.Seeders}\t{t.SizeGb}Gb\t{t.Name}");

                        if (movieJob != null)
                            Console.ResetColor();
                    }

                    Console.WriteLine("\n");
                    var bestTorrent = TorrentRating.GetBestTorrent(foundTorrents);
                    if (bestTorrent == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"None of the torrents above passed the limits (Seeders > 0, Size Gb > {TorrentScoring.FileSizeGbMin} < {TorrentScoring.FileSizeGbMax}), or contained a banned word: {string.Join(", ", TorrentScoring.Tags.Where(x => x.Value == TorrentScoring.BannedTag).Select(x => x.Key))}");
                        Console.ResetColor();
                        Console.WriteLine("\n");
                        Console.WriteLine($"Cancel? [n], [1-{(foundTorrents.Count() + 1)}] for manual selection, CTRL+C = quit)");

                        //await AddToWatchlistPrompt(new Movie
                        //{
                        //    Title = selectedMovie.Title,
                        //    ImdbId = selectedMovie.Id,
                        //    ImdbScore = selectedMovie.Score,
                        //    MetacriticScore = selectedMovie.ScoreMetacritic,
                        //    TorrentName = query,
                        //    ImageUrl = selectedMovie.ImageUrl,
                        //    Status = MovieStatusEnum.WatchingFor
                        //},  movieService);
                        //continue;
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
                        var key2 = Console.ReadKey();
                        if (key2.KeyChar == 'n')
                        {
                            restartFlag2 = true;
                            break;
                        }

                        if (key2.KeyChar == 'y'  &&  bestTorrent == null)
                        {
                            continue;
                        }

                        if (key2.KeyChar != 'y' && int.TryParse(key2.KeyChar.ToString(), out int selectedTorrentIndex))
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
                                var m = new Movie
                                {
                                    Title = selectedMovie.Title,
                                    ImdbId = selectedMovie.Id,
                                    ImdbScore = selectedMovie.Score,
                                    MetacriticScore = selectedMovie.ScoreMetacritic,
                                    TorrentName = selectedTorrent.Name,
                                    TorrentClientTorrentId = result.TorrentId,
                                    TorrentHash = result.Hash,
                                    ImageUrl = selectedMovie.ImageUrl,
                                    SizeGb = selectedTorrent.SizeGb
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
    }
}
