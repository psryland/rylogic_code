//**********************************************************************************
// Pointer to a reference counted object
//  Copyright © Rylogic Ltd 2011
//**********************************************************************************
// Use this pointer to point to objects with 'AddRef' and 'Release' functions
// Note: types that inherit pr::RefCount<> can be used
//
// Use the following registry key to prevent the debugger stepping into this type:
//  [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\NativeDE\StepOver]
//  "pr::RefPtr"="pr\\:\\:RefPtr.*\\:\\:.*=NoStepInto"
// Inheriters use the IncRef/DecRef methods, so that stack tracing works

#pragma once
#ifndef PR_COMMON_REFPTR_H
#define PR_COMMON_REFPTR_H

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
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
	#if PR_REFPTR_TRACE == 1
	template <typename T> void RefPtrTrace(bool,T*);
	#endif

	// A ptr wrapper to a reference counting object.
	// 'T' should have methods 'AddRef' and 'Release'
	template <typename T> struct RefPtr
	{
		mutable T* m_ptr;
		
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
		
		RefPtr()
		:m_ptr(0)
		{}
		
		RefPtr(T* t)
		:m_ptr(t)
		{
			if (m_ptr)
				IncRef(m_ptr);
		}
		
		RefPtr(int nul)
		:m_ptr(0)
		{
			PR_ASSERT(PR_DBG, nul == 0, ""); (void)nul;
		}
		
		RefPtr(RefPtr const& rhs)
		:m_ptr(rhs.m_ptr)
		{
			if (m_ptr)
				IncRef(m_ptr);
		}
		
		template <typename U> RefPtr(RefPtr<U> const& rhs)
		:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			if (m_ptr)
				IncRef(m_ptr);
		}
		
		~RefPtr()
		{
			if (m_ptr)
				DecRef(m_ptr);
		}
		
		// Assignment
		RefPtr& operator = (int nul)
		{
			if (m_ptr) DecRef(m_ptr);
			m_ptr = 0;
			PR_ASSERT(PR_DBG, nul == 0, ""); (void)nul;
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
		template <typename U> RefPtr& operator = (RefPtr<U> const& rhs)
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
		
		// Implicit conversion to bool
		operator bool_type() const
		{
			return m_ptr != 0 ? &bool_tester::x : static_cast<bool_type>(0);
		}
		
		// Implicit conversion to base class
		template <typename U> operator RefPtr<U>() const
		{
			return RefPtr<U>(static_cast<U*>(m_ptr));
		}
		
		// Pointer deref
		T* operator -> ()       { return m_ptr; }
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
			ptr->Release();
		}
	};
	
	template <typename T> inline bool operator == (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr == rhs.m_ptr; }
	template <typename T> inline bool operator != (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr != rhs.m_ptr; }
	template <typename T> inline bool operator <  (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr <  rhs.m_ptr; }
	template <typename T> inline bool operator >  (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr >  rhs.m_ptr; }
	template <typename T> inline bool operator <= (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr <= rhs.m_ptr; }
	template <typename T> inline bool operator >= (RefPtr<T> const& lhs, RefPtr<T> const& rhs) { return lhs.m_ptr >= rhs.m_ptr; }
}

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif

