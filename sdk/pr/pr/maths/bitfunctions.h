//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_BIT_FUNCTIONS_H
#define PR_MATHS_BIT_FUNCTIONS_H

#include "pr/maths/forward.h"

//http://graphics.stanford.edu/~seander/bithacks.html

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
		value |=  mask & bitfield; // set bits from bitfield
		return value;
	}
	
	// Returns true if any bits in 'value & mask != 0'
	template <typename T, typename U> inline bool AnySet(T value, U mask)
	{
		return (value & static_cast<T>(mask)) != 0;
	}
	
	// Return true if all bits in 'value & mask == mask'
	template <typename T, typename U> inline bool AllSet(T value, U mask)
	{
		return (value & static_cast<T>(mask)) == static_cast<T>(mask);
	}
	
	// Reverse the order of bits in 'n'
	inline uint8 ReverseBits(uint8 n)
	{
		return static_cast<uint8>(((n * 0x0802LU & 0x22110LU) | (n * 0x8020LU & 0x88440LU)) * 0x10101LU >> 16);
	}
	
	// Reverse the order of bits in 'n'
	inline uint ReverseBits(uint n)
	{
		n = ((n >> 1) & 0x55555555) | ((n & 0x55555555) << 1);	// swap odd and even bits
		n = ((n >> 2) & 0x33333333) | ((n & 0x33333333) << 2);	// swap consecutive pairs
		n = ((n >> 4) & 0x0F0F0F0F) | ((n & 0x0F0F0F0F) << 4);	// swap nibbles ... 
		n = ((n >> 8) & 0x00FF00FF) | ((n & 0x00FF00FF) << 8);	// swap bytes
		n = ( n >> 16             ) | ( n               << 16);	// swap 2-byte long pairs
		return n;
	}
	
	// Returns a bit mask containing only the lowest bit of 'n'
	template <typename T> inline T LowBit(T n)
	{
		return n - ((n - 1) & n);
	}
	
	// Returns the bit position of the highest bit
	// Also, is the floor of the log base 2 for a 32 bit integer
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
	inline uint LowBitIndex(uint n)
	{
		return HighBitIndex(LowBit(n));
	}
	
	// Return a bit mask contain only the highest bit of 'n'
	// Must be a faster way?
	inline uint HitBit(uint n)
	{
		return 1 << HighBitIndex(n);
	}
	
	// Returns true if 'n' is a exact power of two
	template <typename T> inline bool IsPowerOfTwo(T n)
	{
		return ((n - 1) & n) == 0;
	}
	
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
	
	inline unsigned int Bits(char const* bits)
	{
		unsigned int n = 0;
		for (;*bits != 0; ++bits)
			n = (n << 1) | int(*bits == '1');
		return n;
	}
	
	// Convert up to 32bits to a string of 0's and 1's
	inline char const* Bitstr(unsigned int bits, int count)
	{
		static char str[33];
		int i;
		
		bits <<= (32 - count);
		for (i = 0; i != count; ++i, bits <<= 1)
			str[i] = '0' + ((bits & 0x80000000) != 0);
		str[count] = 0;
		return str;
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
		}
	}
}
#endif

#endif
