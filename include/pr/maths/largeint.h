//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once

#include <cassert>
#include "pr/common/fmt.h"
#include "pr/maths/forward.h"
#include "pr/maths/bitfunctions.h"
#include "pr/str/prstring.h"

namespace pr
{
	struct LargeInt
	{
		enum { MaxLength = 16 };
		unsigned int data[MaxLength];	// data are big endian. i.e. LSB is at data[MaxLength - 1]

		unsigned int const& uint() const { return data[MaxLength - 1]; }
		unsigned int& uint()             { return data[MaxLength - 1]; }
		LargeInt& set(unsigned int* array, unsigned int length);
		LargeInt& operator = (unsigned int value);
	};

	// Forward declare operators
	LargeInt& operator += (LargeInt& lhs, unsigned int rhs);
	LargeInt& operator -= (LargeInt& lhs, unsigned int rhs);
	LargeInt& operator *= (LargeInt& lhs, unsigned int rhs);
	LargeInt& operator /= (LargeInt& lhs, unsigned int rhs);
	LargeInt& operator <<=(LargeInt& lhs, unsigned int rhs);
	LargeInt& operator >>=(LargeInt& lhs, unsigned int rhs);
	LargeInt& operator %= (LargeInt& lhs, unsigned int rhs);

	LargeInt& operator += (LargeInt& lhs, LargeInt const& rhs);
	LargeInt& operator -= (LargeInt& lhs, LargeInt const& rhs);
	LargeInt& operator *= (LargeInt& lhs, LargeInt const& rhs);
	LargeInt& operator /= (LargeInt& lhs, LargeInt const& rhs);
	LargeInt& operator %= (LargeInt& lhs, LargeInt const& rhs);

	LargeInt operator +  (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator -  (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator *  (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator *  (unsigned int lhs, LargeInt const& rhs);
	LargeInt operator /  (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator << (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator >> (LargeInt const& lhs, unsigned int rhs);
	LargeInt operator %  (LargeInt const& lhs, unsigned int rhs);

	LargeInt operator + (LargeInt const& lhs, LargeInt const& rhs);
	LargeInt operator - (LargeInt const& lhs, LargeInt const& rhs);
	LargeInt operator * (LargeInt const& lhs, LargeInt const& rhs);
	LargeInt operator / (LargeInt const& lhs, LargeInt const& rhs);
	LargeInt operator % (LargeInt const& lhs, LargeInt const& rhs);

	LargeInt const LargeIntZero = LargeInt();
	LargeInt const LargeIntOne  = LargeIntZero + 1;

	// This should be an array such as:
	// unsigned int array[] = {0x01234567, 0x89ABCDEF};
	inline LargeInt& LargeInt::set(unsigned int* array, unsigned int length)
	{
		assert(length <= MaxLength);
		memset(data, 0, sizeof(data));
		memcpy(data + MaxLength - length, array, sizeof(unsigned int) * length);
		return *this;
	}
	inline LargeInt& LargeInt::operator = (unsigned int value)
	{
		memset(data, 0, sizeof(data));
		data[MaxLength - 1] = value;
		return *this;
	}

	// Equality operators
	inline bool operator == (LargeInt const& lhs, LargeInt const& rhs)	{ return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (LargeInt const& lhs, LargeInt const& rhs)	{ return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (LargeInt const& lhs, LargeInt const& rhs)
	{
		unsigned int count = LargeInt::MaxLength;
		const unsigned int *l_ptr = lhs.data, *r_ptr = rhs.data;
		while( count && *l_ptr == *r_ptr ) { --count; ++l_ptr; ++r_ptr; }
		return count && *l_ptr < *r_ptr;
	}
	inline bool operator >  (LargeInt const& lhs, LargeInt const& rhs)
	{
		unsigned int count = LargeInt::MaxLength;
		const unsigned int *l_ptr = lhs.data, *r_ptr = rhs.data;
		while( count && *l_ptr == *r_ptr ) { --count; ++l_ptr; ++r_ptr; }
		return count && *l_ptr > *r_ptr;
	}
	inline bool operator <= (LargeInt const& lhs, LargeInt const& rhs)
	{
		unsigned int count = LargeInt::MaxLength;
		const unsigned int *l_ptr = lhs.data, *r_ptr = rhs.data;
		while( count && *l_ptr == *r_ptr ) { --count; ++l_ptr; ++r_ptr; }
		return !count || *l_ptr < *r_ptr;
	}
	inline bool operator >= (LargeInt const& lhs, LargeInt const& rhs)
	{
		unsigned int count = LargeInt::MaxLength;
		const unsigned int *l_ptr = lhs.data, *r_ptr = rhs.data;
		while( count && *l_ptr == *r_ptr ) { --count; ++l_ptr; ++r_ptr; }
		return !count || *l_ptr > *r_ptr;
	}

	// Returns log2(n) (i.e. the position of the highest bit)
	//inline unsigned int HighBit(unsigned int n)	// Defined elsewhere in my stuff
	//{
	//	register unsigned int r = 0;
	//	register unsigned int shift;
	//	shift = ( ( n & 0xFFFF0000 ) != 0 ) << 4; n >>= shift; r |= shift;
	//	shift = ( ( n & 0xFF00     ) != 0 ) << 3; n >>= shift; r |= shift;
	//	shift = ( ( n & 0xF0       ) != 0 ) << 2; n >>= shift; r |= shift;
	//	shift = ( ( n & 0xC        ) != 0 ) << 1; n >>= shift; r |= shift;
	//	shift = ( ( n & 0x2        ) != 0 ) << 0; n >>= shift; r |= shift;
	//	return r;
	//}
	inline unsigned int HighBit(LargeInt const& n)
	{
		for( const unsigned int* word = n.data; word != n.data + LargeInt::MaxLength; ++word )
		{
			if (*word) return HighBitIndex(*word) + (LargeInt::MaxLength - 1 - (unsigned int)(word - n.data)) * 32;
		}
		return 0;
	}

	// Assignment operators
	inline LargeInt& operator += (LargeInt& lhs, unsigned int rhs)
	{
		unsigned int* word = lhs.data + LargeInt::MaxLength - 1;
		while( (*word + rhs) < *word )
		{
			*word = *word + rhs;
			--word;
			rhs = 1;
			if( word < lhs.data ) return lhs;
		}
		*word = *word + rhs;
		return lhs;
	}
	inline LargeInt& operator -= (LargeInt& lhs, unsigned int rhs)
	{
		unsigned int* word = lhs.data + LargeInt::MaxLength - 1;
		while( (*word - rhs) > *word )
		{
			*word = *word - rhs;
			--word;
			rhs = 1;
			if( word < lhs.data ) return lhs;
		}
		*word = *word - rhs;
		return lhs;
	}
	inline LargeInt& operator *= (LargeInt& lhs, unsigned int rhs)
	{
		unsigned int carry = 0;
		for( unsigned int* word = lhs.data + LargeInt::MaxLength - 1; word >= lhs.data; --word )
		{
			unsigned __int64 product = *word;
			product *= rhs;
			product += carry;
			*word = static_cast<unsigned int>(product);
			carry = static_cast<unsigned int>(product >> 32);
		}
		return lhs;
	}
	inline LargeInt& operator /= (LargeInt& lhs, unsigned int rhs)
	{
		LargeInt rvalue;
		rvalue = rhs;
		return lhs /= rvalue;
	}
	inline LargeInt& operator <<=(LargeInt& lhs, unsigned int rhs)
	{
		unsigned int* out			= lhs.data;
		unsigned int* in			= out + rhs / 32;
		unsigned int  shift_up		= rhs % 32;
		unsigned int  shift_down	= 32 - shift_up;

		while( in < lhs.data + LargeInt::MaxLength - 1 )
		{
			unsigned __int64 v1 = *in;
			unsigned __int64 v2 = *(in + 1);
			v1 <<= shift_up;
			v2 >>= shift_down;
			*out = static_cast<unsigned int>(v1 | v2);
			++out;
			++in;
		}
		if( in < lhs.data + LargeInt::MaxLength )
		{
			*out = (*in << shift_up);
			++out;
		}
		while( out != lhs.data + LargeInt::MaxLength )
		{
			*out = 0;
			++out;
		}
		return lhs;
	}
	inline LargeInt& operator >>=(LargeInt& lhs, unsigned int rhs)
	{
		unsigned int* out			= lhs.data + LargeInt::MaxLength - 1;
		unsigned int* in			= out - rhs / 32;
		unsigned int  shift_down	= rhs % 32;
		unsigned int  shift_up		= 32 - shift_down;

		while( in > lhs.data )
		{
			unsigned __int64 v1 = *in;
			unsigned __int64 v2 = *(in - 1);
			v1 >>= shift_down;
			v2 <<= shift_up;
			*out = static_cast<unsigned int>(v1 | v2);
			--out;
			--in;
		}
		if( in >= lhs.data )
		{
			*out = (*in >> shift_down);
			--out;
		}
		while( out >= lhs.data )
		{
			*out = 0;
			--out;
		}
		return lhs;
	}

	inline LargeInt& operator %= (LargeInt& lhs, unsigned int rhs)
	{
		LargeInt div = lhs / rhs;
		return lhs -= div * rhs;
	}

	// LargeInt assignment operators
	inline LargeInt& operator += (LargeInt& lhs, LargeInt const& rhs)
	{
		const unsigned int* in  = rhs.data + LargeInt::MaxLength - 1;
		      unsigned int* out = lhs.data + LargeInt::MaxLength - 1;
		__int64 carry = 0;
		for( int i = 0; i != LargeInt::MaxLength; ++i )
		{
			carry += *out;
			carry += *in;
			*out = static_cast<unsigned int>(carry);
			carry >>= 32;
			--out;
			--in;
		}
		return lhs;
	}
	inline LargeInt& operator -= (LargeInt& lhs, LargeInt const& rhs)
	{
		const unsigned int* in  = rhs.data + LargeInt::MaxLength - 1;
		      unsigned int* out = lhs.data + LargeInt::MaxLength - 1;
		__int64 carry = 0;
		for( int i = 0; i != LargeInt::MaxLength; ++i )
		{
			carry += *out;
			carry -= *in;
			*out = static_cast<unsigned int>(carry);
			carry >>= 32;
			--out;
			--in;
		}
		return lhs;
	}
	inline LargeInt& operator *= (LargeInt& lhs, LargeInt const& rhs)		{ lhs = lhs * rhs; return lhs; }
	inline LargeInt& operator /= (LargeInt& lhs, LargeInt const& rhs)		{ lhs = lhs / rhs; return lhs; }
	inline LargeInt& operator %= (LargeInt& lhs, LargeInt const& rhs)		{ LargeInt div = lhs / rhs; return lhs -= div * rhs; }

	inline LargeInt operator +  (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp += rhs; }
	inline LargeInt operator -  (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp -= rhs; }
	inline LargeInt operator *  (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp *= rhs; }
	inline LargeInt operator *  (unsigned int lhs, LargeInt const& rhs)		{ LargeInt tmp = rhs; return tmp *= lhs; }
	inline LargeInt operator /  (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp /= rhs; }
	inline LargeInt operator << (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp <<= rhs; }
	inline LargeInt operator >> (LargeInt const& lhs, unsigned int rhs)		{ LargeInt tmp = lhs; return tmp >>= rhs; }
	inline LargeInt operator %  (LargeInt const& lhs, unsigned int rhs)		{ LargeInt div = lhs / rhs; return lhs - div * rhs; }

	inline LargeInt operator + (LargeInt const& lhs, LargeInt const& rhs)	{ LargeInt tmp = lhs; return tmp += rhs; }
	inline LargeInt operator - (LargeInt const& lhs, LargeInt const& rhs)	{ LargeInt tmp = lhs; return tmp -= rhs; }
	inline LargeInt operator * (LargeInt const& lhs, LargeInt const& rhs)
	{
		LargeInt accumulator = LargeIntZero;
		unsigned __int64 product;
		for( int j = 0; j != LargeInt::MaxLength; ++j )
		{
			unsigned int carry = 0;
			const unsigned int* lvalue = lhs.data + LargeInt::MaxLength - 1;
			const unsigned int* rvalue = rhs.data + LargeInt::MaxLength - 1 - j;
			for( unsigned int* acc = accumulator.data + LargeInt::MaxLength - 1 - j; acc >= accumulator.data; --acc, --lvalue )
			{
				product  = *lvalue;
				product *= *rvalue;
				product += carry;
				*acc += static_cast<unsigned int>(product);
				carry = static_cast<unsigned int>(product >> 32);
			}
		}
		return accumulator;
	}
	inline LargeInt operator / (LargeInt const& lhs, LargeInt const& rhs)
	{
		unsigned int highest_bit_lhs = HighBit(lhs);
		unsigned int highest_bit_rhs = HighBit(rhs);
		if( highest_bit_rhs > highest_bit_lhs ) { return LargeIntZero; }
		int right_shift = static_cast<int>(highest_bit_lhs - highest_bit_rhs);

		LargeInt result	= LargeIntZero;
		LargeInt numer	= lhs;
		LargeInt denom	= rhs; denom <<= right_shift;

		while( right_shift >= 0 )
		{
			unsigned int highest_bit_denom = HighBit(denom);
			unsigned int highest_bit_numer = HighBit(numer);
			if( highest_bit_denom > highest_bit_numer )
			{
				unsigned int diff =  highest_bit_denom - highest_bit_numer;
				denom >>= diff;
				right_shift -= diff;
			}
			while( numer < denom )
			{
				denom >>= 1;
				--right_shift;
				if( right_shift < 0 ) { return result; }
			}
			numer -= denom;
			result += LargeIntOne << right_shift;
			denom >>= 1;
			--right_shift;
		}
		return result;
	}
	inline LargeInt operator % (LargeInt const& lhs, LargeInt const& rhs)	{ LargeInt div = lhs / rhs; return lhs - div * rhs; }

	//inline unsigned int operator % (LargeInt const& lhs, unsigned int rhs)
	//{
	//	lhs;
	//	rhs;
	//	LargeInt result;
	//	unsigned int reminder = 0;
	//	for (S32 i=MAXLENGTH-1; i >= 0; i--)
	//	{
	//		U64 destValue = reminder;
	//		destValue <<= 32;
	//		destValue += (U64)(*(u32Array + i));
	//		result.u32Array[i] = destValue / source;
	//		reminder = destValue % source;
	//	}
	//	return reminder;
	//}

	//inline static unsigned int divide(unsigned int* dest, unsigned int source)
	//{
	//	LargeInt result;
	//	unsigned int reminder = 0;
	//	for (S32 i=MAXLENGTH-1; i >= 0; i--)
	//	{
	//		U64 destValue = reminder;
	//		destValue <<= 32;
	//		destValue += (U64)(*(dest + i));
	//		result.u32Array[i] = destValue / source;
	//		reminder = destValue % source;
	//	}
	//	return reminder;
	//}

	inline std::string ToString(LargeInt const& large_int)
	{
		std::string str;
		for( int i = 0; i != LargeInt::MaxLength; ++i )
		{
			str += Fmt("%8.8X", large_int.data[i]);
		}
		int leading_zeros = 0;
		for( const char *in = str.c_str(), *in_end = str.c_str() + str.size() - 1; in != in_end; ++in )
		{
			if( *in != '0' ) break;
			++leading_zeros;
		}
		return str.c_str() + leading_zeros;
	}

	//inline void toFile(const FILE* file)
	//{
	//	for (S32 i=MAXLENGTH-1; i>=0; i--)
	//		for (S32 j=3; j>=0; j--)
	//			fputc((u32Array[i] >> (j << 3)) & 0xFF, file);
	//}

	//inline void fromFile(const FILE* file)
	//{
	//	for (S32 i=MAXLENGTH-1; i>=0; i--)
	//	{
	//		u32Array[i] = 0;
	//		for (S32 j=3; j>=0; j--)
	//			u32Array[i] |= ((unsigned int)fgetc(file)) << (j << 3);
	//	}
	//}
}
