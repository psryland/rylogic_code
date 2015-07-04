//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/location.h"

namespace pr
{
	namespace script2
	{
		// Helper for a generic character pointer
		union SrcConstPtr
		{
			wchar_t const* wptr;
			char    const* aptr;
			SrcConstPtr() :wptr() {}
			SrcConstPtr(wchar_t const* p) :wptr(p) {}
			SrcConstPtr(char const* p) :aptr(p) {}
		};

		// Interface to a stream of wchar_t's, essentually a pointer-like interface
		struct Src
		{
			virtual ~Src() {}

			// Debugging helper interface
			virtual ESrcType Type() const { return m_type; }
			virtual Location const& Loc() const { static Location s_loc; return s_loc; }
			virtual SrcConstPtr DbgPtr() const = 0;

			// Pointer-like interface
			virtual wchar_t operator * () const = 0;
			virtual Src&    operator ++() = 0;

			// Convenience methods
			Src& operator +=(int n)
			{
				for (;n--;) ++*this;
				return *this;
			}

		protected:

			ESrcType m_type;
			Src(ESrcType type)
				:m_type(type)
			{}
		};

		// An empty source
		struct NullSrc :Src
		{
			NullSrc() :Src(ESrcType::Null) {}
			SrcConstPtr DbgPtr() const override { return SrcConstPtr(); }
			wchar_t operator * () const override { return 0; }
			NullSrc& operator ++() { return *this; }
		};

		// Allow any type that implements a pointer to implement 'Src'
		template <typename TPtr, typename TLoc = TextLoc> struct Ptr :Src
		{
			TPtr m_ptr; // The pointer-like data source
			TLoc m_loc;

			Ptr(TPtr ptr, ESrcType src_type = ESrcType::Pointer)
				:Src(src_type)
				,m_ptr(ptr)
				,m_loc()
			{}

			// Debugging helper interface
			SrcConstPtr DbgPtr() const override
			{
				return &*m_ptr;
			}
			TextLoc const& Loc() const override
			{
				return m_loc;
			}

			// Pointer-like interface
			wchar_t operator * () const override
			{
				return wchar_t(*m_ptr);
			}
			Ptr& operator ++() override
			{
				if (*m_ptr) ++m_ptr;
				return *this;
			}
		};
		template <typename TLoc = TextLoc> using PtrA = Ptr<char const*, TLoc>;
		template <typename TLoc = TextLoc> using PtrW = Ptr<wchar_t const*, TLoc>;

		// Src buffer. Provides random access within a buffered range.
		template <typename TBuf = pr::deque<wchar_t>> struct Buffer :Src
		{
			using value_type = typename TBuf::value_type;
			using buffer_type = TBuf;

			mutable TBuf m_buf; // The buffered stream data read from 'm_src'
			Src* m_src;         // The character stream that feeds 'm_buf'
			NullSrc m_null;     // Used when 'm_src' == nullptr;

			Buffer(Src& src)
				:Src(src.Type())
				,m_buf()
				,m_src(&src)
				,m_null()
			{}
			template <typename Iter> Buffer(ESrcType type)
				:Src(type)
				,m_buf()
				,m_src(&m_null)
				,m_null()
			{}
			template <typename Iter> Buffer(ESrcType type, Iter first, Iter last)
				:Src(type)
				,m_buf(first, last)
				,m_src(&m_null)
				,m_null()
			{}
			Buffer(Buffer&& rhs)
				:Src(std::move(rhs))
				,m_buf(std::move(rhs.m_buf))
				,m_src(rhs.m_src)
				,m_null()
			{}
			Buffer(Buffer const& rhs)
				:Src(rhs)
				,m_buf(rhs.m_buf)
				,m_src(rhs.m_src)
				,m_null()
			{}

			// Debugging helper interface
			Location const& Loc() const override
			{
				return m_src->Loc();
			}
			SrcConstPtr DbgPtr() const override
			{
				return m_src->DbgPtr();
			}
			TBuf const* DbgBuf() const
			{
				return &m_buf;
			}

			// Pointer-like interface
			value_type operator *() const override
			{
				return empty() ? **m_src : m_buf[0];
			}
			Buffer& operator ++() override
			{
				if (!empty()) pop_front();
				else ++*m_src;
				return *this;
			}

			// Array access to the buffered data. Buffer size grows to accomodate 'i'
			value_type operator [](size_t i) const
			{
				if (i == 0) return **this;
				ensure(i);
				return m_buf[i];
			}
			value_type& operator [](size_t i)
			{
				// Returning a reference requires the character be buffered
				ensure(i);
				return m_buf[i];
			}

			// Returns true if no data is buffered
			bool empty() const
			{
				return m_buf.empty();
			}

			// The count of buffered characters
			size_t size() const
			{
				return m_buf.size();
			}

			// Removes all buffered data
			void clear()
			{
				// don't define resize() as its confusing which portion is kept (front or back?)
				m_buf.resize(0);
			}

			// Return the first buffered character
			value_type front() const
			{
				return m_buf.front()
			}

			// Return the last buffered character
			value_type back() const
			{
				return m_buf.back();
			}

			// Iterator range access to the buffer
			auto begin() const -> decltype(std::begin(m_buf))
			{
				return std::begin(m_buf);
			}
			auto end() const -> decltype(std::end(m_buf))
			{
				return std::end(m_buf);
			}

			// Returns the source that is feeding the buffer
			Src& stream() const
			{
				return *m_src;
			}
			
			// Push a character onto the front of the buffer (making it the next character read)
			void push_front(value_type ch)
			{
				m_buf.push_front(ch);
			}

			// Pop n characters from the front of the buffer
			void pop_front()
			{
				m_buf.pop_front();
			}
			void pop_front(size_t n)
			{
				auto first = std::begin(m_buf);
				m_buf.erase(first, first + n);
			}

			// Pop n characters from the back of the buffer
			void pop_back(size_t n = 1)
			{
				auto first = std::begin(m_buf);
				auto count = m_buf.size();
				m_buf.erase(first + count - n, first + count);
			}

			// Buffer the next 'n' characters from the source stream
			void buffer(size_t n = 1) const
			{
				for (;n-- > 0; ++*m_src)
					m_buf.push_back(**m_src);
			}

			// Ensure a total of 'n' characters are buffered
			void ensure(size_t n) const
			{
				if (n < m_buf.size()) return;
				buffer(1 + n - m_buf.size());
			}

			// Insert 'count' * 'ch's at 'ofs' in the buffer
			void insert(size_t ofs, size_t count, value_type ch)
			{
				ensure(ofs);
				m_buf.insert(std::begin(m_buf) + ofs, count, ch);
			}

			// Insert 'count' 'ch's at 'ofs' in the buffer
			template <typename Iter> void insert(size_t ofs, Iter first, Iter last)
			{
				ensure(ofs);
				m_buf.insert(std::begin(m_buf) + ofs, first, last);
			}

			// Erase a range within the buffered characters
			void erase(size_t ofs = 0, size_t count = ~0U)
			{
				auto first = std::begin(m_buf) + ofs;
				count = std::min(count, m_buf.size() - ofs);
				m_buf.erase(first, first + count);
			}

			// Return the buffered text as a string
			pr::string<value_type> str(size_t ofs = 0, size_t count = ~0U) const
			{
				auto first = std::begin(m_buf) + ofs;
				count = std::min(count, m_buf.size() - ofs);
				return pr::string<value_type>(first, first + count);
			}

			// String compare - note asymmetric: i.e. buf="abcd", str="ab", buf.match(str) == true
			// Buffers the input stream and compares it to 'str' return true if they match.
			// If 'adv_if_match' is true, the matching characters are popped from the buffer.
			// Note: *only* buffers matching characters. This prevents the buffer containing extra
			// data after a mismatch, and can be used to determine the length of a partial match.
			// Returns the length of the match == Length(str) if successful, 0 if not a match.
			// Not returning partial match length as that makes use as a boolean expression tricky.
			template <typename Str> int match(Str const& str) const
			{
				// Can't use wcscmp(), m_buf is not guaranteed contiguous
				size_t i = 0, count = pr::str::Length(str);

				// If the buffer contains data already, test that first
				for (auto buf_count = size_buf(); i != count && i < buf_count && str[i] == m_buf[i]; ++i) {}

				// Buffer extra matching characters if needed
				for (; i != count && str[i] == *stream(); buffer(), ++i) {}

				// Return match success/fail
				return i == count ? count : 0;
			}
			template <typename Char> int match(Char const* str) const
			{
				size_t i = 0;

				// If the buffer contains data already, test that first
				for (auto buf_count = size(); str[i] && i < buf_count && str[i] == m_buf[i]; ++i) {}

				// Buffer extra matching characters if needed
				for (; *stream() && *stream() == str[i]; buffer(), ++i) {}

				// Return match success/fail
				return str[i] == 0 ? int(i) : 0;
			}
			template <typename Str> int match(Str const& str, bool adv_if_match)
			{
				auto r = match(str);
				if (r && adv_if_match) pop_front(r);
				return r;
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_script_core)
		{
			using namespace pr::str;
			using namespace pr::script2;

			{// Simple buffering
				char const str[] = "123abc";
				PtrA<> ptr(str);
				Buffer<> buf(ptr);

				PR_CHECK(*buf  , L'1');
				PR_CHECK(buf[5], L'c');
				PR_CHECK(buf[0], L'1');

				PR_CHECK(*(++buf)   , L'2');
				PR_CHECK(*(buf += 3), L'b');
				PR_CHECK(*(++buf)   , L'c');

				PR_CHECK(*(++buf),  0);
			}
			{// Matching
				wchar_t const str[] = L"0123456789";
				PtrW<> ptr(str);
				Buffer<> buf(ptr);

				PR_CHECK(buf.match(L"0123") != 0, true);
				PR_CHECK(buf.match(L"012345678910") != 0, false);
				buf += 5;
				PR_CHECK(buf.match(L"567") != 0, true);
			}
		}
	}
}
#endif
