using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System;
using System.IO;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class MediaInfo : IMediaInfo
	{
		public string VideoCodec { get; set; }
		public int HorizontalVideoResolution { get; set; }
		public int? AVBitrateKbs { get; set; }
		public FileInfo FileInfo { get; set; }
		public string AudioCodec { get; set; }
		public TimeSpan Duration { get; set; }
	}
}
