using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using net.jancerveny.sofaking.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.client.console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool endApp = false;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Production.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var proxies = new List<string>();
            configuration.GetSection("TPBPRoxies").Bind(proxies);
            if(proxies.Count == 0)
            {
                throw new Exception("TPB Proxies configuration missing");
            }

            TPBProxies.SetProxies(proxies.ToArray());
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddHttpClient()
                .AddSingleton<TPBCrawlerService>()
                .BuildServiceProvider();

            while (!endApp)
            {
                Console.WriteLine("Enter a movie name to look for: (CTRL+C to quit)");
                var query = Console.ReadLine();
                var crawler = serviceProvider.GetService<TPBCrawlerService>();
                var results = await crawler.Search(query);
                foreach (var t in results)
                {
                    Console.WriteLine($"{t.Seeders}\t{t.SizeGb}Gb\t{t.Name}");
                }
                var bestTorrent = TorrentRating.GetBestTorrent(results);
                Console.WriteLine("\n");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Best torrent: ({bestTorrent.Seeders} Seeders), {bestTorrent.SizeGb}Gb {bestTorrent.Name}");
                Console.ResetColor();
                Console.WriteLine("\n");
                Console.WriteLine("Download? (y/n, q = quit)");
                var key = Console.ReadKey();
                if (key.KeyChar == 'y')
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = bestTorrent.MagnetLink,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                if (key.KeyChar == 'q')
                {
                    endApp = true;
                }
            }

            return;
        }
    }
}
