//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/imposter.h"

struct MyType
{
	int m_value;
	MyType(int value) :m_value(value) {}
};
typedef pr::Imposter<MyType> MyTypeImpost;

SUITE(PRImposter)
{
	int FuncByValue(MyType mt)
	{
		return mt.m_value;
	}
	int FuncByRef(MyType const& mt)
	{
		return mt.m_value;
	}
	int FuncByAddr(MyType const* mt)
	{
		return mt->m_value;
	}

	TEST(Construction)
	{
		MyTypeImpost impost;

		pr::imposter::construct(impost, 5);
		CHECK(impost.get().m_value == 5);

		MyTypeImpost impost2 = impost;
		CHECK(impost2.get().m_value == 5);

		MyTypeImpost impost3;
		//CHECK_ASSERT(impost3 = impost);
		
		pr::imposter::construct(impost3, 2);
		impost3 = impost;
		CHECK(impost3.get().m_value == 5);

		CHECK(FuncByValue(impost) == 5);
		CHECK(FuncByRef(impost2) == 5);
	}
}
