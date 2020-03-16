using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class TransmissionTorrentAddResponse
    {
        [JsonPropertyName("arguments")]
        public TransmissionTorrentAddResponseArguments Arguments { get; set; }
    }

    public class TransmissionTorrentAddResponseArguments
    {
        [JsonPropertyName("torrent-added")]
        public TransmissionTorrentAddResponseTorrent TorrentAdded { get; set; }
    }
    public class TransmissionTorrentAddResponseTorrent : ITorrentAddedResponse
    {
        [JsonPropertyName("id")]
        public int TorrentId { get; set; }
        [JsonPropertyName("hashString")]
        public string Hash { get; set; }
        [JsonPropertyName("name")]
        public string TorrentName { get; set; }
    }
}
