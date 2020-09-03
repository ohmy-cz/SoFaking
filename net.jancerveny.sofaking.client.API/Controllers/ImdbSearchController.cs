using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.DataLayer.Models;

namespace net.jancerveny.sofaking.client.API.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SearchController : ControllerBase
	{
		private readonly ILogger<MovieController> _logger;
		private readonly IVerifiedMovieSearchService _movieSearchService;

		public SearchController(ILogger<MovieController> logger, IVerifiedMovieSearchService movieSearchService)
		{
			_logger = logger;
			_movieSearchService = movieSearchService;
		}

		[HttpGet]
		public async Task<IEnumerable<IVerifiedMovie>> GetAsync(string query)
		{
			return await _movieSearchService.Search(query);
		}
	}
}
