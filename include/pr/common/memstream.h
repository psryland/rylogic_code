//**********************************************************************************
// Resource
//  Copyright (c) Rylogic Ltd 2009
//**********************************************************************************

#pragma once

#include <streambuf>
#include <iostream>
#include <type_traits>
#include <cassert>

namespace pr
{
	// See: http://www.cplusplus.com/reference/streambuf/basic_streambuf/

	// A stream buffer that wraps a constant buffer of bytes
	template <typename Elem>
	class view_streambuf :public std::basic_streambuf<Elem>
	{
		using char_type = typename std::basic_streambuf<Elem>::char_type;
		using int_type = typename std::basic_streambuf<Elem>::int_type;
		using pos_type = typename std::basic_streambuf<Elem>::pos_type;
		using off_type = typename std::basic_streambuf<Elem>::off_type;

		char_type const* m_beg;
		char_type const* m_end;
		char_type const* m_ptr;

	public:

		view_streambuf(void const* data, size_t size)
			:std::basic_streambuf<Elem>()
			,m_beg(static_cast<char_type const*>(data))
			,m_end(m_beg + size)
			,m_ptr(m_beg)
		{}

	protected:

		// Get the current character in the controlled input sequence without changing the current position (or return eof()).
		int_type underflow() override
		{
			return m_ptr != m_end
				? traits_type::to_int_type(*m_ptr)
				: traits_type::eof();
		}

		// Get the current character in the controlled input sequence and then advance the position indicator to the next character.
		int_type uflow() override
		{
			return m_ptr != m_end
				? traits_type::to_int_type(*m_ptr++)
				: traits_type::eof();
		}

		// Puts a character back into the input sequence, possibly modifying the input sequence
		int_type pbackfail(int_type ch)
		{
			return m_ptr != m_beg && (ch == traits_type::eof() || ch == m_ptr[-1])
				? traits_type::to_int_type(*--m_ptr)
				: traits_type::eof();
		}

		// Read "s-how-many-c". Get an estimate on the number of characters available in the associated input sequence.
		std::streamsize showmanyc()
		{
			assert(std::less_equal<char_type const*>()(m_ptr, m_end));
			return m_end - m_ptr;
		}

		// Sets the position indicator of the input and/or output sequence to an absolute position.
		pos_type seekpos(pos_type pos, std::ios_base::openmode which = std::ios_base::in | std::ios_base::out) override
		{
			// change to specified position, according to mode
			if (which & std::ios_base::in)
			{
				m_ptr = m_beg + clamp(ptrdiff_t(pos), 0, m_end - m_beg);
				return pos_type(m_ptr - m_beg);
			}
			return std::basic_streambuf<Elem>::seekpos(pos, which);
		}

		// Sets the position indicator of the input and/or output sequence relative to some other position.
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
			return std::basic_streambuf<Elem>::seekoff(ofs, dir, which);
		}
	
		// Clamp to a range
		static ptrdiff_t clamp(ptrdiff_t x, ptrdiff_t mn, ptrdiff_t mx)
		{
			return x < mn ? mn : x > mx ? mx : x;
		}
	};

	// A stream buffer that wraps callbacks to handle reading/writing data
	template <typename Elem>
	class callback_streambuf :public std::basic_streambuf<Elem>
	{
	public:

		// Notes:
		//  - the callback functions shouldn't throw, but return 0 bytes read/written.
		//  - the end of the buffer is determined implicitly from the success of reading/writting.
		//  - read/write can be null if only writing/reading respectively.
		using char_type = typename std::basic_streambuf<Elem>::char_type;
		using int_type = typename std::basic_streambuf<Elem>::int_type;
		using pos_type = typename std::basic_streambuf<Elem>::pos_type;
		using off_type = typename std::basic_streambuf<Elem>::off_type;
		static_assert(sizeof(char_type) == sizeof(uint8_t));

		// Callback signature. Returns the number of characters successfully read/written.
		using write_t = std::streamsize (*)(void*, off_type ofs, char_type const* bytes, std::streamsize count);
		using read_t  = std::streamsize (*)(void*, off_type ofs, char_type* bytes, std::streamsize count);

	private:

		off_type m_gpos; // The read position in the buffer
		off_type m_ppos; // The write position in the buffer
		off_type m_end;  // The maximum size of the buffer
		write_t m_write; // The write callback
		read_t m_read;   // The read callback
		void* m_ctx;     // User context pointer

	public:

		callback_streambuf(read_t read, write_t write, void* ctx, pos_type end = 0)
			:m_gpos()
			,m_ppos()
			,m_end(end)
			,m_write(write)
			,m_read(read)
			,m_ctx(ctx)
		{}

	private:

		// Get the current character in the controlled input sequence without changing the current position (or return eof()).
		int_type underflow() override
		{
			// Note: 'm_gpos' not incremented
			char_type ch;
			return m_read(m_ctx, m_gpos, &ch, 1) == 1
				? traits_type::to_int_type(ch)
				: traits_type::eof();
		}

		// Overflow is called to grow the output (or return eof())
		int_type overflow(int_type c) override
		{
			// Note: 'm_ppos' not incremented
			auto ch = traits_type::to_char_type(c);
			return m_write(m_ctx, m_ppos, &ch, 1) == 1
				? traits_type::to_int_type(ch)
				: traits_type::eof();
		}

		// Get the current character in the controlled input sequence and then advance the position indicator to the next character.
		int_type uflow() override
		{
			int_type ch = underflow();
			if (ch != traits_type::eof())
			{
				m_gpos += 1;
				m_end = std::max(m_end, m_gpos);
			}
			return ch;
		}

		// Puts a character back into the input sequence, possibly modifying the input sequence
		int_type pbackfail(int_type c) override
		{
			// If at the start of the input buffer, then fail
			if (m_gpos == 0)
				return traits_type::eof();

			// If 'c' is 'eof()' then we don't care what the previous char was
			if (c == traits_type::eof())
				return m_gpos -= 1, c;

			// If 'c' matches the previous char, then put back succeeds
			if (char_type ch; m_read(m_ctx, pos_type(m_gpos - 1), &ch, 1) == 1 && traits_type::to_int_type(ch) == c)
				return m_gpos -= 1, ch;
			
			// If overwriting the input sequence with 'c' succeeds, then put back succeeds
			if (char_type ch = traits_type::to_char_type(c); m_write != nullptr && m_write(m_ctx, pos_type(m_gpos - 1), &ch, 1) == 1)
				return m_gpos -= 1, ch; 
			
			return traits_type::eof();
		}

		// Retrieves characters from the controlled input sequence and stores them in the array pointed by s,
		// until either n characters have been extracted or the end of the sequence is reached.
		std::streamsize xsgetn(char_type* s, std::streamsize n) override
		{
			auto count = m_read(m_ctx, m_gpos, s, n);
			m_gpos += count;
			m_end = std::max(m_gpos, m_end);
			return count;
		}

		// Writes characters from the array pointed to by s into the controlled output sequence,
		// until either n characters have been written or the end of the output sequence is reached.
		std::streamsize xsputn(char_type const* s, std::streamsize n) override 
		{ 
			auto count = m_write(m_ctx, m_ppos, s, n);
			m_ppos += count;
			m_end = std::max(m_ppos, m_end);
			return count;
		};

		// Read "s-how-many-c". Get an estimate on the number of characters available in the associated input sequence.
		std::streamsize showmanyc() override
		{
			return m_end - m_gpos;
		}

		// Sets the position indicator of the input and/or output sequence to an absolute position.
		pos_type seekpos(pos_type pos, std::ios_base::openmode which = std::ios_base::in | std::ios_base::out) override
		{
			if (which & std::ios_base::in)
			{
				// Clamp to the known range of data
				m_gpos = clamp(pos, 0, m_end);
				return pos_type(m_gpos);
			}
			if (which & std::ios_base::out)
			{
				// Fill to the required size
				for (; m_ppos < pos && overflow(0) != traits_type::eof();) {}
				m_ppos = clamp(pos, 0, m_end);;
				return pos_type(m_ppos);
			}
			return std::basic_streambuf<Elem>::seekpos(pos, which);
		}

		// Sets the position indicator of the input and/or output sequence relative to some other position.
		pos_type seekoff(off_type ofs, std::ios_base::seekdir dir, std::ios_base::openmode which = std::ios_base::in | std::ios_base::out) override
		{
			static auto const inout = std::ios_base::in | std::ios_base::out;
			switch (dir)
			{
			case std::ios_base::beg: return seekpos(0     + ofs, which);
			case std::ios_base::end: return seekpos(m_end + ofs, which);
			case std::ios_base::cur: return
				which == std::ios_base::in ? seekpos(m_gpos + ofs, which) :
				which == std::ios_base::out ? seekpos(m_ppos + ofs, which) :
				which == inout && m_gpos == m_ppos ? seekpos(m_gpos + ofs, which) :
				throw std::runtime_error("relative seek for both in and out position is invalid when the current positions are not equal");
			}
			return std::basic_streambuf<Elem>::seekoff(ofs, dir, which);
		}
		
		// Clamp to a range
		static off_type clamp(off_type x, off_type mn, off_type mx)
		{
			return x < mn ? mn : x > mx ? mx : x;
		}
	};

	// An istream wrapper around an immutable buffer
	template<typename Elem = uint8_t>
	struct mem_istream :public std::basic_istream<Elem>
	{
		using base_t = typename std::basic_istream<Elem>;
		using char_type = typename base_t::char_type;

		view_streambuf<Elem> buf;
		mem_istream(void const* data, std::size_t size)
			:std::basic_istream<char_type>(&buf)
			,buf(data, size)
		{}

		mem_istream& read(char_type* src, std::streamsize count)
		{
			auto& base = *static_cast<base_t*>(this);
			base.read(reinterpret_cast<char_type*>(src), count);
			return *this;
		}
	};

	// An ostream wrapper around a user provided container
	template <typename Container = std::vector<uint8_t>>
	struct mem_ostream :public std::basic_ostream<uint8_t>
	{
		using base_t = typename std::basic_ostream<uint8_t>;
		using value_type = typename Container::value_type;
		using char_type = typename base_t::char_type;

		callback_streambuf<char_type> buf;
		Container& data;

		mem_ostream(Container& data)
			:std::basic_ostream<char_type>(&buf)
			,buf(nullptr, write, this)
			,data(data)
		{}

		mem_ostream& write(char_type const* src, std::streamsize count)
		{
			auto& base = *static_cast<base_t*>(this);
			base.write(reinterpret_cast<char_type const*>(src), count);
			return *this;
		}

	private:

		// Convert from size in bytes to count in 'value_type's
		constexpr static size_t size_to_count(size_t size_in_bytes)
		{
			return (size_in_bytes + sizeof(value_type) - 1) / sizeof(value_type);
		}

		// Callback function that adds data to 'data'
		static std::streamsize write(void* ctx, std::streambuf::off_type ofs, char_type const* bytes, std::streamsize count)
		{
			auto& me = *static_cast<mem_ostream*>(ctx);
			me.data.resize(std::max(me.data.size(), size_to_count(size_t(ofs + count))));
			std::memcpy(reinterpret_cast<char_type*>(me.data.data()) + ofs, bytes, static_cast<size_t>(count));
			return count;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(MemStreamTests)
	{
		{ // mem_istream
			int data[] = { 1, 2, 3 };
			pr::mem_istream<char> strm(data, sizeof(data));
			PR_CHECK(!!strm, true);

			int out[3];
			strm.read((char*)out, sizeof(int) * 3);
			PR_CHECK(memcmp(data, out, sizeof(int) * 3) == 0, true);

			strm.seekg(0);
			PR_CHECK(!!strm, true);

			strm.read((char*)out, sizeof(int) * 3);
			PR_CHECK(memcmp(data, out, sizeof(int) * 3) == 0, true);

			size_t pos = (size_t)strm.tellg();
			PR_CHECK(pos, sizeof(int) * 3);

			strm.seekg(4);
			strm.read((char*)out, sizeof(int) * 1);
			PR_CHECK(memcmp(&data[1], out, sizeof(int) * 1) == 0, true);

			pos = (size_t)strm.tellg();
			PR_CHECK(pos, sizeof(int) * 2);
		}
		{ // mem_ostream
			uint8_t src[] = { 1,1,1,1, 2,2,2,2, 3,3,3,3, 4,4,4,4 };
			int expected[] = { 0x01010101, 0x02020202, 0x03030303, 0x04040404 };

			std::vector<int> data;
			pr::mem_ostream strm(data);
		
			strm.write(&src[0], 12);
			PR_CHECK(data.size(), 3U);
			PR_CHECK(memcmp(data.data(), &expected[0], sizeof(int) * 3) == 0, true);

			strm.seekp(0);
			strm.write(&src[0] + 4, 12);
			PR_CHECK(data.size(), 3U);
			PR_CHECK(memcmp(data.data(), &expected[1], sizeof(int) * 3) == 0, true);

			strm.seekp(0);
			strm.write(&src[0], 16);
			PR_CHECK(data.size(), 4U);
			PR_CHECK(memcmp(data.data(), &expected[0], sizeof(int) * 4) == 0, true);

			size_t pos = (size_t)strm.tellp();
			PR_CHECK(pos, 16U);
		}
	}
}
#endif
