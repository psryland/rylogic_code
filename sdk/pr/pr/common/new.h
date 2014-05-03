//*****************************************************************************************
// New
//  Copyright © Rylogic Ltd 2014
//*****************************************************************************************
// Helpers for new-ing objects

#pragma once
#ifndef PR_COMMON_NEW_H
#define PR_COMMON_NEW_H

#include <new>
#include <memory>

namespace pr
{
	template <typename T, typename... Args>
	inline std::unique_ptr<T> New(Args&&... args)
	{
		return std::unique_ptr<T>(new T(std::forward<Args>(args)...));
	}

	// Use as a mix in base class
	template <size_t Alignment> struct AlignTo
	{
		// Overload operator new/delete to ensure alignment
		void* __cdecl operator new(size_t count) { return _aligned_malloc(count, Alignment); }
		void __cdecl operator delete (void* obj) { _aligned_free(obj); }
	};

	// Use when inheritance isn't desirable
	#define PR_ALIGNED_OPERATOR_NEW(alignment)\
		void* __cdecl operator new (size_t count) { return _aligned_malloc(count, alignment); }\
		void  __cdecl operator delete (void* obj) { _aligned_free(obj); }
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/meta/alignment_of.h"

namespace pr
{
	namespace unittests
	{
		struct Wotzit :pr::AlignTo<32>
		{
			int m_int;
			Wotzit() :m_int() {}
			Wotzit(int i, int j, int k) :m_int(i+j+k) {}
		};

		PRUnitTest(pr_common_new)
		{
			auto p = pr::New<Wotzit>();
			PR_CHECK(0, p->m_int);
			PR_CHECK(pr::meta::is_aligned_to<32>(p.get()), true);

			p = pr::New<Wotzit>(1,2,3);
			PR_CHECK(6, p->m_int);
			PR_CHECK(pr::meta::is_aligned_to<32>(p.get()), true);
		}
	}
}
#endif
#endif
