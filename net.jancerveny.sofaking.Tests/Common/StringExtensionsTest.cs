using net.jancerveny.sofaking.Common.Constants;
using Xunit;

namespace net.jancerveny.sofaking.Tests.Common
{
	public class StringExtensionsTest
	{
		[Fact]
		public void UppercaseCapitalTest()
		{
			Assert.Equal("AEon Flux", StringExtensions.Utf8ToAscii("Æon Flux"));
		}

		[Fact]
		public void UppercaseTest()
		{
			Assert.Equal("AEON FLUX", StringExtensions.Utf8ToAscii("ÆON FLUX"));
		}

		[Fact]
		public void LowercaseTest()
		{
			Assert.Equal("aeon flux", StringExtensions.Utf8ToAscii("æon flux"));
		}

		[Fact]
		public void CzechAlphabetTest()
		{
			Assert.Equal("escrzyaieuuotdn", StringExtensions.Utf8ToAscii("ěščřžýáíéúůóťďň"));
		}

		[Fact]
		public void DanishAlphabetTest()
		{
			Assert.Equal("aeoeaa", StringExtensions.Utf8ToAscii("æøå"));
		}
	}
}
