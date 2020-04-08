using net.jancerveny.sofaking.Common.Utils;
using Xunit;

namespace net.jancerveny.sofaking.Tests.Common
{
	public class CountryCodesTest
	{
		[Fact]
		public void ConvertThreeLetterNameToTwoLetterName()
		{
			Assert.Equal("en", CountryCodes.ConvertThreeLetterNameToTwoLetterName("eng"));
		}
	}
}
