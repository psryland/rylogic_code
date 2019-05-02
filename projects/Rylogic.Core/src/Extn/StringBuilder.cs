//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Diagnostics;
using System.IO;
using System.Text;
using Rylogic.Common;

namespace Rylogic.Extn
{
	public static class StringBuilder_
	{
		/// <summary>Return the list element in the list</summary>
		public static char Back(this StringBuilder sb)
		{
			return sb[sb.Length - 1];
		}

		/// <summary>Return a substring starting at 'ofs'</summary>
		public static string ToString(this StringBuilder sb, int ofs)
		{
			return sb.ToString(ofs, sb.Length - ofs);
		}

		/// <summary>Write this string to a file</summary>
		public static void ToFile(this StringBuilder sb, string filepath, bool append = false, bool create_dir_if_necessary = true)
		{
			// Ensure the directory exists
			var dir = Path_.Directory(filepath);
			if (create_dir_if_necessary && !Path_.DirExists(dir))
				Directory.CreateDirectory(dir);

			using (var f = new StreamWriter(new FileStream(filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
				f.Write(sb.ToString());
		}

		/// <summary>Replace the string builder contents with 'value'</summary>
		public static StringBuilder Assign<T>(this StringBuilder sb, T value)
		{
			sb.Clear();
			sb.Append(value);
			return sb;
		}

		/// <summary>Append a bunch of stuff</summary>
		public static StringBuilder Append(this StringBuilder sb, params object[] parts)
		{
			foreach (var p in parts) sb.Append(p.ToString());
			return sb;
		}

		/// <summary>Append 'sep' if the current string is not empty</summary>
		public static StringBuilder AppendSep(this StringBuilder sb, string sep)
		{
			if (sb.Length == 0) return sb;
			return sb.Append(sep);
		}

		/// <summary>Resize the string builder to 'newsize', padding with 'fill' as needed</summary>
		public static StringBuilder Resize(this StringBuilder sb, int newsize, char fill = '\0')
		{
			if (sb.Length > newsize)
				sb.Length = newsize;
			else
				sb.Append(fill, newsize - sb.Length);
			return sb;
		}
	
		/// <summary>Trim chars from the end of the string builder</summary>
		public static StringBuilder TrimEnd(this StringBuilder sb, params char[] chars)
		{
			while (sb.Length != 0 && chars.IndexOf(sb[sb.Length-1]) != -1) --sb.Length;
			return sb;
		}
		public static StringBuilder TrimEnd(this StringBuilder sb, string str)
		{
			while (sb.Length >= str.Length && string.CompareOrdinal(str, 0, sb.ToString(), sb.Length-str.Length, str.Length) == 0) sb.Length -= str.Length;
			return sb;
		}

		/// <summary>Trim chars from the start of the string builder</summary>
		public static StringBuilder TrimStart(this StringBuilder sb, params char[] chars)
		{
			int i = 0;
			while (i != sb.Length && chars.IndexOf(sb[i]) != -1) ++i;
			return sb.Remove(0, i);
		}
		public static StringBuilder TrimStart(this StringBuilder sb, string str)
		{
			int i = 0;
			while (sb.Length - i >= str.Length && string.CompareOrdinal(str, 0, sb.ToString(), i, str.Length) == 0) i += str.Length;
			return sb.Remove(0, i);
		}

		/// <summary>Trim characters from the start/end of the string</summary>
		public static StringBuilder Trim(this StringBuilder sb, params char[] chars)
		{
			return sb.TrimEnd(chars).TrimStart(chars);
		}
		public static StringBuilder Trim(this StringBuilder sb, string str)
		{
			return sb.TrimEnd(str).TrimStart(str);
		}
		
		/// <summary>Append 'lines' ensuring exactly one new line between each</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, params string[] lines)
		{
			sb.TrimEnd('\n','\r').AppendLine();
			foreach (var line in lines)
				sb.AppendLine(line.Trim('\n','\r'));
			return sb;
		}

		/// <summary>Append 'line' ensuring exactly one new line in between</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, string line)
		{
			return sb.TrimEnd('\n','\r').AppendLine().AppendLine(line.Trim('\n','\r'));
		}

		/// <summary>Append 'line' ensuring exactly one new line in between</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, StringBuilder line)
		{
			return sb.TrimEnd('\n','\r').AppendLine().AppendLine(line.ToString());
		}

		/// <summary>Reverse the characters in the string</summary>
		public static StringBuilder Reverse(this StringBuilder sb)
		{
			var end = sb.Length - 1;
			for (int i = 0, iend = sb.Length / 2; i < iend; ++i)
			{
				var tmp = sb[i];
				sb[i] = sb[end - i];
				sb[end - i] = tmp;
			}
			return sb;
		}

		/// <summary>Return a substring of the internal string</summary>
		public static string Substring(this StringBuilder sb, int start, int length)
		{
			return sb.ToString(start, length);
		}

		/// <summary>Remove all occurrences of 'ch' from the internal string</summary>
		public static StringBuilder Remove(this StringBuilder sb, char ch, out int removed_count)
		{
			removed_count = 0;

			// Find the first occurrence
			int i; for (i = 0; i != sb.Length && sb[i] != ch; ++i) {}
			if (i == sb.Length) return sb;

			// Copy backward removing 'ch's
			int j; for (j = i; i != sb.Length; ++i)
			{
				if (sb[i] == ch) ++removed_count;
				else sb[j++] = sb[i];
			}

			// Resize 'sb'
			sb.Length = j;
			return sb;
		}
		public static StringBuilder Remove(this StringBuilder sb, char ch)
		{
			int removed_count;
			return sb.Remove(ch, out removed_count);
		}

		/// <summary>True if the start of the contained string matches 'str'</summary>
		public static bool StartsWith(this StringBuilder sb, string str, int start, bool ignore_case = false)
		{
			Debug.Assert(start >= 0 && start <= sb.Length);

			// Check for sub string match
			int i = start, iend = i + str.Length;
			if (ignore_case) for (; i != sb.Length && i != iend && char.ToLower(sb[i]) == char.ToLower(str[i - start]); ++i) {}
			else             for (; i != sb.Length && i != iend && sb[i] == str[i - start]; ++i) {}
			return i == iend;
		}
		public static bool StartsWith(this StringBuilder sb, string str, bool ignore_case = false)
		{
			return StartsWith(sb, str, 0, ignore_case);
		}

		/// <summary>Index of the first occurrence of 'ch'</summary>
		public static int IndexOf(this StringBuilder sb, char ch, int start, int length, bool ignore_case = false)
		{
			Debug.Assert(start >= 0 && start <= sb.Length);
			Debug.Assert(length >= 0 && start + length <= sb.Length);

			int i = start, iend = i + length;
			if (ignore_case) for (; i != iend && char.ToLower(sb[i]) != char.ToLower(ch); ++i) { }
			else             for (; i != iend && sb[i] != ch; ++i) { }
			return i != iend ? i : -1;
		}
		public static int IndexOf(this StringBuilder sb, char value, bool ignore_case = false)
		{
			return IndexOf(sb, value, 0, sb.Length, ignore_case);
		}

		/// <summary>Index of the first occurrence of 'str'</summary>
		public static int IndexOf(this StringBuilder sb, string str, int start, int length, bool ignore_case = false)
		{
			Debug.Assert(start >= 0 && start <= sb.Length);
			Debug.Assert(length >= 0 && start + length <= sb.Length);

			// Empty strings match immediately
			if (str.Length == 0)
				return start;

			for (int s = start, e = s + length; s != e; ++s)
			{
				// Find the index of the first character in 'str'
				s = sb.IndexOf(str[0], s, e - s, ignore_case:ignore_case);
				if (s == -1) return -1;

				// Check for sub string match
				if (sb.StartsWith(str, s, ignore_case))
					return s;
			}
			return -1;
		}
		public static int IndexOf(this StringBuilder sb, string str, bool ignore_case = false)
		{
			return IndexOf(sb, str, 0, sb.Length, ignore_case);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture] public class TestStringBuilderExtns
	{
		[Test] public void StringBuilderExtensions()
		{
			const string  s = "  \t\nA string XXX\n  \t  ";

			{// Trim start
				var sb = new StringBuilder(s);
				Assert.Equal(s.TrimStart(' ', '\t', '\n', 'X'), sb.TrimStart(' ', '\t', '\n', 'X').ToString());
			}
			{// Trim end
				var sb = new StringBuilder(s);
				Assert.Equal(s.TrimEnd(' ', '\t', '\n', 'X'), sb.TrimEnd(' ', '\t', '\n', 'X').ToString());
			}
			{// Trim
				var sb = new StringBuilder(s);
				Assert.Equal(s.Trim(' ', '\t', '\n', 'X'), sb.Trim(' ', '\t', '\n', 'X').ToString());
			}
			{// Substring
				var sb = new StringBuilder(s);
				Assert.Equal(sb.Substring(6, 6), "string");
				Assert.Equal(sb.Substring(0, 3), "  \t");
				Assert.Equal(sb.Substring(13, sb.Length - 13), "XXX\n  \t  ");
			}
			{// Remove
				var sb = new StringBuilder(s);
				int removed;
				sb.Remove(' ', out removed);
				Assert.Equal(sb.ToString(), "\t\nAstringXXX\n\t");
				Assert.Equal(removed, 8);
			}
			{// StartsWith
				var sb = new StringBuilder(s);
				Assert.Equal(sb.StartsWith("  \t\n"), true);
				Assert.Equal(sb.StartsWith("A string", 4), true);
				Assert.Equal(sb.StartsWith("A string  ", 4), false);

				sb.Length = 0;
				Assert.Equal(sb.StartsWith("1"), false);
				Assert.Equal(sb.StartsWith(""), true);
			}
			{// IndexOf
				var sb = new StringBuilder(s);
				Assert.Equal(sb.IndexOf('a', ignore_case:true), 4);
				Assert.Equal(sb.IndexOf('X', 1, 4), -1);
				Assert.Equal(sb.IndexOf("xx", ignore_case:true), 13);
				Assert.Equal(sb.IndexOf("XXXX"), -1);
				Assert.Equal(sb.IndexOf("XXX", 14, sb.Length - 14), -1);

				sb.Length = 0;
				Assert.Equal(sb.IndexOf("1"), -1);
				Assert.Equal(sb.IndexOf(""), 0);
			}
		}
	}
}
#endif
