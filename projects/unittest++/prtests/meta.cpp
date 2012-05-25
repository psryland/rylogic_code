//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/meta/typeof.h"
#include "pr/meta/typelist.h"

SUITE(PRMeta)
{
	TEST(TypeOf)
	{
		int a = 0;
		typeof(a) b = 1;
		double c = 0.0;
		CHECK(sizeof(typeof(c)) == sizeof(double));
		c = a = b;
	}
	
//	template <typename Parms> void Func(Parms const& parms)
//	{
//		if (exists(parms.m_p0)) printf("size of p0 = %d\n", sizeof(parms.m_p0));
//		if (exists(parms.m_p1)) printf("size of p1 = %d\n", sizeof(parms.m_p1));
//		if (exists(parms.m_p2)) printf("size of p2 = %d\n", sizeof(parms.m_p2));
//		if (exists(parms.m_p3)) printf("size of p3 = %d\n", sizeof(parms.m_p3));
//		if (exists(parms.m_p4)) printf("size of p4 = %d\n", sizeof(parms.m_p4));
//	}
//
//	TEST(TypeList)
//	{
//		Func(PKs(5, "Paul was here", 'a'));
//	}
}
