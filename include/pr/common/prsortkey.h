//***********************************************************************
//
// SortKey
//
//***********************************************************************
#ifndef PR_SORTKEY_H
#define PR_SORTKEY_H

#include "pr/common/PRTypes.h"

namespace pr
{
	typedef uint64 SortKey;

	namespace sortkey
	{
		const SortKey Max = 0xFFFFFFFFFFFFFFFF;
		const SortKey Min = 0x0;

		inline const uint32& High(const SortKey& key) 	{ return reinterpret_cast<const uint32&>(((uint32*)&key)[1]); }
		inline const uint32& Low (const SortKey& key)	{ return reinterpret_cast<const uint32&>(key); }
		inline       uint32& High(      SortKey& key)	{ return reinterpret_cast<      uint32&>(((uint32*)&key)[1]); }
		inline       uint32& Low (      SortKey& key)	{ return reinterpret_cast<      uint32&>(key); }
	}//namespace sortkey
}//namespace pr

#endif//PR_SORTKEY_H
