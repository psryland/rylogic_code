//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <intrin.h>
#include <cassert>

#if _MSC_VER < 1900
#  ifndef alignas
#    define alignas(alignment) __declspec(align(alignment))
#  endif
#endif

namespace pr
{
	namespace script
	{
		// Generic character shift register
		template <typename Derived, typename TStore, typename Char>
		struct Buf
		{
			using value_type = Char;

			// Constants
			enum
			{
				Capacity = sizeof(TStore) / sizeof(Char),
				Front    = 0,
				Back     = Capacity - 1,
			};

			// Shift register storage
			union { Char m_ch[Capacity]; TStore m_ui; }; Char const m_term; // Used to ensure 'm_ch' is null terminated
			static_assert(sizeof(TStore) == sizeof(Char[Capacity]), "");

			// Char traits
			template <typename Ch> struct base_traits;
			template <> struct base_traits<char>
			{
				static size_t strlen(char const* str)                    { return ::strlen(str); }
				static size_t strnlen(char const* str, size_t max_count) { return ::strnlen(str, max_count); }
			};
			template <> struct base_traits<wchar_t>
			{
				static size_t strlen(wchar_t const* str)                    { return ::wcslen(str); }
				static size_t strnlen(wchar_t const* str, size_t max_count) { return ::wcsnlen(str, max_count); }
			};
			struct traits :base_traits<Char> {};

			Buf()
				:m_ui()
				,m_term()
			{}
			Buf(Buf&& rhs)
				:m_ui(rhs.m_ui)
				,m_term()
			{}
			Buf(Buf const& rhs)
				:m_ui(rhs.m_ui)
				,m_term()
			{}
			Buf& operator =(Buf&& rhs)
			{
				if (this != &rhs)
					std::swap(m_ui, rhs.m_ui);
				return *this;
			}
			Buf& operator =(Buf const& rhs)
			{
				if (this != &rhs)
				{
					m_ui = rhs.m_ui;
				}
				return *this;
			}
			template <typename Ptr> explicit Buf(Ptr& src) :Buf()
			{
				Load(src);
			}

			// Load the buffer from a source
			//  If 'src' has less than 2 characters then 0s are shifted into the buffer
			template <typename Ptr> void Load(Ptr& src)
			{
				int n = Capacity;
				for (; *src != 0 && n--; ++src) shift(*src);
				for (; n-- > 0;) shift(Char());
			}
			template <typename Ptr> void Load(Ptr const* src)
			{
				int n = Capacity;
				for (; *src != 0 && n--; ++src) shift(*src);
				for (; n-- > 0;) shift(Char());
			}

			// Reset the buffer
			void clear()
			{
				m_ui = TStore();
			}

			// Shift a character into the buffer
			void shift(Char ch)
			{
				m_ui = Derived::right_shift(m_ui);
				m_ch[Back] = ch;
			}

			// Access elements in the buffer
			Char front() const
			{
				return m_ch[Front];
			}
			Char back() const
			{
				return m_ch[Back];
			}

			// Allow dereference to mean the front of the buffer
			// Note, no operator++() however since we don't know
			// the source that feeds this buffer
			Char operator *() const
			{
				return front();
			}

			// Array access into the buffer
			Char operator [](size_t i) const
			{
				assert(i < Capacity);
				return m_ch[i];
			}
			Char& operator [](size_t i)
			{
				assert(i < Capacity);
				return m_ch[i];
			}

			// String access (std::string-like interface)
			Char const* c_str() const
			{
				return m_ch;
			}
			size_t size() const
			{
				return traits::strnlen(m_ch, Capacity);
			}

			// This returns true if 'buf' *contains* 'this', i.e. 'this' is a substring of 'buf' starting at m_ch[0].
			// Note: buf1.match(buf2) != buf2.match(buf1) generally
			bool match(Buf const& buf) const
			{
				// return m_ui != 0 && (m_ui & buf.m_ui) == m_ui;
				if (!front()) return false;
				return Derived::lhs_bits_set(m_ui, buf.m_ui);
			}

			// Equality operator
			bool operator == (Buf const& rhs)
			{
				return Derived::equal(m_ui, rhs.m_ui);
			}
			bool operator != (Buf const& rhs)
			{
				return !Derived::equal(m_ui, rhs.m_ui);
			}

			// Default implementation
			static TStore right_shift(TStore const& ui)
			{
				return ui >> 8 * sizeof(Char);
			}
			static bool lhs_bits_set(TStore const& lhs, TStore const& rhs)
			{
				return (lhs & rhs) == lhs;
			}
			static bool equal(TStore const& lhs, TStore const& rhs)
			{
				return lhs == rhs;
			}
		};

		// A "shift register" of 8 narrow characters
		struct Buf8 :Buf<Buf8, unsigned long long, char>
		{
			Buf8() :Buf() {}
			template <typename Ptr> explicit Buf8(Ptr const& src) :Buf(src) {}
			template <typename Ptr> explicit Buf8(Ptr& src) :Buf(src) {}
		};

		// A "shift register" of 2 wide characters
		struct BufW2 :Buf<BufW2, unsigned int, wchar_t>
		{
			BufW2() :Buf() {}
			template <typename Ptr> explicit BufW2(Ptr const& src) :Buf(src) {}
			template <typename Ptr> explicit BufW2(Ptr& src) :Buf(src) {}
		};

		// A "shift register" of 4 wide characters
		struct BufW4 :Buf<BufW4, unsigned long long, wchar_t>
		{
			BufW4() :Buf() {}
			template <typename Ptr> explicit BufW4(Ptr const& src) :Buf(src) {}
			template <typename Ptr> explicit BufW4(Ptr& src) :Buf(src) {}
		};

		// A "shift register" of 8 wide characters
		struct alignas(16) BufW8 :Buf<BufW8, __m128i, wchar_t>
		{
			BufW8() :Buf() {}
			template <typename Ptr> explicit BufW8(Ptr const& src) :Buf(src) {}
			template <typename Ptr> explicit BufW8(Ptr& src) :Buf(src) {}

			// Default implementation
			static __m128i right_shift(__m128i const& ui)
			{
				return _mm_srli_si128(ui, sizeof(wchar_t));
			}
			static bool lhs_bits_set(__m128i const& lhs, __m128i const& rhs)
			{
				return _mm_movemask_epi8(_mm_cmpeq_epi16(_mm_and_si128(lhs, rhs), lhs)) == 0xFFFF;
			}
			static bool equal(__m128i const& lhs, __m128i const& rhs)
			{
				return _mm_movemask_epi8(_mm_cmpeq_epi16(lhs, rhs)) == 0xFFFF;
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
		PRUnitTest(pr_script_buf8)
		{
			using namespace pr::str;
			using namespace pr::script;

			{// BufW2
				wchar_t const src[] = L"0123456789";
				BufW2 buf(src);
				PR_CHECK(buf[0], L'0');
				PR_CHECK(buf[1], L'1');
			}
			{// BufW4
				wchar_t* src = L"0123456789";
				BufW4 buf(src);
				PR_CHECK(*src, L'4');
				PR_CHECK(buf[0], L'0');
				PR_CHECK(buf[1], L'1');
				PR_CHECK(buf[2], L'2');
				PR_CHECK(buf[3], L'3');
				buf.shift(*src++);
				PR_CHECK(buf[0], L'1');
				PR_CHECK(buf[1], L'2');
				PR_CHECK(buf[2], L'3');
				PR_CHECK(buf[3], L'4');
			}
			{// BufW8
				wchar_t const src[] = L"0123456";
				PR_CHECK(Equal(BufW8(src).c_str(), src), true);
				PR_CHECK(BufW8(L"Paul"       ).match(BufW8(L"PaulWasHere")), true);
				PR_CHECK(BufW8(L"PaulWasHere").match(BufW8(L"Paul"       )), false);
				PR_CHECK(BufW8(L"ABC") == BufW8(L"ABC"), true);
			}
		}
	}
}
#endif
