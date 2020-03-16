using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class TransmissionHttpClientFactory
    {
        private static TransmissionConfiguration _configuration;
        private static IHttpClientFactory _clientFactory;
        private static AuthenticationHeaderValue _authentication;
        private readonly ILogger<TransmissionHttpClientFactory> _logger;
        public TransmissionHttpClientFactory (ILogger<TransmissionHttpClientFactory> logger, TransmissionConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _clientFactory = clientFactory;
            _authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration.Username}:{_configuration.Password}")));
        }

        public HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri($"{_configuration.Host}:{_configuration.Port}/transmission/rpc");
            client.DefaultRequestHeaders.Authorization = _authentication;
            client.DefaultRequestHeaders.Add("X-Transmission-Session-Id", GetSessionIdAsync().Result);
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            return client;
        }

        private async Task<string> GetSessionIdAsync()
        {
            using (var client = _clientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = _authentication;
                client.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                var sessionIdRequestObject = new
                {
                    method = "session-get"
                };
                var response = await client.PostAsync($"{_configuration.Host}:{_configuration.Port}/transmission/rpc", new StringContent(JsonSerializer.Serialize(sessionIdRequestObject), Encoding.UTF8, "application/json"));
                var sessionId = response.Headers.GetValues("X-Transmission-Session-Id").First();

                _logger.LogWarning($"Transmission session id: {sessionId}");
                return sessionId;
            }
        }
    }
}
