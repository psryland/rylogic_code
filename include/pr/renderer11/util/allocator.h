//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// This file contains a set of helper wrappers for initialising some d3d11 structures
#pragma once
#ifndef PR_RDR_UTIL_ALLOCATOR_H
#define PR_RDR_UTIL_ALLOCATOR_H

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
			Allocator(MemFuncs funcs) :MemFuncs(funcs) {}
			Allocator(Allocator const& rhs) :MemFuncs(rhs) {}
			template <typename U> Allocator(Allocator<U> const& rhs) :MemFuncs(rhs) {}
			template <typename U> struct rebind { typedef Allocator<U> other; };

			// std::allocator interface
			pointer       address   (reference x) const               { return &x; }
			const_pointer address   (const_reference x) const         { return &x; }
			pointer       allocate  (size_type n, void const* =0)     { return static_cast<pointer>(m_alloc(n * sizeof(T), value_alignment)); }
			void          deallocate(pointer p, size_type)            { m_dealloc(p); }
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
	}
}

#endif
