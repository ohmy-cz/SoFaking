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
    public class TorrentClientController : ControllerBase
    {
        private readonly ILogger<TorrentController> _logger;
        private readonly ITorrentClientService _torrentClientService;

        public TorrentClientController(ILogger<TorrentController> logger, ITorrentClientService torrentClientService)
        {
            _logger = logger;
            _torrentClientService = torrentClientService;
        }

        public async Task<IEnumerable<ITorrentClientTorrent>> GetAsync()
        {
            return await _torrentClientService.GetAllTorrents();
        }
    }
}