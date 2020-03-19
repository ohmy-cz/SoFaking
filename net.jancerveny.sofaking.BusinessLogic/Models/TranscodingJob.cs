using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System;
using System.Threading;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class TranscodingJob : ITranscodingJob
	{
		public string SourceFile { get; set; }
		public string DestinationFolder { get; set; }
		public EncodingTargetFlags Action { get; set; }
		public Action OnComplete { get; set; }
		public Action OnError { get; set; }
		public CancellationToken CancellationToken { get; set; }
	}
}
