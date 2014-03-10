//*****************************************************************************************
// New
//  Copyright © Rylogic Ltd 2014
//*****************************************************************************************
// Helpers for new-ing objects

#pragma once
#ifndef PR_COMMON_NEW_H
#define PR_COMMON_NEW_H

#include <new>
#include <memory>
//#include "pr/macros/repeat.h"

namespace pr
{
	template <typename T, template Args...>
	inline std::unique_ptr<T> New(Args...&& args)
	{
		return std::unique_ptr<T>(new T(std::forward<Args...>(args)));
	}
	//
	//// New an object into a unique pointer
	//template <typename T> inline std::unique_ptr<T> New()
	//{
	//	return std::unique_ptr<T>(new T());
	//}

	//// Overloads taking various numbers of parameters
	//#define PR_TN(n) typename U##n
	//#define PR_PARM1(n) U##n&& parm##n
	//#define PR_PARM2(n) std::forward<U##n>(parm##n)
	//#define PR_FUNC(n)\
	//template <typename T, PR_REPEAT(n,PR_TN,PR_COMMA)> inline std::unique_ptr<T> New(PR_REPEAT(n,PR_PARM1,PR_COMMA))\
	//{\
	//	return std::unique_ptr<T>(new T(PR_REPEAT(n,PR_PARM2,PR_COMMA)));\
	//}

	//PR_FUNC(1)
	//PR_FUNC(2)
	//PR_FUNC(3)
	//PR_FUNC(4)
	//PR_FUNC(5)
	//PR_FUNC(6)
	//PR_FUNC(7)

	//#undef PR_TN
	//#undef PR_PARM1
	//#undef PR_PARM2
	//#undef PR_FUNC
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		struct Wotzit
		{
			int m_int;
			Wotzit() :m_int() {}
			Wotzit(int i, int j, int k) :m_int(i+j+k) {}
		};

		PRUnitTest(pr_common_new)
		{
			auto p = pr::New<Wotzit>();
			PR_CHECK(0, p->m_int);

			p = pr::New<Wotzit>(1,2,3);
			PR_CHECK(6, p->m_int);
		}
	}
}
#endif
#endif
