using System;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class EncodingSuccessEventArgs : EventArgs
	{
		public string FinishedFile { get; private set; }
		public EncodingSuccessEventArgs(string finishedFile)
		{
			FinishedFile = finishedFile;
		}
	}
}
