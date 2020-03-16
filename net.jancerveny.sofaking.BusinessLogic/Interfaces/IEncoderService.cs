using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IEncoderService
	{
		void StartTranscoding(string sourcePath, string destinationPath, EncodingTargetFlags target, Action onComplete, Action onError, CancellationToken cancellationToken);
		Task<IMediaInfo> GetMediaInfo(string filePath);
	}
}
