using System;
using System.Threading;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface ITranscodingJob
	{
		string SourceFile { get; set; }
		string DestinationFolder { get; set; }
		EncodingTargetFlags Action { get; set; }
		Action OnComplete { get; set; }
		Action OnError { get; set;  } 
		CancellationToken CancellationToken { get; set; }
	}
}
