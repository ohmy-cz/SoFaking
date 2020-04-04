﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IEncoderService
	{
		Task StartTranscodingAsync(ITranscodingJob transcodingJob, Action onStart, Action onDoneInternal, Action<string> onSuccessInternal, CancellationToken cancellationToken);
		Task<IMediaInfo> GetMediaInfo(string filePath);
		int TargetVideoBitrateKbs { get; }
		int TargetAVBitrateKbs { get; }
		string CurrentFile { get; }
		bool Busy { get; }
		void Kill();
	}
}
