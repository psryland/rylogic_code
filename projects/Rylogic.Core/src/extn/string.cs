//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rylogic.Extn
{
	/// <summary>Helper string building functions</summary>
	public static class Str
	{
		// Notes:
		//  - This seems to be slower than using $"..{}.." even though the StringBuilder
		//    is cached. I guess the compiler is optimising the interpolated strings better.

		/// <summary>Get the text of the cached string builder'</summary>
		[Obsolete] public static string Text
		{
			[DebuggerStepThrough] get { return CachedSB.ToString(); }
		}

		/// <summary>A thread local string builder, cached for better memory performance</summary>
		[Obsolete] public static StringBuilder CachedSB { get { return m_cached_sb ?? (m_cached_sb = new StringBuilder()); } }
		[ThreadStatic] private static StringBuilder m_cached_sb; // no initialised for thread statics

		/// <summary>Reset the cached string builder'</summary>
		[DebuggerStepThrough] [Obsolete()] public static void Reset()
		{
			CachedSB.Clear();
		}

		/// <summary>A helper for gluing strings together</summary>
		[DebuggerStepThrough] [Obsolete()] public static string Build(params object[] parts)
		{
			return Build(CachedSB, parts);
		}

		/// <summary>Append object.ToString()s to 'sb'</summary>
		[DebuggerStepThrough] [Obsolete()] public static void Append(params object[] parts)
		{
			Append(CachedSB, parts);
		}

		/// <summary>A helper for gluing strings together</summary>
		[DebuggerStepThrough] [Obsolete()] public static string Build(StringBuilder sb, params object[] parts)
		{
			sb.Length = 0;
			Append(sb, parts);
			return sb.ToString();
		}

		/// <summary>Append object.ToString()s to 'sb'</summary>
		[DebuggerStepThrough] [Obsolete()] public static void Append(StringBuilder sb, object part)
		{
			// Do not change this to automatically add white space,
			if      (part is string     ) sb.Append((string)part);
			else if (part is IEnumerable) foreach (var x in (IEnumerable)part) Append(sb, x);
			else if (part != null       ) sb.Append(part.ToString());
		}
		[DebuggerStepThrough] [Obsolete()] public static void Append(StringBuilder sb, params object[] parts)
		{
			// Do not change this to automatically add white space,
			foreach (var part in parts)
				Append(sb, part);
		}

		/// <summary>Treats this string as a format string</summary>
		[Obsolete("Use in-fix strings instead")] public static string Fmt(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		/// <summary>Returns true if this string is not null or empty</summary>
		[DebuggerStepThrough] public static bool HasValue(this string str)
		{
			return !string.IsNullOrEmpty(str);
		}

		/// <summary>Returns the substring contained between the first occurrence of 'start_pattern' and the following occurrence of 'end_pattern' (not inclusive). Use null to mean start/end of the string</summary>
		public static string Substring(this string str, string start_pattern, string end_pattern, StringComparison cmp_type = StringComparison.InvariantCulture)
		{
			var sidx = start_pattern != null ? str.IndexOf(start_pattern, 0, cmp_type) : 0;
			if (sidx == -1) return string.Empty;
			if (start_pattern != null) sidx += start_pattern.Length;
			var eidx = end_pattern != null ? str.IndexOf(end_pattern, sidx, cmp_type) : str.Length;
			return str.Substring(sidx, eidx - sidx);
		}

		/// <summary>Use regex to extract multiple strings from this string. (Doesn't include Group(0))</summary>
		public static string[] SubstringRegex(this string str, string pattern, RegexOptions options = RegexOptions.None)
		{
			var m = Regex.Match(str, pattern, options);
			if (!m.Success) return new string[0];
			return m.Groups.Cast<Group>().Skip(1).Select(x => x.Value).ToArray();
		}

		/// <summary>Returns the substring contained between the first occurrence of 'start_pattern' and the following occurrence of 'end_pattern' (not inclusive). Use null to mean start/end of the string</summary>
		public static string SubstringRegex(this string str, string start_pattern, string end_pattern, RegexOptions options = RegexOptions.None)
		{
			var sidx = 0;
			if (start_pattern != null)
			{
				var m = Regex.Match(str, start_pattern, options);
				if (!m.Success) return string.Empty;
				sidx = m.Index + m.Length;
			}

			var sub = str.Substring(sidx);

			var eidx = sub.Length;
			if (end_pattern != null)
			{
				var m = Regex.Match(sub, end_pattern, options);
				if (!m.Success) return sub;
				eidx = m.Index;
			}

			return sub.Substring(0, eidx);
		}

		/// <summary>Replace the line endings in this string with the given characters</summary>
		public static string LineEnding(this string str, string ending = "\r\n")
		{
			return Regex.Replace(str, @"\r\n?|\n", ending);
		}

		/// <summary>Add a newline to the end of this string, if it doesn't have one already</summary>
		public static string EnsureNewLine(this string str, string newline = "\r\n")
		{
			return str.EndsWith(newline) ? str : (str + newline);
		}

		/// <summary>Returns a string containing this character repeated 'count' times</summary>
		public static string Repeat(this char ch, int count)
		{
			return new string(ch, count);
		}

		/// <summary>Returns a string containing this str repeated 'count' times. Note: use the char.Repeat function for single characters</summary>
		public static string Repeat(this string str, int count)
		{
			return new StringBuilder().Insert(0, str, count).ToString();
		}

		/// <summary>Return a string with the characters in reverse order</summary>
		public static string Reverse(this string str)
		{
			return new string(str.ToCharArray().Reversed().ToArray());
		}

		/// <summary>Return a string with 'chars' removed</summary>
		public static string Strip(this string str, params char[] chars)
		{
			return new string(str.ToCharArray().Where(x => !chars.Contains(x)).ToArray());
		}

		/// <summary>Return a string with characters that match the predicate removed</summary>
		public static string Strip(this string str, Func<char, bool> pred)
		{
			return new string(str.ToCharArray().Where(x => !pred(x)).ToArray());
		}

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
		public static string Txfm(this string str, ECapitalise word_start, ECapitalise word_case, ESeparate word_sep, string sep, string delims = null)
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
		public static string Txfm(this string str, EPrettyStyle style, string delims = null)
		{
			switch (style)
			{
			default: throw new Exception("Unknown style");
			case EPrettyStyle.Title:    return Txfm(str, ECapitalise.UpperCase, ECapitalise.LowerCase, ESeparate.Add, " ", delims);
			case EPrettyStyle.Macro:    return Txfm(str, ECapitalise.UpperCase, ECapitalise.UpperCase, ESeparate.Add, "_", delims);
			case EPrettyStyle.LocalVar: return Txfm(str, ECapitalise.LowerCase, ECapitalise.LowerCase, ESeparate.Add, "_", delims);
			case EPrettyStyle.TypeDecl: return Txfm(str, ECapitalise.UpperCase, ECapitalise.LowerCase, ESeparate.Remove, "", delims);
			}
		}

		/// <summary>Return "this string" or "this str..."</summary>
		public static string Summary(this string str, int max_length)
		{
			Debug.Assert(max_length >= 3);
			return str.Length < max_length ? str : str.Substring(0, max_length - 3) + "...";
		}

		/// <summary>Return the first 'max_lines' in this string, with "..." appended if needed</summary>
		public static string SummaryLines(this string str, int max_lines)
		{
			Debug.Assert(max_lines >= 1);
			var idx = str.IndexOf(c => c == '\n' && --max_lines == 0);
			return idx == -1 ? str : str.Substring(0, idx) + Environment.NewLine + "...";
		}

		/// <summary>Pluralise this string based on count.</summary>
		public static string Plural(this string str, int count)
		{
			if (count == 1) return str;
			if (!str.HasValue()) return str;

			// Handle all caps
			var caps = str.All(x => char.IsUpper(x));
			if (caps) str = str.ToLower();

			// Convert 'str' to it's plural form
			var plural = (string)null;
			if (m_mutated_plurals.TryGetValue(str, out plural))
			{
				// Test the special cases first, those that are exceptions to the following "rules"
			}
			if (str.EndsWith("ch") || str.EndsWith("x") || str.EndsWith("s"))
			{
				plural = str + "es";
			}
			else if (str.EndsWith("y"))
			{
				plural = str.Substring(0, str.Length-1) + "ies";
			}
			else
			{
				plural = str + "s";
			}

			// Convert back to caps
			if (caps) plural = plural.ToUpper();
			return plural;
		}
		private static Dictionary<string,string> m_mutated_plurals = new Dictionary<string, string>()
			.ChainAdd("barracks","barracks").ChainAdd("child","children").ChainAdd("goose","geese").ChainAdd("man","men")
			.ChainAdd("mouse","mice").ChainAdd("person","people").ChainAdd("woman","women")
			; // Add to this on demand... (*sigh*... English...)

		/// <summary>
		/// Split the string into substrings at places where 'pred' returns >= 0.
		/// int Pred(IString s, int idx) - "returns the length of the separater that starts at 's[idx]'".
		/// So < 0 means not a separater</summary>
		public static IEnumerable<IString> Split(this string str, Func<IString, int,int> pred)
		{
			return IString.From(str).Split(pred);
		}

		/// <summary>Enumerate over the lines in this string (returned lines do not include new line characters)</summary>
		public static IEnumerable<IString> Lines(this string str, StringSplitOptions opts = StringSplitOptions.None)
		{
			foreach (var subs in IString.From(str).Split((s,i) => s[i] == '\n' ? 1 : -1))
			{
				if (subs.Length > 1 || opts != StringSplitOptions.RemoveEmptyEntries)
				yield return subs;
			}
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

			var remaining = width;
			var sb = new StringBuilder();
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

		/// <summary>Return the string as a byte buffer without encoding</summary>
		public static byte[] ToBytes(this string str)
		{
			var raw = new byte[str.Length * sizeof(char)];
			Buffer.BlockCopy(str.ToCharArray(), 0, raw, 0, raw.Length);
			return raw;
		}
		public static byte[] ToBytes(this string str, int ofs, int count)
		{
			var raw = new byte[str.Length * sizeof(char)];
			Buffer.BlockCopy(str.ToCharArray(ofs, count), 0, raw, 0, raw.Length);
			return raw;
		}

		/// <summary>Return the string as a stream</summary>
		public static Stream ToStream(this string str, Encoding enc = null)
		{
			enc = enc ?? Encoding.UTF8;
			return new MemoryStream(enc.GetBytes(str), false);
		}

		/// <summary>Write this string to a file</summary>
		public static void ToFile(this string str, string filepath, bool append = false, bool create_dir_if_necessary = true)
		{
			// Ensure the directory exists
			var dir = Path_.Directory(filepath);
			if (create_dir_if_necessary && !Path_.DirExists(dir))
				Directory.CreateDirectory(dir);

			using (var f = new StreamWriter(new FileStream(filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
				f.Write(str);
		}

		/// <summary>Return the contents of a file as a string</summary>
		public static string FromFile(string filepath)
		{
			return File.ReadAllText(filepath);
		}

		/// <summary>
		/// Returns the minimum edit distance between two strings.
		/// Useful for determining how "close" two strings are to each other</summary>
		public static int LevenshteinDistance(this string str, string rhs)
		{
			// Degenerate cases
			if (str == rhs) return 0;
			if (str.Length == 0) return rhs.Length;
			if (rhs.Length == 0) return str.Length;
 
			// Create two work vectors of integer distances
			var bufs = new []{new int[rhs.Length+1], new int[rhs.Length+1]};
			var v0 = bufs[0];
			var v1 = bufs[1];

			// Initialize v0 (the previous row of distances)
			// this row is A[0][i]: edit distance for an empty s
			// the distance is just the number of characters to delete from t
			for (int i = 0; i != v0.Length; ++i)
				v0[i] = i;
 
			// Calculate v1 (current row distances) from the previous row v0
			for (int i = 0; i != str.Length; ++i)
			{
				// First element of v1 is A[i+1][0] edit distance is delete (i+1) chars from s to match empty t
				v1[0] = i + 1;
 
				// Use formula to fill in the rest of the row
				for (int j = 0; j != rhs.Length; ++j)
				{
					var cost = str[i] == rhs[j] ? 0 : 1;
					v1[j+1] = Math.Min(v1[j] + 1, Math.Min(v0[j+1] + 1, v0[j] + cost));
				}
 
				// Swap buffers
				v0 = bufs[(i+1)&1];
				v1 = bufs[(i+0)&1];
			}
 
			return v0[rhs.Length];
		}

		/// <summary>Parse this string against 'regex'</summary>
		public static void Parse<P0>(this string str, string regex, out P0 p0)
		{
			var m = Regex.Match(str, regex);
			if (!m.Success || m.Groups.Count != 2) throw new Exception("Failed to parse formatted string");
			p0 = (P0)Convert.ChangeType(m.Groups[1].Value, typeof(P0));
		}
		public static void Parse<P0,P1>(this string str, string regex, out P0 p0, out P1 p1)
		{
			var m = Regex.Match(str, regex);
			if (!m.Success || m.Groups.Count != 3) throw new Exception("Failed to parse formatted string");
			p0 = (P0)Convert.ChangeType(m.Groups[1].Value, typeof(P0));
			p1 = (P1)Convert.ChangeType(m.Groups[2].Value, typeof(P1));
		}
		public static void Parse<P0,P1,P2>(this string str, string regex, out P0 p0, out P1 p1, out P2 p2)
		{
			var m = Regex.Match(str, regex);
			if (!m.Success || m.Groups.Count != 4) throw new Exception("Failed to parse formatted string");
			p0 = (P0)Convert.ChangeType(m.Groups[1].Value, typeof(P0));
			p1 = (P1)Convert.ChangeType(m.Groups[2].Value, typeof(P1));
			p2 = (P2)Convert.ChangeType(m.Groups[3].Value, typeof(P2));
		}

		/// <summary>Parse this string against 'regex'</summary>
		public static bool TryParse<P0>(this string str, string regex, out P0 p0)
		{
			try { str.Parse(regex, out p0); return true; }
			catch { p0 = default(P0); return false; }
		}
		public static bool TryParse<P0,P1>(this string str, string regex, out P0 p0, out P1 p1)
		{
			try { str.Parse(regex, out p0, out p1); return true; }
			catch { p0 = default(P0); p1 = default(P1); return false; }
		}
		public static bool TryParse<P0,P1,P2>(this string str, string regex, out P0 p0, out P1 p1, out P2 p2)
		{
			try { str.Parse(regex, out p0, out p1, out p2); return true; }
			catch { p0 = default(P0); p1 = default(P1); p2 = default(P2); return false; }
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

	/// <summary>An interface for string-like objects (typically StringBuilder or System.String)</summary>
	public abstract class IString :IEnumerable<char>
	{
		protected readonly int m_ofs, m_length;
		protected IString() :this(0, 0) {}
		protected IString(int ofs, int length)
		{
			m_ofs = ofs;
			m_length = length;
		}

		public abstract int Length       { get; }
		public abstract char this[int i] { get; }
		public abstract IString Substring(int ofs, int length);

		/// <summary>
		/// Split the string into substrings at places where 'pred' returns >= 0.
		/// int Pred(IString s, int idx) - "returns the length of the separater that starts at 's[idx]'".
		/// So < 0 means not a separater</summary>
		public IEnumerable<IString> Split(Func<IString, int,int> pred)
		{
			if (Length == 0) yield break;
			for (int s = 0, e, end = Length;;)
			{
				int sep_len = 0;
				for (e = s; e != end && (sep_len = pred(this,e)) < 0; ++e) {}
				yield return Substring(s, e - s);
				if (e == end) break;
				s = e + sep_len;
			}
		}
		public IEnumerable<IString> Split(params char[] sep)
		{
			return Split((s,i) => sep.Contains(s[i]) ? 1 : -1);
		}

		/// <summary>Trim chars from the front/back of the string</summary>
		public IString Trim(params char[] ch)
		{
			int s = 0, e = m_length;
			for (; s != m_length && ch.Contains(this[s]); ++s) {}
			for (; e != s && ch.Contains(this[e-1]); --e) {}
			return Substring(s, e - s);
		}

		IEnumerator<char> IEnumerable<char>.GetEnumerator()
		{
			for (int i = 0, iend = i + m_length; i != iend; ++i)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<char>)this).GetEnumerator();
		}

		public static implicit operator string(IString s)        { return s.ToString(); }
		public static implicit operator IString(string s)        { return new StringProxy(s); }
		public static implicit operator IString(StringBuilder s) { return new StringBuilderProxy(s); }
		public static implicit operator IString(char[] s)        { return new CharArrayProxy(s); }

		public static IString From(string s        , int ofs = 0, int length = int.MaxValue) { return new StringProxy(s, ofs, length); }
		public static IString From(StringBuilder s , int ofs = 0, int length = int.MaxValue) { return new StringBuilderProxy(s, ofs, length); }
		public static IString From(char[] s        , int ofs = 0, int length = int.MaxValue) { return new CharArrayProxy(s, ofs, length); }

		private class StringProxy :IString
		{
			private readonly string m_str;
			public StringProxy(string s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new StringProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return m_str.Substring(m_ofs, m_length);
			}
		}
		private class StringBuilderProxy :IString
		{
			private readonly StringBuilder m_str;
			public StringBuilderProxy(StringBuilder s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_str.Length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new StringBuilderProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return m_str.ToString(m_ofs, m_length);
			}
		}
		private class CharArrayProxy :IString
		{
			private readonly char[] m_str;
			public CharArrayProxy(char[] s, int ofs = 0, int length = int.MaxValue)
				:base(ofs, Math.Min(s.Length, length))
			{
				m_str = s;
			}
			public override int Length
			{
				get { return m_str.Length; }
			}
			public override char this[int i]
			{
				get { return m_str[m_ofs + i]; }
			}
			public override IString Substring(int ofs, int length)
			{
				return new CharArrayProxy(m_str, m_ofs + ofs, length);
			}
			public override string ToString()
			{
				return new string(m_str, m_ofs, m_length);
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture] public class TestStringExtns
	{
		[Test] public void StringWordWrap()
		{
			//                    "123456789ABCDE"
			const string text   = "   A long string that\nis\r\nto be word wrapped";
			const string result = "   A long\n"+
									"string that\n"+
									"is to be word\n"+
									"wrapped";
			string r = text.WordWrap(14);
			Assert.Equal(result, r);
		}
		[Test] public void TestAppendLines()
		{
			const string s = "\n\n\rLine \n Line\n\r";
			var str = s.LineList(s,s);
			Assert.Equal("\n\n\rLine \n Line"+Environment.NewLine+"Line \n Line"+Environment.NewLine+"Line \n Line"+Environment.NewLine, str);
		}
		[Test] public void Substring()
		{
			const string s1 = "aa {one} bb {two}";
			const string s2 = "aa {} bb {two}";
			const string s3 = "Begin dasd End";
			const string s4 = "first:second";
			const string s5 = "<32> regex </32>";

			Assert.Equal("one", s1.Substring("{","}"));
			Assert.Equal("", s2.Substring("{","}"));
			Assert.Equal("dasd", s3.Substring("Begin "," End"));
			Assert.Equal("first", s4.Substring(null,":"));
			Assert.Equal("second", s4.Substring(":", null));
			Assert.Equal("regex", s5.SubstringRegex(@"<\d+>\s", @"\s</\d+>"));
			Assert.Equal("regex </32>", s5.SubstringRegex(@"<\d+>\s", null));
			Assert.Equal("<32> regex", s5.SubstringRegex(null, @"\s</\d+>"));
		}
		[Test] public void Levenshtein()
		{
			var str1 = "Paul Rulz";
			var str2 = "Paul Was Here";
			var d = str1.LevenshteinDistance(str2);
			Assert.Equal(d, 8);
		}
		[Test] public void StringProxy()
		{
			Func<IString, int, char> func = (s,i) => s[i];

			var s0 = "Hello World";
			var s1 = new StringBuilder("Hello World");
			var s2 = "Hello World".ToCharArray();

			Assert.Equal(func(s0, 6), 'W');
			Assert.Equal(func(s1, 6), 'W');
			Assert.Equal(func(s2, 6), 'W');
		}
		[Test] public void StringTransform()
		{
			const string str0 = "SOME_stringWith_weird_Casing_Number03";
			string str;

			str = str0.Txfm(Str.ECapitalise.LowerCase, Str.ECapitalise.LowerCase, Str.ESeparate.Add, "_", "_");
			Assert.Equal("some_string_with_weird_casing_number_03", str);

			str = str0.Txfm(Str.ECapitalise.UpperCase, Str.ECapitalise.LowerCase, Str.ESeparate.Remove, null, "_");
			Assert.Equal("SomeStringWithWeirdCasingNumber03", str);

			str = str0.Txfm(Str.ECapitalise.UpperCase, Str.ECapitalise.UpperCase, Str.ESeparate.Add, "^ ^", "_");
			Assert.Equal("SOME^ ^STRING^ ^WITH^ ^WEIRD^ ^CASING^ ^NUMBER^ ^03", str);

			str = "FieldCAPSBlah".Txfm(Str.ECapitalise.UpperCase, Str.ECapitalise.DontChange, Str.ESeparate.Add, " ");
			Assert.Equal("Field CAPS Blah", str);
		}
		[Test] public void RegexPatterns()
		{
			var tests = new []
			{
				// Quoted path, not at the start of a string, with white spaces, and non word but legal path characters
				new
				{
					Str    = @"words ""p:\path with space -,\file with space.extn"" : 412",
					Match0 = @"""p:\path with space -,\file with space.extn""",
					Drive  = @"p:",
					Dir    = @"\path with space -,\",
					File   = @"file with space.extn",
					Quote  = @"""",
					Valid  = true,
				},

				// Same as above, but in single quotes
				new
				{
					Str    = @"words 'p:\path with space -,\file with space.extn' : 412",
					Match0 = @"'p:\path with space -,\file with space.extn'",
					Drive  = @"p:",
					Dir    = @"\path with space -,\",
					File   = @"file with space.extn",
					Quote  = @"'",
					Valid  = true,
				},

				// Illegal path
				new
				{
					Str    = @"words ""p:\path invalid-<>\file.extn"" : 412",
					Match0 = @"",
					Drive  = @"",
					Dir    = @"",
					File   = @"",
					Quote  = @"",
					Valid  = false,
				},

				// Path without quotes
				new
				{
					Str    = @"words p:\path\file.extn (412) words",
					Match0 = @"p:\path\file.extn",
					Drive  = @"p:",
					Dir    = @"\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Path without a directory, or quotes
				new
				{
					Str    = @"words p:\file.extn (412) words",
					Match0 = @"p:\file.extn",
					Drive  = @"p:",
					Dir    = @"\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Path without quotes, with spaces in the path
				new
				{
					Str    = @"words p:\path\path\broke n\file.extn (412) words",
					Match0 = @"p:\path\path\broke",
					Drive  = @"p:",
					Dir    = @"\path\path\",
					File   = @"broke",
					Quote  = @"",
					Valid  = true,
				},

				// Relative path
				new
				{
					Str    = @"..\path\file.extn",
					Match0 = @"",
					Drive  = @"",
					Dir    = @"",
					File   = @"",
					Quote  = @"",
					Valid  = false,
				},

				// Relative path
				new
				{
					Str = @".\path\file.extn",
					Match0 = @"",
					Drive  = @"",
					Dir    = @"",
					File   = @"",
					Quote  = @"",
					Valid  = false,
				},

				// Relative path
				new
				{
					Str = @".\file.extn",
					Match0 = @"",
					Drive  = @"",
					Dir    = @"",
					File   = @"",
					Quote  = @"",
					Valid  = false,
				},

				// Full path at the start of a string
				new
				{
					Str    = @"p:\path\path\path\file.extn",
					Match0 = @"p:\path\path\path\file.extn",
					Drive  = @"p:",
					Dir    = @"\path\path\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Full path at the start of a string contains white space
				new
				{
					Str    = @"p:\path\path\broke n\file.extn",
					Match0 = @"p:\path\path\broke",
					Drive  = @"p:",
					Dir    = @"\path\path\",
					File   = @"broke",
					Quote  = @"",
					Valid  = true,
				},

				// Full path at the start of a string, in quotes
				new
				{
					Str = @"""p:\path\file.extn""",
					Match0 = @"""p:\path\file.extn""",
					Drive  = @"p:",
					Dir    = @"\path\",
					File   = @"file.extn",
					Quote  = @"""",
					Valid  = true,
				},

				// Full path at the start of a string followed by words
				new
				{
					Str = @"p:\path\path\path\file.extn (412) words",
					Match0 = @"p:\path\path\path\file.extn",
					Drive  = @"p:",
					Dir    = @"\path\path\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},
			};
			
			foreach (var test in tests)
			{
				var m = Regex.Match(test.Str, Regex_.FullPathPattern);
				Assert.Equal(m.Success              , test.Valid);
				Assert.Equal(m.Groups[0      ].Value, test.Match0);
				Assert.Equal(m.Groups["drive"].Value, test.Drive);
				Assert.Equal(m.Groups["dir"  ].Value, test.Dir);
				Assert.Equal(m.Groups["file" ].Value, test.File);
				Assert.Equal(m.Groups["q"    ].Value, test.Quote);
			}
		}
	}
}
#endif

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