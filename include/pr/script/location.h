//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"

namespace pr
{
	namespace script
	{
		// The location within a stream of characters
		// By default, Loc is an empty object that passes characters straight through
		// Derived versions of 'Loc' record file, line, and column position
		struct Location
		{
			virtual ~Location()
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

			// Output the stream name (usually file name)
			virtual string StreamName() const { return L"[source in memory]"; }

			// Output the line number
			virtual size_t Line() const { return 0; }

			// Output the column number
			virtual size_t Col() const { return 0; }

			// Output the location as a string
			string ToString() const { return StreamName().append(Fmt(L"(%d:%d)", Line(), Col())); }
		};

		// The location within a stream of characters
		struct TextLoc :Location
		{
			size_t m_line;
			size_t m_col;
			int m_tab_size;

			TextLoc()
				:m_line()
				,m_col()
				,m_tab_size(4)
			{}
			TextLoc(Location const& loc, int tab_size = 4)
				:m_line(loc.Line())
				,m_col(loc.Col())
				,m_tab_size(tab_size)
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

			// Output the line number
			size_t Line() const override { return m_line + 1; }

			// Output the column number
			size_t Col() const override { return m_col + 1; }
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
			string m_file;

			FileLoc()
				:TextLoc()
				,m_file()
			{}
			FileLoc(Location const& loc, int tab_size = 4)
				:TextLoc(loc, tab_size)
				,m_file(loc.StreamName())
			{}
			FileLoc(TextLoc const& loc)
				:TextLoc(loc)
				,m_file(loc.StreamName())
			{}
			FileLoc(string file, size_t line, size_t col, int tab_size = 4)
				:TextLoc(line, col, tab_size)
				,m_file(file)
			{}

			// Output the stream name (usually file name)
			string StreamName() const override { return m_file; }
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
			using namespace pr::script;

			char const* str =
				"123\n"
				"abc\n"
				"\tx";

			TextLoc loc;
			for (auto s = str; *s; ++s)
				loc.inc(*s);

			PR_CHECK(loc.m_line, 2U);
			PR_CHECK(loc.m_col, 5U);
		}
	}
}
#endif