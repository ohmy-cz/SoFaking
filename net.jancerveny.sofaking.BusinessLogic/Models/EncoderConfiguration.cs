﻿using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class EncoderConfiguration
	{
		public string FFMPEGBinary { get; set; }
		public string OutputVideoCodec { get; set; }
		public string OutputAudioCodec { get; set; }
		public double OutputVideoBitrateMbits { get; set; }
		public double OutputAudioBitrateMbits { get; set; }
		/// <summary>
		/// NVidia GPU acceleration
		/// </summary>
		public bool CanUseCuda { get; set; }
	}
}
