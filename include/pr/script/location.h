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
		// The location within a stream of characters.
		// By default, Loc is an empty object that passes characters straight through
		// Derived versions of 'Loc' record file, line, and column position
		struct Location
		{
		private:

			// The name of the stream source
			string m_stream_name;

			// The character offset into the stream
			size_t m_pos;

			// Line number in the character stream
			size_t m_line;

			// Column number in the character stream
			size_t m_col;

			// The number of columns that a tab character corresponds to
			int m_tab_size;

			// True if the line and column values are valid
			bool m_lc_valid;

			// By default, 1 tab = 1 column. This allows for tabs = multiple columns tho
			static int const DefTabSize = 1;

		public:

			Location()
				:Location(string())
			{}
			explicit Location(string stream_name)
				:Location(stream_name, 0, 0, 0, true)
			{}
			explicit Location(size_t pos)
				:Location(nullptr, pos, 0, 0, pos == 0)
			{}
			Location(string stream_name, size_t pos)
				:Location(stream_name, pos, 0, 0, pos == 0)
			{}
			Location(size_t pos, size_t line, size_t col)
				:Location(nullptr, pos, line, col, true)
			{}
			Location(string stream_name, size_t pos, size_t line, size_t col, bool lc_valid, int tab_size = DefTabSize)
				:m_stream_name(stream_name)
				,m_pos(pos)
				,m_line(line)
				,m_col(col)
				,m_tab_size(tab_size)
				,m_lc_valid(lc_valid)
			{}
			
			// Advance the location by interpreting 'ch'
			wchar_t inc(wchar_t ch)
			{
				if (ch != 0)
				{
					++m_pos;
				}
				if (m_lc_valid)
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
				}
				return ch;
			}
			char inc(char ch)
			{
				return char(inc(wchar_t(ch)));
			}

			// Output the stream name (usually file name)
			string StreamName() const
			{
				return m_stream_name;
			}

			// Output the character index
			size_t Pos() const
			{
				return m_pos;
			}
			void Pos(size_t pos, bool lc_valid)
			{
				Pos(pos, m_line, m_col, lc_valid);
			}
			void Pos(size_t pos, size_t line, size_t col, bool lc_valid)
			{
				m_pos      = pos;
				m_line     = line;
				m_col      = col;
				m_lc_valid = lc_valid;
			}

			// Output the line number
			size_t Line() const
			{
				return m_line + 1;
			}

			// Output the column number
			size_t Col() const
			{
				return m_col + 1;
			}

			// True if the line/column values are valid
			bool LCValid() const
			{
				return m_lc_valid;
			}

			// Output the location as a string
			string ToString() const
			{
				auto s = StreamName();
				if (LCValid()) s.append(Fmt(L"(%d:%d)", Line(), Col()));
				s.append(Fmt(L" (offset:%d)", Pos()));
				return s;
			}
		};

		// The location within a stream of characters
		inline bool operator == (Location const& lhs, Location const& rhs)
		{
			return lhs.Pos() == rhs.Pos() && lhs.StreamName() == rhs.StreamName();
		}
		inline bool operator != (Location const& lhs, Location const& rhs)
		{
			return !(lhs == rhs);
		}
		inline bool operator <  (Location const& lhs, Location const& rhs)
		{
			if (lhs.StreamName() != rhs.StreamName()) return lhs.StreamName() < rhs.StreamName();
			return lhs.Pos() <= rhs.Pos();
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

			Location loc(L"", 0, 0, 0, true, 4);
			for (auto s = str; *s; ++s)
				loc.inc(*s);

			PR_CHECK(loc.Line(), 3U);
			PR_CHECK(loc.Col(), 6U);
		}
	}
}
#endif