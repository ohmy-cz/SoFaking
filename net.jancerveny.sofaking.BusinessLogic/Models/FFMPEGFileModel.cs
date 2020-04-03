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
		public FFMPEGStreamModel MainAudioStream(string[] langs) => !Streams.Any() ? null : Streams
			.Where(x =>
				x.StreamType == StreamTypeEnum.Audio)
			.OrderByDescending(x =>
				langs.ToList().IndexOf(x.Language)) // We can not require the language, because some video files have only one audio stream without language identification.
			.ThenByDescending(x =>
				x.StreamDetails.ToLower().Contains("(default)"))
			.ThenByDescending(x =>
				x.StreamDetails.ToLower().Contains("truehd") || 
				x.StreamDetails.ToLower().Contains("dts") || 
				x.StreamDetails.ToLower().Contains("atmos"))
			.FirstOrDefault();
		public FFMPEGStreamModel MainVideoStream => !Streams.Any() ? null : Streams.Where(x => x.StreamType == StreamTypeEnum.Video).FirstOrDefault();
	}
}
