//**********************************************************************************
// Pointer to an allocated object
//  Copyright © Rylogic Ltd 2011
//**********************************************************************************
//
#ifndef PR_AUTO_POINTER_H
#define PR_AUTO_POINTER_H
	
//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	// Use the following registry key to prevent the debugger stepping into this type:
	//	[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\NativeDE\StepOver]
	//	"pr::AutoPtr"="pr\\:\\:AutoPtr.*\\:\\:.*=NoStepInto"
	template <typename T> struct AutoPtr
	{
		mutable T* m_ptr;
		
		AutoPtr()
		:m_ptr(0)
		{}
		
		explicit AutoPtr(int i)
		:m_ptr(0)
		{
			PR_ASSERT(PR_DBG, i == 0, ""); (void)i;
		}
		
		template <typename Other> explicit AutoPtr(Other* t)
		:m_ptr(t)
		{}
		
		template <typename Other> AutoPtr(AutoPtr<Other>& rhs)
		:m_ptr(rhs.release())
		{}
		
		~AutoPtr()
		{
			delete m_ptr;
		}
		
		AutoPtr& operator = (int i)
		{
			PR_ASSERT(PR_DBG, i == 0, ""); (void)i;
			reset();
			m_ptr = 0;
			return *this;
		}
		
		template <typename Other> AutoPtr<T>& operator = (Other* rhs)
		{
			reset(rhs);
			return *this;
		}
		
		template <typename Other> AutoPtr<T>& operator = (AutoPtr<Other>& rhs)
		{
			reset(rhs.release());
			return *this;
		}
		
		template <typename Other> operator AutoPtr<Other>()
		{
			return AutoPtr<Other>(*this);
		}
		
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
		operator bool_type() const
		{
			return m_ptr != 0 ? &bool_tester::x : static_cast<bool_type>(0);
		}
		
		T const* operator -> () const { return get(); }
		T* operator -> ()             { return get(); }
		T const& operator * () const  { PR_ASSERT(PR_DBG, m_ptr, ""); return *get(); }
		T& operator * ()              { PR_ASSERT(PR_DBG, m_ptr, ""); return *get(); }

		T const* get() const          { return m_ptr; }
		T* get()                      { return m_ptr; }
		T* release()                  { T* ptr = m_ptr; m_ptr = 0; return ptr; }
		void reset(T* ptr = 0)        { if (ptr != m_ptr) delete m_ptr; m_ptr = ptr; }
	};
	
	template <typename T> inline bool operator == (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr == rhs.m_ptr; }
	template <typename T> inline bool operator != (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr != rhs.m_ptr; }
	template <typename T> inline bool operator <  (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr <  rhs.m_ptr; }
	template <typename T> inline bool operator >  (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr >  rhs.m_ptr; }
	template <typename T> inline bool operator <= (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr <= rhs.m_ptr; }
	template <typename T> inline bool operator >= (AutoPtr<T> const& lhs, AutoPtr<T> const& rhs) { return lhs.m_ptr >= rhs.m_ptr; }
}
	
#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif
	
#endif

