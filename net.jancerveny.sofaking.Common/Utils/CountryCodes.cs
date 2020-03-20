using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace net.jancerveny.sofaking.Common.Utils
{
	public static class CountryCodes
	{
        public static string ConvertThreeLetterNameToTwoLetterName(string threeLetterISOLanguageName)
        {
            if (threeLetterISOLanguageName == null || threeLetterISOLanguageName.Length != 3)
            {
                throw new ArgumentException($"{nameof(threeLetterISOLanguageName)} must be three letters.");
            }

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (CultureInfo culture in cultures)
            {
                if (culture.ThreeLetterISOLanguageName.ToLower() == threeLetterISOLanguageName.ToLower())
                {
                    return culture.TwoLetterISOLanguageName;
                }
            }

            throw new ArgumentException("Could not get country code");
        }
    }
}
