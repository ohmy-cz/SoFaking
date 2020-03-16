using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class TransmissionService : ITorrentClientService
    {
        private readonly TransmissionHttpClientFactory _clientFactory;
        private readonly TransmissionConfiguration _transmissionConfiguration;
        private readonly ILogger<TransmissionService> _logger;

        public TransmissionService(ILogger<TransmissionService> logger, TransmissionConfiguration transmissionConfiguration, TransmissionHttpClientFactory clientFactory)
        {
            if (transmissionConfiguration == null) throw new ArgumentNullException(nameof(transmissionConfiguration));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
            _transmissionConfiguration = transmissionConfiguration;
            _clientFactory = clientFactory;
        }

        public async Task<ITorrentAddedResponse> AddTorrentAsync(string magnetLink)
        {
            _logger.LogInformation($"Adding a new Torrent: {magnetLink}");
            using (var client = _clientFactory.CreateClient())
            {
                var transmissionRequestObject = new TransmissionTorrentAddRequestObject { 
                    Arguments = new TransmissionTorrentAddRequestArguments
                    {
                        Paused = false,
                        DownloadDir = _transmissionConfiguration.DownloadDir, // TODO: Can be read from previous RPC
                        Filename = magnetLink
                    }
                };

                var response = await client.PostAsync(string.Empty, new StringContent(JsonSerializer.Serialize(transmissionRequestObject), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected HTTP response code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }

                return JsonSerializer.Deserialize<TransmissionTorrentAddResponse>(await response.Content.ReadAsStringAsync())?.Arguments?.TorrentAdded;
            }
        }

        /// <summary>
        /// Get status of all torrents
        /// </summary>
        public async Task<IReadOnlyList<ITorrentClientTorrent>> GetAllTorrents()
        {
            _logger.LogInformation($"Getting all Transmission torrents");
            using (var client = _clientFactory.CreateClient())
            {
                var transmissionRequestObject = new TransmissionTorrentGetRequest
                {
                    Arguments = new TransmissionTorrentGetRequestArguments
                    {
                        //IDs = ids
                    }
                };

                var response = await client.PostAsync(string.Empty, new StringContent(JsonSerializer.Serialize(transmissionRequestObject), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected HTTP response code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TransmissionTorrentGetResponse>(json).Arguments?.Torrents;
            }
        }

        public async Task<bool> RemoveTorrent(int id)
        {
            _logger.LogDebug($"Removing torrent {id}");
            using (var client = _clientFactory.CreateClient())
            {
                var transmissionRequestObject = new TransmissionTorrentRemoveRequest
                {
                    Arguments = new TransmissionTorrentRemoveRequestArguments
                    {
                        IDs = new int[] { id }
                    }
                };

                var response = await client.PostAsync(string.Empty, new StringContent(JsonSerializer.Serialize(transmissionRequestObject), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected HTTP response code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }

                return true;
            }
        }
    }
}
