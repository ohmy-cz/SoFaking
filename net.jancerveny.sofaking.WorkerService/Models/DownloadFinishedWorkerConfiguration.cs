using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.WorkerService.Models
{
	public class DownloadFinishedWorkerConfiguration
	{
		public string FinishedDownloadsDir { get; set; }
		public string[] AcceptedVideoCodecs { get; set; }
		public string[] AcceptedAudioCodecs { get; set; }
		public string TranscodingTempDir { get; set; }
		public string FinishedDir { get; set; }
		public string FFMPEGBinary { get; set; }
		public string Resolution { get; set; }
		public int MaxPS4FileSizeGb { get; set; }
	}
}
