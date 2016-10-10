//*********************************************************
//
//  Cryptography functions
//
//*********************************************************
#pragma once
#ifndef PR_CRYPT_CRYPT_H
#define PR_CRYPT_CRYPT_H

// deprecate, use crc or md5 directly
#include "pr/common/prtypes.h"
#include "pr/common/crc.h"

namespace pr
{
	namespace crypt
	{
		struct  MD5         { uint8 m_key[16];      };
		struct  MD5Context  { uint8 m_context[104]; };
		void    MD5Begin(MD5Context& context);
		void    MD5Add(MD5Context& context, void const* data, uint size);
		void    MD5AddFile(MD5Context& context, char const* filename);
		MD5     MD5End(MD5Context& context);

		// MD5 Operators
		inline bool operator < (MD5 const& a, MD5 const& b)
		{
			const uint8* pa = a.m_key + 16;
			const uint8* pb = b.m_key + 16;
			while (pa != a.m_key) if (*(--pa) != *(--pb)) return *pa < *pb;
			return false;
		}
		inline bool operator == (MD5 const& a, MD5 const& b)
		{
			const uint8* pa = a.m_key + 16;
			const uint8* pb = b.m_key + 16;
			while (pa != a.m_key) if (*(--pa) != *(--pb)) return false;
			return true;
		}
		inline bool operator != (MD5 const& a, MD5 const& b)
		{
			return !(a == b);
		}

	}
}

#endif
