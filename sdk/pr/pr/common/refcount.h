//************************************************************************
// Ref counting base class
//  Copyright © Rylogic Ltd 2011
//************************************************************************
// Usage:
//	struct Thing :pr::RefCount<Thing> {};
//	Thing* thing = new Thing();
//	thing->AddRef();
//	thing->Release(); // thing deleted here
//	
//	Typically this would be used with pr::RefPtr<Thing>.
//  e.g.
//	{
//		RefPtr<Thing> ptr(new Thing()); // thing deleted when last reference goes out of scope
//	}
//
#ifndef PR_REF_COUNT_H
#define PR_REF_COUNT_H
	
#include <windows.h>
	
//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	// Reference counting mix-in base class
	// 'Deleter' is a type containing a static function with signiture: 'void RefCountZero(RefCount* obj)'
	// Its purpose is to release resources owned by the ref counted object because there are no more references to it.
	// 'RefCount' itself contains a 'RefCountZero' function so that clients can use 'RefCount<Derived>'
	// which will pick up the default behaviour of deleting the ref counted object when the count hits zero
	// 'Shared' should be true if AddRef()/Release() can be called from multiple threads
	template <typename Deleter, bool Shared = true> struct RefCount
	{
		mutable volatile long m_ref_count;

		RefCount()
		:m_ref_count(0)
		{
		}
		virtual ~RefCount()
		{
		}
		RefCount(RefCount const& rhs) :m_ref_count(0)
		{
		}
		RefCount& operator = (RefCount const& rhs)
		{
			if (&rhs != this) m_ref_count = 0;
			return *this;
		}
		long AddRef() const
		{
			return Shared ? ::InterlockedIncrement(&m_ref_count) : ++m_ref_count;
		}
		long Release() const
		{
			PR_ASSERT(PR_DBG, m_ref_count > 0, "");
			long ref_count = Shared ? ::InterlockedDecrement(&m_ref_count) : --m_ref_count;
			if (!ref_count) { Deleter::RefCountZero(const_cast<RefCount<Deleter,Shared>*>(this)); }
			return ref_count;
		}
		static void RefCountZero(RefCount<Deleter,Shared>* doomed)
		{
			delete doomed;
		}
	};
}
	
#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
