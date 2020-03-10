using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class TorrentAddedResponse : ITorrentAddedResponse
    {
        [JsonPropertyName("id")]
        public int TorrentId { get; set; }
        [JsonPropertyName("hashString")]
        public string Hash { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
