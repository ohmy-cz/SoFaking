using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.Common.Models;
using net.jancerveny.sofaking.DataLayer;

namespace net.jancerveny.sofaking.client.API
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			var proxies = new List<string>();
			Configuration.GetSection("TPBProxies").Bind(proxies);
			var sofakingConfiguration = new SoFakingConfiguration();
			Configuration.GetSection("Sofaking").Bind(sofakingConfiguration);
			if (proxies.Count == 0)
			{
				throw new Exception("TPB Proxies configuration missing");
			}
			TPBProxies.SetProxies(proxies.ToArray());
			var transmissionConfiguration = new TransmissionConfiguration();
			Configuration.GetSection("Transmission").Bind(transmissionConfiguration);

			services.AddHttpClient();
			services.AddControllers().AddJsonOptions(opt =>
			{
				opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
				opt.JsonSerializerOptions.PropertyNamingPolicy = null;
			});
			services.AddSingleton(transmissionConfiguration);
			services.AddSingleton<TransmissionHttpClientFactory>();
			services.AddSingleton(new SoFakingContextFactory());
			services.AddSingleton<MovieService>();
			services.AddSingleton<TPBParserService>();
			services.AddSingleton<ITorrentClientService, TransmissionService>();
			services.AddSingleton<IVerifiedMovieSearchService, ImdbService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseCors(configurePolicy => configurePolicy.AllowAnyOrigin());

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
