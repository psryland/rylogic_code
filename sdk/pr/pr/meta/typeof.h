//******************************************
// Compile time typeof
//  Copyright © Rylogic Ltd 2010
//******************************************

#ifndef PR_META_TYPEOF_H
#define PR_META_TYPEOF_H

#include <vector>
#include <list>
#include <string>

namespace pr
{
	namespace meta
	{
		namespace impl
		{
			// Ensure the __COUNTER__ compiler variable is >= 1
			class typeof_inc_counter { static const int i = __COUNTER__; };
			template <int N> struct size_to_type;
		}
	}
}

#define PR_IMPL_REGISTER_TYPEOF2(N,T)\
	namespace pr\
	{\
		namespace meta\
		{\
			namespace impl\
			{\
				template<> struct size_to_type<N>
				{\
					typedef T type;\
				};\
				char (*type_to_size(T&))[N];\
			}\
		}\
	}

#define PR_IMPL_REGISTER_TYPEOF(N,T) PR_IMPL_REGISTER_TYPEOF2(N,T)

#define PR_REGISTER_TYPEOF(x)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, x                           )\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, x const                     )\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, x*                          )\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, x const*                    )\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x              >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x const        >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x*             >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x const*       >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x        > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x const  > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x*       > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::vector<x const* > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x                >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x const          >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x*               >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x const*         >)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x          > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x const    > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x*         > const)\
	PR_IMPL_REGISTER_TYPEOF(__COUNTER__, std::list<x const*   > const)\

// Use:
//  std::vector<int> cont;
//  for (typeof(cont)::iterator i = cont.begin(), iend = cont.end(); i != iend; ++i) {}
#define typeof(x) pr::meta::impl::size_to_type<sizeof(*pr::meta::impl::type_to_size(x))>::type

PR_REGISTER_TYPEOF(bool)
PR_REGISTER_TYPEOF(char)
PR_REGISTER_TYPEOF(unsigned char)
PR_REGISTER_TYPEOF(short)
PR_REGISTER_TYPEOF(unsigned short)
PR_REGISTER_TYPEOF(int)
PR_REGISTER_TYPEOF(unsigned int)
PR_REGISTER_TYPEOF(long)
PR_REGISTER_TYPEOF(unsigned long)
PR_REGISTER_TYPEOF(long long)
PR_REGISTER_TYPEOF(unsigned long long)
PR_REGISTER_TYPEOF(float)
PR_REGISTER_TYPEOF(double)
PR_REGISTER_TYPEOF(std::string)
PR_REGISTER_TYPEOF(std::wstring)

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_meta_typeof)
		{
			int a = 0;
			typeof(a) b = 1;
			double c = 0.0;
			PR_CHECK(sizeof(typeof(c)) == sizeof(double));
			c = a = b;
		}
	}
}
#endif

#endif
