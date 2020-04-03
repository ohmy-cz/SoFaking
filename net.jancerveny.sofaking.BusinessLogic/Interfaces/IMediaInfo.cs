using System;
using System.IO;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IMediaInfo
	{
		string VideoCodec { get; set; }
		int HorizontalVideoResolution { get; set; }
		int? AVBitrateKbs { get; set; }
		public FileInfo FileInfo { get; set; }
		string AudioCodec { get; set; }
		TimeSpan Duration { get; set; }
	}
}
