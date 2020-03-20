using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace net.jancerveny.sofaking.Common.Utils
{
	public static class CountryCodes
	{
        public static string ConvertThreeLetterNameToTwoLetterName(string threeLetterCountryCode)
        {
            if (threeLetterCountryCode == null || threeLetterCountryCode.Length != 3)
            {
                throw new ArgumentException($"{nameof(threeLetterCountryCode)} must be three letters.");
            }

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (CultureInfo culture in cultures)
            {
                RegionInfo region = new RegionInfo(culture.LCID);
                if (region.ThreeLetterISORegionName.ToLower() == threeLetterCountryCode.ToLower())
                {
                    return region.TwoLetterISORegionName;
                }
            }

            throw new ArgumentException("Could not get country code");
        }
    }
}
