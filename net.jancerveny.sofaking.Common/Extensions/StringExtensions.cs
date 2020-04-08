using System;
using System.Collections.Generic;
using System.Linq;

namespace net.jancerveny.sofaking.Common.Constants
{
	public static class StringExtensions
	{
		private static Dictionary<char, string> Translations => new Dictionary<char, string> {
			{ 'ě', "e" },
			{ 'š', "s" },
			{ 'č', "c" },
			{ 'ř', "r" },
			{ 'ž', "z" },
			{ 'ý', "y" },
			{ 'á', "a" },
			{ 'í', "i" },
			{ 'é', "e" },
			{ 'ú', "u" },
			{ 'ů', "u" },
			{ 'ď', "d" },
			{ 'ť', "t" },
			{ 'ň', "n" },
			{ 'ï', "i" },
			{ 'ë', "e" },
			{ 'ö', "o" },
			{ 'ü', "u" },
			{ 'ä', "a" },
			{ 'ć', "c" },
			{ 'ó', "o" },
			{ 'è', "e" },
			{ 'ê', "e" },
			{ 'æ', "ae" },
			{ 'ø', "oe" },
			{ 'å', "aa" },
		};

		public static string Utf8ToAscii(this String str)
		{
			string output = string.Empty;
			foreach(char ch in str)
			{
				string match = Translations.Where(x => x.Key == char.ToLowerInvariant(ch)).Select(x => x.Value).FirstOrDefault();
				if (!string.IsNullOrWhiteSpace(match))
				{
					if(char.IsLower(ch))
					{
						output += match;
						continue;
					}
					output += match.ToUpperInvariant();
					continue;
				}

				output += ch;
			}

			return output;
		}
	}
}
