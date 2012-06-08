//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Text;
using pr.extn;

namespace pr.extn
{
	/// <summary>Extensions for strings</summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Word wraps the given text to fit within the specified width.
		/// </summary>
		/// <param name="text">Text to be word wrapped</param>
		/// <param name="width">Width, in characters, to which the text should be word wrapped</param>
		/// <returns>The modified text</returns>
		public static string WordWrap(this string text, int width)
		{
			if (width < 1) throw new ArgumentException("Word wrap must have a width >= 1");
			
			text = text.Replace("\r\n","\n");
			text = text.Replace("\n"," ");

			int remaining = width;
			StringBuilder sb = new StringBuilder();
			for (int pos = 0; pos != text.Length;)
			{
				int i,j;
				for (i = pos; i != text.Length &&  Char.IsWhiteSpace(text[i]); ++i) {} // Find the start of the next word
				for (j = i  ; j != text.Length && !Char.IsWhiteSpace(text[j]); ++j) {} // Find the end of the next word
				int len = j - pos;
				if (len < remaining)
				{
					sb.Append(text.Substring(pos, len));
					remaining -= len;
					pos = j;
				}
				else if (remaining == width)
				{
					sb.Append(text.Substring(pos, width)).Append('\n');
					pos += width;
				}
				else
				{
					sb.Append('\n');
					remaining = width;
					pos = i;
				}
			}
			return sb.ToString();
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestStringExtensions()
		{
			//                    "123456789ABCDE"
			const string text   = "   A long string that\nis\r\nto be word wrapped";
			const string result = "   A long\n"+
			                      "string that\n"+
			                      "is to be word\n"+
			                      "wrapped";
			string r = text.WordWrap(14);
			Assert.AreEqual(result, r);
		}
	}
}
#endif


	
	
	//    /// <summary>Scanf style string parsing</summary>
	//    public static int Scanf(this string str, string format, params object[] args)
	//    {
	//        // i is for 'str', j is for 'format'
	//        int i = 0, j = 0, count = 0;
	//        try
	//        {
	//            for (;;)
	//            {
	//                // while the format string matches 'str'
	//                for (; i != str.Length && j != format.Length && str[i] == format[j] && format[j] != '{'; ++i, ++j) {}
	//                if (format[j] != '{') break;
	//                int idx = int.Parse(format.Substring(0));
	//                for (int i = 0; i != str.Length && count != args.Length; ++count)
	//                {
	//                    if (args[count] is Box<char>)
	//                    {
	//                        ((Box<char>)args[count]).obj = str[i];
	//                        ++i;
	//                    }
	//                    else if (args[count] is string)
	//                    {
	//                        int ws = str.IndexOfAny(new []{' ','\t','\n','\v','\r'}); if (ws == -1) ws = str.Length;
	//                        args[count] = str.Substring(i, ws - i);
	//                        i = ws;
	//                    }
	//                    else if (args[count] is int)
	//                    {
	//                        args[count] = int.Parse(str.Substring(i));
	//                    }
	//                    else break;
	//                }
	//            }
	//        }
	//        catch (Exception) {}
	//        return count;
	//    }
	
	//    /// <summary>
	//    /// This method is the inverse of <see cref="String.Format"/>.
	//    /// </summary>
	//    /// <param name="str">The string to format.</param>
	//    /// <param name="format">The format string to parse.</param>
	//    /// <returns>The parsed values.</returns>
	//    public static IEnumerable<string> Parse(this string str, string format)
	//    {
	//        Regex match_double = new Regex(@"(\{\d+\}\{\d+\})");
	//        if (match_double.IsMatch(format))
	//            throw new ArgumentException("Invalid format string. You must put at least one character between all parse tokens.");
	
	//        Regex empty_braces = new Regex(@"(\{\})");
	//        if (empty_braces.IsMatch(format))
	//            throw new ArgumentException("Do not include {} in your format string.");
	
	//        Regex expression = new Regex(@"(\{\d+\})+");
	//        foreach (Match match in expression.Matches(format))
	//        {
	//            match.Index
	
	//        }
	//        //string[] boundaries = format.Split(matches).ToArray();
	
	//        //int startPosition = 0;
	//        //for (int i = 0; i < matches.Count; ++i)
	//        //{
	//        //    startPosition += boundaries[i].Length;
	//        //    var nextBoundary = boundaries[i + 1];
	//        //    if (string.IsNullOrEmpty(nextBoundary))
	//        //    {
	//        //        yield return str.Substring(startPosition);
	//        //    }
	//        //    else
	//        //    {
	//        //        var nextBoundaryStartIndex = str.IndexOf(nextBoundary, startPosition);
	//        //        var parsedLength = nextBoundaryStartIndex - startPosition;
	//        //        var parsedValue = str.Substring(startPosition, parsedLength);
	//        //        startPosition += parsedValue.Length;
	//        //        yield return parsedValue;
	//        //    }
	//        //}
	//    }
	//}
