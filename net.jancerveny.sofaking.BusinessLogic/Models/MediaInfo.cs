﻿using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System.IO;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class MediaInfo : IMediaInfo
	{
		public string VideoCodec { get; set; }
		public string VideoResolution { get; set; }
		public int? VideoBitrateKbs { get; set; }
		public FileInfo FileInfo { get; set; }
		public string AudioCodec { get; set; }
	}
}
