//*****************************************************************************************
// New
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************************
// Helpers for new-ing objects

#pragma once

#include <new>
#include <memory>

#define PR_TRACK_ALIGNED_ALLOCATIONS 0
#if PR_TRACK_ALIGNED_ALLOCATIONS
#   include <unordered_set>
#   include <exception>
#endif

namespace pr
{
	namespace impl
	{
		#if PR_TRACK_ALIGNED_ALLOCATIONS
		template <size_t Alignment> inline std::unordered_set<void*>& Allocations()
		{
			static std::unordered_set<void*> s_allocs; // warning, careful with multithreading..
			return s_allocs;
		};
		#endif

		template <size_t Alignment> inline void* AlignedAlloc(size_t count)
		{
			#if PR_TRACK_ALIGNED_ALLOCATIONS

			void* obj = _aligned_malloc_dbg(count, Alignment, __FILE__, __LINE__);
			Allocations<Alignment>().insert(obj);
			return obj;

			#else

			return _aligned_malloc(count, Alignment);

			#endif
		}
		template <size_t Alignment> inline void AlignedFree(void* obj)
		{
			#if PR_TRACK_ALIGNED_ALLOCATIONS

			if (Allocations<Alignment>().erase(obj) != 1)
				throw std::exception("This object was not allocated with the AlignedAlloc function or with the same alignment");
			_aligned_free_dbg(obj);

			#else

			_aligned_free(obj);

			#endif
		}
	}

	// This is basically the same as std::make_unique except that it calls
	// operator new() on 'T'. std::make_unique uses placement new instead
	template <typename T, typename... Args>
	inline std::unique_ptr<T> New(Args&&... args)
	{
		return std::unique_ptr<T>(new T(std::forward<Args>(args)...));
	}

	// Use as a mix in base class
	template <size_t Alignment> struct AlignTo
	{
		// Overload operator new/delete to ensure alignment
		static void* __cdecl operator new(size_t count)
		{
			return pr::impl::AlignedAlloc<Alignment>(count);
		}
		static void __cdecl operator delete (void* obj)
		{
			pr::impl::AlignedFree<Alignment>(obj);
		}
	};

	// Use when inheritance isn't desirable
	#define PR_ALIGNED_OPERATOR_NEW(alignment)\
		void* __cdecl operator new (size_t count) { return pr::impl::AlignedAlloc<alignment>(count); }\
		void  __cdecl operator delete (void* obj) { pr::impl::AlignedFree<alignment>(obj); }
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
