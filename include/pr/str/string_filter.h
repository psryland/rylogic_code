//**********************************
// String Util
//  Copyright (c) Rylogic Ltd 2015
//**********************************
// Utility string functions that operate on:
//   std::string, std::wstring, pr::string<>, etc
//   char[], wchar_t[], etc
//   char*, wchar_t*, etc
// Note: char-array strings are not handled as special cases because there is no
// guarantee that the entire buffer is filled by the string, the null terminator
// may be midway through the buffer

#pragma once

#include <array>
#include <type_traits>
#include <cmath>
#include "pr/str/string_core.h"

namespace pr::str
{
	// A helper class for recognising literal strings in a stream of characters.
	template <typename Char>
	struct InLiteral
	{
		// Notes:
		//  - Literal strings are closed automatically by newline characters. Higher level
		//    logic handles the unmatched quote character. This is needed for parsing inactive
		//    code blocks in the preprocessor, which ignores unclosed literal strings/characters.
		
		using char_t = Char;

		bool m_single_line_strings;
		char_t m_escape_character;
		char_t m_quote_character;
		bool m_in_literal_string;
		bool m_escape;

		explicit InLiteral(bool single_line_strings = true, char_t escape_character = '\\') noexcept
			:m_single_line_strings(single_line_strings)
			,m_escape_character(escape_character)
			,m_quote_character()
			,m_in_literal_string(false)
			,m_escape(false)
		{}

		// Processes the current character in 'src'.
		// Returns true if currently within a string/character literal
		bool WithinLiteralString(char_t ch) noexcept
		{
			if (m_in_literal_string)
			{
				if (m_escape)
				{
					// If escaped, then still within the literal
					m_escape = false;
					return true;
				}
				else if (ch == m_quote_character)
				{
					m_in_literal_string = false;
					return true; // terminating quote is part of the literal
				}
				else if (m_single_line_strings && ch == '\n')
				{
					m_in_literal_string = false;
					return false; // terminating '\n' is not part of the literal
				}
				else
				{
					m_escape = ch == m_escape_character;
					return true;
				}
			}
			else if (ch == '\"' || ch == '\'')
			{
				m_quote_character = static_cast<char>(ch);
				m_in_literal_string = true;
				m_escape = false;
				return true;
			}
			else
			{
				return false;
			}
		}
	};

	// A helper class for recognising line or block comments in a stream of characters.
	struct InComment
	{
		enum class EType { None = 0, Line = 1, Block = 2 };

		EType m_comment;
		std::wstring m_line_comment;
		std::wstring m_line_end;
		std::wstring m_block_beg;
		std::wstring m_block_end;
		wchar_t m_line_continuation_character;
		bool m_escape;
		int m_emit;

		explicit InComment(wchar_t const* line_comment = L"//", wchar_t const* line_end = L"\n", wchar_t const* block_beg = L"/*", wchar_t const* block_end = L"*/", wchar_t line_continuation_character = '\\')
			:m_comment(EType::None)
			,m_line_comment(line_comment)
			,m_line_end(line_end)
			,m_block_beg(block_beg)
			,m_block_end(block_end)
			,m_line_continuation_character(line_continuation_character)
			,m_escape()
			,m_emit()
		{}

		// Processes the current character in 'src'.
		// Returns true if currently within a string/character literal
		template <typename TSrc> bool WithinComment(TSrc& src)
		{
			// This function requires 'src' because we need to look ahead
			// to say whether we're in a comment.

			switch (m_comment)
			{
			case EType::None:
				{
					if (m_emit == 0 && Match(src, m_line_comment))
					{
						m_comment = EType::Line;
						m_emit = static_cast<int>(m_line_comment.size());
						m_escape = false;
					}
					else if (m_emit == 0 && Match(src, m_block_beg))
					{
						m_comment = EType::Block;
						m_emit = static_cast<int>(m_block_beg.size());
					}
					break;
				}
			case EType::Line:
				{
					if (*src == '\0')
					{
						m_comment = EType::None;
						m_emit = 0;
					}
					else if (m_emit == 0 && !m_escape && Match(src, m_line_end))
					{
						m_comment = EType::None;
						m_emit = 0; // line comments don't include the line end
					}
					m_escape = *src == m_line_continuation_character;
					break;
				}
			case EType::Block:
				{
					if (*src == '\0')
					{
						m_comment = EType::Block;
						m_emit = 0;
					}
					else if (m_emit == 0 && Match(src, m_block_end))
					{
						m_comment = EType::None;
						m_emit = static_cast<int>(m_block_end.size());
					}
					break;
				}
			default:
				{
					throw std::runtime_error("Unknown comment state");
				}
			}

			auto in_comment = m_comment != EType::None || m_emit != 0;
			m_emit -= static_cast<int>(m_emit != 0);
			return in_comment;
		}

	private:

		// True if 'src' starts with 'pattern'
		template <typename TSrc> bool Match(TSrc& src, std::wstring_view pattern)
		{
			int i = 0, iend = static_cast<int>(pattern.size());
			for (; i != iend && src[i] == pattern[i]; ++i) {}
			return i == iend;
		}
	};

	// A helper class for escaping a string to a C-string
	template <typename Char>
	struct Escape
	{
		enum class EEscSeq { None, Hex2, Octal3, Unicode4, Unicode8 };
		static int const HighBit = 1 << (8*sizeof(Char) -  1);
		static int const MaxLiteralLength = 8;
		using char_t = Char;

		char_t m_escape_character;
		char_t m_buf[MaxLiteralLength + 1];
		int m_buf_count;

		explicit Escape(char_t escape_character = '\\')
			:m_escape_character(escape_character)
			,m_buf()
			,m_buf_count()
		{}

		// Append characters to 'out' after translating escape sequences
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		void Translate(char_t ch, Str& out, size_t& len)
		{
			switch (ch)
			{
			case '\a': Append(out, "\\a", len); break;
			case '\b': Append(out, "\\b", len); break;
			case '\f': Append(out, "\\f", len); break;
			case '\n': Append(out, "\\n", len); break;
			case '\r': Append(out, "\\r", len); break;
			case '\t': Append(out, "\\t", len); break;
			case '\v': Append(out, "\\v", len); break;
			case '\?': Append(out, "\\?", len); break;
			case '\'': Append(out, "\\\'", len); break;
			case '\"': Append(out, "\\\"", len); break;
			case '\\': Append(out, "\\\\", len); break;
			default:
				{
					// If not an encoded character
					if ((HighBit & ch) == 0)
					{
						Append(out, ch, len);
					}
					else
					{
						// Buffer encoded characters
						m_buf[m_buf_count++] = ch;

						// Convert from 'char_t' to 'char32_t'
						struct convert_t :std::codecvt<char32_t, char_t, std::mbstate_t> {} converter;
						
						auto const* in = &m_buf[0];
						auto const* in_end = in + m_buf_count;
						auto in_next = in;

						auto c32 = char32_t{};
						auto c32_next = &c32;

						std::mbstate_t mb = {};
						auto r = converter.in(mb, in, in_end, in_next, &c32, &c32 + 1, c32_next);
						if (r == std::codecvt_base::ok)
						{
							char_t code[9] = {};
							if (c32 <= 0xFFFF)
							{
								char_traits<char_t>::uitostr(c32, &code[0], _countof(code), 16);
								Append(out, "\\u", len);
							}
							else
							{
								char_traits<char_t>::uitostr(c32, &code[0], _countof(code), 16);
								Append(out, "\\U", len);
							}
							for (size_t i = 0, iend = char_traits<char_t>::length(code); i != iend; ++i)
							{
								Append(out, '0', len);
							}
							Append(out, code, len);
							break;
						}
						if (r == std::codecvt_base::partial)
						{
							// wait for more characters
							break;
						}
						throw std::runtime_error("Unicode encoding error");
					}
				}
			}
		}
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		void Translate(Char ch, Str& out)
		{
			auto len = Size(out);
			Translate(ch, out, len);
		}
	};

	// A helper class for translating a C-string to unescaped characters
	template <typename Char>
	struct Unescape
	{
		enum class EEscSeq { None, Hex2, Octal3, Unicode4, Unicode8 };
		static int const MaxLiteralLength = 8;
		using char_t = Char;

		char_t m_escape_character;
		char_t m_buf[MaxLiteralLength + 1];
		int m_buf_count;
		EEscSeq m_seq;
		bool m_escape;

		explicit Unescape(char_t escape_character = '\\')
			:m_escape_character(escape_character)
			,m_buf()
			,m_buf_count()
			,m_seq(EEscSeq::None)
			,m_escape(false)
		{}

		// True while in an escape sequence. (Used externally when looping over characters between quotes)
		bool WithinEscapeSequence() const
		{
			return m_escape;
		}

		// Append characters to 'out' after translating escape sequences
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		void Translate(char_t ch, Str& out, size_t& len)
		{
			for (; m_escape; )
			{
				// Unicode character in hex, e.g. '\x1234'
				if (ch == 'x' || ch == 'u' || ch == 'U' || m_seq == EEscSeq::Hex2 || m_seq == EEscSeq::Unicode4 || m_seq == EEscSeq::Unicode8)
				{
					// This is the start of a hex unicode character sequence
					if (m_seq == EEscSeq::None)
					{
						assert(m_buf_count == 0 && "Buffer should have been reset to 0 at the end of the last code");
						m_seq =
							ch == 'x' ? EEscSeq::Hex2 :
							ch == 'u' ? EEscSeq::Unicode4 :
							ch == 'U' ? EEscSeq::Unicode8 :
							throw std::runtime_error("Unknown unicode sequence identifier");
						return;
					}

					// Buffer the hex digit
					if (IsHexDigit(ch))
						m_buf[m_buf_count++] = ch;
					else
						throw std::runtime_error("Invalid hex digit in character code");

					// End of the sequence?
					if ((m_seq == EEscSeq::Hex2     && m_buf_count == 2) ||
						(m_seq == EEscSeq::Unicode4 && m_buf_count == 4) ||
						(m_seq == EEscSeq::Unicode8 && m_buf_count == 8))
					{
						// Terminate the string in 'm_buf'
						m_buf[m_buf_count] = 0;

						// Convert the character code to an integer
						char32_t const code[2] = { static_cast<char32_t>(char_traits<char_t>::strtoul(&m_buf[0], nullptr, 16)), 0 };

						// Convert from char32_t to to utf-8/utf-16 based on 'char_t'
						struct convert_t :std::codecvt<char32_t, char_t, std::mbstate_t> {} converter;
						auto converted = ConvertEncoding<std::array<char_t,16>>(&code[0], converter);
						Append(out, converted.data(), len);
						
						// Leave the 'escaped' state
						m_escape = false;
						m_seq = EEscSeq::None;
						m_buf_count = 0;
					}
					return;
				}

				// Unicode character in octal, e.g. '\012'
				if (ch == '0' || ch == '1' || ch == '2' || ch == '3' || m_seq == EEscSeq::Octal3)
				{
					// This is the start of the octal unicode character
					if (m_seq == EEscSeq::None)
					{
						// The first octal digit is part of the code
						assert(m_buf_count == 0 && "Buffer should have been reset to 0 at the end of the last code");
						m_buf[m_buf_count++] = ch;
						m_seq = EEscSeq::Octal3;
						return;
					}

					// Buffer the octal digit
					if (IsOctDigit(ch))
						m_buf[m_buf_count++] = ch;
					else
						throw std::runtime_error("Invalid octal digit in character code");

					// End of the sequence?
					if (m_seq == EEscSeq::Octal3 && m_buf_count == 3)
					{
						// Terminate the string in 'm_buf'
						m_buf[m_buf_count] = 0;

						// Convert the character code to an integer value
						char32_t const code[2] = { static_cast<char32_t>(char_traits<char_t>::strtoul(&m_buf[0], nullptr, 8)), 0 };

						// Convert from char32_t to to utf-8/utf-16 based on 'char_t'
						struct convert_t :std::codecvt<char32_t, char_t, std::mbstate_t> {} converter;
						auto converted = ConvertEncoding<std::array<char_t,16>>(&code[0], converter);
						Append(out, converted.data(), len);

						// Leave the 'escaped' state
						m_escape = false;
						m_seq = EEscSeq::None;
						m_buf_count = 0;
					}
					return;
				}

				// Single character escape sequence
				m_escape = false;
				switch (ch)
				{
				case 'a':  Append(out, '\a', len); return;
				case 'b':  Append(out, '\b', len); return;
				case 'f':  Append(out, '\f', len); return;
				case 'n':  Append(out, '\n', len); return;
				case 'r':  Append(out, '\r', len); return;
				case 't':  Append(out, '\t', len); return;
				case 'v':  Append(out, '\v', len); return;
				case '?':  Append(out, '\?', len); return;
				case '\'': Append(out, '\'', len); return;
				case '\"': Append(out, '\"', len); return;
				case '\\': Append(out, '\\', len); return;
				default: throw std::runtime_error("Unknown escape sequence");
				}
			}
			
			// 'ch' is not part of an escape sequence
			if (ch == m_escape_character)
				m_escape = true;
			else
				Append(out, ch, len);
		}
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		void Translate(char_t ch, Str& out)
		{
			auto len = Size(out);
			Translate(ch, out, len);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::str
{
	PRUnitTest(StringFilterTests)
	{
		{// InLiteral
			{
				// Escaped quotes are ignored
				InLiteral<char> lit;
				char const* ptr = " \"\\\"\" ";
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr), false);
				PR_CHECK(*ptr, '\0');
			}
			{
				// Literals must match " to " and ' to '
				InLiteral<char> lit;
				char const* ptr = "\"'\" '\"' ";
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr), false);
				PR_CHECK(*ptr, '\0');
			}
			{
				// Literals *are* closed by '\n'
				InLiteral<char> lit;
				char const* ptr = "\" '\n ";
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr++), false);
				PR_CHECK(lit.WithinLiteralString(*ptr), false);
				PR_CHECK(*ptr, '\0');
			}
			{
				// Literals are not closed by EOS
				InLiteral<char> lit;
				char const* ptr = "\" ";
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr++), true);
				PR_CHECK(lit.WithinLiteralString(*ptr), true);
				PR_CHECK(*ptr, '\0');
			}
		}
		{// InComment
			{
				// Simple block comment
				InComment lit;
				char const* src = " /**/ ";
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(*src, '\0');
			}
			{
				// No substring matching within block comment markers
				InComment lit;
				char const* src = "/*/*/ /**/*/";
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(*src, '\0');
			}
			{
				// Line comment ends at unescaped new line (exclusive)
				InComment lit;
				char const* src = " // \\\n \n ";
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(*src, '\0');
			}
			{
				// Line comment ends at EOS
				InComment lit;
				char const* src = " // ";
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), true); ++src;
				PR_CHECK(lit.WithinComment(src), false); ++src;
				PR_CHECK(*src, '\0');
			}
		}
		{// Escape
			std::string str = u8"abc\123\u00b1\a\b\f\n\r\t\v\\\"\'\?";
			std::string res = "abcS\\u00b1\\a\\b\\f\\n\\r\\t\\v\\\\\\\"\\\'\\?";
			std::string out;

			size_t len = 0;
			Escape<char> esc;
			for (auto ch : str)
				esc.Translate(ch, out, len);

			PR_CHECK(out, res);
		}
		{// Unescape
			std::string str = "abc\\123\\u00b1\\a\\b\\f\\n\\r\\t\\v\\\\\\\"\\\'\\?";
			std::string res = u8"abc\123\u00b1\a\b\f\n\r\t\v\\\"\'\?";
			std::string out;

			size_t len = 0;
			Unescape<char> esc;
			for (auto ch : str)
				esc.Translate(ch, out, len);

			PR_CHECK(out, res);
		}
	}
}
#endif
