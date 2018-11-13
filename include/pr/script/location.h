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

			// The character offset into the stream (0-based)
			std::streamoff m_pos;

			// Line number in the character stream (natural number, i.e. 1-based)
			int m_line;

			// Column number in the character stream (natural number, i.e. 1-based)
			int m_col;

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
				:Location(stream_name, 0, 1, 1, true)
			{}
			explicit Location(std::streamoff pos)
				:Location(nullptr, pos, 1, 1, pos == 0)
			{}
			Location(string stream_name, std::streamoff pos)
				:Location(stream_name, pos, 1, 1, pos == 0)
			{}
			Location(std::streamoff pos, int line, int col)
				:Location(nullptr, pos, line, col, true)
			{}
			Location(string stream_name, std::streamoff pos, int line, int col)
				:Location(stream_name, pos, line, col, true)
			{}
			Location(string stream_name, std::streamoff pos, int line, int col, bool lc_valid, int tab_size = DefTabSize)
				:m_stream_name(stream_name)
				,m_pos(pos)
				,m_line(std::max(line, 1))
				,m_col(std::max(col, 1))
				,m_tab_size(tab_size)
				,m_lc_valid(lc_valid)
			{
				assert("Line index should be natural number, 1-based" && line >= 1);
				assert("Column index should be natural number, 1-based" && col >= 1);
			}

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
						m_col = 1;
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
			void StreamName(string stream_name)
			{
				m_stream_name = stream_name;
			}

			// Output the character index
			std::streamoff Pos() const
			{
				return m_pos;
			}
			void Pos(std::streamoff pos, bool lc_valid)
			{
				Pos(pos, m_line, m_col, lc_valid);
			}
			void Pos(std::streamoff pos, int line, int col, bool lc_valid)
			{
				assert("Line index should be natural number, 1-based" && line >= 1);
				assert("Column index should be natural number, 1-based" && col >= 1);
				m_pos      = pos;
				m_line     = line;
				m_col      = col;
				m_lc_valid = lc_valid;
			}

			// Get/Set the line number (as a natural number, 1-based)
			int Line() const
			{
				return m_line;
			}
			void Line(int line)
			{
				assert("Line index should be natural number, 1-based" && line >= 1);
				m_line = line;
			}

			// Get/Set the column number (as a natural number, 1-based)
			int Col() const
			{
				return m_col;
			}
			void Col(int col)
			{
				assert("Column index should be natural number, 1-based" && col >= 1);
				m_col = col;
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
namespace pr::script
{
	PRUnitTest(LocationTests)
	{
		char const* str =
			"123\n"
			"abc\n"
			"\tx";

		Location loc(L"", 0, 1, 1, true, 4);
		for (auto s = str; *s; ++s)
			loc.inc(*s);

		PR_CHECK(loc.Line(), 3);
		PR_CHECK(loc.Col(), 6);
	}
}
#endif