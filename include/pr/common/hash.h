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

		static const uint64_t FNV_offset_basis64 = 14695981039346656037ULL;
		static const uint64_t FNV_prime64        = 1099511628211ULL;
		static const uint32_t FNV_offset_basis32 = 2166136261U;
		static const uint32_t FNV_prime32        = 16777619U;

		// Convert 'ch' to lower case
		constexpr int Lower(int ch)
		{
			return ch + 32*(ch >= 'A' && ch <= 'Z');
		}

		namespace impl
		{
			// 32 bit multiply without an overflow warning...
			constexpr uint32_t Mul32(uint32_t a, uint32_t b)
			{
				return uint32_t((uint64_t(a) * uint64_t(b)) & ~0U);
			}
			static_assert(Mul32(0x12345678, 0x12345678) == 0x1DF4D840, "");

			// 64 bit multiply without a warning...
			constexpr uint64_t lo(uint64_t x) { return x & uint32_t(-1); }
			constexpr uint64_t hi(uint64_t x) { return x >> 32; }
			constexpr uint64_t Mul64(uint64_t a, uint64_t b)
			{
				return 
					(lo(a) * lo(b) & uint32_t(-1)) +
					(((((hi(lo(a)*lo(b)) + lo(a)*hi(b)) & uint32_t(-1)) + hi(a)*lo(b)) & uint32_t(-1)) << 32);
			}
			static_assert(Mul64(0x1234567887654321, 0x1234567887654321) == 0x290D0FCAD7A44A41, "");
		}

		// Hash and accumulate 
		constexpr uint32_t Hash32(uint32_t ch, uint32_t h)
		{
			return impl::Mul32(h ^ ch, FNV_prime32);
		}
		constexpr uint64_t Hash64(uint64_t ch, uint64_t h)
		{
			return impl::Mul64(h ^ ch, FNV_prime64);
		}

		// Sentinel hash
		template <typename Ty> constexpr uint32_t Hash32(Ty const* str, Ty term = 0, uint32_t h = FNV_offset_basis32)
		{
			return *str == term ? h : Hash32(str + 1, term, Hash32(uint32_t(*str), h));
		}
		template <typename Ty> constexpr uint64_t Hash64(Ty const* str, Ty term = 0, uint64_t h = FNV_offset_basis64)
		{
			return *str == term ? h : Hash64(str + 1, term, Hash64(uint64_t(*str), h));
		}

		// Case insensitive sentinel hash
		template <typename Ty> constexpr uint32_t HashI32(Ty const* str, Ty term = 0, uint32_t h = FNV_offset_basis32)
		{
			return *str == term ? h : HashI32(str + 1, term, Hash32(Lower(*str), h));
		}
		template <typename Ty> constexpr uint64_t HashI64(Ty const* str, Ty term = 0, uint64_t h = FNV_offset_basis64)
		{
			return *str == term ? h : HashI64(str + 1, term, HAsh64(Lower(*str), h));
		}

		// Range hash
		template <typename Ty> constexpr uint32_t Hash32(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return str == end ? h : Hash32(str + 1, end, Hash32(uint32_t(*str), h));
		}
		template <typename Ty> constexpr uint32_t Hash64(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis64)
		{
			return str == end ? h : Hash64(str + 1, end, Hash64(uint64_t(*str), h));
		}

		// Case insensitive range hash
		template <typename Ty> constexpr uint32_t HashI32(Ty const* str, Ty const* end, uint32_t h = FNV_offset_basis32)
		{
			return str == end ? h : HashI32(str + 1, end, Hash32(Lower(*str), h));
		}
		template <typename Ty> constexpr uint64_t HashI64(Ty const* str, Ty const* end, uint64_t h = FNV_offset_basis64)
		{
			return str == end ? h : HashI64(str + 1, end, HAsh64(Lower(*str), h));
		}

		// Default hash = 32 bit hash, even on 64bit builds for consistency
		template <typename Ty> constexpr uint32_t Hash(Ty ch, uint32_t h)
		{
			return Hash32(ch, h);
		}
		template <typename Ty> constexpr HashValue Hash(Ty const* str, Ty term = 0)
		{
			return static_cast<HashValue>(Hash32(str, term));
		}
		template <typename Ty> constexpr HashValue HashI(Ty const* str, Ty term = 0)
		{
			return static_cast<HashValue>(HashI32(str, term));
		}
		template <typename Ty> constexpr HashValue Hash(Ty const* str, Ty const* end)
		{
			return Hash32(str, end);
		}
		template <typename Ty> constexpr HashValue HashI(Ty const* str, Ty const* end)
		{
			return HashI32(str, end);
		}

		//template <unsigned int N> class C; C<HashI("ABC")> c;
		static_assert(Hash("ABC") == 1552166763U, "Compile time CRC failed");
		static_assert(HashI("ABC") == 440920331U, "Compile time CRC failed");

		// Hash a range of elements using the default 'Hash' function
		template <typename Iter> inline HashValue Hash(Iter first, Iter last)
		{
			auto r = FNV_offset_basis32;
			for (; first != last; ++first) r = Hash(*first, r);
			return r;
		}

		// Hash a POD
		template <typename Ty> inline HashValue HashObj(Ty const& obj)
		{
			auto ptr = static_cast<char const*>(static_cast<void const*>(&obj));
			return Hash(ptr, ptr + sizeof(Ty));
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
				const auto h0  = sizeof(size_t) == 8 ? Hash64(data) : Hash32(data);
				auto h1 = std::hash<std::string>{}(&data[0]);
				PR_CHECK(h0 == h1, true);
			}
			//enum
			//{
			//	Blah = -0x7FFFFFFF,
			//};
			//char const data2[] = "paul was here. crc this, mofo";
			//{
			//	auto h0 = HashData(data, sizeof(data));
			//	PR_CHECK(h0, 0x12345678);
			//
			//	auto h1 = HashData64(data, sizeof(data));
			//	PR_CHECK(h1,h1);
			//}
			//{ // Check accumulative hash works
			//	pr::hash::HashValue h0 = pr::hash::HashData(data, sizeof(data));
			//	pr::hash::HashValue h1 = pr::hash::HashData(data + 5, sizeof(data) - 5, pr::hash::HashData(data, 5));
			//	pr::hash::HashValue h2 = pr::hash::HashData(data + 9, sizeof(data) - 9, pr::hash::HashData(data, 9));
			//	PR_CHECK(h0, h1);
			//	PR_CHECK(h0, h2);
			//}
			//{
			//	auto h0 = pr::hash::HashLwr(data);
			//	auto h1 = pr::hash::HashC(data2);
			//	PR_CHECK(h1, h0);
			//}

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
		}
	}
}
#endif
