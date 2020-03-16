using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class TransmissionTorrentGetResponse
	{

		[JsonPropertyName("arguments")]
		public TransmissionTorrentGetResponseArguments Arguments { get; set; }

		[JsonPropertyName("result")]
		public string Result { get; set; }
	}

	public class TransmissionTorrentGetResponseArguments
	{
		[JsonPropertyName("torrents")]
		public TransmissionTorrent[] Torrents { get; set; }
	}
}
