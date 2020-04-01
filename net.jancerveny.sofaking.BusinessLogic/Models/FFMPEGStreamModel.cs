using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class FFMPEGStreamModel
	{
		public int StreamId { get; set; }
		public int StreamIndex { get; set; }
		public string Language { get; set; }
		public StreamTypeEnum StreamType { get; set; }
		public string StreamCodec { get; set; }
		public string StreamDetails { get; set; }
		public Dictionary<string, string> Metadata { get; set; }
		public int HorizontalResolution => StreamType == StreamTypeEnum.Video ? int.Parse(new Regex(@"(\d{3,4})x\d{3,4}").Match(StreamDetails).Groups[1].Value) : -1;
		public double FPS => StreamType == StreamTypeEnum.Video ? double.Parse(new Regex(@"([\d.]+) fps").Match(StreamDetails).Groups[1].Value) : -1;
		// The ffmpeg timespan has two decimals too many for C# to understand, so we need to drop them.
		public TimeSpan? Duration => StreamType == StreamTypeEnum.Video || StreamType == StreamTypeEnum.Audio ? (TimeSpan?)TimeSpan.ParseExact(Metadata.Where(x => x.Key.ToLower().Contains("duration")).Select(x => x.Value.Substring(0, x.Value.Length-2)).FirstOrDefault(), "hh\\:mm\\:ss\\.fffffff", null) : null;
	}
}

public enum StreamTypeEnum {
	Audio,
	Video,
	Subtitle
}
