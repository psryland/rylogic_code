using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Rylogic.Common;
using Rylogic.Extn;

namespace RyLogViewer
{
	public static class Tools
	{
		/// <summary>Return the range that spans 'ranges' (assuming the list is ordered)</summary>
		public static RangeI GetRange(IList<RangeI> ranges)
		{
			return ranges.Count != 0
				? new RangeI(ranges.Front().Beg, ranges.Back().End)
				: RangeI.Zero;
		}

		/// <summary>Convert an encoding name to encoding object</summary>
		public static Encoding GetEncoding(string enc_name, Encoding prev = null)
		{
			// If 'enc_name' is empty, this is the auto detect case, in which case we don't
			// modify it from what it's already set to. Auto detect will set it on file load.
			if (enc_name.Length == 0) return prev ?? Encoding.UTF8;
			if (enc_name == Encoding.ASCII.EncodingName) return Encoding.ASCII;
			if (enc_name == Encoding.UTF8.EncodingName) return Encoding.UTF8;
			if (enc_name == Encoding.Unicode.EncodingName) return Encoding.Unicode;
			if (enc_name == Encoding.BigEndianUnicode.EncodingName) return Encoding.BigEndianUnicode;
			throw new NotSupportedException("Text file encoding '" + enc_name + "' is not supported");
		}

		/// <summary>Convert a line ending to a byte array</summary>
		public static byte[] GetLineEnding(string ending, byte[] prev = null)
		{
			// If 'ending' is empty, this is the auto detect case, in which case we don't
			// modify it from what it's already set to. Auto detect will set it on file load.
			if (ending.Length == 0) return prev ?? Encoding.UTF8.GetBytes("\n");
			return Encoding.UTF8.GetBytes(Robitise(ending));
		}

		/// <summary>Guess the file encoding for the current file. 'certain' is true if the returned encoding isn't just a guess</summary>
		public static Encoding GuessEncoding(Stream src, out bool certain)
		{
			try
			{
				// Preserve the position in 'src'
				using (src.PreservePosition())
				{
					Encoding encoding;

					src.Position = 0;
					var buf = new byte[4];
					var read = src.Read(buf, 0, buf.Length);

					// Look for a BOM
					if (read >= 3 && buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF)
						encoding = Encoding.UTF8;
					else if (read >= 2 && buf[0] == 0xFE && buf[1] == 0xFF)
						encoding = Encoding.BigEndianUnicode;
					else if (read >= 2 && buf[0] == 0xFF && buf[1] == 0xFE)
						encoding = Encoding.Unicode;
					else // If no valid bomb is found, assume UTF-8 as that is a superset of ASCII
						encoding = Encoding.UTF8;

					certain = true;
					return encoding;
				}
			}
			catch (FileNotFoundException) { } // Ignore failures, the file may not have any data yet

			certain = false;
			return Encoding.UTF8;
		}

		/// <summary>Auto detect the line end format. Must be called from the main thread. 'certain' is true if the returned row delimiter isn't just a guess</summary>
		public static byte[] GuessLineEnding(Stream src, Encoding encoding, int max_line_length, out bool certain)
		{
			try
			{
				using (src.PreservePosition())
				{
					src.Position = 0;
					using (var sr = new StreamReader(src, encoding, false, 4096, true))
					{
						// Read the maximum line length from the file
						var buf = new char[max_line_length + 1];
						var read = sr.ReadBlock(buf, 0, max_line_length);
						if (read != 0)
						{
							// Look for the first newline/carriage return character
							var i = Array.FindIndex(buf, 0, read, c => c == '\n' || c == '\r');
							if (i != -1)
							{
								// Match \r\n, \r, or \n
								// Don't match more than this because they could represent consecutive blank lines
								certain = true;
								return buf[i] == '\r' && buf[i + 1] == '\n'
									? encoding.GetBytes(buf, i, 2)
									: encoding.GetBytes(buf, i, 1);
							}
						}
					}
				}
			}
			catch (FileNotFoundException) { } // Ignore failures, the file may not have any data yet

			certain = false;
			return encoding.GetBytes("\n");
		}

		/// <summary>Replace the \r,\n,\t characters with '&lt;CR&gt;', '&lt;LF&gt;', and '&lt;TAB&gt;'</summary>
		public static string Humanise(string str)
		{
			// Substitute control characters for "<CR>,<LF>,<TAB>"
			str = str.Replace("\r", "<CR>");
			str = str.Replace("\n", "<LF>");
			str = str.Replace("\t", "<TAB>");

			// Substitute UNICODE characters for a pseudo escaped characters
			str = Regex.Replace(str, @"[^\x20-\x7E]", new MatchEvaluator(m =>
			{
				var ch = (int)m.Groups[0].Value[0];
				return
					ch > 0xFFFF ? $"\\u{ch:x6}" :
					ch > 0xFF ? $"\\u{ch:x4}" :
					$"\\u{ch:x2}";
			}));
			return str;
		}

		/// <summary>Replace the '&lt;CR&gt;', '&lt;LF&gt;', and '&lt;TAB&gt;' strings with \r,\n,\t characters</summary>
		public static string Robitise(string str)
		{
			// Substitute "<CR>,<LF>,<TAB>" for the actual characters
			str = Regex.Replace(str, Regex.Escape("<CR>"), "\r", RegexOptions.IgnoreCase);
			str = Regex.Replace(str, Regex.Escape("<LF>"), "\n", RegexOptions.IgnoreCase);
			str = Regex.Replace(str, Regex.Escape("<TAB>"), "\t", RegexOptions.IgnoreCase);

			// Substitute "\uXXXX" for the UNICODE characters
			str = Regex.Replace(str, @"\\u([\da-f]+)", new MatchEvaluator(m =>
			{
				var xxxx = m.Groups[1].Value;
				var code = int.Parse(xxxx, NumberStyles.HexNumber);
				return new string((char)code, 1);
			}), RegexOptions.IgnoreCase);

			return str;
		}

		/// <summary>
		/// Returns the index in 'buf' of one past the next delimiter, starting from 'start'.
		/// If not found, returns -1 when searching backwards, or length when searching forwards</summary>
		public static int FindNextDelim(byte[] buf, int start, int length, byte[] delim, bool backward)
		{
			Debug.Assert(start >= -1 && start <= length);
			int i = start, di = backward ? -1 : 1;
			for (; i >= 0 && i < length; i += di)
			{
				// Quick test using the first byte of the delimiter
				if (buf[i] != delim[0]) continue;

				// Test the remaining bytes of the delimiter
				bool is_match = (i + delim.Length) <= length;
				for (int j = 1; is_match && j != delim.Length; ++j) is_match = buf[i + j] == delim[j];
				if (!is_match) continue;

				// 'i' now points to the start of the delimiter,
				// shift it forward to one past the delimiter.
				i += delim.Length;
				break;
			}
			return i;
		}
	}
}
