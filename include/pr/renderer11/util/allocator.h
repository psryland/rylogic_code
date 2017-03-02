//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// This file contains a set of helper wrappers for initialising some d3d11 structures
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Functions the client provides to the renderer
		struct MemFuncs
		{
			typedef void* (__cdecl *AllocFunc  )(size_t size_in_bytes, size_t alignment);
			typedef void  (__cdecl *DeallocFunc)(void* mem);

			AllocFunc   m_alloc;
			DeallocFunc m_dealloc;

			MemFuncs(AllocFunc alloc = _aligned_malloc, DeallocFunc dealloc = _aligned_free)
				:m_alloc(alloc)
				,m_dealloc(dealloc)
			{}
		};

		// An C++ standard compliant allocator that uses the client provided MemFuncs
		template <typename T> struct Allocator :MemFuncs
		{
			// Note: Allocator's are created as temporary objects. Their allocations
			// out- live the allocator so leak detection cannot be implemented in the allocator.
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

			// Constructors
			Allocator(MemFuncs funcs)
				:MemFuncs(funcs)
			{}
			Allocator(Allocator const& rhs)
				:MemFuncs(rhs)
			{}
			template <typename U> Allocator(Allocator<U> const& rhs)
				:MemFuncs(rhs)
			{}
			template <typename U> struct rebind
			{
				using other = Allocator<U>;
			};

			// std::allocator interface
			pointer allocate(size_type n, void const* =0)
			{
				auto p = static_cast<pointer>(m_alloc(n * sizeof(T), value_alignment));
				return p;
			}
			void deallocate(pointer p, size_type)
			{
				m_dealloc(p);
			}
			pointer       address   (reference x) const               { return &x; }
			const_pointer address   (const_reference x) const         { return &x; }
			size_type     max_size  () const                          { return std::numeric_limits<size_type>::max() / sizeof(T); }
			void          construct (pointer p)                       { new (p) T; }
			void          construct (pointer p, const_reference val)  { new (p) T(val); }
			void          destroy   (pointer p)                       { if (p) p->~T(); }
			template <typename U1>             void construct(pointer p, U1 const& parm1)                  { new (p) T(parm1); }
			template <typename U1,typename U2> void construct(pointer p, U1 const& parm1, U2 const& parm2) { new (p) T(parm1,parm2); }

			// helpers
			T*   New()                    { pointer p = allocate(1); construct(p); return p; }
			T*   New(const_reference val) { pointer p = allocate(1); construct(p, val); return p; }
			void Delete(T* p)             { destroy(p); deallocate(p, 1); }

			template <typename... Args> T* New(Args&&... args)
			{
				pointer p = allocate(1);
				new (p) T(std::forward<Args>(args)...);
				return p;
			}
		};
		template <typename T, typename U> inline bool operator == (Allocator<T> const& lhs, Allocator<U> const& rhs) { return lhs.m_alloc == rhs.m_alloc && lhs.m_dealloc == rhs.m_dealloc; }
		template <typename T, typename U> inline bool operator != (Allocator<T> const& lhs, Allocator<U> const& rhs) { return !(lhs == rhs); }

		// Allocation tracker/memory leak detector
		template <typename T = void> struct AllocationsTracker :private pr::vector<T*>
		{
			AllocationsTracker()
			{}
			~AllocationsTracker()
			{
				assert("Memory leaks detected" && empty());
			}
			AllocationsTracker(AllocationsTracker const&) = delete;
			AllocationsTracker& operator =(AllocationsTracker const&) = delete;

			// Returning bool so these can be used in asserts
			bool add(T* ptr)
			{
				push_back(ptr);
				return true;
			}
			bool remove(T* ptr)
			{
				auto iter = std::find(begin(), end(), ptr);
				assert("'ptr' is not a tracked allocation" && iter != end());
				erase_fast(iter);
				return true;
			}
		};
	}
}
