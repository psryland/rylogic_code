//**********************************
// Script character source
//  Copyright (c) Rylogic Ltd 2014
//**********************************
#pragma once
#ifndef PR_SCRIPT_NEWLINE_STRIP_H
#define PR_SCRIPT_NEWLINE_STRIP_H

#include "pr/script/script_core.h"
#include "pr/script/char_stream.h"

namespace pr
{
	namespace script
	{
		// A char stream that removes blank lines
		// 'lines_min' - minimum number of consequtive newlines
		// 'lines_max' - maximum number of consequtive newlines
		// 'lines_max' == 0 removes all newlines from the text (excluding literal strings)
		// 'lines_max' == 1 removes all blank lines from the text (excluding literal strings)
		// 'lines_min' > 0 inserts newlines
		struct NewLineStrip :Src
		{
			Buffer<> m_buf;
			size_t m_lines_max;
			size_t m_lines_min;

			NewLineStrip(Src& src, size_t lines_max = 1, size_t lines_min = 0)
				:Src()
				,m_buf(src)
				,m_lines_max(lines_max)
				,m_lines_min(lines_min < lines_max ? lines_min : lines_max)
			{}

			ESrcType type() const override { return m_buf.type(); }
			Loc  loc() const override      { return m_buf.loc(); }
			void loc(Loc& l) override      { m_buf.loc(l); }

		protected:
			char peek() const override { return *m_buf; }
			void next() override       { ++m_buf; }
			void seek() override 
			{
				for (;;)
				{
					if (!m_buf.empty()) break;

					// Read through literal strings
					if (*m_buf == '\"')
					{
						m_buf.BufferLiteralString();
						continue;
					}

					// Read through literal chars
					if (*m_buf == '\'')
					{
						m_buf.BufferLiteralChar();
						continue;
					}

					// Transform newlines
					if (*m_buf == '\n')
					{
						// Look for consecutive lines that contain only whitespace characters
						// If the number of lines is less than 'm_lines_min' add lines up to 'm_lines_min'
						// If the number of lines is greater than 'm_lines_max' delete lines back to 'm_lines_max'
						size_t line_count = 0;
						for (; *m_buf.m_src;)
						{
							if (*m_buf.m_src == '\n')                    { m_buf.clear(); ++line_count; ++m_buf.m_src; }
							else if (pr::str::IsLineSpace(*m_buf.m_src)) { m_buf.buffer(); }
							else break;
						}
						auto clamp_line_count = [=](size_t x) { return x < m_lines_min ? m_lines_min : x > m_lines_max ? m_lines_max : x; };
						for (line_count = clamp_line_count(line_count); line_count; --line_count)
						{
							m_buf.push_front('\n');
						}
						continue;
					}
					break;
				}
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_newline_strip)
		{
			using namespace pr;
			using namespace pr::script;

			char const* str_in =
				"  \n"
				"      \n"
				"   \n"
				"\" multi-line \n"
				"\n"
				"\n"
				"string \"     \n"
				"         \n"
				"     \n"
				"abc  \n"
				"\n"
				"\n"
				"";
			{
				char const* str_out =
					"  \n"
					"\" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"abc  \n"
					"";

				PtrSrc src(str_in);
				NewLineStrip strip(src);
				std::string out;
				for (auto s = str_out; *s; ++s, ++strip)
					out.push_back(*strip);

				PR_CHECK(out.c_str(), str_out);
				PR_CHECK(*strip, 0);
			}
			{
				char const* str_out =
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     abc  ";

				PtrSrc src(str_in);
				NewLineStrip strip(src,0);
				std::string out;
				for (auto s = str_out; *s; ++s, ++strip)
					out.push_back(*strip);

				PR_CHECK(out.c_str(), str_out);
				PR_CHECK(*strip, 0);
			}
			{
				char const* str_out =
					"  \n"
					"\n"
					"\" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"\n"
					"abc  \n"
					"\n"
					"";

				PtrSrc src(str_in);
				NewLineStrip strip(src,2,2);
				std::string out;
				for (auto s = str_out; *s; ++s, ++strip)
					out.push_back(*strip);

				PR_CHECK(out.c_str(), str_out);
				PR_CHECK(*strip, 0);
			}
		}
	}
}
#endif

#endif
