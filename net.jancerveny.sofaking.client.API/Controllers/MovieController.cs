using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.DataLayer.Models;

namespace net.jancerveny.sofaking.client.API.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class MovieController : ControllerBase
	{
		private readonly ILogger<MovieController> _logger;
		private readonly MovieService _movieService;

		public MovieController(ILogger<MovieController> logger, MovieService movieService)
		{
			_logger = logger;
			_movieService = movieService;
		}

		[HttpGet]
		public async Task<IEnumerable<Movie>> GetAsync()
		{
			return (await _movieService.GetMoviesAsync()).OrderByDescending(x => x.Status).ThenByDescending(x => x.Deleted);
		}

		/// <summary>
		/// Merged movie from verified search and selected torrent
		/// </summary>
		/// <param name="movie"></param>
		/// <returns></returns>
		public async Task<bool> PostAsync(Movie movie)
		{
			return await _movieService.AddMovie(movie);
		}
	}
}
