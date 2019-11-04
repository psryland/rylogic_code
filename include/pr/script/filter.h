//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/buf8.h"
#include "pr/script/fail_policy.h"
#include "pr/script/script_core.h"

namespace pr::script
{
	// Removes line continuation sequences in a character stream
	struct StripLineContinuations :Src
	{
	private:

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			for (;;)
			{
				if (m_src.Match(L"\\\n")) { m_src.Next(2); continue; }
				if (m_src.Match(L"\\\r\n")) { m_src.Next(3); continue; }
				break;
			}

			auto ch = *m_src;
			if (ch != '\0') ++m_src;
			return ch;
		}

	public:
		explicit StripLineContinuations(Src& src)
			:Src(src, EEncoding::already_decoded)
		{}
	};

	// Removes comments from a character stream
	struct StripComments :Src
	{
	private:

		InLiteral m_lit;
		string_t m_line_comment;
		string_t m_line_end;
		string_t m_block_beg;
		string_t m_block_end;

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			for (;;)
			{
				// Read through literal strings or characters
				if (m_lit.WithinLiteralString(*m_src))
					break;

				// Skip comments
				if (!m_line_comment.empty() && *m_src == m_line_comment[0] && m_src.Match(m_line_comment))
				{
					EatLineComment(m_src, m_line_comment);
					continue;
				}
				if (!m_block_beg.empty() && !m_block_end.empty() && *m_src == m_block_beg[0] && m_src.Match(m_block_beg))
				{
					EatBlockComment(m_src, m_block_beg, m_block_end);
					continue;
				}

				break;
			}

			auto ch = *m_src;
			if (ch != '\0') ++m_src;
			return ch;
		}

	public:

		explicit StripComments(Src& src, char_t const* line_comment = L"//", char_t const* line_end = L"\n", char_t const* block_beg = L"/*", char_t const* block_end = L"*/")
			:Src(src, EEncoding::already_decoded)
			,m_lit()
			,m_line_comment(line_comment)
			,m_line_end(line_end)
			,m_block_beg(block_beg)
			,m_block_end(block_end)
		{}
	};

	// Removes newlines from a character stream
	struct StripNewLines :Src
	{
		// Notes:
		//  - Transforms consecutive blank lines.
		//  - Look for consecutive lines that contain only whitespace characters.
		//  - If the number of lines is less than 'm_lines_min' add lines up to 'm_lines_min'
		//  - If the number of lines is greater than 'm_lines_max' delete lines back to 'm_lines_max'
		//  - Blank lines are replaced with a single new line character

	private:

		int m_lines_max;
		int m_lines_min;
		InLiteral m_lit;
		InComment m_com;
		int m_emit;
		bool m_line_start;

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			auto consecutive_lines = 0;
			auto& buffer = m_src.Buffer();
			for (auto len = 0;;)
			{
				// Don't retest until inserted new lines have been consumed.
				if (m_emit != 0)
					break;

				// Read through literal strings or characters
				if (m_lit.WithinLiteralString(*m_src))
					break;

				// Read through comments
				if (m_com.WithinComment(m_src))
					break;

				// Don't trim white space from the end of lines
				if (!m_line_start && *m_src != '\n')
					break;

				// Buffer up to the next non whitespace or line end character
				BufferWhile(m_src, [](Src& s, int i) { return str::IsLineSpace(s[i]); }, 0, &len);
				if (m_src[len] != '\n')
					break;

				// Consume the blank line and the new line character
				++consecutive_lines;
				m_src += len + 1;
				m_line_start = true;
			}

			// Insert new lines into the to buffer to match the limits
			if (consecutive_lines != 0)
			{
				consecutive_lines = std::max(m_lines_min, std::min(m_lines_max, consecutive_lines));
				buffer.insert(std::begin(buffer), consecutive_lines, '\n');
				m_emit = consecutive_lines;
			}

			auto ch = *m_src;
			if (ch != '\0') ++m_src;
			m_line_start = ch == '\n';
			m_emit -= m_emit != 0;
			return ch;
		}

	public:

		explicit StripNewLines(Src& src, int lines_min = 0, int lines_max = 1, bool support_c_strings = false)
			:Src(src, EEncoding::already_decoded)
			,m_lines_max()
			,m_lines_min()
			,m_lit(support_c_strings, support_c_strings ? '\\' : '\0')
			,m_com()
			,m_emit()
			,m_line_start(true)
		{
			SetLimits(lines_min, lines_max);
		}

		// Set the min/max line count
		void SetLimits(int lines_min = 0, int lines_max = 1)
		{
			m_lines_max = lines_max;
			m_lines_min = std::min(lines_min, lines_max);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr::script
{
	PRUnitTest(FilterTests)
	{
		using namespace pr::str;
		{// StripLineContinuations
			char const str_in[] = "Li\
				on";
			char const str_out[] = "Li				on";

			StringSrc src(str_in);
			StripLineContinuations strip(src);

			auto out = &str_out[0];
			for (; *strip; ++strip, ++out)
			{
				if (*strip == *out) continue;
				PR_CHECK(*strip, *out);
			}
			PR_CHECK(*out, 0);
		}
		{// StripComments
			char const str_in[] = 
				"123// comment         \n"
				"456/* blo/ck */789\n"
				"// many               \n"
				"// lines              \n"
				"// \"string\"         \n"
				"/* \"string\" */      \n"
				"\"string \\\" /*a*/ //b\"  \n"
				"/not a comment\n"
				"/*\n"
				"  more lines\n"
				"*/\n"
				"// multi\\\n"
				" line\\\n"
				" comment\n"
				"/*/ comment */\n";
			char const str_out[] = 
				"123\n"
				"456789\n"
				"\n"
				"\n"
				"\n"
				"      \n"
				"\"string \\\" /*a*/ //b\"  \n"
				"/not a comment\n"
				"\n"
				"\n"
				"\n";

			StringSrc src0(str_in);
			StripLineContinuations src1(src0);
			StripComments strip(src1);

			auto out = &str_out[0];
			for (;*strip; ++strip, ++out)
			{
				if (*strip == *out) continue;
				PR_CHECK(*strip, *out);
			}
			PR_CHECK(*out, 0);
		}
		{// StripNewLines
			char const str_in[] =
				"  \n"
				"      \n"
				"   \n"
				"  \" multi-line \n"
				"\n"
				"\n"
				"string \"     \n"
				"         \n"
				"     \n"
				"abc  \n"
				"\n"
				"\n"
				"";

			{// min 0, max 0 lines
				char const str_out[] =
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     abc  ";

				StringSrc src0(str_in);
				StripNewLines strip(src0, 0, 0);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
			{// min 0, max 1 lines
				char const str_out[] =
					"\n"
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"abc  \n"
					"";

				StringSrc src0(str_in);
				StripNewLines strip(src0, 0, 1);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
			{// min 2, max 2 lines
				char const str_out[] =
					"\n"
					"\n"
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"\n"
					"abc  \n"
					"\n"
					"";

				StringSrc src0(str_in);
				StripNewLines strip(src0,2,2);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
		}
	}
}
#endif
