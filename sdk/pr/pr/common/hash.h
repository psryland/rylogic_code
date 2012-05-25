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
		typedef unsigned char uchar;
		typedef unsigned int  uint;
		typedef unsigned long long uint64;
		
		// Termination predicates
		struct Sentinal
		{
			char m_sentinal;
			Sentinal(char sentinal = 0) :m_sentinal(sentinal) {}
			bool operator ()(char ch) const { return ch == m_sentinal; }
		};
		struct Delimiters
		{
			char const* m_delim; // The null terminated list of delimiters
			Delimiters(char const* delim) :m_delim(delim) {}
			bool operator ()(char ch) const { char const* s = m_delim; for (; *s && *s != ch; ++s) {} return *s != 0; }
		};
		
		// Adapters
		template <typename titer> struct AdpLower
		{
			titer m_src;
			AdpLower(titer src) :m_src(src) {}
			AdpLower& operator ++()         { ++m_src; return *this; }
			char      operator * () const   { return static_cast<char>(::tolower(*m_src)); }
		};
		template <typename titer>           inline AdpLower<titer>  Lower(titer src)       { return AdpLower<titer>(src); }
		template <typename tchar, size_t N> inline AdpLower<tchar*> Lower(tchar (&src)[N]) { return AdpLower<tchar*>(src); }
		
		// CRC32 table - the polynomial '0x1EDC6F41' is chosen because that's what the intrinsics are based on
		// Hopefully, one day I can optionally compile in the intrinsic versions without changing the generated hashes
		template <typename Uint, Uint Poly> struct CRCTable
		{
			Uint m_data[256];
			CRCTable()
			{
				for (uint i = 0; i != 256; ++i)
				{
					Uint part = i;
					for (int j = 0; j != 8; ++j) part = (part >> 1) ^ ((part & 1) ? Poly : 0);
					m_data[i] = part;
				}
			}
			static Uint const* Data() { static CRCTable table; return table.m_data; }
		};
		typedef CRCTable<uint   , 0x1EDC6F41U>   CRCTable32;
		typedef CRCTable<uint64 , 0x1EDC6F41ULL> CRCTable64;
		
		// Convert a contiguous block of bytes to a hash
		inline HashValue HashData(void const* data, size_t size, HashValue hash = -1)
		{
			uint const*  table = CRCTable32::Data();
			uchar const* src   = static_cast<uchar const*>(data);
			uint&        h     = reinterpret_cast<uint&>  (hash);
			for (; size--; ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}
		
		// Convert a contiguous block of bytes into a 64bit hash
		inline HashValue64 HashData64(void const* data, size_t size, HashValue64 hash = -1LL)
		{
			uint64 const* table = CRCTable64::Data();
			uchar const*  src   = static_cast<uchar const*>(data);
			uint64&       h     = reinterpret_cast<uint64&>(hash);
			for (; size--; ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}
		
		// Hash a range of bytes
		template <typename Iter> inline HashValue HashC(Iter first, Iter last, HashValue hash = -1)
		{
			uint const* table = CRCTable32::Data();
			uint&       h     = reinterpret_cast<uint&>(hash);
			for (; !(first == last); ++first) { h = table[(h ^ *first) & 0xff] ^ (h >> 8); }
			return hash;
		}
		
		// Convert a character source into a hash value. Reads from '*src' until 'term(*src)' is true
		template <typename Iter, typename Term> inline HashValue Hash(Iter& src, Term term, HashValue hash = -1)
		{
			uint const* table = CRCTable32::Data();
			uint&       h     = reinterpret_cast<uint&>(hash);
			for (; !term(*src); ++src) { h = table[(h ^ *src) & 0xff] ^ (h >> 8); }
			return hash;
		}
		template <typename Iter, typename Term> inline HashValue HashC(Iter src, Term term, HashValue hash = -1)
		{
			return Hash<Iter, Term>(src, term, hash);
		}
		
		// Convert the input to lower case before creating a hash
		template <typename Iter, typename Term> inline HashValue HashLwr(Iter src, Term term, HashValue hash = -1)
		{
			return HashC(Lower(src), term, hash);
		}
		template <typename Iter> inline HashValue HashLwr(Iter src, HashValue hash = -1)
		{
			return HashC(Lower(src), Sentinal(), hash);
		}
		
		// Read from 'src' until it returns null
		template <typename Iter> inline HashValue Hash(Iter& src, HashValue hash = -1)
		{
			return Hash<Iter, Sentinal>(src, Sentinal(), hash);
		}
		template <typename Iter> inline HashValue HashC(Iter src, HashValue hash = -1)
		{
			return Hash<Iter, Sentinal>(src, Sentinal(), hash);
		}
		
		// http://www.azillionmonkeys.com/qed/hash.html, © Copyright 2004-2008 by Paul Hsieh
		inline uint FastHash(void const* data, uint len, uint hash)
		{
			// Local function for reading 16bit chunks of 'data'
			struct This { static unsigned short Read16bits(uchar const* d)
			{
				#if (defined(__GNUC__) && defined(__i386__)) || defined(__WATCOMC__) || defined(_MSC_VER) || defined (__BORLANDC__) || defined (__TURBOC__)
				return *reinterpret_cast<unsigned short const*>(d);
				#else
				return (static_cast<uint>(reinterpret_cast<uchar const*>(d)[1]) << 8) +
						static_cast<uint>(reinterpret_cast<uchar const*>(d)[0]);
				#endif
			}};

			if (len == 0 || data == 0)
				return 0;

			uchar const* ptr = static_cast<uchar const*>(data);
			uint tmp;
			uint rem = len & 3;
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
	}
}

#endif
