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

#include <cmath>
#include <type_traits>
#include "pr/str/string_core.h"

namespace pr::str
{
	// A helper class for recognising literal strings in a stream of characters.
	template <typename Char>
	struct InLiteralString
	{
		// Notes:
		//  - Literal strings are closed automatically by newline characters. Higher level
		//    logic handles the unmatched quote character. This is needed for parsing inactive
		//    code blocks in the preprocessor, which ignores unclosed literal strings/characters.
		
		using char_t = Char;

		bool m_single_line_strings;
		char m_escape_character;
		char m_quote_character;
		bool m_in_literal_string;
		bool m_escape;

		explicit InLiteralString(bool single_line_strings = true, char escape_character = '\\') noexcept
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
				InLiteralString<char> lit;
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
				InLiteralString<char> lit;
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
				InLiteralString<char> lit;
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
				InLiteralString<char> lit;
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
	}
}
#endif
