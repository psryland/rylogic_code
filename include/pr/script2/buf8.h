//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <intrin.h>
#include <cassert>

// C++11's alignas
#ifndef alignas
#define alignas(alignment) __declspec(align(alignment))
#endif

namespace pr
{
	namespace script2
	{
		// A "shift register" of 8 characters
		struct alignas(16) Buf8
		{
			static int const Front = 0, Back = 7, Capacity = 8;
			union { __m128i m_ui; wchar_t m_ch[Capacity]; };
			static_assert(sizeof(__m128i) == sizeof(wchar_t[Capacity]), "");

			Buf8()
				:m_ui()
			{}
			template <typename Ptr> Buf8(Ptr const& src) :Buf8()
			{
				Load(src);
			}
			template <typename Ptr> Buf8(Ptr& src) :Buf8()
			{
				Load(src);
			}

			// Load the buffer from a source
			//  If 'src' has less than 8 characters then 0s are shifted into the buffer
			template <typename Ptr> void Load(Ptr const& src)
			{
				auto s = src;
				Load(s);
			}
			template <typename Ptr> void Load(Ptr& src)
			{
				auto n = Capacity;
				for (; *src && n--; ++src) shift(*src);
				for (; n-- > 0;) shift(wchar_t());
			}

			// Reset the buffer
			void clear()
			{
				m_ui = _mm_setzero_si128();
			}

			// Shift a character into the buffer
			void shift(wchar_t wide_char)
			{
				m_ui = _mm_srli_si128(m_ui, 2);
				m_ch[Back] = wide_char;
			}

			// Access elements in the buffer
			wchar_t front() const
			{
				return m_ch[Front];
			}
			wchar_t back() const
			{
				return m_ch[Back];
			}

			// Array access into the buffer
			wchar_t operator [](int i) const
			{
				assert(i < Capacity);
				return m_ch[i];
			}
			wchar_t& operator [](int i)
			{
				assert(i < Capacity);
				return m_ch[i];
			}

			// String access
			wchar_t const* c_str() const
			{
				assert(m_ch[Back] == 0 && "string not terminated");
				return m_ch;
			}

			// This returns true if 'buf' *contains* 'this', i.e. 'this' is a substring of 'buf' starting at m_ch[0].
			// Note: buf1.match(buf2) != buf2.match(buf1) generally
			bool match(Buf8 const& buf) const
			{
				// return m_ui != 0 && (m_ui & buf.m_ui) == m_ui;
				if (!front()) return false;
				return _mm_movemask_epi8(_mm_cmpeq_epi16(_mm_and_si128(m_ui, buf.m_ui), m_ui)) == 0xFFFF;
			}
		};
		inline bool operator == (Buf8 const& lhs, Buf8 const& rhs)
		{
			return _mm_movemask_epi8(_mm_cmpeq_epi16(lhs.m_ui, rhs.m_ui)) == 0xFFFF;
		}
		inline bool operator != (Buf8 const& lhs, Buf8 const& rhs)
		{
			return !(lhs == rhs);
		}


		// Extends Buf8 to contain the character source
		template <typename Ptr> struct alignas(16) Buf8Src :private Buf8
		{
			Ptr* m_ptr;
			bool m_clean_up;

			Buf8Src()
				:Buf8()
				,m_ptr()
				,m_clean_up()
			{}
			Buf8Src(Ptr* src, bool clean_up)
				:Buf8(src)
				,m_ptr(src)
				,m_clean_up(clean_up)
			{}
			Buf8Src(Buf8Src&& rhs)
				:Buf8(rhs)
				,m_ptr(rhs.m_ptr)
				,m_clean_up(rhs.m_clean_up)
			{
				m_rhs.m_ptr = nullptr;
				m_rhs.m_clean_up = false;
			}
			Buf8Src(Buf8Src const&) = delete;
			Buf8Src& operator =(Buf8Src const&) = delete;
			~Buf8Src()
			{
				if (m_clean_up)
					delete src;
			}

			// Pointer interface
			wchar_t operator*() const
			{
				return front();
			}
			Buf8Src& operator ++()
			{
				shift(*m_ptr);
				m_ptr += *m_ptr != 0;
				return *this;
			}
			Buf8Src& operator +=(int n)
			{
				for (;n--;) ++*this;
				return *this;
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
		PRUnitTest(pr_script2_buf8)
		{
			using namespace pr::str;
			using namespace pr::script2;

			wchar_t const src[] = L"0123456";
			PR_CHECK(Equal(Buf8(src).c_str(), src), true);
			PR_CHECK(Buf8(L"Paul"       ).match(Buf8(L"PaulWasHere")), true);
			PR_CHECK(Buf8(L"PaulWasHere").match(Buf8(L"Paul"       )), false);
			PR_CHECK(Buf8(L"ABC") == Buf8(L"ABC"), true);
		}
	}
}
#endif
