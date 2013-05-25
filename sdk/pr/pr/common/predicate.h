//******************************************
// Predicate
//  Copyright © March 2008 Paul Ryland
//******************************************
// A collection of helpful predicates

#ifndef PR_COMMON_PREDICATES_H
#define PR_COMMON_PREDICATES_H

#include "pr/meta/if.h"
#include "pr/meta/ispointer.h"

namespace pr
{
	namespace pred
	{
		namespace impl
		{
			template <typename T> struct PtrRelease { static void Release(T  t) { t->Release(); } };
			template <typename T> struct RefRelease { static void Release(T& t) { t.Release(); } };
		}
		
		// Delete a pointer
		struct Delete
		{
			template <typename T> void operator () (T* p) const { delete p; }
		};
		
		// Calls release on a pointer or a reference
		struct Release
		{
			template <typename T> void operator () (T& t) const
			{
				typedef typename meta::if_< meta::is_pointer<T>, impl::PtrRelease<T>, impl::RefRelease<T> >::type Type;
				Type::Release(t);
			}
		};
		
		// A predicate that always returns true
		struct AlwaysTrue
		{
			template <typename Type> bool operator ()(Type const&) const { return true; }
		};
		
		// A predicate that always returns false
		struct AlwaysFalse
		{
			template <typename Type> bool operator ()(Type const&) const { return false; }
		};
	}
}
	
#endif
