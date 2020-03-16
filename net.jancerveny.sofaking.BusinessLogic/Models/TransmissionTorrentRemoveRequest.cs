using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class TransmissionTorrentRemoveRequest
    {
        [JsonPropertyName("method")]
        public string Method => "torrent-remove";

        [JsonPropertyName("arguments")]
        public TransmissionTorrentRemoveRequestArguments Arguments { get; set; }
    }

    public class TransmissionTorrentRemoveRequestArguments
    {
        [JsonPropertyName("ids")]
        public int[] IDs { get; set; }
    }
}
