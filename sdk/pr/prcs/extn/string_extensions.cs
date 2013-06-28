//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Text;

namespace pr.extn
{
	/// <summary>Extensions for strings</summary>
	public static class StringExtensions
	{
		/// <summary>Treats this string as a format string</summary>
		[JetBrains.Annotations.StringFormatMethod("fmt")]
		public static string Fmt(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		/// <summary>Returns true if this string is not null or empty</summary>
		public static bool HasValue(this string str)
		{
			return !string.IsNullOrEmpty(str);
		}

		/// <summary>Word wraps the given text to fit within the specified width.</summary>
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

		/// <summary>Append 'lines' delimited by single newline characters</summary>
		public static string LineList(this string text, params string[] lines)
		{
			return new StringBuilder(text).AppendLineList(lines).ToString();
		}

		/// <summary>Append 'line' delimited by a single newline character</summary>
		public static string LineList(this string text, string line)
		{
			return new StringBuilder(text).AppendLineList(line).ToString();
		}

		//public static string HaackFormat(this string format, object source)
		//{
		//    var formattedStrings = (from expression in SplitFormat(format) select expression.Eval(source)).ToArray();
		//    return string.Join("", formattedStrings);
		//}

		//private static int IndexOfSingle(string format, int start_index, char ch)
		//{
		//    for (int idx = format.IndexOf(ch, start_index); idx != -1; idx = format.IndexOf(ch,idx+1))
		//    {
		//        if (idx + 1 < format.Length && format[idx+1] == ch) continue;
		//        return idx;
		//    }
		//    return -1;
		//}

		//private static IEnumerable<string> SplitFormat(string format)
		//{
		//    int exprEndIndex = -1;
		//    int expStartIndex;
		//    do
		//    {
		//        expStartIndex = IndexOfSingle(format, exprEndIndex + 1, '{');
		//        if (expStartIndex == -1)
		//        {
		//            //everything after last end brace index.
		//            if (exprEndIndex + 1 < format.Length)
		//            {
		//                yield return new LiteralFormat(format.Substring(exprEndIndex + 1));
		//            }
		//            break;
		//        }

		//        if (expStartIndex - exprEndIndex - 1 > 0)
		//        {
		//            //everything up to next start brace index
		//            yield return new LiteralFormat(format.Substring(exprEndIndex + 1, expStartIndex - exprEndIndex - 1));
		//        }

		//        int endBraceIndex = IndexOfSingle(format, expStartIndex + 1, '}');
		//        if (endBraceIndex < 0)
		//        {
		//            //rest of string, no end brace (could be invalid expression)
		//            yield return new FormatExpression(format.Substring(expStartIndex));
		//        }
		//        else
		//        {
		//            exprEndIndex = endBraceIndex;
		//            //everything from start to end brace.
		//            yield return new FormatExpression(format.Substring(expStartIndex, endBraceIndex - expStartIndex + 1));

		//        }
		//    }
		//    while (expStartIndex > -1);
		//}


  //static int IndexOfExpressionEnd(this string format, int startIndex)
  //{
  //  int endBraceIndex = format.IndexOf('}', startIndex);
  //  if (endBraceIndex == -1) {
  //    return endBraceIndex;
  //  }
  //  //start peeking ahead until there are no more braces...
  //  // }}}}
  //  int braceCount = 0;
  //  for (int i = endBraceIndex + 1; i < format.Length; i++) {
  //    if (format[i] == '}') {
  //      braceCount++;
  //    }
  //    else {
  //      break;
  //    }
  //  }
  //  if (braceCount % 2 == 1) {
  //    return IndexOfExpressionEnd(format, endBraceIndex + braceCount + 1);
  //  }

  //  return endBraceIndex;
  //}
	
	}

	/*
	 * public static class HaackFormatter
{
  



}
And the code for the supporting classes
public class FormatExpression : ITextExpression
{
  bool _invalidExpression = false;

  public FormatExpression(string expression) {
    if (!expression.StartsWith("{") || !expression.EndsWith("}")) {
      _invalidExpression = true;
      Expression = expression;
      return;
    }

    string expressionWithoutBraces = expression.Substring(1
        , expression.Length - 2);
    int colonIndex = expressionWithoutBraces.IndexOf(':');
    if (colonIndex < 0) {
      Expression = expressionWithoutBraces;
    }
    else {
      Expression = expressionWithoutBraces.Substring(0, colonIndex);
      Format = expressionWithoutBraces.Substring(colonIndex + 1);
    }
  }

  public string Expression { 
    get; 
    private set; 
  }

  public string Format
  {
    get;
    private set;
  }

  public string Eval(object o) {
    if (_invalidExpression) {
      throw new FormatException("Invalid expression");
    }
    try
    {
      if (String.IsNullOrEmpty(Format))
      {
        return (DataBinder.Eval(o, Expression) ?? string.Empty).ToString();
      }
      return (DataBinder.Eval(o, Expression, "{0:" + Format + "}") ?? 
        string.Empty).ToString();
    }
    catch (ArgumentException) {
      throw new FormatException();
    }
    catch (HttpException) {
      throw new FormatException();
    }
  }
}

public class LiteralFormat : ITextExpression
{
  public LiteralFormat(string literalText) {
    LiteralText = literalText;
  }

  public string LiteralText { 
    get; 
    private set; 
  }

  public string Eval(object o) {
    string literalText = LiteralText
        .Replace("{{", "{")
        .Replace("}}", "}");
    return literalText;
  }
}
	 * 
	 * 
	 * 
	 * static string Format( this string str
, params Expression<Func<string,object>[] args)
{ var parameters=args.ToDictionary
( e=>string.Format("{{{0}}}",e.Parameters[0].Name)
,e=>e.Compile()(e.Parameters[0].Name));

var sb = new StringBuilder(str);
foreach(var kv in parameters) 
{ sb.Replace( kv.Key
,kv.Value != null ? kv.Value.ToString() : ""); 
} 
return sb.ToString();
}
	 */
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using extn;
	
	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestExtensions
		{
			[Test] public static void StringWordWrap()
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
			[Test] public static void TestAppendLines()
			{
				const string s = "\n\n\rLine \n Line\n\r";
				var str = s.LineList(s,s);
				Assert.AreEqual("\n\n\rLine \n Line"+Environment.NewLine+"Line \n Line"+Environment.NewLine+"Line \n Line"+Environment.NewLine, str);
			}
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
