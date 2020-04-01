using System;
using System.Collections.Generic;

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
	}
}
