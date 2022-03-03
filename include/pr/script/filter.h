//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/buf.h"
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

		InComment m_com;

		// Return the next byte or decoded character from the underlying stream, or EOS for the end of the stream.
		int Read() override
		{
			for (; *m_src; ++m_src)
			{
				// Eat comments
				if (m_com.WithinComment(m_src))
					continue;

				break;
			}

			auto ch = *m_src;
			if (ch != '\0') ++m_src;
			return ch;
		}

	public:

		explicit StripComments(Src& src, InLiteral::EFlags literal_flags, InComment::Patterns const& comment_patterns)
			:Src(src, EEncoding::already_decoded)
			,m_com(comment_patterns, literal_flags)
		{}
		explicit StripComments(Src& src, InComment::Patterns const& comment_patterns)
			:StripComments(src, InLiteral::EFlags::Escaped | InLiteral::EFlags::SingleLineStrings, comment_patterns)
		{}
		explicit StripComments(Src& src)
			:StripComments(src, InComment::Patterns())
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
		//  - Does not handle comments, if you want comments handled, wrap the src in a 'StripComments' filter.

	private:

		InLiteral m_lit;
		int m_lines_max;
		int m_lines_min;
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
				if (m_lit.WithinLiteral(*m_src))
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

		explicit StripNewLines(Src& src, int lines_min = 0, int lines_max = 1, InLiteral::EFlags literal_flags = InLiteral::EFlags::None)
			:Src(src, EEncoding::already_decoded)
			,m_lit(literal_flags)
			,m_lines_max()
			,m_lines_min()
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

