//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rylogic.Common;
using Rylogic.Str;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	/// <summary>Helper string building functions</summary>
	public static class Str_
	{
		public static char[] WhiteSpaceChars = new[] { ' ', '\t', '\r', '\n', '\v' };

		// Character classes
		public static bool IsNewLine(char ch) => ch == '\n';
		public static bool IsLineSpace(char ch) => ch == ' ' || ch == '\t' || ch == '\r';
		public static bool IsWhiteSpace(char ch) => IsLineSpace(ch) || IsNewLine(ch) || ch == '\v' || ch == '\f';
		public static bool IsDecDigit(char ch) => (ch >= '0' && ch <= '9');
		public static bool IsBinDigit(char ch) => (ch >= '0' && ch <= '1');
		public static bool IsOctDigit(char ch) => (ch >= '0' && ch <= '7');
		public static bool IsHexDigit(char ch) => IsDecDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
		public static bool IsDigit(char ch) => IsDecDigit(ch);
		public static bool IsAlpha(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
		public static bool IsIdentifier(char ch, bool first) => ch == '_' || IsAlpha(ch) || (!first && IsDigit(ch));

		/// <summary>Returns true if this string is not null or empty</summary>
		public static bool HasValue(this string? str)
		{
			return !string.IsNullOrEmpty(str);
		}

		/// <summary>Return the index of the first character to satisfy 'pred' or str.Length</summary>
		public static int Find(this string str, Func<char, bool> pred)
		{
			return Find(str, pred, 0);
		}
		public static int Find(this string str, Func<char, bool> pred, int start_index)
		{
			// This is not called 'IndexOf' because it doesn't return -1 when not found.
			// Silently handing 'start_index' being out of range to make use of this
			// function in for loops more convenient;
			int i = start_index;
			if (i < 0 || i >= str.Length) return str.Length;
			for (; i != str.Length && !pred(str[i]); ++i) {}
			return i;
		}

		/// <summary>Returns the substring contained between the first occurrence of 'start_pattern' and the following occurrence of 'end_pattern' (not inclusive). Use null to mean start/end of the string</summary>
		public static string Substring(this string str, string? start_pattern, string? end_pattern, StringComparison cmp_type = StringComparison.InvariantCulture)
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
			if (!m.Success) return Array.Empty<string>();
			return m.Groups.Cast<Group>().Skip(1).Select(x => x.Value).ToArray();
		}

		/// <summary>Returns the substring contained between the first occurrence of 'start_pattern' and the following occurrence of 'end_pattern' (not inclusive). Use null to mean start/end of the string</summary>
		public static string SubstringRegex(this string str, string? start_pattern, string? end_pattern, RegexOptions options = RegexOptions.None)
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

		/// <summary>True if this string contains the character 'ch'</summary>
		public static bool Contains(this string str, char ch)
		{
			return str.IndexOf(ch) != -1;
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

		/// <summary>Transforms a string by the given casing rules</summary>
		public static string Txfm(this string str, ECapitalise word_start, ECapitalise word_case, ESeparate word_sep, string? sep, string? delims = null)
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
					default: throw new ArgumentOutOfRangeException(nameof(word_start));
					case ECapitalise.DontChange: sb.Append(str[i]); break;
					case ECapitalise.UpperCase:  sb.Append(char.ToUpper(str[i])); break;
					case ECapitalise.LowerCase:  sb.Append(char.ToLower(str[i])); break;
					}
				}
				else
				{
					switch (word_case)
					{
					default: throw new ArgumentOutOfRangeException(nameof(word_case));
					case ECapitalise.DontChange: sb.Append(str[i]); break;
					case ECapitalise.UpperCase:  sb.Append(char.ToUpper(str[i])); break;
					case ECapitalise.LowerCase:  sb.Append(char.ToLower(str[i])); break;
					}
				}
			}
			return sb.ToString();
		}
		public static string Txfm(this string str, EPrettyStyle style, string? delims = null)
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

		/// <summary>Add/Remove quotes from a string if it doesn't already have them</summary>
		public static string Quotes(this string str, bool add)
		{
			if (add && (str.Length >= 2 && str[0] == '\"' && str[str.Length-1] == '\"')) return str; // already quoted
			if (!add && (str.Length < 2 || str[0] != '\"' || str[str.Length-1] != '\"')) return str; // already not quoted
			return add ? $"\"{str}\"" : str.Substring(1, str.Length - 2);
		}

		/// <summary>Returns this string with 'prefix' prepended, and 'postfix' appended, if this string is not null or empty</summary>
		public static string Surround(this string str, string prefix, string postfix)
		{
			if (string.IsNullOrEmpty(str)) return str;
			return prefix + str + postfix;
		}

		/// <summary>Pluralise this string based on count.</summary>
		public static string Plural(this string str, int count)
		{
			if (count == 1) return str;
			if (!str.HasValue()) return str;

			// Handle all caps
			var caps = str.All(x => char.IsUpper(x));
			if (caps) str = str.ToLower();

			m_mutated_plurals ??= new Dictionary<string, string>
			{
				// Add to this on demand... (*sigh*... English...)
				{ "barracks","barracks" },
				{ "child","children" },
				{ "goose","geese" },
				{ "man","men" },
				{ "mouse","mice" },
				{ "person","people" },
				{ "woman","women" },
			};

			// Convert 'str' to it's plural form
			if (m_mutated_plurals.TryGetValue(str, out var plural))
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
		private static Dictionary<string,string>? m_mutated_plurals;

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
		public static Stream ToStream(this string str, Encoding? enc = null)
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

		/// <summary> Returns the minimum edit distance between two strings. Useful for determining how "close" two strings are to each other</summary>
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

		/// <summary>Remove leading white space and trailing tabs around '\n' characters.</summary>
		public static string ProcessIndentedNewlines(string str)
		{
			return Regex.Replace(str, @"[\s\t]*\n[\t]*", "\n");
		}

		/// <summary>
		/// Look for 'identifier' within the range [start, start+count) ensuring it is not a substring of a larger identifier.
		/// Returns the index of it's position or -1 if not found.</summary>
		public static int IndexOfIdentifier(this string src, string identifier, int start, int count)
		{
			// Notes:
			//  - This is basically 'pr::str::FindIdentifier'.
			//  - Identifiers are: [A-Za-z_]+[A-Za-z0-9_]* i.e. the first character is a special case.
			//    Consider: ' 1token', ' _1token', and ' _1111token'.
			if (identifier.Length == 0 || !IsIdentifier(identifier[0], true) || !identifier.Skip(1).All(x => IsIdentifier(x, false)))
				throw new ArgumentException($"Value '{identifier}' is not a valid identifier");

			var beg = start;
			var end = beg + count;
			for (int i = beg; i != end;)
			{
				// Find the next instance of 'value'
				var idx = src.IndexOf(identifier, i, end - i);
				if (idx == -1) break;
				i = idx + identifier.Length;

				// Check for characters after. i.e. don't return "bobble" if "bob" is the identifier
				var j = idx + identifier.Length;
				if (j != end && IsIdentifier(src[j], false))
					continue;

				// Look for any characters before. i.e. don't return "plumbob" if "bob" is the identifier
				// This has to be a search, consider: ' 1token', ' _1token', and ' _1111token'.
				for (j = idx; j-- != beg && IsIdentifier(src[j], false);) { }
				if (++j != idx && IsIdentifier(src[j], true))
					continue;

				// Found one
				return idx;
			}
			return -1;
		}
		public static int IndexOfIdentifier(this string src, string identifier, int start) => IndexOfIdentifier(src, identifier, start, src.Length - start);
		public static int IndexOfIdentifier(this string src, string identifier) => IndexOfIdentifier(src, identifier, 0);

		/// <summary>Parse this string against 'regex'</summary>
		public static P0 ConvertTo<P0>(this string str)
		{
			return Util.ConvertTo<P0>(str);
		}
		public static Tuple<P0,P1> ConvertTo<P0, P1>(this string str, char sep = ',')
		{
			var parts = str.Split(sep);
			return Tuple.Create(
				Util.ConvertTo<P0>(parts[0]),
				Util.ConvertTo<P1>(parts[1]));
		}
		public static Tuple<P0,P1,P2> ConvertTo<P0,P1,P2>(this string str, char sep = ',')
		{
			var parts = str.Split(sep);
			return Tuple.Create(
				Util.ConvertTo<P0>(parts[0]),
				Util.ConvertTo<P1>(parts[1]),
				Util.ConvertTo<P2>(parts[2]));
		}

		/// <summary>Parse this string against 'regex'</summary>
		public static bool TryConvertTo<P0>(this string str, out P0 p)
		{
			try { p = str.ConvertTo<P0>(); return true; }
			catch { p = default!; return false; }
		}
		public static bool TryConvertTo<P0, P1>(this string str, out Tuple<P0, P1> p, char sep = ',')
		{
			try { p = str.ConvertTo<P0, P1>(); return true; }
			catch { p = default!; return false; }
		}
		public static bool TryConvertTo<P0, P1, P2>(this string str, out Tuple<P0, P1, P2> p, char sep = ',')
		{
			try { p = str.ConvertTo<P0, P1, P2>(); return true; }
			catch { p = default!; return false; }
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
			catch { p0 = default!; return false; }
		}
		public static bool TryParse<P0,P1>(this string str, string regex, out P0 p0, out P1 p1)
		{
			try { str.Parse(regex, out p0, out p1); return true; }
			catch { p0 = default!; p1 = default!; return false; }
		}
		public static bool TryParse<P0,P1,P2>(this string str, string regex, out P0 p0, out P1 p1, out P2 p2)
		{
			try { str.Parse(regex, out p0, out p1, out p2); return true; }
			catch { p0 = default!; p1 = default!; p2 = default!; return false; }
		}

		public enum ECapitalise
		{
			DontChange = 0,
			UpperCase = 1,
			LowerCase = 2,
		}
		public enum ESeparate
		{
			DontChange = 0,
			Add = 1,
			Remove = 2,
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
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture]
	public class TestStringExtns
	{
		[Test]
		public void StringWordWrap()
		{
			//                    "123456789ABCDE"
			const string text = "   A long string that\nis\r\nto be word wrapped";
			const string result = "   A long\n" +
									"string that\n" +
									"is to be word\n" +
									"wrapped";
			string r = text.WordWrap(14);
			Assert.Equal(result, r);
		}
		[Test]
		public void TestAppendLines()
		{
			const string s = "\n\n\rLine \n Line\n\r";
			var str = s.LineList(s, s);
			Assert.Equal("\n\n\rLine \n Line" + Environment.NewLine + "Line \n Line" + Environment.NewLine + "Line \n Line" + Environment.NewLine, str);
		}
		[Test]
		public void Substring()
		{
			const string s1 = "aa {one} bb {two}";
			const string s2 = "aa {} bb {two}";
			const string s3 = "Begin dasd End";
			const string s4 = "first:second";
			const string s5 = "<32> regex </32>";

			Assert.Equal("one", s1.Substring("{", "}"));
			Assert.Equal("", s2.Substring("{", "}"));
			Assert.Equal("dasd", s3.Substring("Begin ", " End"));
			Assert.Equal("first", s4.Substring(null, ":"));
			Assert.Equal("second", s4.Substring(":", null));
			Assert.Equal("regex", s5.SubstringRegex(@"<\d+>\s", @"\s</\d+>"));
			Assert.Equal("regex </32>", s5.SubstringRegex(@"<\d+>\s", null));
			Assert.Equal("<32> regex", s5.SubstringRegex(null, @"\s</\d+>"));
		}
		[Test]
		public void Levenshtein()
		{
			var str1 = "Paul Rulz";
			var str2 = "Paul Was Here";
			var d = str1.LevenshteinDistance(str2);
			Assert.Equal(d, 8);
		}
		[Test]
		public void ProcessIndentedNewlines()
		{
			const string str = "\nwords    and     \n\t\t\tmore  words  \n\t\t and more\nwords\n";
			var result = Str_.ProcessIndentedNewlines(str);
			Assert.Equal("\nwords    and\nmore  words\n and more\nwords\n", result);
		}
		[Test]
		public void IndexOfIdentifier()
		{
			// Found be cause '1' is not a valid identifier first character
			Assert.Equal(2, " 1token".IndexOfIdentifier("token"));

			// Found because only looking in the range [2,)
			Assert.Equal(2, " 1token".IndexOfIdentifier("token", 2));

			// Not found because 'token' is a substring of 'token2'
			Assert.Equal(-1, " 1token2".IndexOfIdentifier("token"));

			// Found the second 'token' that isn't a substring
			Assert.Equal(9, " 1token2 token ".IndexOfIdentifier("token"));

			// Found because only looking in the range [2,7)
			Assert.Equal(2, " 1token2".IndexOfIdentifier("token", 2, 5));

			// Not found because 'token' is a substring of '_1token'
			Assert.Equal(-1, " _1token".IndexOfIdentifier("token"));

			// Found because only looking the range [3,)
			Assert.Equal(3, " _1token".IndexOfIdentifier("token", 3));

			// Not found because 'token' is a substring of 'token2'
			Assert.Equal(-1, " _1token2".IndexOfIdentifier("token", 3));

			// Found because only looking in the range [3,8)
			Assert.Equal(3, " _1token2".IndexOfIdentifier("token", 3, 5));

			// Not found because 'token' is a substring of '_1token'
			Assert.Equal(-1, " _1token2".IndexOfIdentifier("token", 0, 8));

			// Not found because 'token' is a substring of '_1111token'
			Assert.Equal(-1, " _1111token".IndexOfIdentifier("token"));

			// Found because '1111' is not a valid identifier first character
			Assert.Equal(6, " _1111token".IndexOfIdentifier("token", 2));
		}
		[Test]
		public void StringTransform()
		{
			const string str0 = "SOME_stringWith_weird_Casing_Number03";
			string str;

			str = str0.Txfm(Str_.ECapitalise.LowerCase, Str_.ECapitalise.LowerCase, Str_.ESeparate.Add, "_", "_");
			Assert.Equal("some_string_with_weird_casing_number_03", str);

			str = str0.Txfm(Str_.ECapitalise.UpperCase, Str_.ECapitalise.LowerCase, Str_.ESeparate.Remove, null, "_");
			Assert.Equal("SomeStringWithWeirdCasingNumber03", str);

			str = str0.Txfm(Str_.ECapitalise.UpperCase, Str_.ECapitalise.UpperCase, Str_.ESeparate.Add, "^ ^", "_");
			Assert.Equal("SOME^ ^STRING^ ^WITH^ ^WEIRD^ ^CASING^ ^NUMBER^ ^03", str);

			str = "FieldCAPSBlah".Txfm(Str_.ECapitalise.UpperCase, Str_.ECapitalise.DontChange, Str_.ESeparate.Add, " ");
			Assert.Equal("Field CAPS Blah", str);
		}
		[Test]
		public void RegexPatterns()
		{
			var tests = new[]
			{
				// Quoted path, not at the start of a string, with white spaces, and non word but legal path characters
				new
				{
					Str    = @"words ""a:\path with space -,\file with space.extn"" : 412",
					Match0 = @"""a:\path with space -,\file with space.extn""",
					Drive  = @"a:",
					Dir    = @"\path with space -,\",
					File   = @"file with space.extn",
					Quote  = @"""",
					Valid  = true,
				},

				// Same as above, but in single quotes
				new
				{
					Str    = @"words 'a:\path with space -,\file with space.extn' : 412",
					Match0 = @"'a:\path with space -,\file with space.extn'",
					Drive  = @"a:",
					Dir    = @"\path with space -,\",
					File   = @"file with space.extn",
					Quote  = @"'",
					Valid  = true,
				},

				// Illegal path
				new
				{
					Str    = @"words ""a:\path invalid-<>\file.extn"" : 412",
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
					Str    = @"words a:\path\file.extn (412) words",
					Match0 = @"a:\path\file.extn",
					Drive  = @"a:",
					Dir    = @"\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Path without a directory, or quotes
				new
				{
					Str    = @"words a:\file.extn (412) words",
					Match0 = @"a:\file.extn",
					Drive  = @"a:",
					Dir    = @"\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Path without quotes, with spaces in the path
				new
				{
					Str    = @"words a:\path\path\broke n\file.extn (412) words",
					Match0 = @"a:\path\path\broke",
					Drive  = @"a:",
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
					Str    = @"a:\path\path\path\file.extn",
					Match0 = @"a:\path\path\path\file.extn",
					Drive  = @"a:",
					Dir    = @"\path\path\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},

				// Full path at the start of a string contains white space
				new
				{
					Str    = @"a:\path\path\broke n\file.extn",
					Match0 = @"a:\path\path\broke",
					Drive  = @"a:",
					Dir    = @"\path\path\",
					File   = @"broke",
					Quote  = @"",
					Valid  = true,
				},

				// Full path at the start of a string, in quotes
				new
				{
					Str = @"""a:\path\file.extn""",
					Match0 = @"""a:\path\file.extn""",
					Drive  = @"a:",
					Dir    = @"\path\",
					File   = @"file.extn",
					Quote  = @"""",
					Valid  = true,
				},

				// Full path at the start of a string followed by words
				new
				{
					Str = @"a:\path\path\path\file.extn (412) words",
					Match0 = @"a:\path\path\path\file.extn",
					Drive  = @"a:",
					Dir    = @"\path\path\path\",
					File   = @"file.extn",
					Quote  = @"",
					Valid  = true,
				},
			};

			foreach (var test in tests)
			{
				var m = Regex.Match(test.Str, Regex_.FullPathPattern);
				Assert.Equal(m.Success, test.Valid);
				Assert.Equal(m.Groups[0].Value, test.Match0);
				Assert.Equal(m.Groups["drive"].Value, test.Drive);
				Assert.Equal(m.Groups["dir"].Value, test.Dir);
				Assert.Equal(m.Groups["file"].Value, test.File);
				Assert.Equal(m.Groups["q"].Value, test.Quote);
			}
		}
	}
}
#endif
