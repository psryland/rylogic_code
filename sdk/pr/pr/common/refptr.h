//**********************************************************************************
// Pointer to a reference counted object
//  Copyright © Rylogic Ltd 2011
//**********************************************************************************
// Use this pointer to point to objects with 'AddRef' and 'Release' functions
// Note: types that inherit pr::RefCount<> can be used
#pragma once
#ifndef PR_REF_POINTER_H
#define PR_REF_POINTER_H
	
//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	// Use the following registry key to prevent the debugger stepping into this type:
	//	[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\NativeDE\StepOver]
	//	"pr::RefPtr"="pr\\:\\:RefPtr.*\\:\\:.*=NoStepInto"
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
				m_ptr->AddRef();
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
				m_ptr->AddRef();
		}
		
		template <typename U> RefPtr(RefPtr<U> const& rhs)
		:m_ptr(static_cast<T*>(rhs.m_ptr))
		{
			if (m_ptr)
				m_ptr->AddRef();
		}
		
		~RefPtr()
		{
			if (m_ptr)
				m_ptr->Release();
		}
		
		// Assignment
		RefPtr& operator = (int nul)
		{
			if (m_ptr) m_ptr->Release();
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
				if (m_ptr) m_ptr->AddRef();
				if (ptr) ptr->Release();
			}
			return *this;
		}
		template <typename U> RefPtr& operator = (RefPtr<U> const& rhs)
		{
			if (m_ptr != static_cast<T*>(rhs.m_ptr))
			{
				T* ptr = m_ptr;
				m_ptr = static_cast<T*>(rhs.m_ptr);
				if (m_ptr) m_ptr->AddRef();
				if (ptr) ptr->Release();
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
			else
			{
				long count = m_ptr->AddRef() - 1;
				m_ptr->Release();
				return count;
			}
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

