//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"

namespace pr
{
	namespace script2
	{
		// The location within a stream of characters
		// By default, Loc is an empty object that passes characters straight through
		// Derived versions of 'Loc' record file, line, and column position
		struct Loc
		{
			virtual ~Loc()
			{}

			// Advance the location by interpreting 'ch'
			virtual wchar_t inc(wchar_t ch)
			{
				return ch;
			}
			char inc(char ch)
			{
				return char(inc(wchar_t(ch)));
			}

			// Output the location as a string
			virtual std::string str() const
			{
				return "[no location available]";
			}
		};

		// The location within a stream of characters
		struct TextLoc :Loc
		{
			size_t m_line;
			size_t m_col;
			int m_tab_size;

			TextLoc()
				:m_line()
				,m_col()
				,m_tab_size(4)
			{}
			TextLoc(size_t line, size_t col, int tab_size = 4)
				:m_line(line)
				,m_col(col)
				,m_tab_size(tab_size)
			{}

			// Advance the location by interpreting 'ch'
			wchar_t inc(wchar_t ch) override
			{
				if (ch == L'\n')
				{
					++m_line;
					m_col = 0;
				}
				else if (ch == L'\t')
				{
					m_col += m_tab_size;
				}
				else if (ch != 0)
				{
					++m_col;
				}
				return ch;
			}

			// Output the location as a string
			std::string str() const override
			{
				return pr::Fmt("%d:%d", m_line+1, m_col+1);
			}
		};
		inline bool operator == (TextLoc const& lhs, TextLoc const& rhs)
		{
			return lhs.m_line == rhs.m_line && lhs.m_col == rhs.m_col;
		}
		inline bool operator != (TextLoc const& lhs, TextLoc const& rhs)
		{
			return !(lhs == rhs);
		}
		inline bool operator <  (TextLoc const& lhs, TextLoc const& rhs)
		{
			if (lhs.m_line != rhs.m_line) return lhs.m_line < rhs.m_line;
			return lhs.m_col <= rhs.m_col;
		}

		// A file location
		struct FileLoc :TextLoc
		{
			using string = pr::string<wchar_t, _MAX_PATH>;
			string m_file;

			FileLoc()
				:TextLoc()
				,m_file()
			{}
			FileLoc(string file, size_t line, size_t col, int tab_size = 4)
				:TextLoc(line, col, tab_size)
				,m_file(file)
			{}

			// Output the location as a string
			std::string str() const override
			{
				return pr::Narrow(m_file).append("(").append(TextLoc::str()).append(")");
			}
		};
		inline bool operator == (FileLoc const& lhs, FileLoc const& rhs)
		{
			return lhs.m_file == rhs.m_file && static_cast<TextLoc const&>(lhs) == static_cast<TextLoc const&>(rhs);
				
		}
		inline bool operator != (FileLoc const& lhs, FileLoc const& rhs)
		{
			return !(lhs == rhs);
		}
		inline bool operator <  (FileLoc const& lhs, FileLoc const& rhs)
		{
			if (lhs.m_file != rhs.m_file) return lhs.m_file < rhs.m_file;
			return static_cast<TextLoc const&>(lhs) < static_cast<TextLoc const&>(rhs);
		}
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_location)
		{
			using namespace pr::script2;

			char const* str =
				"123\n"
				"abc\n"
				"\tx";

			TextLoc loc;
			for (auto s = str; *s; ++s)
				loc.inc(*s);

			PR_CHECK(loc.m_line, 2);
			PR_CHECK(loc.m_col, 5);
		}
	}
}
#endif