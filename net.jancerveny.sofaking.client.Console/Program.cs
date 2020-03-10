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
                .AddSingleton<TPBCrawlerService>()
                .AddSingleton<ITorrentClientService, TransmissionService>()
                .BuildServiceProvider();


            while (true)
            {
                Console.Clear();
                Console.ResetColor();
                Console.WriteLine("Enter a movie name to look for: (CTRL+C to quit)");
                var query = Console.ReadLine();
                var crawler = _serviceProvider.GetService<TPBCrawlerService>();
                var results = await crawler.Search(query);
                if(results.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No results found.");
                    Console.ResetColor();
                    //Console.ReadKey();
                    await AddToWatchlist(new Movie
                    {
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
                    //Console.ReadKey();
                    await AddToWatchlist(new Movie
                    {
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
                Console.WriteLine("Download? (y/n, CTRL+C = quit)");
                var key = Console.ReadKey();
                if (key.KeyChar == 'y')
                {
                    var transmission = _serviceProvider.GetService<ITorrentClientService>();
                    try
                    {
                        var result = await transmission.AddTorrentAsync(bestTorrent.MagnetLink);
                        await AddToWatchlist(new Movie
                        {
                            TorrentName = query,
                            TorrentClientTorrentId = result.TorrentId,
                            TorrentHash = result.Hash,
                            Status = MovieStatusEnum.Queued
                        });
                    } catch(Exception ex)
                    {
                        Console.WriteLine($"Could not add torrent to download: {ex.Message}");
                        Console.ReadKey();
                    }
                    //ProcessStartInfo psi = new ProcessStartInfo
                    //{
                    //    FileName = bestTorrent.MagnetLink,
                    //    UseShellExecute = true
                    //};
                    //Process.Start(psi);
                }
            }
        }

        private static async Task AddToWatchlist(Movie movie)
        {

            Console.WriteLine("Add to watchlist?");
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
                    Console.WriteLine($"Could not add torrent to download: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }
    }
}
