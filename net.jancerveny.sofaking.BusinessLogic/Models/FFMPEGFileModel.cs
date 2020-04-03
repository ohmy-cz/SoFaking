using System;
using System.Collections.Generic;
using System.Linq;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class FFMPEGFileModel
	{
		public int BitrateKbs { get; set; }
		public double Start { get; set; }
		public TimeSpan Duration { get; set; }
		public List<FFMPEGStreamModel> Streams { get; set; }
		public FFMPEGFileModel()
		{
			Streams = new List<FFMPEGStreamModel>();
		}
		public FFMPEGStreamModel MainAudioStream(string lang) => !Streams.Any() ? null : Streams
			.Where(x =>
				x.StreamType == StreamTypeEnum.Audio)
			.OrderBy(x =>
				(string.IsNullOrWhiteSpace(lang) ? true : x.Language == lang)) // We can not require the language, because some video files have only one audio stream without language identification.
			.ThenBy(x =>
				x.StreamDetails.ToLower().Contains("(default)"))
			.ThenBy(x =>
				x.StreamCodec.ToLower() == "atmos")
			.ThenBy(x =>
				x.StreamCodec.ToLower() == "truehd")
			.ThenBy(x =>
				x.StreamCodec.ToLower().Contains("dts"))
			.ThenBy(x =>
				x.StreamCodec.ToLower() == "ac3")
			.FirstOrDefault();
		public FFMPEGStreamModel MainVideoStream => !Streams.Any() ? null : Streams.Where(x => x.StreamType == StreamTypeEnum.Video).FirstOrDefault();
	}
}
