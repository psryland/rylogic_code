//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/property.h"

struct PropTest
{
	pr::PropertyRW<int>   GetSet;
	pr::PropertyR<float>  Get;
	pr::PropertyW<bool>   Set;
	pr::PropertyRWF<char> Fieldless;

	PropTest()
	:m_getset(0)
	,GetSet   (this, &PropTest::get_GetSet, &PropTest::set_GetSet)
	,Get      (this, &PropTest::get_Get)
	,Set      (this, &PropTest::set_Set)
	,Fieldless(this, &PropTest::get_Feildless, &PropTest::set_Feildless)
	{
		GetSet   .Bind(this, &PropTest::get_GetSet, &PropTest::set_GetSet);
		Get      .Bind(this, &PropTest::get_Get);
		Set      .Bind(this, &PropTest::set_Set);
		Fieldless.Bind(this, &PropTest::get_Feildless, &PropTest::set_Feildless);
	}

private:
	int m_getset;
	int   get_GetSet() const        { return m_getset; }
	void  set_GetSet(int value)     { m_getset = value; }
	float get_Get() const           { return 3.14f; }
	void  set_Set(bool value)       { (void)value; }
	char  get_Feildless() const     { return Fieldless.m_value; }
	void  set_Feildless(char a)     { Fieldless.m_value = a; }
};

SUITE(PRProperty)
{
	TEST(TestProperty)
	{
		PropTest test;
		test.GetSet = 3;
		CHECK(test.GetSet == 3);
		CHECK(test.Get == 3.14f);
		CHECK(test.Set = true);

		test.GetSet = (int)test.Get;
	
		test.Fieldless = 'z';
		CHECK(test.Fieldless == 'z');
	}
}
