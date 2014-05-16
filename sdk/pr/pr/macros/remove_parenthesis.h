//*************************************
// Remove parenthesis from macro parameters
// Copyright (c) Rylogic Ltd 2014
//*************************************
#ifndef PR_MACROS_REMOVE_PARENS_H
#define PR_MACROS_REMOVE_PARENS_H

// Credit to Steve Robb for this:
#define PR_JOIN_AND_EXPAND3(...) __VA_ARGS__
#define PR_JOIN_AND_EXPAND2(a, ...) PR_JOIN_AND_EXPAND3(a##__VA_ARGS__)
#define PR_JOIN_AND_EXPAND(a, ...) PR_JOIN_AND_EXPAND2(a, __VA_ARGS__)
#define PR_RPIMPLPR_RPIMPL
#define PR_RPIMPL(...) PR_RPIMPL __VA_ARGS__

// Use with 0 or 1 arguments only
#define PR_REMOVE_PARENS(...) PR_JOIN_AND_EXPAND(PR_RPIMPL,PR_RPIMPL __VA_ARGS__)

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		namespace macros
		{
			struct Thing1
			{
				int val;
				Thing1() { val = 45; }
			};
			template <typename A, typename B> struct Thing2
			{
				A a;
				B b;
			};

			PRUnitTest(pr_macros_remove_parens)
			{
				#define DECLARE_VAR(type, name, init) PR_REMOVE_PARENS(type) name PR_REMOVE_PARENS(init)
				DECLARE_VAR(float, pi, = 3.14f);
				DECLARE_VAR(Thing1, t1, );
				DECLARE_VAR((Thing2<int, float>), t2, (= { 10, 5.99f }));
				#undef DECLARE_VAR

				PR_CHECK(pi, 3.14f);
				PR_CHECK(t1.val, 45);
				PR_CHECK(t2.a, 10);
				PR_CHECK(t2.b, 5.99f);
			}
		}
	}
}
#endif

#endif
