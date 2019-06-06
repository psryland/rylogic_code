//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// This file contains a set of helper wrappers for initialising some d3d11 structures
#pragma once

#include "pr/renderer11/forward.h"

namespace pr::rdr
{
	// Functions the client provides to the renderer
	struct MemFuncs
	{
		using AllocFunc = void* (__cdecl *)(size_t size_in_bytes, size_t alignment);
		using DeallocFunc = void  (__cdecl *)(void* mem);

		AllocFunc   m_alloc;
		DeallocFunc m_dealloc;

		MemFuncs(AllocFunc alloc = _aligned_malloc, DeallocFunc dealloc = _aligned_free)
			:m_alloc(alloc)
			,m_dealloc(dealloc)
		{}
	};

	// An C++ standard compliant allocator that uses the client provided MemFuncs
	template <typename T>
	struct Allocator :MemFuncs
	{
		// Note: Allocator's are created as temporary objects. Their allocations
		// out-live the allocator so leak detection cannot be implemented in the allocator.
		// Also, the allocator can't have any state.
		using value_type = T;
		using pointer = T*;
		using reference = T&;
		using const_pointer = T const*;
		using const_reference = T const&;
		using size_type = size_t;
		using difference_type = ptrdiff_t;
		static int const value_alignment = std::alignment_of<T>::value;

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

		// New/Delete instance
		template <typename... Args>
		T* New(Args&&... args)
		{
			pointer p = allocate(1);
			new (p) T(std::forward<Args>(args)...);
			return p;
		}
		void Delete(T* p)
		{
			destroy(p);
			deallocate(p, 1);
		}
	};
	template <typename T, typename U> inline bool operator == (Allocator<T> const& lhs, Allocator<U> const& rhs) { return lhs.m_alloc == rhs.m_alloc && lhs.m_dealloc == rhs.m_dealloc; }
	template <typename T, typename U> inline bool operator != (Allocator<T> const& lhs, Allocator<U> const& rhs) { return !(lhs == rhs); }

	// Allocation tracker/memory leak detector
	template <typename T = void>
	struct AllocationsTracker
	{
		static bool const RecordCallStacks = false;

		struct Allocation
		{
			using call_stack_t = typename std::conditional<RecordCallStacks, std::string, void*>::type;

			T* m_ptr;
			call_stack_t m_callstack;

			Allocation(T* ptr = nullptr)
				:m_ptr(ptr)
				,m_callstack()
			{}
			
			constexpr size_t operator()(Allocation const& val) const
			{
				// std::hash
				static uint8_t const* zero = nullptr;
				return size_t(reinterpret_cast<uint8_t const*>(val.m_ptr) - zero);
			}
			constexpr bool operator()(Allocation const& lhs, Allocation const& rhs) const
			{
				// std::equal_to
				return lhs.m_ptr == rhs.m_ptr;
			}
		};
		using allocation_container_t = std::unordered_set<Allocation, Allocation, Allocation>;
		allocation_container_t m_live;

		AllocationsTracker()
			:m_live()
		{}
		~AllocationsTracker()
		{
			assert("Memory leaks detected" && m_live.empty());
		}
		AllocationsTracker(AllocationsTracker const&) = delete;
		AllocationsTracker& operator =(AllocationsTracker const&) = delete;

		// Returning bool so these can be used in asserts
		bool add(T* ptr)
		{
			Allocation al(ptr);
			if constexpr (RecordCallStacks)
				pr::DumpStack([&](auto& name, auto& file, auto line){ al.m_callstack.append(pr::FmtS("%s(%d): %s\n", file.c_str(), line, name.c_str())); },1,10);
			
			m_live.insert(al);
			return true;
		}
		bool remove(T* ptr)
		{
			auto iter = m_live.find(ptr);
			assert("'ptr' is not a tracked allocation" && iter != std::end(m_live));
			m_live.erase(iter);
			return true;
		}
	};
}
