using System;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class EncodingProgressEventArgs : EventArgs
	{
		public double ProgressPercent { get; private set; }
		public string CurrentFile { get; private set; }
		public int? SizeKb { get; private set; }
		public TimeSpan? ProcessedDuration { get; private set; }
		public double? FPS { get; private set; }
		public EncodingProgressEventArgs(double progressPercent, string currentFile, int? sizeKb = null, TimeSpan? processedDuration = null, double? fps = null)
		{
			ProgressPercent = progressPercent;
			CurrentFile = currentFile;
			SizeKb = sizeKb;
			ProcessedDuration = processedDuration;
			FPS = fps;
		}
	}
}
