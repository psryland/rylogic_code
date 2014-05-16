//******************************************
// min<> max<>
//  Copyright (c) Rylogic Ltd 2013
//******************************************

#ifndef PR_META_MIN_MAX_H
#define PR_META_MIN_MAX_H

namespace pr
{
	namespace meta
	{
		namespace impl
		{
			template <typename Type, Type L, Type R, bool LMin> struct min0     { static const Type value = R; };
			template <typename Type, Type L, Type R> struct min0<Type,L,R,true> { static const Type value = L; };
			
			template <typename Type, Type L, Type R, bool LMin> struct max0     { static const Type value = L; };
			template <typename Type, Type L, Type R> struct max0<Type,L,R,true> { static const Type value = R; };
		}
	
		// minimum/maximum of L,R
		template <typename Type, Type L, Type R> struct min :impl::min0<Type,L,R,L<R> {};
		template <typename Type, Type L, Type R> struct max :impl::max0<Type,L,R,L<R> {};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_meta_min_max)
		{
			static_assert(pr::meta::min<int,+5,+2>::value == +2, "");
			static_assert(pr::meta::min<int,-5,+2>::value == -5, "");
			static_assert(pr::meta::min<int,+5,-2>::value == -2, "");
			static_assert(pr::meta::min<int,-5,-2>::value == -5, "");

			static_assert(pr::meta::max<int,+5,+2>::value == +5, "");
			static_assert(pr::meta::max<int,-5,+2>::value == +2, "");
			static_assert(pr::meta::max<int,+5,-2>::value == +5, "");
			static_assert(pr::meta::max<int,-5,-2>::value == -2, "");
		}
	}
}
#endif
#endif
