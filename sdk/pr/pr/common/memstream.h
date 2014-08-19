//**********************************************************************************
// Resource
//  Copyright (c) Rylogic Ltd 2009
//**********************************************************************************

#pragma once

#include <streambuf>
#include <iostream>
#include <cassert>

namespace pr
{
	// Based on http://www.mr-edd.co.uk/blog/beginners_guide_streambuf

	// A stream buffer that wraps a constant buffer
	class imemstreambuf :public std::streambuf
	{
		char const* m_beg;
		char const* m_end;
		char const* m_ptr;

		// Underflow is called by the stream to refill the buffer once exhausted (or return eof()).
		int_type underflow() override
		{
			return m_ptr != m_end
				? traits_type::to_int_type(*m_ptr)
				: traits_type::eof();
		}

		int_type uflow()
		{
			return m_ptr != m_end
				? traits_type::to_int_type(*m_ptr++)
				: traits_type::eof();
		}

		int_type pbackfail(int_type ch)
		{
			return (m_ptr == m_beg || (ch != traits_type::eof() && ch != m_ptr[-1]))
				? traits_type::eof()
				: traits_type::to_int_type(*--m_ptr);
		}

		std::streamsize showmanyc()
		{
			assert(std::less_equal<const char *>()(m_ptr, m_end));
			return m_end - m_ptr;
		}

		pos_type seekpos(pos_type pos, std::ios_base::openmode which = std::ios_base::in | std::ios_base::out) override
		{
			// change to specified position, according to mode
			if (which & std::ios_base::in)
			{
				m_ptr = m_beg + clamp(ptrdiff_t(pos), 0, m_end - m_beg);
				return pos_type(m_ptr - m_beg);
			}
			return std::streambuf::seekpos(pos, which);
		}

		pos_type seekoff(off_type ofs, std::ios_base::seekdir dir, std::ios_base::openmode which = std::ios_base::in | std::ios_base::out) override
		{
			// change position by offset, according to way and mode
			if (which & std::ios_base::in)
			{
				switch (dir)
				{
				case std::ios_base::beg: m_ptr = m_beg + clamp(ptrdiff_t(ofs), m_beg - m_beg, m_end - m_beg); break;
				case std::ios_base::cur: m_ptr = m_ptr + clamp(ptrdiff_t(ofs), m_beg - m_ptr, m_end - m_ptr); break;
				case std::ios_base::end: m_ptr = m_end + clamp(ptrdiff_t(ofs), m_beg - m_end, m_end - m_end); break;
				}
				return pos_type(m_ptr - m_beg);
			}
			return std::streambuf::seekoff(ofs, dir, which);
		}

		ptrdiff_t clamp(ptrdiff_t x, ptrdiff_t mn, ptrdiff_t mx) const
		{
			return x < mn ? mn : x > mx ? mx : x;
		}

	public:
		imemstreambuf(void const* base, size_t size)
			:m_beg((char const*)base)
			,m_end(m_beg + size)
			,m_ptr(m_beg)
		{}
	};
	
	// An istream wrapper around an immutable buffer
	struct imemstream :virtual imemstreambuf, std::istream
	{
		imemstream(void const* data, std::size_t size)
			:imemstreambuf(data, size)
			,std::istream(static_cast<std::streambuf*>(this))
		{}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_memstream)
		{
			int data[] = {1,2,3};
			pr::imemstream strm(data, sizeof(data));
			PR_CHECK(!!strm, true);

			int out[3];
			strm.read((char*)out, sizeof(int)*3);
			PR_CHECK(memcmp(data,out,sizeof(int)*3) == 0, true);

			strm.seekg(0);
			PR_CHECK(!!strm, true);
			
			strm.read((char*)out, sizeof(int)*3);
			PR_CHECK(memcmp(data,out,sizeof(int)*3) == 0, true);
			
			size_t pos = (size_t)strm.tellg();
			PR_CHECK(pos, sizeof(int)*3);

			strm.seekg(4);
			strm.read((char*)out, sizeof(int)*1);
			PR_CHECK(memcmp(&data[1],out,sizeof(int)*1) == 0, true);
			
			pos = (size_t)strm.tellg();
			PR_CHECK(pos, sizeof(int)*2);
		}
	}
}
#endif
