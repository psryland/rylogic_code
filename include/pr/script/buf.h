//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************
#pragma once
#include <type_traits>
#include <cstdint>
#include <cassert>
#include <intrin.h>
#include "pr/str/string_core.h"

#if _MSC_VER < 1900
#  ifndef alignas
#    define alignas(alignment) __declspec(align(alignment))
#  endif
#endif

namespace pr::script
{
	// Generic character shift register
	template <int N, typename Char>
	struct Buf
	{
		using value_type = Char;
		using storage_t = 
			std::conditional_t<sizeof(value_type) * N <= sizeof(uint32_t), uint32_t,
			std::conditional_t<sizeof(value_type) * N <= sizeof(uint64_t), uint64_t,
			std::conditional_t<sizeof(value_type) * N <= sizeof(__m128i), __m128i,
			std::aligned_storage_t<sizeof(value_type) * N> > > >;

		// Constants
		static constexpr int Capacity = sizeof(storage_t) / sizeof(value_type);
		static constexpr int Back = Capacity - 1;
		static constexpr int Front = 0;

		static_assert(sizeof(storage_t) == sizeof(value_type[Capacity]));
		static_assert(std::is_trivially_copyable_v<storage_t>);

		// Shift register storage
		union
		{
			value_type m_ch[Capacity];
			storage_t m_store;
		};
		value_type const m_term; // Used to ensure 'm_ch' is null terminated

		Buf()
			:m_store()
			,m_term()
		{}
		Buf(Buf const& rhs)
			:m_store(rhs.m_store)
			,m_term()
		{}
		template <typename Ptr>
		explicit Buf(Ptr&& src)
			:Buf()
		{
			Load(std::forward<Ptr>(src));
		}
		Buf& operator =(Buf const& rhs)
		{
			if (this == &rhs) return *this;
			if constexpr (std::is_integral_v<storage_t>)
				m_store = rhs.m_store;
			else
				memcpy(&m_store, &rhs.m_store, sizeof(storage_t));
			return *this;
		}

		// Load the buffer from a source
		template <typename Ptr>
		void Load(Ptr& src)
		{
			int n = Capacity;
			for (; *src != 0 && n--; ++src)
				shift(*src);
			for (; n-- > 0;)
				shift(value_type());
		}
		template <typename Ptr>
		void Load(Ptr const& src)
		{
			std::decay_t<Ptr const> ptr = src;
			Load(ptr);
		}

		// Reset the buffer
		void clear()
		{
			m_store = storage_t{};
		}

		// Shift a character into the buffer
		void shift(value_type ch)
		{
			if constexpr (std::is_same_v<storage_t, __m128i>)
			{
				m_store = _mm_srli_si128(m_store, sizeof(value_type));
			}
			else if constexpr (std::is_integral_v<storage_t>)
			{
				m_store = m_store >> (8 * sizeof(value_type));
			}
			else
			{
				auto ptr = reinterpret_cast<value_type*>(&m_store);
				memmove(ptr, ptr + 1, sizeof(value_type) * (Capacity - 1));
			}
			m_ch[Back] = ch;
		}

		// Access elements in the buffer
		value_type front() const
		{
			return m_ch[Front];
		}
		value_type back() const
		{
			return m_ch[Back];
		}

		// Allow dereference to mean the front of the buffer
		// Note, no operator++() however since we don't have the source that feeds this buffer.
		value_type operator *() const
		{
			return front();
		}

		// Array access into the buffer
		value_type operator [](size_t i) const
		{
			assert(i < Capacity);
			return m_ch[i];
		}
		value_type& operator [](size_t i)
		{
			assert(i < Capacity);
			return m_ch[i];
		}

		// String access (std::string-like interface)
		value_type const* c_str() const
		{
			return m_ch;
		}
		size_t size() const
		{
			return pr::char_traits<value_type>::length(m_ch, Capacity);
		}

		// This returns true if 'buf' *contains* 'this', i.e. 'this' is a substring of 'buf' starting at m_ch[0].
		// Note: buf1.match(buf2) != buf2.match(buf1) generally
		bool match(Buf const& buf) const
		{
			if (!front()) return false;

			auto& lhs = *this;
			auto& rhs = buf;
			if constexpr (std::is_same_v<storage_t, __m128i>)
			{
				return _mm_movemask_epi8(_mm_cmpeq_epi16(_mm_and_si128(lhs.m_store, rhs.m_store), lhs.m_store)) == 0xFFFF;
			}
			else if constexpr (std::is_integral_v<storage_t>)
			{
				return (lhs.m_store & rhs.m_store) == lhs.m_store;
			}
			else
			{
				auto result = true;
				auto lptr = reinterpret_cast<value_type const*>(&lhs);
				auto rptr = reinterpret_cast<value_type const*>(&rhs);
				for (int i = 0; i != Capacity; ++i) result &= (lptr[i] & rptr[i]) == lptr[i];
				return result;
			}
		}

		// Equality operator
		friend bool operator == (Buf const& lhs, Buf const& rhs)
		{
			if constexpr (std::is_same_v<storage_t, __m128i>)
			{
				return _mm_movemask_epi8(_mm_cmpeq_epi16(lhs.m_store, rhs.m_store)) == 0xFFFF;
			}
			else if constexpr (std::is_integral_v<storage_t>)
			{
				return lhs.m_store == rhs.m_store;
			}
			else
			{
				return memcmp(&lhs, &rhs, sizeof(value_type) * Capacity) == 0;
			}
		}
		friend bool operator != (Buf const& lhs, Buf const& rhs)
		{
			return !(lhs == rhs);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
#include "pr/script/script_core.h"
namespace pr::script
{
	PRUnitTest(BufTests)
	{
		using namespace pr::str;

		{// BufW2
			wchar_t const data[] = L"0123456789";
			wchar_t const* const src = &data[0];
			Buf<2, wchar_t> buf(src);
			PR_CHECK(buf[0], L'0');
			PR_CHECK(buf[1], L'1');
			PR_CHECK(*src, L'0');

		}
		{// BufW4
			wchar_t const data[] = L"0123456789";
			wchar_t const* src = &data[0];
			Buf<4, wchar_t> buf(src);
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
			using BufW8 = Buf<8, wchar_t>;
			wchar_t const src[] = L"0123456";
			PR_CHECK(Equal(BufW8(src).c_str(), src), true);
			PR_CHECK(BufW8(L"Paul"       ).match(BufW8(L"PaulWasHere")), true);
			PR_CHECK(BufW8(L"PaulWasHere").match(BufW8(L"Paul"       )), false);
			PR_CHECK(BufW8(L"ABC") == BufW8(L"ABC"), true);
		}
		{// Src
			script::StringSrc src("0123456789");
			Buf<4,wchar_t> buf(src);
			PR_CHECK(*src, L'4');
			PR_CHECK(buf[0], L'0');
			PR_CHECK(buf[1], L'1');
			PR_CHECK(buf[2], L'2');
			PR_CHECK(buf[3], L'3');
			buf.shift(*src);
			PR_CHECK(buf[0], L'1');
			PR_CHECK(buf[1], L'2');
			PR_CHECK(buf[2], L'3');
			PR_CHECK(buf[3], L'4');
		}
	}
}
#endif



	#if 0
	// Generic character shift register
	template <typename Derived, typename TStore, typename Char>
	struct Buf
	{
		using value_type = Char;
		using storage_type = TStore;

		// Constants
		static constexpr int Capacity = sizeof(storage_type) / sizeof(value_type);
		static constexpr int Back = Capacity - 1;
		static constexpr int Front = 0;

		// Shift register storage
		union
		{
			value_type m_ch[Capacity];
			storage_type m_ui;
			static_assert(sizeof(storage_type) == sizeof(value_type[Capacity]));
		};
		value_type const m_term; // Used to ensure 'm_ch' is null terminated

		Buf()
			:m_ui()
			,m_term()
		{}
		Buf(Buf const& rhs)
			:m_ui(rhs.m_ui)
			,m_term()
		{}
		template <typename Ptr>
		explicit Buf(Ptr&& src)
			:Buf()
		{
			Load(std::forward<Ptr>(src));
		}
		Buf& operator =(Buf const& rhs)
		{
			if (this == &rhs) return *this;
			m_ui = rhs.m_ui;
			return *this;
		}

		// Load the buffer from a source
		template <typename Ptr>
		void Load(Ptr& src)
		{
			int n = Capacity;
			for (; *src != 0 && n--; ++src)
				shift(*src);
			for (; n-- > 0;)
				shift(value_type());
		}
		template <typename Ptr>
		void Load(Ptr const& src)
		{
			std::decay_t<Ptr const> ptr = src;
			Load(ptr);
		}

		// Reset the buffer
		void clear()
		{
			m_ui = storage_type();
		}

		// Shift a character into the buffer
		void shift(value_type ch)
		{
			Derived::shift_right(m_ui);
			m_ch[Back] = ch;
		}

		// Access elements in the buffer
		value_type front() const
		{
			return m_ch[Front];
		}
		value_type back() const
		{
			return m_ch[Back];
		}

		// Allow dereference to mean the front of the buffer
		// Note, no operator++() however since we don't know
		// the source that feeds this buffer
		value_type operator *() const
		{
			return front();
		}

		// Array access into the buffer
		value_type operator [](size_t i) const
		{
			assert(i < Capacity);
			return m_ch[i];
		}
		value_type& operator [](size_t i)
		{
			assert(i < Capacity);
			return m_ch[i];
		}

		// String access (std::string-like interface)
		value_type const* c_str() const
		{
			return m_ch;
		}
		size_t size() const
		{
			return pr::char_traits<value_type>::length(m_ch, Capacity);
		}

		// This returns true if 'buf' *contains* 'this', i.e. 'this' is a substring of 'buf' starting at m_ch[0].
		// Note: buf1.match(buf2) != buf2.match(buf1) generally
		bool match(Buf const& buf) const
		{
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
		static void shift_right(storage_type& store)
		{
			if constexpr (std::is_integral_v<storage_type>)
			{
				store = store >> (8 * sizeof(value_type));
			}
			else
			{
				auto ptr = reinterpret_cast<value_type*>(&store);
				memmove(ptr, ptr + 1, sizeof(value_type) * (Capacity - 1));
			}
		}
		static bool lhs_bits_set(storage_type const& lhs, storage_type const& rhs)
		{
			if constexpr (std::is_integral_v<storage_type>)
			{
				return (lhs & rhs) == lhs;
			}
			else
			{
				auto result = true;
				auto lptr = reinterpret_cast<value_type const*>(&lhs);
				auto rptr = reinterpret_cast<value_type const*>(&rhs);
				for (int i = 0; i != Capacity; ++i) result &= (lptr[i] & rptr[i]) == lptr[i];
				return result;
			}
		}
		static bool equal(storage_type const& lhs, storage_type const& rhs)
		{
			if constexpr (std::is_integral_v<storage_type>)
			{
				return lhs == rhs;
			}
			else
			{
				return memcmp(&lhs, &rhs, sizeof(value_type) * Capacity) == 0;
			}
		}
	};

	// A "shift register" of 8 narrow characters
	struct Buf8 :Buf<Buf8, unsigned long long, char>
	{
		Buf8()
			:Buf()
		{}
		template <typename Ptr>
		explicit Buf8(Ptr&& src)
			:Buf(std::forward<Ptr>(src))
		{}
	};

	// A "shift register" of 2 wide characters
	struct BufW2 :Buf<BufW2, unsigned int, wchar_t>
	{
		BufW2()
			:Buf()
		{}
		template <typename Ptr>
		explicit BufW2(Ptr&& src)
			:Buf(std::forward<Ptr>(src))
		{}
	};

	// A "shift register" of 4 wide characters
	struct BufW4 :Buf<BufW4, unsigned long long, wchar_t>
	{
		BufW4()
			:Buf()
		{}
		template <typename Ptr>
		explicit BufW4(Ptr&& src)
			:Buf(std::forward<Ptr>(src))
		{}
	};

	// A "shift register" of 8 wide characters
	struct alignas(16) BufW8 :Buf<BufW8, __m128i, wchar_t>
	{
		BufW8()
			:Buf()
		{}
		template <typename Ptr>
		explicit BufW8(Ptr&& src)
			:Buf(src)
		{}

		// Default implementation
		static void shift_right(__m128i& ui)
		{
			ui = _mm_srli_si128(ui, sizeof(wchar_t));
		}
		static bool lhs_bits_set(__m128i const& lhs, __m128i const& rhs)
		{
			return _mm_movemask_epi8(_mm_cmpeq_epi16(_mm_and_si128(lhs, rhs), lhs)) == 0xFFFF;
		}
		static bool equal(__m128i const& lhs, __m128i const& rhs)
		{
			
		}
	};

	// A "shift register" of N wide characters
	template <int N, typename store_t = std::aligned_storage_t<N*sizeof(wchar_t)>>
	struct BufW :Buf<BufW<N>, store_t, wchar_t>
	{
		BufW() = default;
		template <typename Ptr>
		explicit BufW(Ptr&& src)
			:Buf(src)
		{}
	};
	#endif
