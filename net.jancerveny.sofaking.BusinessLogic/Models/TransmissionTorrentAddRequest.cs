using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class TransmissionTorrentAddRequestObject
    {
        [JsonPropertyName("method")]
        public string Method => "torrent-add";

        [JsonPropertyName("arguments")]
        public TransmissionTorrentAddRequestArguments Arguments { get; set; }
    }
    public class TransmissionTorrentAddRequestArguments
    {
        [JsonPropertyName("paused")]
        public bool Paused { get; set; }
        [JsonPropertyName("download-dir")]
        public string DownloadDir { get; set; }
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
    }
}
