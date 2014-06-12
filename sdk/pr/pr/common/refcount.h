//************************************************************************
// Ref counting base class
//  Copyright (c) Rylogic Ltd 2011
//************************************************************************
// Usage:
//   struct Thing :pr::RefCount<Thing> {};
//   Thing* thing = new Thing();
//   thing->AddRef();
//   thing->Release(); // thing deleted here
//
//   Typically this would be used with pr::RefPtr<Thing>.
//   e.g.
//   {
//       RefPtr<Thing> ptr(new Thing()); // thing deleted when last reference goes out of scope
//   }
//
#pragma once

#include <windows.h>

namespace pr
{
	// Reference counting mix-in base class
	// 'Deleter' is a type containing a static function with signature: 'void RefCountZero(RefCount* obj)'
	// Its purpose is to release resources owned by the ref counted object because there are no more references to it.
	// 'RefCount' itself contains a 'RefCountZero' function so that clients can use 'RefCount<Derived>'
	// which will pick up the default behaviour of deleting the ref counted object when the count hits zero
	// 'Shared' should be true if AddRef()/Release() can be called from multiple threads
	template <typename Deleter, bool Shared = true> struct RefCount
	{
		mutable volatile long m_ref_count;

		RefCount()
			:m_ref_count(0)
		{}

		virtual ~RefCount()
		{}

		RefCount(RefCount&& rhs)
			:m_ref_count(rhs.m_ref_count)
		{
			rhs.m_ref_count = 0;
		}

		RefCount& operator = (RefCount&& rhs)
		{
			std::swap(m_ref_count, rhs.m_ref_count);
			return *this;
		}

		long AddRef() const
		{
			return Shared ? ::InterlockedIncrement(&m_ref_count) : ++m_ref_count;
		}
		
		long Release() const
		{
			assert(m_ref_count > 0);
			long ref_count = Shared ? ::InterlockedDecrement(&m_ref_count) : --m_ref_count;
			if (!ref_count) { Deleter::RefCountZero(const_cast<RefCount<Deleter,Shared>*>(this)); }
			return ref_count;
		}
		
		static void RefCountZero(RefCount<Deleter,Shared>* doomed)
		{
			delete doomed;
		}

	private:
		RefCount(RefCount const&) // Ref counted objects should be copyable
			:m_ref_count(0)
		{
			// This object has just been constructed, therefore AddRef() has
			// never been called meaning there are no references to it,
			// even tho the object we're copying may have references.
		}
		RefCount& operator = (RefCount const& rhs)
		{
			// See notes in copy constructor
			if (&rhs != this) m_ref_count = 0;
			return *this;
		}
	};

	// Return the current refcount for a ref pointer
	template <typename T> inline long PtrRefCount(T* ptr)
	{
		if (!ptr) return 0;

		// A crash here indicates that 'ptr' has already been released.
		// If ptr is a D3DPtr, check that two or more D3DPtrs haven't
		// been created from the same raw pointer. e.g.
		// ID3DInterface* raw (ref count = 1)
		// D3DPtr p0(raw) (ref count = 1 still because the DecRef in the constructor)
		// D3DPtr p1(raw) (ref count = 1 still because the DecRef in the constructor)
		// p1->~D3DPtr()  (ref count = 0)
		// p0->~D3DPtr()  "app.exe has triggered a break point" (i.e. crashed)
		//
		// Watch out for:
		//   D3DPtr<IBlah> p = CorrectlyCreatedBlah();
		//   D3DPtr<IBlahBase> b = p.m_ptr; -- this is wrong, it should be: b = p;
		long count = ptr->AddRef() - 1;
		ptr->Release();
		return count;
	}
}
