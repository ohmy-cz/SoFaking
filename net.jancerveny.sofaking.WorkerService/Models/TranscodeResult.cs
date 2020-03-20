using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.WorkerService.Models
{
	public class TranscodeResult
	{
		public TranscodeResultEnum Result { get; }
		public string[] FilesToMove { get; }
		public TranscodeResult(TranscodeResultEnum result)
		{
			Result = result;
		}

		public TranscodeResult(TranscodeResultEnum result, string[] filesToMove)
		{
			Result = result;
			FilesToMove = filesToMove;
		}
	}
}
