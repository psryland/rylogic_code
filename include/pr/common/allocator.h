//***********************************************************************
// Default Allocator
//  Copyright (c) 2007 Paul Ryland
//***********************************************************************
#pragma once

#include <new>
#include <type_traits>
#include <malloc.h>
#include <cassert>

//#define PR_DBG_MEMORY_ALLOC 0
#ifndef PR_DBG_MEMORY_ALLOC
#ifdef NDEBUG
constexpr int PR_DBG_MEMORY_ALLOC = 0;
#else
constexpr int PR_DBG_MEMORY_ALLOC = 1;
#endif 
#endif

namespace pr
{
	// A C++ standards compliant allocator for use in containers
	template <typename T, int A = std::alignment_of_v<T>>
	struct aligned_alloc
	{
		using value_type = T;
		using propagate_on_container_copy_assignment = std::true_type;
		using propagate_on_container_move_assignment = std::true_type;
		using propagate_on_container_swap = std::true_type;
		using is_always_equal = std::true_type;

		static constexpr int value_alignment_v = A;

		// constructors
		aligned_alloc() noexcept
		{}
		template <typename U>
		aligned_alloc(aligned_alloc<U, A> const&) noexcept
		{}
		template <typename U> struct rebind
		{
			using other = aligned_alloc<U, A>;
		};

		// Allocation
		[[nodiscard]] value_type* allocate(size_t n, void const* = 0)
		{
			// Avoid the undefined behaviour of _aligned_malloc(0)
			if (n == 0)
				return nullptr;
			
			value_type* ptr;
			if constexpr (PR_DBG_MEMORY_ALLOC)
				ptr = static_cast<T*>(_aligned_malloc_dbg(n * sizeof(T), value_alignment_v, __FILE__, __LINE__));
			else
				ptr = static_cast<T*>(_aligned_malloc(n * sizeof(T), value_alignment_v));

			if (ptr == nullptr)
				throw std::bad_alloc(); // Allocators should throw bad_alloc on allocation failure

			return ptr;
		}
		void deallocate(value_type* p, size_t)
		{
			if constexpr (PR_DBG_MEMORY_ALLOC)
				_aligned_free_dbg(p);
			else
				_aligned_free(p);
		}

		// helpers
		[[nodiscard]] T* New()
		{
			auto p = allocate(1);
			construct(p);
			return p;
		}
		void Delete(T* p)
		{
			destroy(p);
			deallocate(p, 1);
		}

		friend constexpr bool operator == (aligned_alloc const&, aligned_alloc const&) { return true; }
		friend constexpr bool operator != (aligned_alloc const&, aligned_alloc const&) { return false; }
	};

	// Deprecated:
	// Use by physics still

	// Allocation interface
	typedef void* (*AllocFunction  )(std::size_t size, std::size_t alignment);
	typedef void  (*DeallocFunction)(void* p);

	// Default allocation functions
	inline void* DefaultAlloc(std::size_t size, std::size_t alignment = 1) noexcept
	{
		return _aligned_malloc(size, alignment);
	}
	inline void  DefaultDealloc(void* p) noexcept
	{
		return _aligned_free(p);
	}

	// A struct for allocating blocks of memory
	struct DefaultAllocator
	{
		static void* Alloc(std::size_t size, std::size_t alignment = 1) noexcept
		{
			return DefaultAlloc(size, alignment);
		}
		static void  Dealloc(void* p) noexcept
		{
			return DefaultDealloc(p);
		}
		friend bool operator == (DefaultAllocator const&, DefaultAllocator const&) noexcept
		{
			return true;
		}
	};

	// An allocator that complains if allocation is requested
	struct NoAllocationAllocator
	{
		static void* Alloc(std::size_t, std::size_t) noexcept
		{
			assert(false && "Allocation made.");
			return nullptr;
		}
		static void  Dealloc(void*) noexcept
		{
			assert(false && "What the hell are you deleting?!?");
		}
		friend bool operator == (NoAllocationAllocator const&, NoAllocationAllocator const&) noexcept
		{
			return true;
		}
	};
}
