using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
    public class TransmissionTorrentGetRequest
    {
        [JsonPropertyName("method")]
        public string Method => "torrent-get";

        [JsonPropertyName("arguments")]
        public TransmissionTorrentGetRequestArguments Arguments { get; set; }
    }

    public class TransmissionTorrentGetRequestArguments
    {
        //[JsonPropertyName("ids")]
        //public int[] IDs { get; set; }

        [JsonPropertyName("fields")]
        public string[] Fields => _fields;

        private static string[] _fields => typeof(TransmissionTorrent)
            .GetMembers()
            .Where(x => x.MemberType == MemberTypes.Property)
            .Select(x => x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? x.Name)
            .ToArray();
    }
}
