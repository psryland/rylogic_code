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

#include <cassert>
#include <type_traits>
#include "pr/common/refcount.h"

#define PR_REFPTR_TRACE 0
#if PR_REFPTR_TRACE == 1
	#include <d3d11.h>
	#include "pr/common/fmt.h"
	#include "pr/common/assert.h"
	#include "pr/win32/stackdump.h"
#endif

namespace pr
{
	#if PR_REFPTR_TRACE == 1
	// A function prototype for clients to implement/specialise to aid stack traces, etc
	// Example code:
	// #define PR_REFPTR_TRACE 1
	// #include "pr/common/assert.h"
	// #include "pr/win32/stackdump.h"
	// template <typename T> inline long PtrRefCount(T*);
	// template <typename T> inline void RefPtrTrace(bool, T*){}
	// template <> inline void RefPtrTrace<ID3D11Buffer>(bool add, ID3D11Buffer* ptr)
	// {
	//    OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
	//    pr::DumpStack([](std::string const& name, std::string const& file, int line)
	//    {
	//        OutputDebugStringA(pr::FmtS("%s(%d): %s\n", file.c_str(), line, name.c_str()));
	//    },3,5);
	// }
	template <typename T> void RefPtrTrace(bool,T*) {}
	template <typename T> long PtrRefCount(T*);
	#endif

	// A ptr wrapper to a reference counting object.
	// 'T' should have methods 'AddRef' and 'Release'
	// Not the same as std::shared_ptr<> because it assumes
	// the pointed-to object has AddRef()/Release() methods
	template <typename T>
	struct RefPtr
	{
		mutable T* m_ptr;

		// Default Construct
		RefPtr()
			:m_ptr(nullptr)
		{}

		// Construct from nullptr
		RefPtr(nullptr_t)
			:m_ptr(nullptr)
		{}

		// Move construct
		RefPtr(RefPtr&& rhs)
			:m_ptr(rhs.m_ptr)
		{
			rhs.m_ptr = nullptr;
		}

		// Copy construct
		RefPtr(RefPtr const& rhs)
			:m_ptr(rhs.m_ptr)
		{
			if (m_ptr)
				IncRef(m_ptr);
		}

		// Move construct from convertible pointer
		template<class U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr(RefPtr<U>&& rhs)
			:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			rhs.m_ptr = nullptr;
		}

		// Copy construct from convertible pointer
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr(RefPtr<U> const& rhs)
			:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			if (m_ptr)
				IncRef(m_ptr);
		}

		// Construct from a raw pointer that is convertible to 'T'
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr(U* t, bool add_ref)
			:m_ptr(t)
		{
			if (!m_ptr) return;

			// There are two common cases:
			//  - the object is created with an initial ref count of zero (COM case).
			//  - the object is created with an initial ref count of one (DirectX case)
			if (add_ref)
				IncRef(m_ptr);
		}

		// Destructor
		~RefPtr()
		{
			if (m_ptr)
				DecRef(m_ptr);
		}

		// Assignment to null
		RefPtr& operator = (nullptr_t)
		{
			if (m_ptr) DecRef(m_ptr);
			m_ptr = nullptr;
			return *this;
		}

		// Move assign
		RefPtr& operator = (RefPtr&& rhs)
		{
			if (this == &rhs) return *this;
			auto ptr = m_ptr;
			m_ptr = rhs.m_ptr;
			rhs.release();
			if (ptr) DecRef(ptr);
			return *this;
		}

		// Copy assign
		RefPtr& operator = (RefPtr const& rhs)
		{
			if (this == &rhs) return *this;
			auto ptr = m_ptr;
			m_ptr = rhs.m_ptr;
			if (m_ptr) IncRef(m_ptr);
			if (ptr) DecRef(ptr);
			return *this;
		}

		// Move assign to convertible pointer type
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr& operator = (RefPtr<U>&& rhs)
		{
			auto ptr = m_ptr;
			m_ptr = static_cast<T*>(rhs.m_ptr);
			rhs.release();
			if (ptr) DecRef(ptr);
			return *this;
		}

		// Copy assign to convertible pointer type
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		RefPtr& operator = (RefPtr<U> const& rhs)
		{
			auto ptr = m_ptr;
			m_ptr = static_cast<T*>(rhs.m_ptr);
			if (m_ptr) IncRef(m_ptr);
			if (ptr) DecRef(ptr);
			return *this;
		}

		// Conversion to bool
		explicit operator bool() const
		{
			return m_ptr != nullptr;
		}

		// Implicit conversion to base class
		template <typename U, class = typename std::enable_if<std::is_convertible<U*,T*>::value>::type>
		operator RefPtr<U>() const
		{
			return RefPtr<U>(static_cast<U*>(m_ptr), true);
		}

		// Pointer de-reference
		T* operator -> () const
		{
			return m_ptr;
		}

		// The current reference count
		long RefCount() const
		{
			if (!m_ptr) return 0;
			auto count = m_ptr->AddRef() - 1;
			m_ptr->Release();
			return count;
		}

		// std::unique_ptr<> interface
		// Don't implement 'reset' because it's ambiguous whether to AddRef
		// the provided pointer. Use a RefPtr constructor then assign.
		T* get() const
		{
			return m_ptr;
		}
		T* release()
		{
			auto ptr = m_ptr;
			m_ptr = nullptr;
			return ptr;
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

			// Before releasing the reference, check that there is at least one reference to release.
			// This test will probably crash rather than assert, but hey, good enough.
			// If this fails, check that two or more RefPtrs haven't been created from the same raw pointer.
			// e.g.
			//   ID3DInterface* raw (ref count = 1)
			//   D3DPtr p0(raw) (ref count = 1 still because the following DecRef)
			//   D3DPtr p1(raw) (ref count = 1 still because the following DecRef)
			//   p1->~D3DPtr()  (ref count = 0)
			//   p0->~D3DPtr()  "app.exe has triggered a break point" (i.e. crashed)
			#if PR_REFPTR_TRACE == 1
			assert(pr::PtrRefCount(ptr) > 0 && "Pointer reference count is 0");
			#endif
			ptr->Release();
		}

	public:

		friend bool operator == (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr == rhs.m_ptr; }
		friend bool operator != (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr != rhs.m_ptr; }
		friend bool operator <  (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr < rhs.m_ptr; }
		friend bool operator >  (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr > rhs.m_ptr; }
		friend bool operator <= (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr <= rhs.m_ptr; }
		friend bool operator >= (RefPtr const& lhs, RefPtr const& rhs) { return lhs.m_ptr >= rhs.m_ptr; }
		friend bool operator == (RefPtr const& lhs, T const* rhs) { return lhs.m_ptr == rhs; }
		friend bool operator != (RefPtr const& lhs, T const* rhs) { return lhs.m_ptr != rhs; }
		friend bool operator == (T const* lhs, RefPtr const& rhs) { return lhs == rhs.m_ptr; }
		friend bool operator != (T const* lhs, RefPtr const& rhs) { return lhs != rhs.m_ptr; }
		friend bool operator == (RefPtr const& lhs, nullptr_t) { return lhs.m_ptr == nullptr; }
		friend bool operator != (RefPtr const& lhs, nullptr_t) { return lhs.m_ptr != nullptr; }
	};

	// Implementation
	namespace impl
	{
		template <typename T> inline long RefCount(T* ptr, decltype(&T::m_ref_count)*)
		{
			return ptr->m_ref_count;
		}
		template <typename T> inline long RefCount(T* ptr, ...)
		{
			auto count = ptr->AddRef();
			ptr->Release();
			return count - 1;
		}
	}

	// The interface required by RefPtr.
	// Note, the 'T' in RefPtr<T> doesn't actually need to inherit this interface
	// due to template duck-typing, but it can be useful for RefPtr<IRefCounted>
	// pointers to different typed objects.
	struct IRefCounted
	{
		virtual ~IRefCounted() {}
		virtual long AddRef() const = 0;
		virtual void Release() const = 0;
	};

	// Check RefPtr size
	static_assert(sizeof(RefPtr<IRefCounted>) == sizeof(void*), "Must be the same size as a raw pointer so arrays of RefPtrs can be cast to arrays of raw pointers");

	// Return the current ref count for a ref pointer
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
		return impl::RefCount(ptr, nullptr);
	}

	// Some helper trace methods
	#if PR_REFPTR_TRACE == 1
	//template <> inline void RefPtrTrace<ID3D11Device>(bool add, ID3D11Device* ptr)
	//{
	//	OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
	//	pr::DumpStack([](std::string const& name, std::string const& file, int line){ OutputDebugStringA(pr::FmtS("%s(%d): %s\n", file.c_str(), line, name.c_str())); },3,5);
	//}
	//template <> inline void RefPtrTrace<ID3D11Buffer>(bool add, ID3D11Buffer* ptr)
	//{
	//	OutputDebugStringA(pr::FmtS("[%s] - [%p] - Count = %d\n", add ? "AddRef" : "Release", ptr, PtrRefCount(ptr)));
	//	pr::DumpStack([](std::string const& name, std::string const& file, int line){ OutputDebugStringA(pr::FmtS("%s(%d): %s\n", file.c_str(), line, name.c_str())); },3,5);
	//}
	#endif
}

