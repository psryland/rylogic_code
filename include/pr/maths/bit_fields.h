//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
//http://graphics.stanford.edu/~seander/bithacks.html
#pragma once

#include "pr/maths/forward.h"

namespace pr
{
	// Bit manipulation functions
	inline uint Bit32(int n)
	{
		return 1U << n;
	}
	inline uint64 Bit64(int n)
	{
		return 1i64 << n;
	}

	// If 'state' is true, returns 'value | mask'. If false, returns 'value &~ mask'
	template <typename T, typename U> inline T SetBits(T value, U mask, bool state)
	{
		return state ? value | static_cast<T>(mask) : value & ~static_cast<T>(mask);
	}

	// Sets the masked bits of 'value' to the state 'bitfield'
	template <typename T, typename U> inline T SetBits(T value, U mask, U bitfield)
	{
		value &= ~mask;            // clear masked bits to zero
		value |=  mask & bitfield; // set bits from bit field
		return value;
	}

	// Returns true if any bits in 'value & mask != 0'
	template <typename T, typename U> inline bool AnySet(T value, U mask)
	{
		return (value & T(mask)) != T();
	}

	// Return true if all bits in 'value & mask == mask'
	template <typename T, typename U> inline bool AllSet(T value, U mask)
	{
		return (value & T(mask)) == T(mask);
	}

	// Reverse the order of bits in 'v'
	inline uint8 ReverseBits8(uint8 v)
	{
		//if constexpr(sizeof(void*) == 8)
			return static_cast<uint8>(((v * 0x80200802ULL) & 0x0884422110ULL) * 0x0101010101ULL >> 32);
		//else
		//	return static_cast<uint8>(((v * 0x0802LU & 0x22110LU) | (n * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
	}

	// Reverse the order of bits in 'v'
	inline uint ReverseBits32(uint v)
	{
		v = ((v >> 1) & 0x55555555) | ((v & 0x55555555) << 1);	// swap odd and even bits
		v = ((v >> 2) & 0x33333333) | ((v & 0x33333333) << 2);	// swap consecutive pairs
		v = ((v >> 4) & 0x0F0F0F0F) | ((v & 0x0F0F0F0F) << 4);	// swap nibbles ...
		v = ((v >> 8) & 0x00FF00FF) | ((v & 0x00FF00FF) << 8);	// swap bytes
		v = ( v >> 16             ) | ( v               << 16);	// swap 2-byte long pairs
		return v;
	}

	// Reverse the order of the lower 'n' bits in 'v'. e.g. ReverseBits32(0b00101101, 4) returns 0b00101011
	inline uint ReverseBits32(uint v, int n)
	{
		return (v & (0xFFFFFFFFU << n)) | (ReverseBits32(v) >> (32 - n));
	}

	// Reverse the order of bits in 'v'
	inline uint64 ReverseBits64(uint64 v)
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
	inline uint64 ReverseBits64(uint64 v, int n)
	{
		return (v & (0xFFFFFFFFFFFFFFFFULL << n)) | (ReverseBits64(v) >> (64 - n));
	}

	// Returns a bit mask containing only the lowest bit of 'n'
	template <typename T, typename = std::enable_if<std::is_integral<T>::value>::type> inline T LowBit(T n)
	{
		return n - ((n - 1) & n);
	}
	template <typename T, typename = std::enable_if<std::is_enum<T>::value>::type> inline T LowBit(T n, int = 0)
	{
		auto x = static_cast<std::underlying_type<T>::type>(n);
		return static_cast<T>(LowBit(x));
	}

	// Returns the bit position of the highest bit
	// Also, is the floor of the log base 2 for a 32 bit integer
	template <typename T> inline T HighBitIndex(T n)
	{
		typedef std::make_unsigned<T>::type UT;

		T pos = 0;
		int bits = 4 * sizeof(T);
		for (UT mask = UT(~0) >> bits; bits; bits >>= 1, mask >>= bits)
		{
			auto shift = ((n & ~mask) != 0) * bits;
			n >>= shift;
			pos |= shift;
		}
		return pos;
	}
	inline uint HighBitIndex(uint n)
	{
		unsigned int shift, pos = 0;
		shift = ((n & 0xFFFF0000) != 0) << 4; n >>= shift; pos |= shift;
		shift = ((n &     0xFF00) != 0) << 3; n >>= shift; pos |= shift;
		shift = ((n &       0xF0) != 0) << 2; n >>= shift; pos |= shift;
		shift = ((n &        0xC) != 0) << 1; n >>= shift; pos |= shift;
		shift = ((n &        0x2) != 0) << 0; n >>= shift; pos |= shift;
		return pos;
	}

	// Returns the bit position of the lowest bit
	template <typename T> inline T LowBitIndex(T n)
	{
		return HighBitIndex(LowBit(n));
	}

	// Return a bit mask contain only the highest bit of 'n'
	// Must be a faster way?
	template <typename T> inline T HighBit(T n)
	{
		return T(1) << HighBitIndex(n);
	}

	// Returns true if 'n' is a exact power of two
	template <typename T> inline bool IsPowerOfTwo(T n)
	{
		return ((n - 1) & n) == 0;
	}

	// Returns the number of set bits in 'n'
	template <typename T> inline uint CountBits(T n)
	{
		uint count = 0;
		while (n)
		{
			++count;
			n &= (n - 1);
		}
		return count;
	}

	// http://infolab.stanford.edu/~manku/bitcount/bitcount.html
	// Constant time bit count works for 32-bit numbers only.
	// Fix last line for 64-bit numbers
	template <> inline uint CountBits(unsigned int n)
	{
		unsigned int tmp;
		tmp = n - ((n >> 1) & 033333333333)
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
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_bitfunctions)
		{
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
				char const* mask_str = "1001110010";
				auto mask = Bits<uint>(mask_str);
				PR_CHECK(mask, 626U);
				PR_CHECK(BitStr(mask), mask_str);
				PR_CHECK(HighBitIndex(mask), 9U);
				PR_CHECK(LowBitIndex(mask), 1U);
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
				PR_CHECK(LowBitIndex(mask), 2U);
				PR_CHECK(LowBit(mask), 4);
				PR_CHECK(HighBit(mask), 0x8000000000000000ULL);
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
}
#endif
