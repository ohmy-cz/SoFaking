using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IEncoderService
	{
		void StartTranscoding(ITranscodingJob transcodingJob, Action onCompleteInternal);
		Task<IMediaInfo> GetMediaInfo(string filePath);
		int TargetVideoBitrateKbs { get; }
		string CurrentFile { get; }
	}
}
