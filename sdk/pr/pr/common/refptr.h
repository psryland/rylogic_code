//**********************************************************************************
// Pointer to a reference counted object
//  Copyright (c) Rylogic Ltd 2011
//**********************************************************************************
// Use this pointer to point to objects with 'AddRef' and 'Release' functions
// Note: types that inherit pr::RefCount<> can be used
//
// Use the following registry key to prevent the debugger stepping into this type:
//  [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\NativeDE\StepOver]
//  "pr::RefPtr"="pr\\:\\:RefPtr.*\\:\\:.*=NoStepInto"
// Inheritors use the IncRef/DecRef methods, so that stack tracing works

#pragma once

#include <type_traits>

#define PR_REFPTR_TRACE 0
#if PR_REFPTR_TRACE == 1
#include "pr/common/assert.h"
#include "pr/common/stackdump.h"
#endif

namespace pr
{
	#if PR_REFPTR_TRACE == 1
	// A function prototype for clients to implement/specialise to aid stack traces, etc
	// Example code:
	// #define PR_REFPTR_TRACE 1
	// template <typename T> inline long PtrRefCount(T*);
	// template <typename T> inline void RefPtrTrace(bool, T*){}
	// template <> inline void RefPtrTrace<ID3D11Buffer>(bool add, ID3D11Buffer* ptr)
	// {
	//    OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
	//    pr::StackDump(3,5,[](std::string const& file, int line)
	//    {
	//        OutputDebugStringA(pr::FmtS("%s(%d):\n", file.c_str(), line));
	//    });
	// }
	template <typename T> void RefPtrTrace(bool,T*) {}
	template <typename T> long PtrRefCount(T*);
	#endif

	// A ptr wrapper to a reference counting object.
	// 'T' should have methods 'AddRef' and 'Release'
	// Not the same as std::shared_ptr<> because it assumes
	// the pointed to object has AddRef()/Release() methods
	template <typename T> struct RefPtr
	{
		mutable T* m_ptr;

		RefPtr()
			:m_ptr(nullptr)
		{}

		RefPtr(nullptr_t)
			:m_ptr(nullptr)
		{}

		// Construct from any pointer convertable to 'T'
		template <typename U> RefPtr(U* t)
			:m_ptr(t)
		{
			if (m_ptr)
				IncRef(m_ptr);
		}

		// Copy construct
		RefPtr(RefPtr const& rhs)
			:m_ptr(rhs.m_ptr)
		{
			if (m_ptr)
				IncRef(m_ptr);
		}

		// Copy construct from convertable pointer
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr(RefPtr<U> const& rhs)
			:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			if (m_ptr)
				IncRef(m_ptr);
		}

		// Move construct
		RefPtr(RefPtr&& rhs)
			:m_ptr(rhs.m_ptr)
		{
			rhs.m_ptr = nullptr;
		}

		// Move construct from convertable pointer
		template<class U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr(RefPtr<U>&& rhs)
			:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			rhs.m_ptr = nullptr;
		}

		// Destructor
		~RefPtr()
		{
			if (m_ptr)
				DecRef(m_ptr);
		}

		// Assignment
		RefPtr& operator = (nullptr_t)
		{
			if (m_ptr) DecRef(m_ptr);
			m_ptr = nullptr;
			return *this;
		}
		RefPtr& operator = (RefPtr const& rhs)
		{
			if (this != &rhs)
			{
				T* ptr = m_ptr;
				m_ptr = rhs.m_ptr;
				if (m_ptr) IncRef(m_ptr);
				if (ptr) DecRef(ptr);
			}
			return *this;
		}
		RefPtr& operator = (RefPtr&& rhs)
		{
			std::swap(m_ptr, rhs.m_ptr);
			return *this;
		}
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr& operator = (RefPtr<U> const& rhs)
		{
			if (m_ptr != static_cast<T*>(rhs.m_ptr))
			{
				T* ptr = m_ptr;
				m_ptr = static_cast<T*>(rhs.m_ptr);
				if (m_ptr) IncRef(m_ptr);
				if (ptr) DecRef(ptr);
			}
			return *this;
		}
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr& operator = (RefPtr<U>&& rhs)
		{
			std::swap(m_ptr, static_cast<T*>(rhs.m_ptr));
			return *this;
		}

		// Implicit conversion to bool
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
		operator bool_type() const
		{
			return m_ptr != nullptr ? &bool_tester::x : static_cast<bool_type>(nullptr);
		}

		// Implicit conversion to base class
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		operator RefPtr<U>() const
		{
			return RefPtr<U>(static_cast<U*>(m_ptr));
		}

		// Pointer deref
		T* operator -> () const { return m_ptr; }

		// The current reference count
		long RefCount() const
		{
			if (!m_ptr) return 0;
			long count = m_ptr->AddRef() - 1;
			m_ptr->Release();
			return count;
		}

	protected:
		long IncRef(T* ptr) const
		{
			#if PR_REFPTR_TRACE == 1
			RefPtrTrace(true, ptr);
			#endif
			return ptr->AddRef();
		}
		void DecRef(T* ptr) const
		{
			#if PR_REFPTR_TRACE == 1
			RefPtrTrace(false, ptr);
			#endif

			// Before releasing the reference, check that there is at least one reference to release
			// This test will probably crash rather than assert, but hey, good enough.
			// If this fails, check that two or more D3DPtrs haven't been created from the same raw pointer.
			// e.g.
			//   ID3DInterface* raw (ref count = 1)
			//   D3DPtr p0(raw) (ref count = 1 still because the following DecRef)
			//   D3DPtr p1(raw) (ref count = 1 still because the following DecRef)
			//   p1->~D3DPtr()  (ref count = 0)
			//   p0->~D3DPtr()  "app.exe has triggered a break point" (i.e. crashed)
			assert(pr::PtrRefCount(ptr) > 0 && "Pointer reference count is 0");
			ptr->Release();
		}
	};
	static_assert(sizeof(RefPtr<void>) == sizeof(void*), "Must be the same size as a raw pointer so arrays of RefPtrs can be cast to arrays of raw pointers");

	template <typename T> inline bool operator == (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr == rhs.m_ptr; }
	template <typename T> inline bool operator != (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr != rhs.m_ptr; }
	template <typename T> inline bool operator <  (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr <  rhs.m_ptr; }
	template <typename T> inline bool operator >  (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr >  rhs.m_ptr; }
	template <typename T> inline bool operator <= (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr <= rhs.m_ptr; }
	template <typename T> inline bool operator >= (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr >= rhs.m_ptr; }

	// Some helper trace methods
	#if PR_REFPTR_TRACE == 1
	#if defined(__d3d11_h__)
		template <> inline void RefPtrTrace<ID3D11Device>(bool add, ID3D11Device* ptr)
		{
			OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
			pr::StackDump(3,5,[](std::string const& file, int line){ OutputDebugStringA(pr::FmtS("%s(%d):\n", file.c_str(), line)); });
		}
		template <> inline void RefPtrTrace<ID3D11Buffer>(bool add, ID3D11Buffer* ptr)
		{
			OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
			pr::StackDump(3,5,[](std::string const& file, int line){ OutputDebugStringA(pr::FmtS("%s(%d):\n", file.c_str(), line)); });
		}
	#endif
	#endif
}
namespace std
{
	template <typename T> void swap(pr::RefCount<T>& lhs, pr::RefCount<T>& rhs)
	{
		swap(lhs.m_ref_count, rhs.m_ref_count);
	}
}
