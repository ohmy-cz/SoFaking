using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class EncodingErrorEventArgs
	{
		public string Error { get; private set; }
		public EncodingErrorEventArgs(string error)
		{
			Error = error;
		}
	}
}
