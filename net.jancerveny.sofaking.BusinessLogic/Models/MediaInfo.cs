using net.jancerveny.sofaking.BusinessLogic.Interfaces;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class MediaInfo : IMediaInfo
	{
		public string VideoCodec { get; set; }
		public string VideoResolution { get; set; }
		public int? VideoBitrateKbs { get; set; }
		public string AudioCodec { get; set; }
	}
}
