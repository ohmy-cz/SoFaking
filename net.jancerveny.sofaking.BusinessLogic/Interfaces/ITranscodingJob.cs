﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface ITranscodingJob
	{
		string SourceFile { get; set; }
		EncodingTargetFlags Action { get; set; }
		//Action OnComplete { get; set; }
		//Action OnError { get; set; } 
		CancellationToken CancellationToken { get; set; }
		Dictionary<FFMPEGMetadataEnum, string> Metadata { get; set; }
		TimeSpan Duration { get; set; }
		string CoverImageJpg { get; set; }
		public Dictionary<string, string> Subtitles { get; set; }
		public bool UseCuda { get; set; }
	}
}
