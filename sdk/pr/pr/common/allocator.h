//***********************************************************************
// Default Allocator
//  Copyright (c) 2007 Paul Ryland
//***********************************************************************
#pragma once

#include <malloc.h>
#include <new>

namespace pr
{
	// A C++ standards compliant allocator for use in containers
	template <typename T> struct aligned_alloc
	{
		typedef T         value_type;
		typedef T*        pointer;
		typedef T&        reference;
		typedef T const*  const_pointer;
		typedef T const&  const_reference;
		typedef size_t    size_type;
		typedef ptrdiff_t difference_type;
		enum
		{
			value_alignment = std::alignment_of<T>::value
		};

		// constructors
		aligned_alloc() {}
		aligned_alloc(aligned_alloc const&) {}
		template <typename U> aligned_alloc(aligned_alloc<U> const&) {}
		template <typename U> struct rebind { typedef aligned_alloc<U> other; };

		// std::allocator interface
		pointer       address   (reference x) const               { return &x; }
		const_pointer address   (const_reference x) const         { return &x; }
		pointer       allocate  (size_type n, void const* =0)     { return static_cast<T*>(_aligned_malloc(n * sizeof(T), value_alignment)); }
		void          deallocate(pointer p, size_type)            { _aligned_free(p); }
		size_type     max_size  () const throw()                  { return std::numeric_limits<size_type>::max() / sizeof(T); }
		void          construct (pointer p)                       { ::new ((void*)p) T(); }
		void          construct (pointer p, const_reference val)  { ::new ((void*)p) T(val); }
		template <class U, class... Args>
		void          construct (U* p, Args&&... args)            { ::new ((void*)p) T(std::forward<Args>(args)...); }
		template <class U>
		void          destroy   (U* p)                            { if (p) p->~U(); }

		// helpers
		T*   New()        { pointer p = allocator(1); construct(p); return p; }
		void Delete(T* p) { destroy(p); deallocate(p, 1); }
	};
	template <typename T, typename U> inline bool operator == (aligned_alloc<T> const& lhs, aligned_alloc<U> const& rhs) { return lhs.m_alloc == rhs.m_alloc && lhs.m_dealloc == rhs.m_dealloc; }
	template <typename T, typename U> inline bool operator != (aligned_alloc<T> const& lhs, aligned_alloc<U> const& rhs) { return !(lhs == rhs); }

	//// Deprecated:

	//// Allocation interface
	//typedef void* (*AllocFunction  )(std::size_t size, std::size_t alignment);
	//typedef void  (*DeallocFunction)(void* p);

	//inline void* DefaultAlloc  (std::size_t size, std::size_t alignment = 1) { return _aligned_malloc(size, alignment); }
	//inline void  DefaultDealloc(void* p)                                     { return _aligned_free(p); }

	//// A struct for allocating blocks of memory
	//struct DefaultAllocator
	//{
	//	static void* Alloc(std::size_t size, std::size_t alignment = 1) { return DefaultAlloc(size, alignment); }
	//	static void  Dealloc(void* p)                                   { return DefaultDealloc(p); }
	//};
	//inline bool operator == (DefaultAllocator const&, DefaultAllocator const&) { return true; }

	//// An allocator that complains if allocation is requested
	//struct NoAllocationAllocator
	//{
	//	static void* Alloc(std::size_t, std::size_t)                    { PR_ASSERT(PR_DBG, false, "Allocation made."); return 0; }
	//	static void  Dealloc(void*)                                     { PR_ASSERT(PR_DBG, false, "What the hell are you deleting?!?"); }
	//};
	//inline bool operator == (NoAllocationAllocator const&, NoAllocationAllocator const&) { return true; }
}
