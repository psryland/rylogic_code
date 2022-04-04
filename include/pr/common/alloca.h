//******************************************
// AllocA macro
//  Copyright (c) March 2008 Paul Ryland
//******************************************
#pragma once

#include <new>

namespace pr
{
	// Construct a range
	template <typename Type, typename Iter> inline Iter construct(Iter first, Iter last)
	{
		for (Iter i = first; i != last; ++i) { new (&*i) Type; }
		return first;
	}
	template <typename Type, typename Iter> inline Iter construct(Iter first, std::size_t count)
	{
		for (Iter i = first; count--; ++i) { new (&*i) Type; }
		return first;
	}

	// Destruct a range
	template <typename Type, typename Iter> inline void destruct(Iter first, Iter last)
	{
		for (Iter i = first; i != last; ++i) { i->~Type(); }
	}
	template <typename Type, typename Iter> inline void destruct(Iter first, std::size_t count)
	{
		for (Iter i = first; count--; ++i) { i->~Type(); }
	}

	// Helper types for constructing and destructing objects allocated using _alloca() or _malloca()
	template <typename Type> struct AllocAScope
	{
		Type*& m_ptr;
		size_t m_count;

		AllocAScope(Type*& ptr, size_t count, void* mem) :m_ptr(ptr) ,m_count(count) { m_ptr = construct<Type>(static_cast<Type*>(mem), m_count); }
		~AllocAScope()                                                               { destruct<Type>(m_ptr, m_count); m_ptr = 0;}

	private:
		AllocAScope(AllocAScope const&); // no copying
		AllocAScope& operator=(AllocAScope const&); // no copying
	};
	template <typename Type> struct MAllocAScope
	{
		Type*& m_ptr;
		size_t m_count;

		MAllocAScope(Type*& ptr, size_t count, void* mem) :m_ptr(ptr) ,m_count(count) { m_ptr = construct<Type>(static_cast<Type*>(mem), m_count); }
		~MAllocAScope()                                                               { destruct<Type>(m_ptr, m_count); _freea(m_ptr); m_ptr = 0; }

	private:
		MAllocAScope(MAllocAScope const&); // no copying
		MAllocAScope& operator=(MAllocAScope const&); // no copying
	};
}

// Notes about _alloca()
// -Do not use try/catch for _alloca, use __try/__except,
//  since _alloca throws Structured Exceptions, not C++ exceptions.
// -In Windows XP, if _malloca is called inside a try/catch block,
//  you must call _resetstkoflw in the catch block.
// -Do not use _alloca in:
//   Windows NT SEH exception filter expression: __except (_malloca () )
//   Windows NT SEH final exception handler:    __finally {_malloca () }
//   C++ exception handler catch clause expression


// Allocate stack memory for an array of POD types (no constructors called)
// Use: MyType* ptr = PR_ALLOCA_POD(MyType, 4);
#define PR_ALLOCA_POD(Type, Count)\
	static_cast<Type*>(_alloca(sizeof(Type) * (Count)))


// Allocate stack memory for an array of any type (constructors and destructors called)
// Use: auto ptr = PR_ALLOCA(ptr, MyType, 4);
#define PR_ALLOCA(pointer, type, count)\
	static_cast<type*>(nullptr);\
	pr::AllocAScope<type> alloca_scope_##pointer(pointer, (count), _alloca((count)*sizeof(type)))


// Allocate stack or heap memory for an array of any type (constructors and destructors called)
// Use: auto ptr = PR_MALLOCA(ptr, MyType, 4);
#define PR_MALLOCA(pointer, type, count)\
	static_cast<type*>(nullptr);\
	pr::MAllocAScope<type> malloca_scope_##pointer(pointer, (count), _malloca((count)*sizeof(type)))
