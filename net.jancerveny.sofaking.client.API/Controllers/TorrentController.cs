using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.Common.Models;

namespace net.jancerveny.sofaking.client.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TorrentController : ControllerBase
    {
        private readonly ILogger<TorrentController> _logger;
        private readonly TPBParserService _torrentSearchService;

        public TorrentController(ILogger<TorrentController> logger, TPBParserService torrentSearchService)
        {
            _logger = logger;
            _torrentSearchService = torrentSearchService;
        }
        public async Task<IEnumerable<TorrentSearchResult>> GetAsync(IVerifiedMovie selectedMovie)
        {
            return await _torrentSearchService.Search($"{selectedMovie.Title} {selectedMovie.ReleaseYear}");
        }
    }
}