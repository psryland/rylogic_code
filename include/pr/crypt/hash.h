//*************************************
// Hash
//  Copyright (c) Rylogic Ltd 2009
//*************************************
// The main purpose of this header is compile time string hashing.
// For runtime hashing, prefer 'std::hash<T>{}(...)'
// e.g. std::hash<Thing>{}(&thing);
// The compile time hash below uses the same algorithm (FNV-1a hash) and
// magic numbers as std::hash. Note, though, that std::hash uses different
// magic values for 64bit and 32bit builds.
// WARNING: 
//  Careful with std::hash<>, it's not specialised for raw strings.
//  'std::hash<char const*>{}(str)' finds the hash of the pointer, not the string.

#pragma once
#include <cstdint>
#include <type_traits>
#include <iterator>

namespace pr
{
	namespace hash
	{
		// Notes:
		// 'constexpr' functions have to use recursion for non-C++14 compilers
		// When C++14 is supported, reimplement as iterative functions

		// 'HashValue' is a signed int so that comparisons
		// with enum values don't generate signed/unsigned warnings
		using HashValue = int;
		using HashValue64 = long long;

		static uint64_t const FNV_offset_basis64 = 14695981039346656037ULL;
		static uint64_t const FNV_prime64        = 1099511628211ULL;
		static uint32_t const FNV_offset_basis32 = 2166136261U;
		static uint32_t const FNV_prime32        = 16777619U;

		namespace impl
		{
			// Convert 'ch' to lower case
			constexpr int Lower(int ch)
			{
				return ch + 32*(ch >= 'A' && ch <= 'Z');
			}

			// 32 bit multiply without an overflow warning...
			constexpr uint32_t Mul32(uint32_t a, uint32_t b)
			{
				return uint32_t((uint64_t(a) * uint64_t(b)) & ~0U);
			}
			static_assert(Mul32(0x12345678, 0x12345678) == 0x1DF4D840, "");

			// 64 bit multiply without a warning...
			constexpr uint64_t Lo32(uint64_t x) { return x & uint32_t(-1); }
			constexpr uint64_t Hi32(uint64_t x) { return x >> 32; }
			constexpr uint64_t Mul64(uint64_t a, uint64_t b)
			{
				auto ffffffff = uint32_t(-1);
				auto ab = Lo32(a) * Lo32(b);
				auto aB = Lo32(a) * Hi32(b);
				auto Ab = Hi32(a) * Lo32(b);
				auto hi = ((((Hi32(ab) + aB) & ffffffff) + Ab) & ffffffff) << 32;
				auto lo = ab & ffffffff;
				return hi + lo;
			}
			static_assert(Mul64(0x1234567887654321, 0x1234567887654321) == 0x290D0FCAD7A44A41, "");

			// EnableIf for types expected to be strings or not strings
			template <typename Ty> struct intg32
			{
				static bool const value =
					sizeof(Ty) <= sizeof(uint32_t) &&
					std::is_integral<Ty>::value;
			};
			template <typename Ty> struct intg64
			{
				static bool const value =
					sizeof(Ty) <= sizeof(uint64_t) &&
					std::is_integral<Ty>::value;
			};
			template <typename Ty> using str32 = typename std::enable_if< intg32<Ty>::value, Ty>::type;
			template <typename Ty> using pod32 = typename std::enable_if<!intg32<Ty>::value, Ty>::type;
			template <typename Ty> using str64 = typename std::enable_if< intg64<Ty>::value, Ty>::type;
			template <typename Ty> using pod64 = typename std::enable_if<!intg64<Ty>::value, Ty>::type;
			static_assert(intg32<char>::value == true, "");
			static_assert(intg32<wchar_t>::value == true, "");
			static_assert(intg32<int>::value == true, "");
			static_assert(intg32<char[5]>::value == false, "");
			static_assert(intg32<double>::value == false, "");
		}

		// Compile Time ***************************************************************************

		// Compile time hash and accumulate 
		constexpr uint32_t Hash32CT(uint32_t ch, uint32_t h)
		{
			return impl::Mul32(h ^ ch, FNV_prime32);
		}
		constexpr uint64_t Hash64CT(uint64_t ch, uint64_t h)
		{
			return impl::Mul64(h ^ ch, FNV_prime64);
		}

		// Sentinel hash
		template <typename Ty, typename = impl::str32<Ty>> constexpr uint32_t Hash32CT(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			return *str == term ? h : Hash32CT(str + 1, term, Hash32CT(uint32_t(*str), h));
		}
		template <typename Ty, typename = impl::str64<Ty>> constexpr uint64_t Hash64CT(Ty const* str, Ty term = Ty(), uint64_t h = FNV_offset_basis64)
		{
			return *str == term ? h : Hash64CT(str + 1, term, Hash64CT(uint64_t(*str), h));
		}

		// Case insensitive sentinel hash
		template <typename Ty, typename = impl::str32<Ty>> constexpr uint32_t HashI32CT(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			return *str == term ? h : HashI32CT(str + 1, term, Hash32CT(impl::Lower(*str), h));
		}
		template <typename Ty, typename = impl::str64<Ty>> constexpr uint64_t HashI64CT(Ty const* str, Ty term = Ty(), uint64_t h = FNV_offset_basis64)
		{
			return *str == term ? h : HashI64CT(str + 1, term, Hash64CT(impl::Lower(*str), h));
		}

		// Range hash
		template <typename Ty, typename = impl::str32<Ty>> constexpr uint32_t Hash32CT(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return str == end ? h : Hash32CT(str + 1, end, Hash32CT(uint32_t(*str), h));
		}
		template <typename Ty, typename = impl::str64<Ty>> constexpr uint32_t Hash64CT(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis64)
		{
			return str == end ? h : Hash64CT(str + 1, end, Hash64CT(uint64_t(*str), h));
		}

		// Case insensitive range hash
		template <typename Ty, typename = impl::str32<Ty>> constexpr uint32_t HashI32CT(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return str == end ? h : HashI32CT(str + 1, end, Hash32CT(impl::Lower(*str), h));
		}
		template <typename Ty, typename = impl::str64<Ty>> constexpr uint64_t HashI64CT(Ty const* str, Ty const* end, uint64_t h = FNV_offset_basis64)
		{
			return str == end ? h : HashI64CT(str + 1, end, Hash64CT(impl::Lower(*str), h));
		}

		// Default hash = 32 bit hash, even on 64bit builds for consistency
		template <typename Ty, typename = impl::str32<Ty>> constexpr HashValue HashCT(Ty ch, uint32_t h = FNV_offset_basis32)
		{
			return static_cast<HashValue>(Hash32CT(ch, h));
		}
		template <typename Ty, typename = impl::str32<Ty>> constexpr HashValue HashCT(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			return static_cast<HashValue>(Hash32CT(str, term, h));
		}
		template <typename Ty, typename = impl::str32<Ty>> constexpr HashValue HashICT(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			return static_cast<HashValue>(HashI32CT(str, term, h));
		}
		template <typename Ty, typename = impl::str32<Ty>> constexpr HashValue HashCT(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return Hash32CT(str, end, h);
		}
		template <typename Ty, typename = impl::str32<Ty>> constexpr HashValue HashICT(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return HashI32CT(str, end, h);
		}

		//template <unsigned int N> class C; C<HashI("ABC")> c;
		static_assert(HashCT("ABC") == 1552166763U, "Compile time CRC failed");
		static_assert(HashICT("ABC") == 440920331U, "Compile time CRC failed");

		// Run Time *******************************************************************************

		// Hash a single 'ch'
		template <typename Ty, typename = impl::str32<Ty>> inline HashValue Hash(Ty ch, uint32_t h = FNV_offset_basis32)
		{
			return HashCT(ch, h);
		}
		template <typename Ty, typename = impl::str32<Ty>> inline HashValue HashI(Ty ch, uint32_t h = FNV_offset_basis32)
		{
			return HashCT(static_cast<Ty>(impl::Lower(*str)), h);
		}

		// Hash a sentinel string using the compile time 'Hash' function
		template <typename Ty, typename = impl::str32<Ty>> inline HashValue Hash(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			for (; *str != term; ++str) h = HashCT(*str, h);
			return h;
		}
		template <typename Ty, typename = impl::str32<Ty>> inline HashValue HashI(Ty const* str, Ty term = Ty(), uint32_t h = FNV_offset_basis32)
		{
			for (; *str != term; ++str) h = HashCT(static_cast<Ty>(impl::Lower(*str)), h);
			return h;
		}

		// Hash a range of elements using the compile time 'Hash' function
		template <typename Iter, typename Ty = std::iterator_traits<Iter>::value_type, typename = impl::str32<Ty>> inline HashValue Hash(Iter first, Iter last, uint32_t h = FNV_offset_basis32)
		{
			for (; first != last; ++first) h = HashCT(*first, h);
			return h;
		}
		template <typename Iter, typename Ty = std::iterator_traits<Iter>::value_type, typename = impl::str32<Ty>> inline HashValue HashI(Iter first, Iter last, uint32_t h = FNV_offset_basis32)
		{
			for (; first != last; ++first) h = HashCT(static_cast<Ty>(impl::Lower(*first)), h);
			return h;
		}

		// Hash PODs given as arguments. Careful with hidden padding
		template <typename Ty, typename = impl::pod32<Ty>> inline HashValue Hash(Ty const& x)
		{
			return impl::HashPod(x);
		}
		template <typename Ty, typename... Args, typename = impl::pod32<Ty>> inline HashValue Hash(Ty const& x, Args&&... args)
		{
			return impl::HashPod(x, args...);
		}
		namespace impl
		{
			#pragma warning (push)
			#pragma warning (disable:4127)
			template <typename Ty, typename = str32<Ty>> inline HashValue HashPod(Ty x)
			{
				return Hash(x);
			}
			template <typename Ty, typename = str32<Ty>> inline HashValue HashPod(Ty const* x)
			{
				return Hash(x);
			}
			template <typename Ty, typename = pod32<Ty>> inline HashValue HashPod(Ty const& x)
			{
				if constexpr ((sizeof(Ty)&3) == 0)
				{
					auto ptr = reinterpret_cast<uint32_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint32_t));
				}
				else if constexpr ((sizeof(Ty) & 1) == 0)
				{
					auto ptr = reinterpret_cast<uint16_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint16_t));
				}
				else
				{
					auto ptr = reinterpret_cast<uint8_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint8_t));
				}
			}

			template <typename Ty, typename... Args, typename = str32<Ty>> inline HashValue HashPod(Ty x, Args&&... args)
			{
				auto h = static_cast<uint32_t>(HashPod(std::forward<Args>(args)...));
				return Hash(x, h);
			}
			template <typename Ty, typename... Args, typename = str32<Ty>> inline HashValue HashPod(Ty const* x, Args&&... args)
			{
				auto h = static_cast<uint32_t>(HashPod(std::forward<Args>(args)...));
				return Hash(x, Ty(), h);
			}
			template <typename Ty, typename... Args, typename = pod32<Ty>> inline HashValue HashPod(Ty const& x, Args&&... args)
			{
				auto h = static_cast<uint32_t>(HashPod(std::forward<Args>(args)...));

				if constexpr ((sizeof(Ty) & 3) == 0)
				{
					auto ptr = reinterpret_cast<uint32_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint32_t), h);
				}
				else if constexpr ((sizeof(Ty) & 1) == 0)
				{
					auto ptr = reinterpret_cast<uint16_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint16_t), h);
				}
				else
				{
					auto ptr = reinterpret_cast<uint8_t const*>(&x);
					return Hash(ptr, ptr + sizeof(Ty)/sizeof(uint8_t), h);
				}
			}
			#pragma warning (pop)
		}

		// Hash in blocks of 16 bits
		// http://www.azillionmonkeys.com/qed/hash.html, (c) Copyright 2004-2008 by Paul Hsieh
		inline HashValue HsiehHash16(void const* data, size_t len, HashValue hash = -1)
		{
			using uint8_t = unsigned char;
			using uint_t = unsigned int;

			if (len == 0 || data == 0)
				return hash;

			// Local function for reading 16bit chunks of 'data'
			auto Read16bits = [](uint8_t const* d)
			{
				#if (defined(__GNUC__) && defined(__i386__)) || defined(__WATCOMC__) || defined(_MSC_VER) || defined (__BORLANDC__) || defined (__TURBOC__)
				return *reinterpret_cast<unsigned short const*>(d);
				#else
				return (static_cast<uint_t>(reinterpret_cast<uint8_t const*>(d)[1]) << 8) +
						static_cast<uint_t>(reinterpret_cast<uint8_t const*>(d)[0]);
				#endif
			};

			auto ptr = static_cast<uint8_t const*>(data);
			uint_t tmp;
			uint_t rem = len & 3;
			len >>= 2;

			// Main loop
			while (len-- != 0)
			{
				hash +=  Read16bits(ptr);
				tmp   = (Read16bits(ptr + 2) << 11) ^ hash;
				hash  = (hash << 16) ^ tmp;
				ptr  += 2 * sizeof(unsigned short);
				hash += hash >> 11;
			}

			// Handle end cases
			switch (rem)
			{
			default:break;
			case 3: hash += Read16bits(ptr);
					hash ^= hash << 16;
					hash ^= ptr[sizeof(unsigned short)] << 18;
					hash += hash >> 11;
					break;
			case 2: hash += Read16bits(ptr);
					hash ^= hash << 11;
					hash += hash >> 17;
					break;
			case 1: hash += *ptr;
					hash ^= hash << 10;
					hash += hash >> 1;
					break;
			}

			// Force "avalanching" of final 127 bits
			hash ^= hash << 3;
			hash += hash >> 5;
			hash ^= hash << 4;
			hash += hash >> 17;
			hash ^= hash << 25;
			hash += hash >> 6;
			return hash;
		}

		// MurmurHash2, by Austin Appleby
		// Note - This code makes a few assumptions about how your machine behaves -
		// 1. We can read a 4-byte value from any address without crashing
		// 2. sizeof(int) == 4
		// And it has a few limitations -
		// 1. It will not work incrementally.
		// 2. It will not produce the same results on little-endian and big-endian machines.
		inline HashValue MurmurHash2_32(void const* key, int len, HashValue seed = -1)
		{
			// 'm' and 'r' are mixing constants generated offline.
			// They're not really 'magic', they just happen to work well.
			const unsigned int m = 0x5bd1e995;
			const int r = 24;

			// Initialize the hash to a 'random' value
			unsigned int h = seed ^ len;

			// Mix 4 bytes at a time into the hash
			auto data = (const unsigned char *)key;
			while (len >= 4)
			{
				auto k = *(unsigned int *)data;

				k *= m;
				k ^= k >> r;
				k *= m;

				h *= m;
				h ^= k;

				data += 4;
				len -= 4;
			}

			// Handle the last few bytes of the input array
			switch(len)
			{
			case 3: h ^= data[2] << 16;
			case 2: h ^= data[1] << 8;
			case 1: h ^= data[0];
					h *= m;
			};

			// Do a few final mixes of the hash to ensure the last few bytes are well-incorporated.
			h ^= h >> 13;
			h *= m;
			h ^= h >> 15;

			return h;
		}

		// 64-bit hash for 64-bit platforms
		// MurmurHash2, 64-bit versions, by Austin Appleby
		// The same caveats as 32-bit MurmurHash2 apply here - beware of alignment 
		// and endian-ness issues if used across multiple platforms.
		inline HashValue64 MurmurHash2_64(void const* key, int len, HashValue seed = -1)
		{
			using uint64_t = unsigned long long;
			auto const m = 0xc6a4a7935bd1e995UL;
			auto const r = 47;

			uint64_t h = seed ^ (len * m);
			auto data = (uint64_t const*)key;
			auto end = data + (len/8);

			while (data != end)
			{
				auto k = *data++;

				k *= m;
				k ^= k >> r;
				k *= m;

				h ^= k;
				h *= m;
			}

			auto data2 = (unsigned char const*)data;
			switch(len & 7)
			{
			case 7: h ^= uint64_t(data2[6]) << 48;
			case 6: h ^= uint64_t(data2[5]) << 40;
			case 5: h ^= uint64_t(data2[4]) << 32;
			case 4: h ^= uint64_t(data2[3]) << 24;
			case 3: h ^= uint64_t(data2[2]) << 16;
			case 2: h ^= uint64_t(data2[1]) << 8;
			case 1: h ^= uint64_t(data2[0]);
					h *= m;
			};
 
			h ^= h >> r;
			h *= m;
			h ^= h >> r;

			return h;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_hash)
		{
			using namespace pr::hash;

			{// basic hashing
				auto h0 = Hash("");
				PR_CHECK(h0 == pr::hash::FNV_offset_basis32, true);
			}
			{// Compile time hash vs. std::hash
				char const data[] = "Paul was here. CrC this, mofo";
				const auto h0  = sizeof(size_t) == 8 ? Hash64CT(data) : Hash32CT(data);
				auto h1 = std::hash<std::string>{}(&data[0]);
				PR_CHECK(h0 == h1, true);
			}
			{// Compile time vs. run time
				char const data[] = "Paul was here. CrC this, mofo";
				const auto h0 = HashCT(&data[0]);
				const auto h1 = Hash(&data[0]);
				PR_CHECK(h0 == h1, true);

				enum { h2 = HashCT("four") };
				const auto h3 = Hash("four");
				PR_CHECK(h2 == h3, true);

				char const* five = "five";
				enum { h4 = HashCT("five") };
				const auto h5 = Hash(five);
				PR_CHECK(h4 == h5, true);
			}
			{// Hash POD
				struct POD { int i; char c[4]; float f; };
				auto pod0 = POD{32,{ 'A','B','C','D' },6.28f};
				auto pod1 = POD{31,{ 'D','C','B','A' },3.14f};
				auto pod2 = POD{32,{ 'A','B','C','D' },6.28f};
				const auto h0 = Hash(pod0);
				const auto h1 = Hash(pod1);
				const auto h2 = Hash(pod2);
				PR_CHECK(h0 != h1, true);
				PR_CHECK(h0 == h2, true);
			}
			{// Case insensitive hash
				enum E { Blah = HashICT("Blah"), };
				const auto h0 = HashI("Blah");
				PR_CHECK(E(h0) == Blah, true);
			}
			{ // HsiehHash16
				char const data[] = "Hsieh hash test!";
				auto h0 = HsiehHash16(&data[0], sizeof(data));
				PR_CHECK(h0, int(0xe85f5a90));
			}
			{ // MurmurHash
				char const data[] = "Murmur hash test";
				auto h0 = MurmurHash2_32(&data[0], sizeof(data));
				auto h1 = MurmurHash2_64(&data[0], sizeof(data));

				PR_CHECK(h0, 0x6bfb39d7);
				PR_CHECK(h1, 0x52ce8bc5882d9212LL);
			}
			{ // Hash arguments
				char const* s = "was";
				const auto h0 = Hash("Paul", s, L"here", 1976, 12.29, 1234U);
				PR_CHECK(h0, HashValue(0xb0167e22));
			}
		}
	}
}
#endif
