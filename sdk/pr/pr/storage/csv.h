//*****************************************************************************************
// CSV
//  (c)opyright Rylogic Limited 2011
//*****************************************************************************************
// Usage:
//  pr::csv::Csv csv;
//  if (!pr::csv::Load("my_csv.csv", csv)) { printf("Failed to load csv\n"); return; }
//  csv.erase(csv.begin());                                                                 // Erase column header row
//  std::sort(csv.begin(), csv.end(), pr::csv::SortColumn<>(0));                            // Sort by column zero
//  csv.erase(std::unique(csv.begin(), csv.end(), pr::csv::UniqueColumn<>(0)), csv.end());  // Make unique in column zero

#ifndef PR_STORAGE_CSV_H
#define PR_STORAGE_CSV_H

#include <cstdio>
#include <string>
#include <vector>
#include <iostream>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT_STR
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT_STR(grp, exp, str)
#endif

namespace pr
{
	namespace csv
	{
		typedef std::string      Str;
		typedef std::vector<Str> Row;
		typedef std::vector<Row> Csv;
		struct Loc
		{
			std::size_t row, col;
			Loc() :row(0) ,col(0) {}
			void inc(int ch) { if (ch == ',') { ++col; } else if (ch == '\n') { ++row; col = 0; } }
		};
		
		// Range checked lookup
		inline Str const& Item(Row const& row, std::size_t col)                  { static Str null_str; return (col < row.size()) ? row[col] : null_str; }
		inline Row const& Item(Csv const& csv, std::size_t row)                  { static Row null_row; return (row < csv.size()) ? csv[row] : null_row; }
		inline Str const& Item(Csv const& csv, std::size_t row, std::size_t col) { return Item(Item(csv, row), col); }
		
		// Range checked lookup that throws when out of range
		inline Str const& ItemT(Row const& row, std::size_t col)                  { if (col >= row.size()) throw std::exception("column index out of range"); return row[col]; }
		inline Row const& ItemT(Csv const& csv, std::size_t row)                  { if (row >= csv.size()) throw std::exception("row index out of range"   ); return csv[row]; }
		inline Str const& ItemT(Csv const& csv, std::size_t row, std::size_t col) { return ItemT(ItemT(csv, row), col); }
		
		// Set an element in the csv
		inline void SetItem(Csv& csv, std::size_t row, std::size_t col, Str const& str)
		{
			if (csv.size()      <= row) csv.resize(row+1);
			if (csv[row].size() <= col) csv[row].resize(col+1);
			csv[row][col] = str;
		}
		
		// This allows a 'Loc' to be used to remove ',' and '\n' characters from a stream
		// while also maintaining the csv row,col position
		template <typename Stream> inline Stream& operator >> (Stream& s, Loc& loc)
		{
			PR_ASSERT_STR(PR_DBG, s.peek() == '\n' || s.peek() == ',', "Expected ',' or '\n' as the next charactor in the stream");
			loc.inc(s.get());
			return s;
		}
		
		// Increment a stream to the next csv element
		template <typename Stream> inline bool NextItem(Stream& file, Loc* loc = 0)
		{
			int ch;
			for (ch = file.get(); !file.eof() && ch != '\n' && ch != ','; ch = file.get()) {}
			if (loc) loc->inc(ch);
			return !file.bad();
		}
		
		// Increment a stream to the next csv row
		template <typename Stream> inline bool NextRow(Stream& file, Loc* loc = 0)
		{
			int ch;
			for (ch = file.get(); !file.eof() && ch != '\n'; ch = file.get()) {}
			if (loc) loc->inc(ch);
			return !file.bad();
		}
		
		// Read one element from a stream
		template <typename Stream> inline bool Read(Stream& file, Str& str, Loc* loc = 0)
		{
			int ch;
			for (ch = file.get(); !file.eof() && ch != '\n' && ch != ','; ch = file.get()) if (ch != '\r') str.push_back(static_cast<char>(ch));
			if (loc) loc->inc(ch);
			return !file.bad();
		}
		template <typename Stream, std::size_t N> inline bool Read(Stream& file, char (&str)[N], Loc* loc = 0)
		{
			int ch,i;
			for (i = 0, ch = file.get(); !file.eof() && i != N-1 && ch != '\n' && ch != ','; ch = file.get()) if (ch != '\r') str[i++] = static_cast<char>(ch);
			if (!file.eof() && ch != '\n' && ch != ',') NextItem(file, loc); else if (loc) loc->inc(ch);
			str[i] = 0;
			return !file.bad();
		}
		
		// Read one row from the stream
		template <typename Stream> inline bool Read(Stream& file, Row& row, Loc* loc = 0)
		{
			for (Str str; !file.eof() && Read(file, str, loc); str.resize(0))
			{
				bool row_end = file.unget().get() == '\n';
				if (!row_end || !str.empty()) row.push_back(str);
				if (row_end) break;
			}
			return !file.bad();
		}
		
		// Read all csv data from a stream
		template <typename Stream> inline bool Read(Stream& file, Csv& csv, Loc* loc = 0)
		{
			Row row; Str str;
			for (int ch = file.get(); !file.eof(); ch = file.get())
			{
				switch (ch) {
				default:   str.push_back(static_cast<char>(ch)); break;
				case ',':  row.push_back(str); str.resize(0); break;
				case '\n': row.push_back(str); str.resize(0); csv.push_back(row); row.resize(0); break;
				case '\r': break;
				}
				if (loc) loc->inc(ch);
			}
			if (!str.empty()) row.push_back(str);
			if (!row.empty()) csv.push_back(row);
			return !file.bad();
		}
		
		// Populate a Csv object from a file
		template <typename tchar> inline bool Load(tchar const* csv_filename, Csv& csv, Loc* loc = 0)
		{
			std::ifstream in(csv_filename);
			if (!in.is_open()) return false;
			return Read(in, csv, loc);
		}
		
		// Write one row to a stream
		template <typename Stream> inline void Write(Stream& file, Row& row)
		{
			for (Row::const_iterator i = row.begin(), iend = row.end(); i != iend; ++i)
				file << *i << (iend-i > 1 ? ',' : '\n');
		}
		
		// Write all csv data to a stream
		template <typename Stream> inline void Write(Stream& file, Csv& csv)
		{
			for (Csv::const_iterator i = csv.begin(), iend = csv.end(); i != iend; ++i)
				Write(file, *i);
		}
		
		// Write a Csv object to a file
		template <typename tchar> inline bool Save(tchar const* csv_filename, Csv const& csv)
		{
			std::ofstream out(csv_filename);
			if (!out.is_open()) return false;
			for (Csv::const_iterator r = csv.begin(), rend = csv.end(); r != rend; ++r)
			{
				for (Row::const_iterator cbegin = r->begin(), cend = r->end(), c = cbegin; c != cend; ++c)
					((c != cbegin) ? (out << ',') : (out)) << *c;
				out << '\n';
			}
			return true;
		}
		
		// Predicate for sorting on a column
		template <typename ElemCompare = std::less<std::string> >
		struct SortColumn
		{
			ElemCompare m_compare;
			std::size_t m_column;
			SortColumn(std::size_t column, ElemCompare const& compare = std::less<std::string>()) :m_column(column) ,m_compare(compare) {}
			bool operator () (Row const& lhs, Row const& rhs) const
			{
				PR_ASSERT_STR(PR_DBG, m_column < lhs.size() && m_column < rhs.size(), "Attempting to compare rows without enough columns");
				return m_compare(lhs[m_column], rhs[m_column]);
			}
		};
		
		// Predicate for making rows unique based on a particular column
		template <typename ElemCompare = std::equal_to<std::string> >
		struct UniqueColumn
		{
			ElemCompare m_compare;
			std::size_t m_column;
			UniqueColumn(std::size_t column, ElemCompare const& compare = std::equal_to<std::string>()) :m_column(column) ,m_compare(compare) {}
			bool operator () (Row const& lhs, Row const& rhs) const
			{
				PR_ASSERT_STR(PR_DBG, m_column < lhs.size() && m_column < rhs.size(), "Attempting to compare rows without enough columns");
				return m_compare(lhs[m_column], rhs[m_column]);
			}
		};
	}
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR
#endif
	
#endif
