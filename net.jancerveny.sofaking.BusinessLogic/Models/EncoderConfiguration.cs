using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class EncoderConfiguration
	{
		public string FFMPEGBinary { get; set; }
		public string OutputVideoCodec { get; set; }
		public string OutputAudioCodec { get; set; }
		public int OutputVideoBitrateMbits { get; set; }
		public int OutputAudioBitrateMbits { get; set; }
		public string TempFolder { get; set; }
		public string[] AudioLanguages { get; set; }
	}
}
