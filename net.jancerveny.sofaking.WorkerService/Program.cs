using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using net.jancerveny.sofaking.Common.Models;
using net.jancerveny.sofaking.DataLayer;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.WorkerService.Models;
using net.jancerveny.sofaking.BusinessLogic.Models;

namespace net.jancerveny.sofaking.WorkerService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.UseSystemd() // Linux service lifetime management
				.ConfigureAppConfiguration((context, builder) =>
				{
					builder
						.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)) // Required for Linux service
						.AddJsonFile("appsettings.json")
						.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json")
						.AddJsonFile("dbsettings.json")
						.AddJsonFile($"dbsettings.{context.HostingEnvironment.EnvironmentName}.json")
						.AddEnvironmentVariables()
						.Build();
				})
				.ConfigureServices((hostContext, services) =>
				{
					var proxies = new List<string>();
					hostContext.Configuration.GetSection("TPBProxies").Bind(proxies);
					if (proxies.Count == 0)
					{
						throw new Exception("TPB Proxies configuration missing");
					}
					TPBProxies.SetProxies(proxies.ToArray());

					var downloadFinishedWorkerConfiguration = new DownloadFinishedWorkerConfiguration();
					hostContext.Configuration.GetSection("DownloadFinishedWorker").Bind(downloadFinishedWorkerConfiguration);
					var transmissionConfiguration = new TransmissionConfiguration();
					hostContext.Configuration.GetSection("Transmission").Bind(transmissionConfiguration);
					var encoderConfiguration = new EncoderConfiguration();
					hostContext.Configuration.GetSection("Encoder").Bind(encoderConfiguration);
					var sofakingConfiguration = new SoFakingConfiguration();
					hostContext.Configuration.GetSection("Sofaking").Bind(sofakingConfiguration);

					services
						.AddHttpClient()
						.AddSingleton(transmissionConfiguration)
						.AddSingleton(sofakingConfiguration)
						.AddSingleton<TransmissionHttpClientFactory>()
						.AddSingleton(downloadFinishedWorkerConfiguration)
						.AddSingleton(encoderConfiguration)
						.AddSingleton(new SoFakingContextFactory())
						.AddSingleton<MovieService>()
						.AddSingleton<TPBParserService>()
						.AddSingleton<ITorrentClientService, TransmissionService>()
						.AddSingleton<IVerifiedMovieSearchService, ImdbService>()
						//.AddSingleton<IEncoderService, FFMPEGEncoderService>()
						.AddHostedService<SoFakingWorker>();
				});
	}
}
