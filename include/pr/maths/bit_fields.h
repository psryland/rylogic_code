//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
//http://graphics.stanford.edu/~seander/bithacks.html
#pragma once
#include "pr/maths/forward.h"

namespace pr
{
	// Convert to/from uint64 to uint32[2]
	constexpr uint64_t MakeLL(uint32_t hi, uint32_t lo)
	{
		return (static_cast<uint64_t>(hi) << 32) | lo;
	}
	inline void BreakLL(uint64_t ll, uint32_t& hi, uint32_t& lo)
	{
		hi = uint32_t((ll >> 32L) & 0xFFFFFFFF);
		lo = uint32_t((ll       ) & 0xFFFFFFFF);
	}

	// Bit manipulation functions
	constexpr uint32_t Bit32(int n)
	{
		return 1U << n;
	}
	constexpr uint64_t Bit64(int n)
	{
		return 1i64 << n;
	}

	// If 'state' is true, returns 'value | mask'. If false, returns 'value &~ mask'
	template <typename T, typename U> constexpr T SetBits(T value, U mask, bool state)
	{
		using Int = pr::underlying_type_t<T>;
		return state
			? static_cast<T>(static_cast<Int>(value) |  static_cast<Int>(mask))
			: static_cast<T>(static_cast<Int>(value) & ~static_cast<Int>(mask));
	}

	// Sets the masked bits of 'value' to the state 'bitfield'
	template <typename T, typename U> constexpr T SetBits(T value, U mask, U bitfield)
	{
		using Int = pr::underlying_type_t<T>;
		auto result = static_cast<Int>(value);
		result &= ~static_cast<Int>(mask);            // clear masked bits to zero
		result |=  static_cast<Int>(mask & bitfield); // set bits from bit field
		return result;
	}

	// Returns true if any bits in 'value & mask != 0'
	template <typename T, typename U> constexpr bool AnySet(T value, U mask)
	{
		using Int = pr::underlying_type_t<T>;
		return (static_cast<Int>(value) & static_cast<Int>(mask)) != 0;
	}

	// Return true if all bits in 'value & mask == mask'
	template <typename T, typename U> constexpr bool AllSet(T value, U mask)
	{
		using Int = pr::underlying_type_t<T>;
		return (static_cast<Int>(value) & static_cast<Int>(mask)) == static_cast<Int>(mask);
	}

	// Reverse the order of bits in 'v'
	constexpr uint8 ReverseBits8(uint8 v)
	{
		//if constexpr(sizeof(void*) == 8)
			return static_cast<uint8>(((v * 0x80200802ULL) & 0x0884422110ULL) * 0x0101010101ULL >> 32);
		//else
		//	return static_cast<uint8>(((v * 0x0802LU & 0x22110LU) | (n * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
	}

	// Reverse the order of bits in 'v'
	constexpr uint32_t ReverseBits32(uint32_t v)
	{
		v = ((v >> 1) & 0x55555555) | ((v & 0x55555555) << 1);	// swap odd and even bits
		v = ((v >> 2) & 0x33333333) | ((v & 0x33333333) << 2);	// swap consecutive pairs
		v = ((v >> 4) & 0x0F0F0F0F) | ((v & 0x0F0F0F0F) << 4);	// swap nibbles ...
		v = ((v >> 8) & 0x00FF00FF) | ((v & 0x00FF00FF) << 8);	// swap bytes
		v = ( v >> 16             ) | ( v               << 16);	// swap 2-byte long pairs
		return v;
	}

	// Reverse the order of the lower 'n' bits in 'v'. e.g. ReverseBits32(0b00101101, 4) returns 0b00101011
	constexpr uint32_t ReverseBits32(uint32_t v, int n)
	{
		return (v & (0xFFFFFFFFU << n)) | (ReverseBits32(v) >> (32 - n));
	}

	// Reverse the order of bits in 'v'
	constexpr uint64_t ReverseBits64(uint64_t v)
	{
		v = ((v >>  1) & 0x5555555555555555ULL) | ((v & 0x5555555555555555ULL) <<  1); // swap odd and even bits
		v = ((v >>  2) & 0x3333333333333333ULL) | ((v & 0x3333333333333333ULL) <<  2); // swap consecutive pairs
		v = ((v >>  4) & 0x0F0F0F0F0F0F0F0FULL) | ((v & 0x0F0F0F0F0F0F0F0FULL) <<  4); // swap nibbles ...
		v = ((v >>  8) & 0x00FF00FF00FF00FFULL) | ((v & 0x00FF00FF00FF00FFULL) <<  8); // swap bytes
		v = ((v >> 16) & 0x0000FFFF0000FFFFULL) | ((v & 0x0000FFFF0000FFFFULL) << 16); // swap 2-byte pairs
		v = ( v >> 32                         ) | ( v                          << 32); // swap 4-byte pairs
		return v;
	}

	// Reverse the order of the lower 'n' bits in 'v'. e.g. ReverseBits32(0b00101101, 4) returns 0b00101011
	constexpr uint64_t ReverseBits64(uint64_t v, int n)
	{
		return (v & (0xFFFFFFFFFFFFFFFFULL << n)) | (ReverseBits64(v) >> (64 - n));
	}

	// Returns a bit mask containing only the lowest bit of 'n'
	template <typename T, typename = maths::enable_if_intg<T>> constexpr T LowBit(T n)
	{
		return n - ((n - 1) & n);
	}
	template <typename T, typename = maths::enable_if_enum<T>> constexpr T LowBit(T n, int = 0)
	{
		auto x = static_cast<std::underlying_type<T>::type>(n);
		return static_cast<T>(LowBit(x));
	}

	// Returns the bit position of the highest bit
	constexpr int HighBitIndex(uint64_t n)
	{
		unsigned int shift = 0, pos = 0;
		shift = ((n & 0xFFFFFFFF00000000ULL) != 0) << 5; n >>= shift; pos |= shift;
		shift = ((n &         0xFFFF0000ULL) != 0) << 4; n >>= shift; pos |= shift;
		shift = ((n &             0xFF00ULL) != 0) << 3; n >>= shift; pos |= shift;
		shift = ((n &               0xF0ULL) != 0) << 2; n >>= shift; pos |= shift;
		shift = ((n &                0xCULL) != 0) << 1; n >>= shift; pos |= shift;
		shift = ((n &                0x2ULL) != 0) << 0; n >>= shift; pos |= shift;
		return pos;
	}

	// Returns the bit position of the lowest bit
	constexpr int LowBitIndex(uint64_t n)
	{
		return HighBitIndex(LowBit(n));
	}

	// Return the integer log2 of 'n'
	constexpr int IntegerLog2(uint64_t n)
	{
		return HighBitIndex(n);

		// Doesn't work for values greater than (1 << 52)
		// // This only works for IEEE 64bit floats: [1:sign][11:exponent][52:fraction]
		// assert(n != 0);
		// auto v = static_cast<double const>(n);
		// auto c = *reinterpret_cast<long long const*>(&v);
		// return ((c >> 52) & 0x7FF) - 1023;
	}

	// Return a bit mask contain only the highest bit of 'n'
	template <typename T, typename = maths::enable_if_intg<T>> constexpr T HighBit(T n)
	{
		// Must be a faster way?
		return T(1) << HighBitIndex(n);
	}

	// Returns true if 'n' is a exact power of two
	template <typename T> constexpr bool IsPowerOfTwo(T n)
	{
		return ((n - 1) & n) == 0;
	}

	// Return the next highest power of two greater than 'n'
	template <typename T, typename = maths::enable_if_intg<T>> constexpr T PowerOfTwoGreaterThan(T n)
	{
		n--;
		n |= n >> 1;
		n |= n >> 2;
		n |= n >> 4;
		if constexpr (sizeof(T) > 1) n |= n >> 8;
		if constexpr (sizeof(T) > 2) n |= n >> 16;
		if constexpr (sizeof(T) > 4) n |= n >> 32;
		n++;
		return n;
	}

	// Returns the number of set bits in 'n'
	template <typename T> constexpr int CountBits(T n)
	{
		int count = 0;
		while (n)
		{
			++count;
			n &= (n - 1);
		}
		return count;
	}
	template <> constexpr int CountBits(unsigned int n)
	{
		// http://infolab.stanford.edu/~manku/bitcount/bitcount.html
		// Constant time bit count works for 32-bit numbers only.
		// Fix last line for 64-bit numbers
		unsigned int tmp = n
			- ((n >> 1) & 033333333333)
			- ((n >> 2) & 011111111111);
		return ((tmp + (tmp >> 3)) & 030707070707) % 63;
	}

	// Interleaves the lower 16 bits of x and y, so the bits of x
	// are in the even positions and bits from y in the odd.
	// Returns the resulting 32-bit Morton Number.
	inline unsigned int InterleaveBits(unsigned int x, unsigned int y)
	{
		const unsigned int B[] = {0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF};
		const unsigned int S[] = {1, 2, 4, 8};
		x = (x | (x << S[3])) & B[3];
		x = (x | (x << S[2])) & B[2];
		x = (x | (x << S[1])) & B[1];
		x = (x | (x << S[0])) & B[0];
		y = (y | (y << S[3])) & B[3];
		y = (y | (y << S[2])) & B[2];
		y = (y | (y << S[1])) & B[1];
		y = (y | (y << S[0])) & B[0];
		return x | (y << 1);
	}

	// Convert a string of 1s and 0s into a bitmask
	template <typename T> inline T Bits(char const* bits)
	{
		T n = 0;
		for (;*bits != 0; ++bits)
			n = (n << 1) | T(*bits == '1');
		return n;
	}

	// Convert an integral type into a string of 0s and 1s
	template <typename T> inline char const* BitStr(T bits, bool leading_zeros = false)
	{
		thread_local static char str[sizeof(T) * 8 + 1];
		typedef std::make_unsigned<T>::type UT;
		UT mask = UT(1) << (sizeof(T) * 8 - 1);
		
		if (!leading_zeros) // skip leading zeros
			for (; (mask & bits) == 0; mask >>= 1) {}

		char* ptr = str;
		if (mask == 0)
		{
			*ptr++ = '0';
		}
		else
		{
			for (; mask != 0; mask >>= 1)
				*ptr++ = (mask & bits) != 0 ? '1' : '0';
		}
		*ptr = 0;
		return str;
	}

	// Iterator for iterating over a bit mask
	template <typename T> struct bit_citer
	{
		typedef std::forward_iterator_tag iterator_category;
		typedef T value_type;

		value_type m_bits;
		bit_citer(value_type bits) :m_bits(bits) {}

		value_type    operator * () const                   { return LowBit(m_bits); }
		bit_citer<T>& operator ++()                         { m_bits ^= LowBit(m_bits); return *this; }
		bit_citer<T>  operator ++(int)                      { bit_citer<T> iter(m_bits); ++*this; return iter; }
		bool          operator == (bit_citer<T> const& rhs) { return m_bits == rhs.m_bits; }
		bool          operator != (bit_citer<T> const& rhs) { return m_bits != rhs.m_bits; }
	};

	// An iterator over bits in a bit mask
	template <typename T> struct BitEnumerator
	{
		T m_bits;
		BitEnumerator(T bits) :m_bits(bits) {}
		bit_citer<T> begin() const { return bit_citer<T>(m_bits); }
		bit_citer<T> end() const   { return bit_citer<T>(static_cast<T>(0)); }
	};

	// Returns an object for enumerating over the set bits in 'bits'
	template <typename T> BitEnumerator<T> EnumerateBits(T bits)
	{
		return BitEnumerator<T>(bits);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(BitFunctionTest)
	{
		{
			uint32_t hi,lo;
			BreakLL(0x0123456789abcdef, hi, lo);
			PR_CHECK(hi, 0x01234567U);
			PR_CHECK(lo, 0x89abcdefU);

			auto ll = MakeLL(hi, lo);
			PR_CHECK(ll, 0x0123456789abcdefULL);
		}
		{
			char const* mask_str = "1001010011";
			auto mask = Bits<long long>(mask_str);
			PR_CHECK(BitStr(mask), mask_str);

			std::vector<long long> bits;
			for (auto b : pr::EnumerateBits(mask))
				bits.push_back(b);

			PR_CHECK(bits.size(), 5U);
			PR_CHECK(bits[0], 1U << 0);
			PR_CHECK(bits[1], 1U << 1);
			PR_CHECK(bits[2], 1U << 4);
			PR_CHECK(bits[3], 1U << 6);
			PR_CHECK(bits[4], 1U << 9);
		}
		{
			auto n0 = 0b1; // 1
			PR_CHECK(IntegerLog2(n0), 0);
			auto n1 = 0b10; // 2
			PR_CHECK(IntegerLog2(n1), 1);
			auto n2 = 0b1000000; // 64
			PR_CHECK(IntegerLog2(n2), 6);
			auto n3 = 0b101010101010101ULL; // 21845
			PR_CHECK(IntegerLog2(n3), 14);
			auto n4 = 0xFFFFFFFFFFFFFFFFULL; // 18446744073709551615
			PR_CHECK(IntegerLog2(n4), 63);
		}
		{
			char const* mask_str = "1001110010";
			auto mask = Bits<uint32_t>(mask_str);
			PR_CHECK(mask, 626U);
			PR_CHECK(BitStr(mask), mask_str);
			PR_CHECK(HighBitIndex(mask), 9);
			PR_CHECK(LowBitIndex(mask), 1);
			PR_CHECK(LowBit(mask), 2U);
			PR_CHECK(HighBit(mask), 0x200U);
		}
		{
			char const* mask_str = "1111010100010";
			auto mask = Bits<short>(mask_str);
			PR_CHECK(mask, 7842);
			PR_CHECK(HighBitIndex(mask), 12);
			PR_CHECK(LowBitIndex(mask), 1);
			PR_CHECK(LowBit(mask), 2);
			PR_CHECK(HighBit(mask), 0x1000);
		}
		{
			char const* mask_str = "1001001100110010101010010100111010010110010101110110000110100100";
			auto mask = Bits<unsigned long long>(mask_str);
			PR_CHECK(mask, 0x9332A94E965761A4ULL);
			PR_CHECK(HighBitIndex(mask), 63);
			PR_CHECK(LowBitIndex(mask), 2);
			PR_CHECK(LowBit(mask), 4);
			PR_CHECK(HighBit(mask), 0x8000000000000000ULL);
		}
		{
			PR_CHECK(PowerOfTwoGreaterThan(0x12345678), 0x20000000);
			PR_CHECK(PowerOfTwoGreaterThan(0x9876543210UL), 0x10000000000UL);
			PR_CHECK(PowerOfTwoGreaterThan(uint8_t(0x9a)), uint8_t(0x00));
			PR_CHECK(PowerOfTwoGreaterThan(uint16_t(0x9a)), uint16_t(0x100));
		}
		{
			auto a = uint8(0b10110101);
			auto b = uint8(0b10101101);
			auto c = ReverseBits8(a);
			PR_CHECK(b, c);
		}
		{            //01234567890123456789012345678901 32bits
			auto a = 0b01100011110000011111100000001111U;

			auto b = ReverseBits32(a);
			auto B = 0b11110000000111111000001111000110U;
			PR_CHECK(b, B);

			auto c = ReverseBits32(a, 8); // just the lower 8 bits
			auto C = 0b01100011110000011111100011110000U;
			PR_CHECK(c, C);
		}
		{            //0123456789_123456789_123456789_123456789_123456789_123456879_123 64bits
			auto a = 0b0110001111000001111110000000111111110000000001111111111000000000ULL;

			auto b = ReverseBits64(a);
			auto B = 0b0000000001111111111000000000111111110000000111111000001111000110ULL;
			PR_CHECK(b, B);

			auto c = ReverseBits64(a, 12); // just the lower 12 bits
			auto C = 0b0110001111000001111110000000111111110000000001111111000000000111ULL;
			PR_CHECK(c, C);
		}
	}
}
#endif
