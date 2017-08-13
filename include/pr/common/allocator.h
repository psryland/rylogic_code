//***********************************************************************
// Default Allocator
//  Copyright (c) 2007 Paul Ryland
//***********************************************************************
#pragma once

#include <malloc.h>
#include <new>
#include <type_traits>
#include <cassert>

#define PR_DBG_MEMORY_ALLOC 0
#ifndef PR_DBG_MEMORY_ALLOC
#ifdef NDEBUG
#define PR_DBG_MEMORY_ALLOC 0 
#else
#define PR_DBG_MEMORY_ALLOC 1
#endif 
#endif

namespace pr
{
	// A C++ standards compliant allocator for use in containers
	template <typename T, int A = std::alignment_of<T>::value> struct aligned_alloc
	{
		using value_type       = T;
		using pointer          = T*;
		using reference        = T&;
		using const_pointer    = T const*;
		using const_reference  = T const&;
		using size_type        = size_t;
		using difference_type  = ptrdiff_t;
		enum { value_alignment = A };

		// constructors
		aligned_alloc() {}
		aligned_alloc(aligned_alloc const&) {}
		template <typename U> aligned_alloc(aligned_alloc<U, A> const&) {}
		template <typename U> struct rebind { typedef aligned_alloc<U, A> other; };

		// std::allocator interface
		size_type     max_size  () const throw()       { return std::numeric_limits<size_type>::max() / sizeof(T); }
		pointer       address(reference x) const       { return &x; }
		const_pointer address(const_reference x) const { return &x; }

		pointer allocate(size_type n, void const* = 0)
		{
			if (n == 0) return nullptr; // Avoid the undefined behaviour of _aligned_malloc(0)
			#if PR_DBG_MEMORY_ALLOC
			auto ptr = static_cast<T*>(_aligned_malloc_dbg(n * sizeof(T), value_alignment, __FILE__, __LINE__));
			#else
			auto ptr = static_cast<T*>(_aligned_malloc(n * sizeof(T), value_alignment));
			#endif
			if (ptr == nullptr) throw std::bad_alloc(); // Allocators should throw bad_alloc on allocation failure
			return ptr;
		}
		void deallocate(pointer p, size_type)
		{
			#if PR_DBG_MEMORY_ALLOC
			_aligned_free_dbg(p);
			#else
			_aligned_free(p);
			#endif
		}

		// enable if helpers
		template <typename U> using enable_if_pod    = typename std::enable_if< std::is_pod<U>::value>::type;
		template <typename U> using enable_if_nonpod = typename std::enable_if<!std::is_pod<U>::value>::type;
		template <typename U> using enable_if_can_cc = typename std::enable_if<std::is_copy_constructible<U>::value>::type;

		// is_pod
		template <typename = enable_if_pod<T>> void default_construct(void* p, char = 0)
		{
			*static_cast<T*>(p) = T();
		}
		template <typename = enable_if_pod<T>> void move_construct(void* p, T&& val, char = 0)
		{
			*static_cast<T*>(p) = val;
		}
		template <typename = enable_if_pod<T>> void copy_construct(void* p, T const& val, char = 0)
		{
			*static_cast<T*>(p) = val;
		}
		template <class U, typename = enable_if_pod<U>> void destroy(U* p, char = 0)
		{
			if (p == nullptr) return;
			#if PR_DBG_MEMORY_ALLOC
			::memset(p, 0xdd, sizeof(U));
			#endif
		}

		// !is_pod
		template <typename = enable_if_nonpod<T>> void default_construct(void* p, int = 0)
		{
			::new (p) T();
		}
		template <typename = enable_if_nonpod<T>> void move_construct(void* p, T&& val, int = 0)
		{
			::new (p) T(std::forward<T&&>(val));
		}
		template <typename = enable_if_nonpod<T>, typename = enable_if_can_cc<T>> void copy_construct(void* p, T const& val, int = 0)
		{
			::new (p) T(val);
		}
		template <class U, typename = enable_if_nonpod<T>> void destroy(U* p, int = 0)
		{
			if (p == nullptr) return;
			p->~U();
			#if PR_DBG_MEMORY_ALLOC
			::memset(p, 0xdd, sizeof(U));
			#endif
		}

		// forwarding constructor
		template <typename... Args> void construct(void* p, Args&&... args)
		{
			::new (p) T(std::forward<Args>(args)...);
		}

		// helpers
		T* New()
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
	};
	template <typename T, typename U> inline bool operator == (aligned_alloc<T> const&, aligned_alloc<U> const&) { return true; }
	template <typename T, typename U> inline bool operator != (aligned_alloc<T> const&, aligned_alloc<U> const&) { return false; }
	template <typename T, typename U> inline bool operator == (aligned_alloc<T> const&, U const&) { return false; }
	template <typename T, typename U> inline bool operator != (aligned_alloc<T> const&, U const&) { return true; }
	template <typename T, typename U> inline bool operator == (T const&, aligned_alloc<U> const&) { return false; }
	template <typename T, typename U> inline bool operator != (T const&, aligned_alloc<U> const&) { return true; }

	// Deprecated:
	// Use by physics still

	// Allocation interface
	typedef void* (*AllocFunction  )(std::size_t size, std::size_t alignment);
	typedef void  (*DeallocFunction)(void* p);

	inline void* DefaultAlloc  (std::size_t size, std::size_t alignment = 1) { return _aligned_malloc(size, alignment); }
	inline void  DefaultDealloc(void* p)                                     { return _aligned_free(p); }

	// A struct for allocating blocks of memory
	struct DefaultAllocator
	{
		static void* Alloc(std::size_t size, std::size_t alignment = 1) { return DefaultAlloc(size, alignment); }
		static void  Dealloc(void* p)                                   { return DefaultDealloc(p); }
	};
	inline bool operator == (DefaultAllocator const&, DefaultAllocator const&) { return true; }

	// An allocator that complains if allocation is requested
	struct NoAllocationAllocator
	{
		static void* Alloc(std::size_t, std::size_t)                    { assert(false && "Allocation made."); return 0; }
		static void  Dealloc(void*)                                     { assert(false && "What the hell are you deleting?!?"); }
	};
	inline bool operator == (NoAllocationAllocator const&, NoAllocationAllocator const&) { return true; }
}
