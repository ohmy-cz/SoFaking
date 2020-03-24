using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IEncoderService
	{
		void StartTranscoding(ITranscodingJob transcodingJob, Action onDoneInternal, Action onSuccessInternal);
		Task<IMediaInfo> GetMediaInfo(string filePath);
		int TargetBitrateKbs { get; }
		string CurrentFile { get; }
		void Kill();
	}
}
