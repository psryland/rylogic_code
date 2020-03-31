//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <cstdint>
#include <initializer_list>
#include <cassert>
#include "pr/common/fmt.h"
#include "pr/maths/forward.h"
#include "pr/maths/bit_fields.h"

namespace pr
{
	template <int SizeInBytes> struct LargeInt
	{
		// Notes:
		//  - WIP - not implemented yet
		//  - Memory layout: [LSW:sign_bit, Word1, Word2, ... , MSW]
		//  - Not using two's complement because it won't work if I change to using runtime values of N.
		//  - Using the varint technique of storing the sign bit in the LSB of m_buf[0],
		//    so values will look like they are 2x the actual value.
		//  - Signed because it's more useful than unsigned.
		//  - No typedefs because the implementation is specific to uint32_t

		using buffer_t = std::array<uint32_t, SizeInBytes / sizeof(uint32_t)>;
		static constexpr int Bits32 = sizeof(uint32_t) * 8;
		static constexpr int Bits64 = sizeof(uint64_t) * 8;
		static constexpr int Bits31 = Bits32 - 1;
		static constexpr int Bits63 = Bits64 - 1;

		// Buffer that contains the int data
		buffer_t m_buf;

		LargeInt() = default;
		LargeInt(int64_t value)
		{
			assign(value);
		}
		LargeInt(std::initializer_list<uint32_t> data)
		{
			load(data);
		}
		LargeInt& operator = (int64_t value)
		{
			assign(value);
			return *this;
		}

		// Zero the large int
		void zero()
		{
			memset(m_buf.data(), 0, m_buf.size() * sizeof(uint32_t));
		}

		// Assign a value
		void assign(int64_t value)
		{
			ensure_space(2);
			zero();

			// ZigZag encode
			auto zz = static_cast<uint64_t>((value << 1) ^ (value >> Bits63));
			m_buf[1] = static_cast<uint32_t>(zz >> Bits32);
			m_buf[0] = static_cast<uint32_t>(zz >> 0);
		}

		// Cast the LargeInt to a word or dword, throws on overflow
		explicit operator int64_t() const
		{
			if (m_buf.size() > 2 && or_words(2, static_cast<int>(m_buf.size() - 2)) != 0)
				throw std::overflow_error("LargeInt magnitude is larger than the dword size");

			// ZigZag decode
			auto value = (static_cast<uint64_t>(m_buf[1]) << Bits32) | m_buf[0];
			return (value & 1) ? ((value >> 1) ^ -1) : (value >> 1);
		}
		explicit operator int32_t() const
		{
			if (m_buf.size() > 1 && or_words(1, static_cast<int>(m_buf.size() - 1)) != 0)
				throw std::overflow_error("LargeInt magnitude is larger than the word size");

			// ZigZag decode
			auto value = m_buf[0];
			return (value & 1) ? ((value >> 1) ^ -1) : (value >> 1);
		}

		#pragma region Operators
		friend LargeInt operator + (LargeInt const& lhs)
		{
			return lhs;
		}
		friend LargeInt operator - (LargeInt const& lhs)
		{
			auto out = lhs;
			out.m_buf[0] ^= 3;
			return out;
		}
		friend LargeInt operator ~ (LargeInt const& lhs)
		{
			auto out = lhs;
			for (auto& w : out.m_buf) w = ~w;
			return out;
		}
		friend LargeInt operator ! (LargeInt const& lhs)
		{
			return lhs.or_words(0, static_cast<int>(lhs.m_buf.size())) == 0;
		}
		friend bool operator == (LargeInt const& lhs, LargeInt const& rhs)
		{
			// Find the highest non-zero MSW
			auto i = lhs.m_buf.size();
			auto j = rhs.m_buf.size();
			for (; i > j && lhs.m_buf[i--] == 0; ) {}
			for (; j > i && rhs.m_buf[j--] == 0; ) {}
			return i == j && memcmp(lhs.m_buf.data(), rhs.m_buf.data(), sizeof(uint32_t) * i) == 0;
		}
		friend bool operator != (LargeInt const& lhs, LargeInt const& rhs)
		{
			return !(lhs == rhs);
		}
		friend bool operator <  (LargeInt const& lhs, LargeInt const& rhs)
		{
			// Find the highest non-zero MSW
			auto i = lhs.m_buf.size();
			auto j = rhs.m_buf.size();
			for (; i > j && lhs.m_buf[i--] == 0; ) {}
			for (; j > i && rhs.m_buf[j--] == 0; ) {}
			if (i != j)
				return (lhs.sign() * i) < (rhs.sign() * j);

			auto sign_mask = i == 0 ? ~1 : ~0;
			return
				(lhs.sign() * (lhs.m_buf[i] & sign_mask)) <
				(rhs.sign() * (rhs.m_buf[j] & sign_mask));
		}
		friend bool operator >  (LargeInt const& lhs, LargeInt const& rhs)
		{
			auto i = lhs.m_buf.size();
			auto j = rhs.m_buf.size();
			
			// Find the highest non-zero MSW
			for (; i > j && lhs.m_buf[i--] == 0; ) {}
			for (; j > i && rhs.m_buf[j--] == 0; ) {}
			if (i != j)
				return (lhs.sign() * i) > (rhs.sign() * j);

			auto sign_mask = i == 0 ? ~1 : ~0;
			return
				(lhs.sign() * (lhs.m_buf[i] & sign_mask)) >
				(rhs.sign() * (rhs.m_buf[j] & sign_mask));
		}
		friend bool operator <= (LargeInt const& lhs, LargeInt const& rhs)
		{
			return !(lhs > rhs);
		}
		friend bool operator >= (LargeInt const& lhs, LargeInt const& rhs)
		{
			return !(lhs < rhs);
		}
		friend LargeInt& operator += (LargeInt& lhs, LargeInt const& rhs)
		{
			int64_t carry = 0;
			auto sl = lhs.sign();
			auto sr = rhs.sign();
			auto nl = static_cast<int>(lhs.m_buf.size());
			auto nr = static_cast<int>(rhs.m_buf.size());
			for (int l = 0, r = 0, i = 0;; l += l != nl, r != r != nr, ++i)
			{
				carry += l < nl ? lhs.m_buf[l] : 0;
				carry += r < nr ? rhs.m_buf[r] : 0;
				lhs.m_buf[i] = static_cast<uint32_t>(carry);
				carry >>= Bits32;
			}
			(void)sl,sr;//todo
			return lhs;
		}
		//friend LargeInt& operator += (LargeInt& lhs, unsigned int rhs)
		//{
		//	auto word = lhs.data + MaxLength - 1;
		//	while ((*word + rhs) < *word)
		//	{
		//		*word = *word + rhs;
		//		--word;
		//		rhs = 1;
		//		if (word < lhs.data) return lhs;
		//	}
		//	*word = *word + rhs;
		//	return lhs;
		//}
		//friend LargeInt& operator -= (LargeInt& lhs, unsigned int rhs)
		//{
		//	auto word = lhs.data + MaxLength - 1;
		//	while ((*word - rhs) > * word)
		//	{
		//		*word = *word - rhs;
		//		--word;
		//		rhs = 1;
		//		if (word < lhs.data) return lhs;
		//	}
		//	*word = *word - rhs;
		//	return lhs;
		//}
		//friend LargeInt& operator *= (LargeInt& lhs, int rhs)
		//{
		//	auto carry = 0;
		//	for (auto word = lhs.data() + N; word-- != lhs.data();)
		//	{
		//		dword_t product = *word;
		//		product *= rhs;
		//		product += carry;
		//		*word = static_cast<word_t>(product);
		//		carry = static_cast<word_t>(product >> 32);
		//	}
		//	return lhs;
		//}
		//friend LargeInt& operator /= (LargeInt& lhs, unsigned int rhs)
		//{
		//	LargeInt rvalue;
		//	rvalue = rhs;
		//	return lhs /= rvalue;
		//}
		//friend LargeInt& operator <<=(LargeInt& lhs, unsigned int rhs)
		//{
		//	auto out = lhs.data;
		//	auto in = out + rhs / 32;
		//	auto shift_up = rhs % 32;
		//	auto shift_down = 32 - shift_up;

		//	while (in < lhs.data + MaxLength - 1)
		//	{
		//		unsigned __int64 v1 = *in;
		//		unsigned __int64 v2 = *(in + 1);
		//		v1 <<= shift_up;
		//		v2 >>= shift_down;
		//		*out = static_cast<unsigned int>(v1 | v2);
		//		++out;
		//		++in;
		//	}
		//	if (in < lhs.data + MaxLength)
		//	{
		//		*out = (*in << shift_up);
		//		++out;
		//	}
		//	while (out != lhs.data + MaxLength)
		//	{
		//		*out = 0;
		//		++out;
		//	}
		//	return lhs;
		//}
		//friend LargeInt& operator >>=(LargeInt& lhs, unsigned int rhs)
		//{
		//	auto out = lhs.data + MaxLength - 1;
		//	auto in = out - rhs / 32;
		//	auto shift_down = rhs % 32;
		//	auto shift_up = 32 - shift_down;

		//	while (in > lhs.data)
		//	{
		//		unsigned __int64 v1 = *in;
		//		unsigned __int64 v2 = *(in - 1);
		//		v1 >>= shift_down;
		//		v2 <<= shift_up;
		//		*out = static_cast<unsigned int>(v1 | v2);
		//		--out;
		//		--in;
		//	}
		//	if (in >= lhs.data)
		//	{
		//		*out = (*in >> shift_down);
		//		--out;
		//	}
		//	while (out >= lhs.data)
		//	{
		//		*out = 0;
		//		--out;
		//	}
		//	return lhs;
		//}
		//friend LargeInt& operator %= (LargeInt& lhs, unsigned int rhs)
		//{
		//	LargeInt div = lhs / rhs;
		//	return lhs -= div * rhs;
		//}
		//friend LargeInt& operator -= (LargeInt& lhs, LargeInt const& rhs)
		//{
		//	auto in = rhs.data + MaxLength - 1;
		//	auto out = lhs.data + MaxLength - 1;
		//	__int64 carry = 0;
		//	for (int i = 0; i != MaxLength; ++i)
		//	{
		//		carry += *out;
		//		carry -= *in;
		//		*out = static_cast<unsigned int>(carry);
		//		carry >>= 32;
		//		--out;
		//		--in;
		//	}
		//	return lhs;
		//}
		//friend LargeInt& operator *= (LargeInt& lhs, LargeInt const& rhs)
		//{
		//	lhs = lhs * rhs;
		//	return lhs;
		//}
		//friend LargeInt& operator /= (LargeInt& lhs, LargeInt const& rhs)
		//{
		//	lhs = lhs / rhs;
		//	return lhs;
		//}
		//friend LargeInt& operator %= (LargeInt& lhs, LargeInt const& rhs)
		//{
		//	auto div = lhs / rhs;
		//	return lhs -= div * rhs;
		//}
		//friend LargeInt operator +  (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp += rhs;
		//}
		//friend LargeInt operator -  (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp -= rhs;
		//}
		//friend LargeInt operator *  (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp *= rhs;
		//}
		//friend LargeInt operator *  (unsigned int lhs, LargeInt const& rhs)
		//{
		//	auto tmp = rhs;
		//	return tmp *= lhs;
		//}
		//friend LargeInt operator /  (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp /= rhs;
		//}
		//friend LargeInt operator << (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp <<= rhs;
		//}
		//friend LargeInt operator >> (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp >>= rhs;
		//}
		//friend LargeInt operator %  (LargeInt const& lhs, unsigned int rhs)
		//{
		//	auto div = lhs / rhs;
		//	return lhs - div * rhs;
		//}
		friend LargeInt operator + (LargeInt const& lhs, LargeInt const& rhs)
		{
			auto tmp = lhs;
			return tmp += rhs;
		}
		//friend LargeInt operator - (LargeInt const& lhs, LargeInt const& rhs)
		//{
		//	auto tmp = lhs;
		//	return tmp -= rhs;
		//}
		//friend LargeInt operator * (LargeInt const& lhs, LargeInt const& rhs)
		//{
		//	LargeInt accumulator = LargeIntZero;
		//	unsigned __int64 product;
		//	for (int j = 0; j != MaxLength; ++j)
		//	{
		//		unsigned int carry = 0;
		//		const unsigned int* lvalue = lhs.data + MaxLength - 1;
		//		const unsigned int* rvalue = rhs.data + MaxLength - 1 - j;
		//		for (unsigned int* acc = accumulator.data + MaxLength - 1 - j; acc >= accumulator.data; --acc, --lvalue)
		//		{
		//			product = *lvalue;
		//			product *= *rvalue;
		//			product += carry;
		//			*acc += static_cast<unsigned int>(product);
		//			carry = static_cast<unsigned int>(product >> 32);
		//		}
		//	}
		//	return accumulator;
		//}
		//friend LargeInt operator / (LargeInt const& lhs, LargeInt const& rhs)
		//{
		//	unsigned int highest_bit_lhs = HighBit(lhs);
		//	unsigned int highest_bit_rhs = HighBit(rhs);
		//	if (highest_bit_rhs > highest_bit_lhs) { return LargeIntZero; }
		//	int right_shift = static_cast<int>(highest_bit_lhs - highest_bit_rhs);

		//	LargeInt result = LargeIntZero;
		//	LargeInt numer = lhs;
		//	LargeInt denom = rhs; denom <<= right_shift;

		//	while (right_shift >= 0)
		//	{
		//		unsigned int highest_bit_denom = HighBit(denom);
		//		unsigned int highest_bit_numer = HighBit(numer);
		//		if (highest_bit_denom > highest_bit_numer)
		//		{
		//			unsigned int diff = highest_bit_denom - highest_bit_numer;
		//			denom >>= diff;
		//			right_shift -= diff;
		//		}
		//		while (numer < denom)
		//		{
		//			denom >>= 1;
		//			--right_shift;
		//			if (right_shift < 0) { return result; }
		//		}
		//		numer -= denom;
		//		result += LargeIntOne << right_shift;
		//		denom >>= 1;
		//		--right_shift;
		//	}
		//	return result;
		//}
		//friend LargeInt operator % (LargeInt const& lhs, LargeInt const& rhs)
		//{
		//	auto div = lhs / rhs;
		//	return lhs - div * rhs;
		//}

		// ToString
		friend std::string ToString(LargeInt const& lhs)
		{
			std::string str;
			// todo
			//str.reserve(lhs.m_buf.size() * 8);
			//
			//if (sign() < 0) str.append(
			//for (int i = static_cast<int>(lhs.size()); i-- != 0;)
			//{
			//	str.append(Fmt("%8.8X", lhs.m_buf[i]));
			//}

			//int leading_zeros = 0;
			//for (auto s = str.c_str(); *s != '0'; ++s, ++leading_zeros) {}
			//str.erase(0, leading_zeros);

			return str;
		}
		#pragma endregion

		// Export / Import of LargeInt data
		void load(std::initializer_list<uint32_t> data)
		{
			ensure_space(data.size());

			zero();
			memcpy(m_buf.data(), data.begin(), data.size() * sizeof(uint32_t));
		}
		std::initializer_list<uint32_t> save() const
		{
			return m_buf;
		}

private:

		// Returns 1 or -1
		int32_t sign() const
		{
			return 1 - 2 * (m_buf[0] & 1);
		}

		// Bitwise-OR a range of words into a single word. Excludes the sign bit.
		uint32_t or_words(int start, int count) const
		{
			if (count == 0) return 0;
			auto mask = m_buf[start];
			if (start == 0) mask &= ~1; // Exclude the sign bit
			for (int i = 1; i != count; ++i) mask |= m_buf[start+i];
			return mask;
		}

		// Grow the int to at least 'n' words
		constexpr void ensure_space(size_t n)
		{
			if (n * sizeof(uint32_t) <= SizeInBytes) return;
			throw std::overflow_error("LargeInt is fixed size");
		}
	};
	static_assert(std::is_pod_v<LargeInt<4>>);

	// Russian multiplication method
	inline long long RussianMultiply(long a, long b)
	{
		if (a == 0 || b == 0)
			return 0;

		int sign = +1;
		if (a < 0) { a = -a; sign *= -1; }
		if (b < 0) { b = -b; sign *= -1; }

		long long result = 0;
		for (; a != 0; a >>= 1, b <<= 1)
		{
			if (!(a & 1)) continue;
			result += b;
		}
		result *= sign;
		return result;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	#if 0 // Disabled, not implemented fully
	PRUnitTest(LargeIntTests)
	{
		using Int = LargeInt<16>;
		{ // Assignment
			Int a = 0x12345678LL;
			PR_CHECK(static_cast<int64_t>(a), 0x12345678LL);

			Int b = -0x12345678LL;
			PR_CHECK(static_cast<int64_t>(b), -0x12345678LL);

			Int c = 0x0123456789abcdefLL;
			PR_CHECK(static_cast<int64_t>(c), 0x0123456789abcdefLL);

			Int d = std::numeric_limits<int64_t>::max();
			PR_CHECK(static_cast<int64_t>(d), std::numeric_limits<int64_t>::max());

			Int e = std::numeric_limits<int64_t>::lowest();
			PR_CHECK(static_cast<int64_t>(e), std::numeric_limits<int64_t>::lowest());
		}
		{// Unary +/-
			for (int64_t i = 0; i != 10; ++i)
			{
				Int a = +i;
				Int b = -i;
				Int c = -a;

				PR_CHECK(static_cast<int64_t>(a), +i);
				PR_CHECK(static_cast<int64_t>(b), -i);
				PR_CHECK(static_cast<int64_t>(c), +i);
				PR_CHECK(a == c, true);
				PR_CHECK(a == -b, true);
			}
		}
		{// Operator ~
			Int a = {0x55555555UL, 0x55555555UL, 0x55555555UL, 0x55555555UL};
			Int b = {0xAAAAAAAAUL, 0xAAAAAAAAUL, 0xAAAAAAAAUL, 0xAAAAAAAAUL};
			PR_CHECK(~a == b, true);
		}
		{// Operator !
			Int a = 10;
			Int b = !a;
			Int c = !b;
			PR_CHECK(b == 0, true);
			PR_CHECK(c == 1, true);
		}
		{// operator +
			int64_t const v = 5000000000LL;

			Int a = v;
			auto b = a + a;
			PR_CHECK(static_cast<int64_t>(b), v+v);

			auto c = a + -a;
			PR_CHECK(static_cast<int64_t>(c), v-v);

			auto d = -a + a;
			PR_CHECK(static_cast<int64_t>(d), -v+v);

			auto e = -a + -a;
			PR_CHECK(static_cast<int64_t>(e), -v-v);

			//Int a = std::numeric_limits<int64_t>::max();
			//auto b = a + a;
		}
		{// operator ==/!=
			Int a = std::numeric_limits<int64_t>::max();
			Int b = 3;
			Int c = a;
			PR_CHECK(a == b, false);
			PR_CHECK(a == c, true);
			PR_CHECK(a != b, true);
			PR_CHECK(a != c, false);
		}
		//{// operators <,>,<=,>=
		//	Int a = {0x01234567, 0x89ABCDEF};
		//	Int b = {0x89ABCDEF, 0x01234567};
		//	PR_CHECK(a < b, true);
		//	PR_CHECK(b < a, false);
		//	PR_CHECK(a < a, false);
		//
		//	PR_CHECK(a > b, false);
		//	PR_CHECK(b > a, true);
		//	PR_CHECK(a > a, false);
		//
		//	PR_CHECK(a <= b, true);
		//	PR_CHECK(b <= a, false);
		//	PR_CHECK(a <= a, true);
		//
		//	PR_CHECK(a >= b, false);
		//	PR_CHECK(b >= a, true);
		//	PR_CHECK(a >= a, true);
		//}
		{
			// Russian multiply
			PR_CHECK(RussianMultiply(+1423, +321), +1423LL * +321LL);
			PR_CHECK(RussianMultiply(-1423, +321), -1423LL * +321LL);
			PR_CHECK(RussianMultiply(+1423, -321), +1423LL * -321LL);
			PR_CHECK(RussianMultiply(-1423, -321), -1423LL * -321LL);
		}
	}
	#endif
}
#endif


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
	//// Returns log2(n) (i.e. the position of the highest bit)
	//template <int S> unsigned int HighBit(LargeInt<S> const& n)
	//{
	//	//inline unsigned int HighBit(unsigned int n)	// Defined elsewhere in my stuff
	//	//{
	//	//	register unsigned int r = 0;
	//	//	register unsigned int shift;
	//	//	shift = ( ( n & 0xFFFF0000 ) != 0 ) << 4; n >>= shift; r |= shift;
	//	//	shift = ( ( n & 0xFF00     ) != 0 ) << 3; n >>= shift; r |= shift;
	//	//	shift = ( ( n & 0xF0       ) != 0 ) << 2; n >>= shift; r |= shift;
	//	//	shift = ( ( n & 0xC        ) != 0 ) << 1; n >>= shift; r |= shift;
	//	//	shift = ( ( n & 0x2        ) != 0 ) << 0; n >>= shift; r |= shift;
	//	//	return r;
	//	//}
	//	for (auto word = &n.data[0]; word != &n.data[0] + LargeInt::MaxLength; ++word)
	//	{
	//		if (!*word) continue;
	//		return HighBitIndex(*word) + (LargeInt::MaxLength - 1 - (unsigned int)(word - n.data)) * 32;
	//	}
	//	return 0;
	//}