//*************************************
// Hash
//  Copyright © Rylogic Ltd 2009
//*************************************
#pragma once
#ifndef PR_HASH_H
#define PR_HASH_H

// Note: SSE4.2 has an intrinsic for crc32, I don't know how to optionally use it tho :-/
//#include <intrin.h>
	
namespace pr
{
	namespace hash
	{
		// 'HashValue' as a signed int so that comparisons
		// with enum values don't generate signed/unsigned warnings
		typedef int HashValue;
		typedef long long HashValue64;
		typedef unsigned char uint8_t;
		typedef unsigned int  uint_t;
		typedef unsigned long long uint64_t;

		// CRC32 table - the polynomial '0x1EDC6F41' is chosen because that's what the intrinsics are based on
		// Hopefully, one day I can optionally compile in the intrinsic versions without changing the generated hashes
		template <typename Uint, Uint Poly> struct CRCTable
		{
			Uint m_data[256];
			CRCTable()
			{
				for (uint_t i = 0; i != 256; ++i)
				{
					Uint part = i;
					for (int j = 0; j != 8; ++j) part = (part >> 1) ^ ((part & 1) ? Poly : 0);
					m_data[i] = part;
				}
			}
			static Uint const* Data() { static CRCTable table; return table.m_data; }
		};
		typedef CRCTable<uint_t  , 0x1EDC6F41U>   CRCTable32;
		typedef CRCTable<uint64_t, 0x1EDC6F41ULL> CRCTable64;

		// Convert a contiguous block of bytes to a hash
		inline HashValue HashData(void const* data, size_t size, HashValue hash = -1)
		{
			uint_t const*  table = CRCTable32::Data();
			uint8_t const* src   = static_cast<uint8_t const*>(data);
			uint_t&        h     = reinterpret_cast<uint_t&>  (hash);
			for (; size--; ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}
		template <typename T> inline HashValue HashObj(T const& obj, HashValue hash = -1)
		{
			return HashData(&obj, sizeof(obj), hash);
		}

		// Convert a contiguous block of bytes into a 64bit hash
		inline HashValue64 HashData64(void const* data, size_t size, HashValue64 hash = -1LL)
		{
			uint64_t const* table = CRCTable64::Data();
			uint8_t const*  src   = static_cast<uint8_t const*>(data);
			uint64_t&       h     = reinterpret_cast<uint64_t&>(hash);
			for (; size--; ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}

		// Convert a range of types to a hash.
		// Note *src is assumed to be an arbitrary sized contiguous object
		template <typename Iter> inline HashValue Hash(Iter& first, Iter last, HashValue hash = -1)
		{
			uint_t const* table = CRCTable32::Data();
			uint_t&       h     = reinterpret_cast<uint_t&>(hash);
			for (; first != last; ++first)
			{
				// hash over the range of bytes pointed to by *src
				uint8_t const* s = reinterpret_cast<uint8_t const*>(first);
				uint8_t const* s_end = s + sizeof(uint8_t);
				for (; s != s_end; ++s)
					h = table[(h ^ *s) & 0xff] ^ (h >> 8);
			}
			return hash;
		}

		// Convert a range of types to a hash.
		// Note *src is assumed to be an arbitrary sized contiguous object
		template <typename Iter> inline HashValue HashC(Iter first, Iter last, HashValue hash = -1)
		{
			return Hash(first, last, hash);
		}

		// Convert a collection of types terminated by 'term' to a hash.
		// Note *src is assumed to be an arbitrary sized contiguous object
		template <typename Iter, typename Term> inline HashValue Hash(Iter& src, Term term, HashValue hash = -1)
		{
			uint_t const* table = CRCTable32::Data();
			uint_t&       h     = reinterpret_cast<uint_t&>(hash);
			for (; *src != term; ++src)
			{
				// hash over the range of bytes pointed to by *src
				Term const& elem = *src;
				uint8_t const* s = reinterpret_cast<uint8_t const*>(&elem);
				uint8_t const* s_end = s + sizeof(uint8_t);
				for (; s != s_end; ++s)
					h = table[(h ^ *s) & 0xff] ^ (h >> 8);
			}
			return hash;
		}

		// Convert a collection of types terminated by 'term' to a hash.
		// Note *src is assumed to be an arbitrary sized contiguous object
		template <typename Iter, typename Term> inline HashValue HashC(Iter& src, Term term, HashValue hash = -1)
		{
			return Hash(src, term, hash);
		}

		// Hash a char string
		inline HashValue Hash(char const*& src, char term, HashValue hash)
		{
			uint_t const* table = CRCTable32::Data();
			uint_t&       h     = reinterpret_cast<uint_t&>(hash);
			for (; *src != term; ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}
		inline HashValue Hash(char const*& src, char term) { return Hash(src, term, -1); }
		inline HashValue Hash(char const*& src)            { return Hash(src, 0, -1); }

		// Hash a char string
		inline HashValue HashC(char const* src, char term, HashValue hash) { return Hash(src, term, hash); }
		inline HashValue HashC(char const* src, char term) { return HashC(src, term, -1); }
		inline HashValue HashC(char const* src)            { return HashC(src, 0, -1); }

		// Hash a wchar_t string
		inline HashValue Hash(wchar_t const*& src, wchar_t term, HashValue hash)
		{
			uint_t const* table = CRCTable32::Data();
			uint_t&       h     = reinterpret_cast<uint_t&>(hash);
			for (; *src != term; ++src)
			{
				uint8_t const* s = reinterpret_cast<uint8_t const*>(src);
				h = table[(h ^ *s++) & 0xff] ^ (h >> 8);
				h = table[(h ^ *s++) & 0xff] ^ (h >> 8);
			}
			return hash;
		}
		inline HashValue Hash(wchar_t const*& src, wchar_t term) { return Hash(src, term, -1); }
		inline HashValue Hash(wchar_t const*& src)               { return Hash(src, 0, -1); }

		// Hash a wchar_t string
		inline HashValue HashC(wchar_t const* src, wchar_t term, HashValue hash) { return Hash(src, term, hash); }
		inline HashValue HashC(wchar_t const* src, wchar_t term) { return HashC(src, term, -1); }
		inline HashValue HashC(wchar_t const* src)               { return HashC(src, 0, -1); }

		// Hash a char string converting it's characters to lower case first
		inline HashValue HashLwr(char const* src, char term, HashValue hash)
		{
			struct Lowerer
			{
				char const* m_src;
				Lowerer(char const* src) :m_src(src) {}
				Lowerer& operator ++()         { ++m_src; return *this; }
				char     operator * () const   { return static_cast<char>(::tolower(*m_src)); }
			} lowerer(src);
			return Hash(lowerer, term, hash);
		}
		inline HashValue HashLwr(char const* src, char term) { return HashLwr(src, term, -1); }
		inline HashValue HashLwr(char const* src)            { return HashLwr(src, 0, -1); }

		// http://www.azillionmonkeys.com/qed/hash.html, © Copyright 2004-2008 by Paul Hsieh
		inline HashValue FastHash(void const* data, size_t len, HashValue hash)
		{
			// Local function for reading 16bit chunks of 'data'
			struct This { static unsigned short Read16bits(uint8_t const* d)
			{
				#if (defined(__GNUC__) && defined(__i386__)) || defined(__WATCOMC__) || defined(_MSC_VER) || defined (__BORLANDC__) || defined (__TURBOC__)
				return *reinterpret_cast<unsigned short const*>(d);
				#else
				return (static_cast<uint_t>(reinterpret_cast<uint8_t const*>(d)[1]) << 8) +
						static_cast<uint_t>(reinterpret_cast<uint8_t const*>(d)[0]);
				#endif
			}};

			if (len == 0 || data == 0)
				return 0;

			uint8_t const* ptr = static_cast<uint8_t const*>(data);
			uint_t tmp;
			uint_t rem = len & 3;
			len >>= 2;

			// Main loop
			while (len-- != 0)
			{
				hash +=  This::Read16bits(ptr);
				tmp   = (This::Read16bits(ptr + 2) << 11) ^ hash;
				hash  = (hash << 16) ^ tmp;
				ptr  += 2 * sizeof(unsigned short);
				hash += hash >> 11;
			}

			// Handle end cases
			switch (rem)
			{
			default:break;
			case 3: hash += This::Read16bits(ptr);
					hash ^= hash << 16;
					hash ^= ptr[sizeof(unsigned short)] << 18;
					hash += hash >> 11;
					break;
			case 2: hash += This::Read16bits(ptr);
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
		inline HashValue FastHash(void const* data, size_t len) { return FastHash(data, len, -1); }

		//-----------------------------------------------------------------------------
		// MurmurHash2, by Austin Appleby
		// Note - This code makes a few assumptions about how your machine behaves -
		// 1. We can read a 4-byte value from any address without crashing
		// 2. sizeof(int) == 4
		// And it has a few limitations -
		// 1. It will not work incrementally.
		// 2. It will not produce the same results on little-endian and big-endian machines.
		inline unsigned int MurmurHash2_32(void const* key, int len, unsigned int seed)
		{
			// 'm' and 'r' are mixing constants generated offline.
			// They're not really 'magic', they just happen to work well.
			const unsigned int m = 0x5bd1e995;
			const int r = 24;

			// Initialize the hash to a 'random' value
			unsigned int h = seed ^ len;

			// Mix 4 bytes at a time into the hash
			const unsigned char * data = (const unsigned char *)key;
			while (len >= 4)
			{
				unsigned int k = *(unsigned int *)data;

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

		//-----------------------------------------------------------------------------
		// MurmurHash2, 64-bit versions, by Austin Appleby
		// The same caveats as 32-bit MurmurHash2 apply here - beware of alignment 
		// and endian-ness issues if used across multiple platforms.
		// 64-bit hash for 64-bit platforms
		inline uint64_t MurmurHash2_64(void const* key, int len, unsigned int seed)
		{
			uint64_t const m = 0xc6a4a7935bd1e995UL;
			int const r = 47;

			uint64_t h = seed ^ (len * m);
			uint64_t const* data = (uint64_t const*)key;
			uint64_t const* end = data + (len/8);

			while (data != end)
			{
				uint64_t k = *data++;

				k *= m;
				k ^= k >> r;
				k *= m;

				h ^= k;
				h *= m;
			}

			unsigned char const* data2 = (unsigned char const*)data;
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
			enum
			{
				Blah = -0x7FFFFFFF,
			};
			char const data[] = "Paul was here. CrC this, mofo";
			char const data2[] = "paul was here. crc this, mofo";
			{
				pr::hash::HashValue h0 = pr::hash::HashData(data, sizeof(data));
				PR_CHECK(h0,h0);
			
				pr::hash::HashValue64 h1 = pr::hash::HashData64(data, sizeof(data));
				PR_CHECK(h1,h1);
			}
			{ // Check accumulative hash works
				pr::hash::HashValue h0 = pr::hash::HashData(data, sizeof(data));
				pr::hash::HashValue h1 = pr::hash::HashData(data + 5, sizeof(data) - 5, pr::hash::HashData(data, 5));
				pr::hash::HashValue h2 = pr::hash::HashData(data + 9, sizeof(data) - 9, pr::hash::HashData(data, 9));
				PR_CHECK(h0, h1);
				PR_CHECK(h0, h2);
			}
			{
				pr::hash::HashValue h0 = pr::hash::HashLwr(data);
				pr::hash::HashValue h1 = pr::hash::HashC(data2);
				PR_CHECK(h1, h0);
			}
		}
	}
}
#endif

#endif
