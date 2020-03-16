using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	/// <summary>
	/// RPC Response object
	/// </summary>
	public class TransmissionTorrent : ITorrentClientTorrent
	{
		/// <summary>
		/// Do not use this ID unless you have to, as the IDs seem changing
		/// </summary>
		[JsonPropertyName("id")]
		public int Id { get; set; }

		/// <summary>
		/// IDs seems to change over time, so we use hashes instead.
		/// </summary>
		[JsonPropertyName("hashString")]
		public string Hash { get; set; }

		[JsonPropertyName("status")]
		public TorrentStatusEnum Status { get; set; }

		[JsonPropertyName("isFinished")]
		public bool IsFinished { get; set; }

		[JsonPropertyName("eta")]
		public int ETA { get; set; }

		[JsonPropertyName("percentDone")]
		public double PercentDone { get; set; }

		[JsonPropertyName("sizeWhenDone")]
		public long SizeWhenDoneKb { get; set; }

		[JsonPropertyName("leftUntilDone")]
		public long LeftUntilDoneKb { get; set; }

		[JsonPropertyName("downloadDir")]
		public string DownloadDir { get; set; }
		
		/// <summary>
		/// The Folder / File name being downloaded
		/// </summary>
		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}
