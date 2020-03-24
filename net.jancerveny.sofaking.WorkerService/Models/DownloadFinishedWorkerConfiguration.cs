using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.WorkerService.Models
{
	public class DownloadFinishedWorkerConfiguration
	{
		public string MoviesDownloadDir { get; set; }
		public string[] AcceptedVideoCodecs { get; set; }
		public string[] AcceptedAudioCodecs { get; set; }
		public string MoviesFinishedDir { get; set; }
		public string FFMPEGBinary { get; set; }
		/// <summary>
		/// widthxheight, fx. 1920x1080
		/// </summary>
		public string Resolution { get; set; }
		public int MaxPS4FileSizeGb { get; set; }
		/// <summary>
		/// If transcoding takes longer than this amount of hours, we will stop it and move on. FFMpeg sometimes ends transcoding succesfully, but doesn't report back.
		/// </summary>
		public int TranscodingStaleAfterH { get; set; }
	}
}
