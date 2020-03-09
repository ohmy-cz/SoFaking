using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class TransmissionService : ITorrentClientService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly TransmissionConfiguration _transmissionConfiguration;
        private readonly AuthenticationHeaderValue _auth;
        private static string _sessionId;
        private static DateTime? _sessionIdLastUpdated;
        public TransmissionService(TransmissionConfiguration transmissionConfiguration, IHttpClientFactory clientFactory)
        {
            if (transmissionConfiguration == null) throw new ArgumentNullException(nameof(transmissionConfiguration));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
            _transmissionConfiguration = transmissionConfiguration;
            _clientFactory = clientFactory;
            _auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_transmissionConfiguration.Username}:{_transmissionConfiguration.Password}")));
        }

        public async Task AddTorrentAsync(string magnetLink)
        {
            await GetFreshSessionId();
            // TODO: Refactor this to make own Transmission-specific client
            using (var client = _clientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = _auth;
                client.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                client.DefaultRequestHeaders.Add("X-Transmission-Session-Id", _sessionId);
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                var transmissionRequestObject = new TransmissionRequestObject { 
                    Method = "torrent-add",
                    Arguments = new TransmissionRequestObjectArguments
                    {
                        Paused = false,
                        DownloadDir = "/mnt/Movies", // TODO: Make dynamic
                        Filename = magnetLink
                    }
                };

                var response = await client.PostAsync($"{_transmissionConfiguration.Host}:{_transmissionConfiguration.Port}/transmission/rpc", new StringContent(JsonSerializer.Serialize(transmissionRequestObject), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected HTTP response code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                }
            }
        }

        private async Task GetFreshSessionId()
        {
            if(_sessionId != string.Empty && _sessionIdLastUpdated != null && _sessionIdLastUpdated < DateTime.Now.AddMinutes(-5))
            {
                return;
            }
            using (var client = _clientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = _auth;
                var response = await client.GetAsync($"{_transmissionConfiguration.Host}:{_transmissionConfiguration.Port}/transmission/rpc");
                _sessionId = response.Headers.GetValues("X-Transmission-Session-Id").First();
                _sessionIdLastUpdated = DateTime.Now;
            }
        }
    }

    public class TransmissionRequestObject
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("arguments")]
        public TransmissionRequestObjectArguments Arguments { get; set; }
    }

    public class TransmissionRequestObjectArguments
    {
        [JsonPropertyName("paused")]
        public bool Paused { get; set; }
        [JsonPropertyName("download-dir")]
        public string DownloadDir { get; set; }
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
    }
}
