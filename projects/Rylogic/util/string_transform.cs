//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Text;
using pr.util;

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

		public enum EPrettyStyle
		{
			/// <summary>Words start with capitals followed by lower case and are separated by spaces</summary>
			Title,

			/// <summary>Words are all upper case, separated with underscore</summary>
			Macro,

			/// <summary>Words are lower case, separated by underscore</summary>
			LocalVar,

			/// <summary>Words start with capitals followed by lower case and have no separators</summary>
			TypeDecl,
		}

		/// <summary>Transforms a string by the given casing rules</summary>
		public static string Apply(string str, ECapitalise word_start, ECapitalise word_case, ESeparate word_sep, string sep, string delims = null)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			delims = delims ?? string.Empty;

			var sb = new StringBuilder(str.Length);
			for (var i = 0; i != str.Length; ++i)
			{
				// Skip over multiple delimiters
				for (; delims.IndexOf(str[i]) != -1; ++i)
				{
					if (word_sep == ESeparate.DontChange)
						sb.Append(str[i]);
				}

				// Detect word boundaries
				var boundary = i == 0 ||                                  // first letter in the string
					delims.IndexOf(str[i-1]) != -1 ||                     // previous char is a delimiter
					(char.IsLower (str[i-1]) && char.IsUpper (str[i])) || // lower to upper case letters
					(char.IsLetter(str[i-1]) && char.IsDigit (str[i])) || // letter to digit
					(char.IsDigit (str[i-1]) && char.IsLetter(str[i])) || // digit to letter
					(i < str.Length - 1 && char.IsUpper(str[i-1]) && char.IsUpper(str[i]) && char.IsLower(str[i+1]));
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
		public static string Apply(string str, EPrettyStyle style, string delims = null)
		{
			switch (style)
			{
			default: throw new Exception("Unknown style");
			case EPrettyStyle.Title:    return Apply(str, ECapitalise.UpperCase, ECapitalise.LowerCase, ESeparate.Add, " ", delims);
			case EPrettyStyle.Macro:    return Apply(str, ECapitalise.UpperCase, ECapitalise.UpperCase, ESeparate.Add, "_", delims);
			case EPrettyStyle.LocalVar: return Apply(str, ECapitalise.LowerCase, ECapitalise.LowerCase, ESeparate.Add, "_", delims);
			case EPrettyStyle.TypeDecl: return Apply(str, ECapitalise.UpperCase, ECapitalise.LowerCase, ESeparate.Remove, "", delims);
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestStringTransforms
	{
		[Test] public void StringTransform()
		{
			const string str0 = "SOME_stringWith_weird_Casing_Number03";
			string str;

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.LowerCase, StrTxfm.ECapitalise.LowerCase, StrTxfm.ESeparate.Add, "_", "_");
			Assert.AreEqual("some_string_with_weird_casing_number_03", str);

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.LowerCase, StrTxfm.ESeparate.Remove, null, "_");
			Assert.AreEqual("SomeStringWithWeirdCasingNumber03", str);

			str = StrTxfm.Apply(str0, StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.UpperCase, StrTxfm.ESeparate.Add, "^ ^", "_");
			Assert.AreEqual("SOME^ ^STRING^ ^WITH^ ^WEIRD^ ^CASING^ ^NUMBER^ ^03", str);

			str = StrTxfm.Apply("FieldCAPSBlah", StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.DontChange, StrTxfm.ESeparate.Add, " ");
			Assert.AreEqual("Field CAPS Blah", str);
		}
	}
}
#endif
