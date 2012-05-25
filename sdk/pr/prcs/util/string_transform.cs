//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Text;
using NUnit.Framework;

namespace pr.util
{
	/// <summary>Utility function container</summary>
	public static class StrTxfm
	{
		public enum ECapitalise
		{
			DontChange = 0,
			UpperCase  = 1,
			LowerCase  = 2,
		}

		public enum ESeparate
		{
			DontChange = 0,
			Add        = 1,
			Remove     = 2,
		}

		/// <summary>Transforms a string by the given casing rules</summary>
		public static string Apply(string str, ECapitalise word_start, ECapitalise word_case, ESeparate word_sep, string sep, string delims)
		{
			if (string.IsNullOrEmpty(str)) return str;
			StringBuilder sb = new StringBuilder(str.Length);
			if (delims == null) delims = "";

			for (int i = 0; i != str.Length; ++i)
			{
				// Skip over multiple delimiters
				for (; delims.IndexOf(str[i]) != -1; ++i)
				{
					if (word_sep == ESeparate.DontChange) sb.Append(str[i]);
				}

				// Detect word boundaries
				bool boundary = i == 0 ||                                             // first letter in the string
								delims.IndexOf(str[i-1]) != -1 ||                     // previous char is a delimiter
								(char.IsLower (str[i-1]) && char.IsUpper (str[i])) || // lower to upper case letters
								(char.IsLetter(str[i-1]) && char.IsDigit (str[i])) || // letter to digit
								(char.IsDigit (str[i-1]) && char.IsLetter(str[i]));   // digit to letter
				if (boundary)
				{
					if (i != 0 && word_sep == ESeparate.Add && sep != null)
						sb.Append(sep);

					switch (word_start)
					{
					default: throw new ArgumentOutOfRangeException("word_start");
					case ECapitalise.DontChange: sb.Append(str[i]); break;
					case ECapitalise.UpperCase:  sb.Append(char.ToUpper(str[i])); break;
					case ECapitalise.LowerCase:  sb.Append(char.ToLower(str[i])); break;
					}
				}
				else
				{
					switch (word_case)
					{
					default: throw new ArgumentOutOfRangeException("word_case");
					case ECapitalise.DontChange: sb.Append(str[i]); break;
					case ECapitalise.UpperCase:  sb.Append(char.ToUpper(str[i])); break;
					case ECapitalise.LowerCase:  sb.Append(char.ToLower(str[i])); break;
					}
				}
			}
			return sb.ToString();
		}
	}

	/// <summary>String transform unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestStringTransform()
		{
			const string str0 = "SOME_stringWith_weird_Casing_Number03";
			string str;

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.LowerCase, StrTxfm.ECapitalise.LowerCase, StrTxfm.ESeparate.Add, "_", "_");
			Assert.AreEqual("some_string_with_weird_casing_number_03", str);

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.LowerCase, StrTxfm.ESeparate.Remove, null, "_");
			Assert.AreEqual("SomeStringWithWeirdCasingNumber03", str);

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.UpperCase, StrTxfm.ESeparate.Add, "^ ^", "_");
			Assert.AreEqual("SOME^ ^STRING^ ^WITH^ ^WEIRD^ ^CASING^ ^NUMBER^ ^03", str);
		}
	}
}
