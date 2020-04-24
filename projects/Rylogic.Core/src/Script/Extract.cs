using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Script
{
	public static class Extract
	{
		public const string DefaultDelimiters = " \t\n\r";

		/// <summary>Extract a contiguous block of characters up to (and possibly including) a new line character</summary>
		public static bool Line(out string line, Src src, bool inc_cr, string? newline = null)
		{
			newline ??= "\n";
			BufferWhile(src, (s,i) => !newline.Contains(s[i]) ? 1 : 0, 0, out var len);
			if (inc_cr && src[len] != 0) { len += newline.Length; src.ReadAhead(len); }
			line = src.Buffer.ToString(0, len);
			src += len;
			return true;
		}

		/// <summary>Extract a contiguous block of non-delimiter characters from 'src'</summary>
		public static bool Token(out string token, Src src, string? delim = null)
		{
			token = string.Empty;
			delim ??= DefaultDelimiters;

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Copy up to the next delimiter
			BufferWhile(src, (s,i) => !delim.Contains(s[i]) ? 1 : 0, 0, out var len);
			token = src.Buffer.ToString(0, len);
			src += len;
			return true;
		}

		/// <summary>Extract a contiguous block of identifier characters from 'src' incrementing 'src'</summary>
		public static bool Identifier(out string id, Src src, string? delim = null)
		{
			id = string.Empty;
			delim ??= DefaultDelimiters;

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// If the first non-delimiter is not a valid identifier character, then we can't extract an identifier
			if (!Str_.IsIdentifier(src, true))
				return false;

			// Copy up to the first non-identifier character
			BufferWhile(src, (s,i) => Str_.IsIdentifier(s[i], false) ? 1 : 0, 1, out var len);
			id = src.Buffer.ToString(0, len);
			src += len;
			return true;
		}

		/// <summary>Extract multi-part identifiers from 'src' incrementing 'src'. E.g. House.Room.Item</summary>
		public static bool Identifiers(out string[] ids, Src src, char sep = '.', string? delim = null)
		{
			ids = Array.Empty<string>();
			var idents = new List<string>();
			for (; ; )
			{
				if (idents.Count != 0) if (src == sep) ++src; else break;
				if (!Identifier(out var id, src, delim)) return false;
				idents.Add(id);
			}
			ids = idents.ToArray();
			return true;
		}

		/// <summary>
		/// Extract a quoted (") string.
		/// if 'escape' is not 0, it is treated as the escape character
		/// if 'quote' is not null, it is treated as the accepted quote characters</summary>
		public static bool String(out string str, Src src, string? delim = null)
		{
			return String(out str, src, '\0', null, delim);
		}
		public static bool String(out string str, Src src, char escape, string? quotes = null, string? delim = null)
		{
			str = string.Empty;
			delim ??= DefaultDelimiters;

			// Set the accepted quote characters
			quotes ??= "\"\'";

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// If the next character is not an acceptable quote, then this isn't a string
			char quote = src;
			if (quotes.Contains(quote)) ++src; else return false;

			// Copy the string
			var sb = new StringBuilder();
			if (escape != 0)
			{
				// Copy to the closing quote, allowing for the escape character
				for (; src != 0 && src != quote; ++src)
				{
					if (src == escape)
					{
						switch (++src)
						{
						default: break;
						case 'a':  sb.Append('\a'); break;
						case 'b':  sb.Append('\b'); break;
						case 'f':  sb.Append('\f'); break;
						case 'n':  sb.Append('\n'); break;
						case 'r':  sb.Append('\r'); break;
						case 't':  sb.Append('\t'); break;
						case 'v':  sb.Append('\v'); break;
						case '\'': sb.Append('\''); break;
						case '\"': sb.Append('\"'); break;
						case '\\': sb.Append('\\'); break;
						case '0':
						case '1':
						case '2':
						case '3':
							{
								// ASCII character in octal
								var oct = new char[8]; var i = 0;
								for (; i != oct.Length && Str_.IsOctDigit(src); ++i, ++src) oct[i] = src;
								sb.Append((char)Convert.ToInt32(new string(oct, 0, i), 8));
								break;
							}
						case 'x':
							{
								// ASCII or UNICODE character in hex
								var hex = new char[8]; var i = 0;
								for (; i != hex.Length && Str_.IsHexDigit(src); ++i, ++src) hex[i] = src;
								sb.Append((char)Convert.ToInt32(new string(hex, 0, i), 16));
								break;
							}
						}
					}
					else
					{
						sb.Append(src);
					}
				}
			}
			else
			{
				// Copy to the next quote
				for (; src != 0 && src != quote; ++src)
					sb.Append(src);
			}

			// If the string doesn't end with a quote, then it's not a valid string
			if (src == quote) ++src; else return false;
			str = sb.ToString();
			return true;
		}

		/// <summary>
		/// Extract a boolean from 'src'
		/// Expects 'src' to point to a string of the following form:
		/// [delim]{0|1|true|false}
		/// The first character that does not fit this form stops the scan.
		/// '0','1' must be followed by a non-identifier character
		/// 'true', 'false' can have any case</summary>
		public static bool Bool(out bool bool_, Src src, string? delim = null)
		{
			bool_ = false;
			delim ??= DefaultDelimiters;

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Extract the boolean
			switch (char.ToLower(src))
			{
			default: return false;
			case '0': bool_ = false; return !Str_.IsIdentifier(++src, false);
			case '1': bool_ = true; return !Str_.IsIdentifier(++src, false);
			case 't': bool_ = true; return char.ToLower(++src) == 'r' && char.ToLower(++src) == 'u' && char.ToLower(++src) == 'e' && !Str_.IsIdentifier(++src, false);
			case 'f': bool_ = false; return char.ToLower(++src) == 'a' && char.ToLower(++src) == 'l' && char.ToLower(++src) == 's' && char.ToLower(++src) == 'e' && !Str_.IsIdentifier(++src, false);
			}
		}
		public static bool BoolArray(out bool[] bool_, int count, Src src, string? delim = null)
		{
			bool_ = new bool[count];
			for (int i = 0; i != count; ++i)
				if (!Bool(out bool_[i], src, delim))
					return false;
			return true;
		}

		/// <summary>
		/// Extract an integral number from 'src' (basically 'strtol')
		/// Expects 'src' to point to a string of the following form:
		/// [delim] [{+|–}][0[{x|X|b|B}]][digits]
		/// The first character that does not fit this form stops the scan.
		/// If 'radix' is between 2 and 36, then it is used as the base of the number.
		/// If 'radix' is 0, the initial characters of the string are used to determine the base.
		/// If the first character is 0 and the second character is not 'x' or 'X', the string is interpreted as an octal integer;
		/// otherwise, it is interpreted as a decimal number. If the first character is '0' and the second character is 'x' or 'X',
		/// the string is interpreted as a hexadecimal integer. If the first character is '1' through '9', the string is interpreted
		/// as a decimal integer. The letters 'a' through 'z' (or 'A' through 'Z') are assigned the values 10 through 35; only letters
		/// whose assigned values are less than 'radix' are permitted.</summary>
		public static bool Int(out long intg, int radix, Src src, string? delim = null)
		{
			intg = 0;

			if (!BufferNumber(src, ref radix, out var len, ENumType.Int, delim))
				return false;

			try
			{
				var str = src.Buffer.ToString(0, len).ToLower();
				str = str.TrimEnd('u','l');
				if (radix == 2  && str.StartsWith("0b")) str = str.Remove(0, 2);
				if (radix == 8  && str.StartsWith("0o")) str = str.Remove(0, 2);
				if (radix == 16 && str.StartsWith("0x")) str = str.Remove(0, 2);
				intg = Convert.ToInt64(str, radix);
				src += len;
				return true;
			}
			catch { return false; }
		}
		public static bool IntArray(out long[] intg, int count, int radix, Src src, string? delim = null)
		{
			intg = new long[count];
			for (int i = 0; i != count; ++i)
				if (!Int(out intg[i], radix, src, delim))
					return false;
			return true;
		}

		/// <summary>
		/// Extract a floating point number from 'src'
		/// Expects 'src' to point to a string of the following form:
		/// [delim] [{+|-}][digits][.digits][{d|D|e|E}[{+|-}]digits]
		/// The first character that does not fit this form stops the scan.
		/// If no digits appear before the '.' character, at least one must appear after the '.' character.
		/// The decimal digits can be followed by an exponent, which consists of an introductory letter (d, D, e, or E) and an optionally signed integer.
		/// If neither an exponent part nor a '.' character appears, a '.' character is assumed to follow the last digit in the string.</summary>
		public static bool Real(out double real, Src src, string? delim = null)
		{
			real = 0;

			int radix = 10;
			if (!BufferNumber(src, ref radix, out var len, ENumType.FP, delim))
				return false;

			try
			{
				var str = src.Buffer.ToString(0, len).ToLower();
				str = str.TrimEnd('f');
				real = Convert.ToDouble(str);
				src += len;
				return true;
			}
			catch { return false; }
		}
		public static bool RealArray(out double[] real, int count, Src src, string? delim = null)
		{
			real = new double[count];
			for (int i = 0; i != count; ++i)
				if (!Real(out real[i], src, delim))
					return false;
			return true;
		}

		/// <summary>
		/// Extract a number of unknown format from 'src'
		/// Expects 'src' to point to a string containing any allowable number format (int or real).</summary>
		public static bool Number<Num>(out Num num, Src src, int radix = 0, string? delim = null)
			where Num : struct
		{
			num = default!;

			if (!BufferNumber(src, ref radix, out var len, ENumType.Any, delim))
				return false;

			try
			{
				var str = src.Buffer.ToString(0, len).ToLower();
				str = str.TrimEnd('f','u','l');
				if (radix == 2 && str.StartsWith("0b")) str = str.Remove(0, 2);
				if (radix == 8 && str.StartsWith("0o")) str = str.Remove(0, 2);
				if (radix == 16&& str.StartsWith("0x")) str = str.Remove(0, 2);
				num = num switch
				{
					sbyte _ => (Num)(object)Convert.ToSByte(str, radix),
					byte _ => (Num)(object)Convert.ToByte(str, radix),
					short _ => (Num)(object)Convert.ToInt16(str, radix),
					ushort _ => (Num)(object)Convert.ToUInt16(str, radix),
					int _ => (Num)(object)Convert.ToInt32(str, radix),
					uint _ => (Num)(object)Convert.ToUInt32(str, radix),
					long _ => (Num)(object)Convert.ToInt64(str, radix),
					ulong _ => (Num)(object)Convert.ToUInt64(str, radix),
					float _ => (Num)(object)Convert.ToSingle(str),
					double _ => (Num)(object)Convert.ToDouble(str),
					decimal _ => (Num)(object)Convert.ToDecimal(str),
					_ => throw new InvalidOperationException($"Unsupported number type: {num.GetType().Name}"),
				};
				src += len;
				return true;
			}
			catch (Exception ex)
			{
				if (ex is InvalidOperationException) throw;
				return false;
			}
		}

		/// <summary>
		/// Extract an integral type and convert it to an enum value.
		/// This is basically a convenience wrapper around ExtractInt.</summary>
		public static bool EnumValue<TEnum>(out TEnum enum_, Src src, int radix = 10, string? delim = null)
			where TEnum : struct, IConvertible
		{
			enum_ = default!;
			if (!Int(out var val, radix, src, delim))
				return false;

			enum_ = Enum<TEnum>.ToValue(val);
			return true;
		}

		/// <summary>Extracts an enum by its string name.</summary>
		public static bool EnumName<TEnum>(out TEnum enum_, Src src, string? delim = null)
		{
			enum_ = default!;
			if (!Identifier(out var val, src, delim))
				return false;

			enum_ = (TEnum)Enum.Parse(typeof(TEnum), val, ignoreCase: false);
			return true;
		}

		/// <summary>Extract an array of bytes</summary>
		public static bool Data(byte[] data, Src src, int radix = 16, string? delim = null)
		{
			return Data(data, 0, data.Length, src, radix, delim);
		}
		public static bool Data(byte[] data, int start, int count, Src src, int radix = 16, string? delim = null)
		{
			delim ??= DefaultDelimiters;

			for (int i = 0; i != count; ++i)
			{
				// Find the first non-delimiter
				if (!AdvanceToNonDelim(src, delim))
					return false;

				// Buffer contiguous characters
				BufferWhile(src, (s, i) => !delim.Contains(s[i]) ? 1 : 0, 0, out var len);
				var str = src.Buffer.ToString(0, len);
				src += len;

				// Convert the byte characters to bytes
				try { data[start + i] = Convert.ToByte(str, radix); }
				catch { return false; }
			}
			return true;
		}

		/// <summary>
		/// Advance 'src' while 'pred' is true.
		/// Returns true if the function quick-exited due to 'pred', false if src == 0</summary>
		public static bool Advance(Src src, Func<char, bool> pred)
		{
			// Find the first non-delimiter
			for (; src != 0 && pred(src); ++src) { }
			return src != 0;
		}

		/// <summary>Advance 'src' to the next delimiter character. Returns false if src == 0</summary>
		public static bool AdvanceToDelim(Src src, string delim)
		{
			// Advance while 'src' does not point to a delimiter
			return Advance(src, ch => !delim.Contains(ch));
		}

		/// <summary>Advance 'src' to the next non-delimiter character. Returns false if src == 0</summary>
		public static bool AdvanceToNonDelim(Src src, string delim)
		{
			// Advance while 'src' points to a delimiter
			return Advance(src, ch => delim.Contains(ch));
		}

		/// <summary>
		/// Call '++src' until 'pred' returns false.
		/// 'eat_initial' and 'eat_final' are the number of characters to consume before
		/// applying the predicate 'pred' and the number to consume after 'pred' returns false.</summary>
		public static void Eat(Src src, int eat_initial, int eat_final, Func<Src, bool> pred)
		{
			for (src += eat_initial; src != 0 && pred(src); ++src) { }
			src += eat_final;
		}
		public static void EatLineSpace(Src src, int eat_initial, int eat_final)
		{
			Eat(src, eat_initial, eat_final, s => Str_.IsLineSpace(s));
		}
		public static void EatWhiteSpace(Src src, int eat_initial, int eat_final)
		{
			Eat(src, eat_initial, eat_final, s => Str_.IsWhiteSpace(s));
		}
		public static void EatLine(Src src, int eat_initial, int eat_final, bool eat_newline)
		{
			src += eat_initial;
			Eat(src, 0, 0, s => !(s[0] == '\n') && !(s[0] == '\r' && s[1] == '\n'));
			if (eat_newline) src += (src[0] == '\r' && src[1] == '\n') ? 2 : (src[0] == '\n') ? 1 : 0;
			src += eat_final;
		}
		public static void EatBlock(Src src, string block_beg, string block_end)
		{
			if (block_beg.Length == 0)
				throw new Exception($"The block start marker cannot have length = 0");
			if (block_end.Length == 0)
				throw new Exception($"The block end marker cannot have length = 0");
			if (!src.Match(block_beg))
				throw new Exception($"Don't call {nameof(EatBlock)} unless 'src' is pointing at the block start");

			Eat(src, block_beg.Length, block_end.Length, s => !s.Match(block_end));
		}
		public static void EatLiteral(Src src)
		{
			if (src != '\"' && src != '\'')
				throw new Exception($"Don't call {nameof(EatLiteral)} unless 'src' is pointing at a literal string");
			
			var quote = (char)src;
			var escape = false;
			Eat(src, 1, 0, s =>
   			{
				var end = s == quote && !escape;
				escape = s == '\\';
				return !end;
			});
			if (src == quote) ++src;
		}
		public static void EatDelimiters(Src src, string delim)
		{
			for (; delim.Contains(src); ++src) {}
		}
		public static void EatLineComment(Src src, string line_comment = "//")
		{
			if (!src.Match(line_comment))
				throw new Exception($"Don't call {nameof(EatLineComment)} unless 'src' is pointing at a line comment");

			EatLine(src, line_comment.Length, 0, false);
		}
		public static void EatBlockComment(Src src, string block_beg = "/*", string block_end = "*/")
		{
			if (!src.Match(block_beg))
				throw new Exception($"Don't call {nameof(EatBlockComment)} unless 'src' is pointing at a block comment");

			EatBlock(src, block_beg, block_end);
		}

		/// <summary>
		/// Buffer an identifier in 'src'. Returns true if a valid identifier was buffered.
		/// On return, 'len' contains the length of the buffer up to and including the end
		/// of the identifier (i.e. start + strlen(identifier)).</summary>
		public static bool BufferIdentifier(Src src, int start, out int len)
		{
			len = start;
			if (!Str_.IsIdentifier(src[len], true)) return false;
			for (++len; Str_.IsIdentifier(src[len], false); ++len) { }
			return true;
		}

		/// <summary>
		/// Buffer a literal string or character in 'src'. Returns true if a complete literal string or
		/// character was buffered. On return, 'len' contains the length of the buffer up to, and
		/// including, the literal. (i.e. start + strlen(literal))</summary>
		public static bool BufferLiteral(Src src, int start, out int len)
		{
			len = start;

			// Don't call this unless 'src' is pointing at a literal string
			var quote = src[len];
			if (quote != '\"' && quote != '\'')
				return false;

			// Find the end of the literal
			for (bool esc = true; src[len] != '\0' && (esc || src[len] != quote); esc = !esc && src[len] == '\\', ++len) { }
			if (src[len] == quote) ++len; else return false;
			return true;
		}

		/// <summary>
		/// Buffer characters for a number (real or int) in 'src'.
		/// Format: [delim][{+|-}][0[{x|X|b|B}]][digits][.digits][{d|D|e|E|p|P}[{+|-}]digits][U][L][L]
		/// Returns true if valid number characters where buffered</summary>
		public static bool BufferNumber(Src src, ref int radix, out int len, ENumType type = ENumType.Any, string? delim = null)
		{
			len = 0;
			delim ??= DefaultDelimiters;

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Convert a character to it's numerical value
			static int digit(int ch)
			{
				if (ch >= '0' && ch <= '9') return ch - '0';
				if (ch >= 'a' && ch <= 'z') return 10 + ch - 'a';
				if (ch >= 'A' && ch <= 'Z') return 10 + ch - 'A';
				return int.MaxValue;
			}

			var digits_found = false;
			var allow_fp = type.HasFlag(ENumType.FP);
			var fp = false;

			// Look for the optional sign character
			// Ideally we'd prefer not to advance 'src' passed the '+' or '-' if the next
			// character is not the start of a number. However doing so means 'src' can't
			// be a forward only input stream. Therefore, I'm pushing the responsibility
			// back to the caller, they need to check that if *src is a '+' or '-' then
			// the following char is a decimal digit.
			if (src[len] == '+' || src[len] == '-')
				++len;

			// Look for a radix prefix on the number, this overrides 'radix'.
			// If the first digit is zero, then the number may have a radix prefix.
			// '0x' or '0b' must have at least one digit following the prefix
			// Adding 'o' for octal, in addition to standard C literal syntax
			if (src[len] == '0')
			{
				++len;
				var radix_prefix = false;
				if (false) { }
				else if (char.ToLower(src[len]) == 'x') { radix = 16; ++len; radix_prefix = true; }
				else if (char.ToLower(src[len]) == 'o') { radix = 8; ++len; radix_prefix = true; }
				else if (char.ToLower(src[len]) == 'b') { radix = 2; ++len; radix_prefix = true; }
				else
				{
					// If no radix prefix is given, then assume octal zero (for conformance with C) 
					if (radix == 0) radix = Str_.IsDigit(src[len]) ? 8 : 10;
					digits_found = true;
				}

				// Check for the required integer
				if (radix_prefix && digit(src[len]) >= radix)
					return false;
			}
			else if (radix == 0)
			{
				radix = 10;
			}

			// Read digits up to a delimiter, decimal point, or digit >= radix.
			var assumed_fp_len = 0; // the length of the number when we first assumed a FP number.
			for (; src[len] != 0; ++len)
			{
				// If the character is greater than the radix, then assume a FP number.
				// e.g. 09.1 could be an invalid octal number or a FP number. 019 is assumed to be FP.
				var d = digit(src[len]);
				if (d < radix)
				{
					digits_found = true;
					continue;
				}

				if (radix == 8 && allow_fp && d < 10)
				{
					if (assumed_fp_len == 0) assumed_fp_len = len;
					continue;
				}

				break;
			}

			// If we're assuming this is a FP number but no decimal point is found,
			// then truncate the string at the last valid character given 'radix'.
			// If a decimal point is found, change the radix to base 10.
			if (assumed_fp_len != 0)
			{
				if (src[len] == '.') radix = 10;
				else len = assumed_fp_len;
			}

			// FP numbers can be in dec or hex, but not anything else...
			allow_fp &= radix == 10 || radix == 16;
			if (allow_fp)
			{
				// If floating point is allowed, read a decimal point followed by more digits, and an optional exponent
				if (src[len] == '.' && Str_.IsDecDigit(src[++len]))
				{
					fp = true;
					digits_found = true;

					// Read decimal digits up to a delimiter, sign, or exponent
					for (; Str_.IsDecDigit(src[len]); ++len) { }
				}

				// Read an optional exponent
				var ch = char.ToLower(src[len]);
				if (ch == 'e' || ch == 'd' || (ch == 'p' && radix == 16))
				{
					++len;

					// Read the optional exponent sign
					if (src[len] == '+' || src[len] == '-')
						++len;

					// Read decimal digits up to a delimiter, or suffix
					for (; Str_.IsDecDigit(src[len]); ++len) { }
				}
			}

			// Read the optional number suffixes
			if (allow_fp && char.ToLower(src[len]) == 'f')
			{
				fp = true;
				++len;
			}
			if (!fp && char.ToLower(src[len]) == 'u')
			{
				++len;
			}
			if (!fp && char.ToLower(src[len]) == 'l')
			{
				++len;
				if (char.ToLower(src[len]) == 'l')
					++len;
			}
			return digits_found;
		}

		/// <summary>
		/// Buffer up to the next '\n' in 'src'. Returns true if a new line or at least one character is buffered.
		/// if 'include_newline' is false, the new line is removed from the buffer once read.
		/// On return, 'len' contains the length of the buffer up to, and including the new line character (even if the newline character is removed).</summary>
		public static bool BufferLine(Src src, bool include_newline, int start, out int len)
		{
			len = start;
			if (src[len] == '\0') return false;
			for (; src[len] != '\0' && src[len] != '\n'; ++len) { }
			if (src[len] != '\0') { if (include_newline) len += 1; else src.Buffer.Remove(len, 1); }
			return true;
		}

		/// <summary>
		/// Buffer up to and including 'end'. If 'include_end' is false, 'end' is removed from the buffer once read.
		/// On return, 'len' contains the length of the buffer up to, and including 'end' (even if end is removed).</summary>
		public static bool BufferTo(Src src, string end, bool include_end, int start, out int len)
		{
			len = start;
			for (; src[len] != '\0' && !src.Match(end, len); ++len) { }
			if (src[len] != '\0') { if (include_end) len += end.Length; else src.Buffer.Remove(len, end.Length); }
			return src[len] != '\0';
		}

		/// <summary>
		/// Buffer until 'adv' returns 0. Signature for AdvFunc: int(Src&,int)
		/// Returns true if buffering stopped due to 'adv' returning 0.
		/// On return, 'len' contains the length of the buffer up to where 'adv' returned 0.</summary>
		public static bool BufferWhile(Src src, Func<Src, int, int> adv, int start, out int len)
		{
			len = start;
			for (var inc = 0; src[len] != '\0' && (inc = adv(src, len)) != 0; len += inc) { }
			if (src[len] == '\0') len = (int)Math.Min(src.Limit, src.Buffer.Length); // Occurs if 'start' > 'src.Limit' or EOS
			return src[len] != '\0';
		}

		/// <summary>Used to filter the accepted characters when extracting number strings</summary>
		[Flags]
		public enum ENumType
		{
			Int = 1 << 0,
			FP = 1 << 1,
			Any = Int | FP,
		};
	}
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture]
	public partial class TestScript
	{
		[Test]
		public void ExtractLine()
		{
			var src = (Src)new StringSrc("abcefg\nhijk\nlmnop");
			Assert.True(Extract.Line(out var line0, src, false));
			Assert.Equal("abcefg", line0);
			Assert.Equal('\n', src.Peek);
			++src;

			Assert.True(Extract.Line(out var line1, src, true));
			Assert.Equal("hijk\n", line1);
			Assert.Equal('l', src.Peek);

			Assert.True(Extract.Line(out var line2, src, true));
			Assert.Equal("lmnop", line2);
			Assert.Equal('\0', src.Peek);
		}
		[Test]
		public void ExtractToken()
		{
			var src = (Src)new StringSrc("token1 token2:token3-token4");
			Assert.True(Extract.Token(out var tok0, src));
			Assert.Equal("token1", tok0);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Token(out var tok1, src, delim:" \n:;\t"));
			Assert.Equal("token2", tok1);
			Assert.Equal(':', src.Peek);

			Assert.True(Extract.Token(out var tok2, src, delim: " "));
			Assert.Equal(":token3-token4", tok2);
			Assert.Equal('\0', src.Peek);
		}
		[Test]
		public void ExtractIdentifier()
		{
			var src = new StringSrc("_ident ident2:token3");
			Assert.True(Extract.Identifier(out var ident0, src));
			Assert.Equal("_ident", ident0);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Identifier(out var ident1, src));
			Assert.Equal("ident2", ident1);
			Assert.Equal(':', src.Peek);
			src.Next();

			Assert.True(Extract.Identifier(out var ident2, src));
			Assert.Equal("token3", ident2);
			Assert.Equal('\0', src.Peek);
		}
		[Test]
		public void ExtractIdentifiers()
		{
			var src = new StringSrc(" House House.Room  House.Room.Item  ");
			Assert.True(Extract.Identifiers(out var ids0, src));
			Assert.Equal(new[] { "House" }, ids0);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Identifiers(out var ids1, src));
			Assert.Equal(new[] { "House", "Room" }, ids1);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Identifiers(out var ids2, src));
			Assert.Equal(new[] { "House", "Room", "Item" }, ids2);
			Assert.Equal(' ', src.Peek);

			src = new StringSrc(" House..Item");
			Assert.False(Extract.Identifiers(out var ids3, src));

			src = new StringSrc("  House.Broken.");
			Assert.False(Extract.Identifiers(out var ids4, src));
		}
		[Test]
		public void ExtractString()
		{
			const string str = "\"string1\" \"str\\\"i\\ng2\"";

			var src1 = (Src)new StringSrc(str);
			Assert.True(Extract.String(out var str0, src1));
			Assert.Equal("string1", str0);
			Assert.Equal(' ', src1.Peek);

			var src2 = (Src)new StringSrc(str, ((StringSrc)src1).Position);

			Assert.True(Extract.String(out var str1, src1));
			Assert.Equal("str\\", str1);
			Assert.Equal('i', src1.Peek);

			Assert.True(Extract.String(out var str2, src2, escape:'\\'));
			Assert.Equal("str\"i\ng2", str2);
			Assert.Equal('\0', src2.Peek);
		}
		[Test]
		public void ExtractBool()
		{
			var src = new StringSrc("true false 1 0");
			Assert.True(Extract.Bool(out var bool0, src));
			Assert.Equal(true, bool0);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Bool(out var bool1, src));
			Assert.Equal(false, bool1);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Bool(out var bool2, src));
			Assert.Equal(true, bool2);
			Assert.Equal(' ', src.Peek);

			Assert.True(Extract.Bool(out var bool3, src));
			Assert.Equal(false, bool3);
			Assert.Equal('\0', src.Peek);

			src.Position = 0;
			Assert.True(Extract.BoolArray(out var bools, 4, src));
			Assert.Equal(new[] { true, false, true, false }, bools);
			Assert.Equal('\0', src.Peek);
		}
		[Test]
		public void ExtractInt()
		{
			{
				var src = "0";
				Assert.True(Extract.Int(out var i0,  0, new StringSrc(src))); Assert.Equal(0L, i0);
				Assert.True(Extract.Int(out var i1,  8, new StringSrc(src))); Assert.Equal(0L, i1);
				Assert.True(Extract.Int(out var i2, 10, new StringSrc(src))); Assert.Equal(0L, i2);
				Assert.True(Extract.Int(out var i3, 16, new StringSrc(src))); Assert.Equal(0L, i3);
			}
			{
				var src = new StringSrc("\n -1.14 ");
				Assert.True(Extract.Int(out var i0, 10, src));
				Assert.Equal(-1L, i0);
				Assert.Equal('.', src.Peek);
			}
			{
				var src = new StringSrc("0x1abcZ");
				Assert.True(Extract.Int(out var i0, 0, src));
				Assert.Equal(0x1abcL, i0);
				Assert.Equal('Z', src.Peek);
			}
			{
				var src = new StringSrc("0xdeadBeaf");
				Assert.True(Extract.Int(out var i0, 16, src));
				Assert.Equal(0xdeadBeafL, i0);
				Assert.Equal('\0', src.Peek);
			}
			{
				var src = new StringSrc("-1 0xFA 2.3 ");
				Assert.True(Extract.IntArray(out var ints, 3, 0, src));
				Assert.Equal(new[] { -1L, 0xFAL, 2L }, ints);
				Assert.Equal('.', src.Peek);
			}
		}
		[Test]
		public void ExtractReal()
		{
			{
				var src = new StringSrc("\n 6.28 ");
				Assert.True(Extract.Real(out var r0, src));
				Assert.Equal(6.28, r0);
				Assert.Equal(' ', src.Peek);
			}
			{
				var src = new StringSrc("-1.25e-4Z");
				Assert.True(Extract.Real(out var r0, src));
				Assert.Equal(-1.25e-4, r0);
				Assert.Equal('Z', src.Peek);
			}
			{
				var src = new StringSrc("\n 6.28\t6.28e0\n-6.28 ");
				Assert.True(Extract.RealArray(out var reals, 3, src));
				Assert.Equal(new[] { 6.28, 6.28, -6.28 }, reals);
			}
		}
		[Test]
		public void ExtractNumber()
		{
			Assert.True(Extract.Number<int    >(out var num0, new StringSrc("0"       ))); Assert.Equal(0, num0);
			Assert.True(Extract.Number<decimal>(out var num1, new StringSrc("+0"      ))); Assert.Equal(0m, num1);
			Assert.True(Extract.Number<short  >(out var num2, new StringSrc("-0"      ))); Assert.Equal((short)0, num2);
			Assert.True(Extract.Number<float  >(out var num3, new StringSrc("+.0f"    ))); Assert.Equal(+0f, num3);
			Assert.True(Extract.Number<double >(out var num4, new StringSrc("-.1f"    ))); Assert.Equal(-0.1, num4);
			Assert.True(Extract.Number<float  >(out var num5, new StringSrc("1F"      ))); Assert.Equal(1f, num5);
			Assert.True(Extract.Number<double >(out var num6, new StringSrc("3E-05F"  ))); Assert.Equal(3E-05, num6);
			Assert.True(Extract.Number<byte   >(out var num7, new StringSrc("12three" ))); Assert.Equal((byte)12, num7);
			Assert.True(Extract.Number<uint   >(out var num8, new StringSrc("0x123"   ))); Assert.Equal(0x123U, num8);
			Assert.True(Extract.Number<ulong  >(out var num9, new StringSrc("0x123ULL"))); Assert.Equal(0x123UL, num9);
			Assert.True(Extract.Number<long   >(out var numA, new StringSrc("0x123L"  ))); Assert.Equal(0x123L, numA);
			Assert.True(Extract.Number<long   >(out var numB, new StringSrc("0x123LL" ))); Assert.Equal(0x123L, numB);

			Assert.True(Extract.Number<byte  >(out var numC, new StringSrc("0b101010"))); Assert.Equal((byte)0b101010, numC);
			Assert.True(Extract.Number<double>(out var numD, new StringSrc("0923.0"  ))); Assert.Equal(923.0, numD);
			Assert.True(Extract.Number<int   >(out var numE, new StringSrc("0199"    ))); Assert.Equal(1, numE); // because it's octal
			Assert.True(Extract.Number<int   >(out var numF, new StringSrc("0199"), 10)); Assert.Equal(199, numF);

			Assert.False(Extract.Number<int>(out var numG, new StringSrc("0x.0")));
			Assert.False(Extract.Number<int>(out var numH, new StringSrc(".x0" )));
			Assert.False(Extract.Number<int>(out var numI, new StringSrc("-x.0")));
		}
		[Test]
		public void ExtractEnum()
		{
			{
				Assert.True(Extract.EnumName<Extract.ENumType>(out var e0, new StringSrc("Int"))); Assert.Equal(Extract.ENumType.Int, e0);
				Assert.True(Extract.EnumName<Extract.ENumType>(out var e1, new StringSrc("FP"))); Assert.Equal(Extract.ENumType.FP, e1);
				Assert.True(Extract.EnumName<Extract.ENumType>(out var e2, new StringSrc("Any"))); Assert.Equal(Extract.ENumType.Any, e2);
			}
			{
				Assert.True(Extract.EnumValue<Extract.ENumType>(out var e0, new StringSrc("1"))); Assert.Equal(Extract.ENumType.Int, e0);
				Assert.True(Extract.EnumValue<Extract.ENumType>(out var e1, new StringSrc("2"))); Assert.Equal(Extract.ENumType.FP, e1);
				Assert.True(Extract.EnumValue<Extract.ENumType>(out var e2, new StringSrc("3"))); Assert.Equal(Extract.ENumType.Any, e2);
			}
		}
		[Test]
		public void ExtractData()
		{
			var src = new StringSrc("0x12, 0x23, 0x34, 0x45, 0x56, 0x67");
			var data = new byte[6];
			Assert.True(Extract.Data(data, src, delim:" ,"));
			Assert.Equal(new byte[] { 0x12, 0x23, 0x34, 0x45, 0x56, 0x67 }, data);
		}
	}
}
#endif
