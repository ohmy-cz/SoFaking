using System.IO;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IMediaInfo
	{
		string VideoCodec { get; set; }
		string VideoResolution { get; set; }
		int? VideoBitrateKbs { get; set; }
		public FileInfo FileInfo { get; set; }
		string AudioCodec { get; set; }
	}
}
