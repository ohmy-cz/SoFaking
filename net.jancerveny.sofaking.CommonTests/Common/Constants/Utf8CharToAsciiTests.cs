using Microsoft.VisualStudio.TestTools.UnitTesting;
using net.jancerveny.sofaking.Common.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.Common.Constants.Tests
{
	public class Utf8CharToAsciiTests
	{
		[TestMethod()]
		public void Utf8ToAsciiTest()
		{
			Assert.AreEqual("AEon Flux", Utf8CharToAscii.Utf8ToAscii("Æon Flux"));
		}
	}
}