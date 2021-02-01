using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.Common.Models
{
	public class SoFakingConfiguration
	{
		public int MaxHorizontalVideoResolution { get; set; }
		public int MaxSizeGb { get; set; }
		public string [] AudioLanguages { get; set; }
		public string [] SubtitleLanguages { get; set; }
		public string TempFolder { get; set; }
	}
}
