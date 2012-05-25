//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/range.h"

SUITE(Range)
{
	TEST(Range)
	{
		typedef pr::Range<int> IRange;
		IRange r0 = IRange::make(0,5);
		IRange r1 = IRange::make(5,10);
		IRange r2 = IRange::make(3,7);
		IRange r3 = IRange::make(0,10);
		
		CHECK_EQUAL(false, r0.empty());
		CHECK_EQUAL(5U, r0.size());
		
		CHECK_EQUAL(false ,r0.contains(-1));
		CHECK_EQUAL(true  ,r0.contains(0));
		CHECK_EQUAL(true  ,r0.contains(4));
		CHECK_EQUAL(false ,r0.contains(5));
		CHECK_EQUAL(false ,r0.contains(6));
		
		CHECK_EQUAL(true  ,r3.contains(r0));
		CHECK_EQUAL(true  ,r3.contains(r1));
		CHECK_EQUAL(true  ,r3.contains(r2));
		CHECK_EQUAL(false ,r2.contains(r0));
		CHECK_EQUAL(false ,r2.contains(r1));
		CHECK_EQUAL(false ,r2.contains(r3));
		CHECK_EQUAL(false ,r1.contains(r0));
		CHECK_EQUAL(false ,r0.contains(r1));
		
		CHECK_EQUAL(true  ,r3.intersects(r0));
		CHECK_EQUAL(true  ,r3.intersects(r1));
		CHECK_EQUAL(true  ,r3.intersects(r2));
		CHECK_EQUAL(true  ,r2.intersects(r0));
		CHECK_EQUAL(true  ,r2.intersects(r1));
		CHECK_EQUAL(true  ,r2.intersects(r3));
		CHECK_EQUAL(false ,r1.intersects(r0));
		CHECK_EQUAL(false ,r0.intersects(r1));
		
		r0.shift(3);
		r1.shift(-2);
		CHECK(r0 == r1);
		
		CHECK(r3.mid() == r2.mid());
		
		r0.shift(-3);
		r0.resize(3);
		CHECK_EQUAL(3U, r0.size());
	}
	TEST(IterRange)
	{
		typedef std::vector<int> Vec;
		typedef pr::Range<Vec::const_iterator> IRange;
		Vec vec; for (int i = 0; i != 10; ++i) vec.push_back(i);
		
		IRange r0 = IRange::make(vec.begin(),vec.begin()+5);
		IRange r1 = IRange::make(vec.begin()+5,vec.end());
		IRange r2 = IRange::make(vec.begin()+3,vec.begin()+7);
		IRange r3 = IRange::make(vec.begin(),vec.end());
		
		CHECK_EQUAL(false, r0.empty());
		CHECK_EQUAL(5U, r0.size());
		
		CHECK_EQUAL(true  ,r0.contains(vec.begin()));
		CHECK_EQUAL(true  ,r0.contains(vec.begin() + 4));
		CHECK_EQUAL(false ,r0.contains(vec.begin() + 5));
		CHECK_EQUAL(false ,r0.contains(vec.end()));
		
		CHECK_EQUAL(true  ,r3.contains(r0));
		CHECK_EQUAL(true  ,r3.contains(r1));
		CHECK_EQUAL(true  ,r3.contains(r2));
		CHECK_EQUAL(false ,r2.contains(r0));
		CHECK_EQUAL(false ,r2.contains(r1));
		CHECK_EQUAL(false ,r2.contains(r3));
		CHECK_EQUAL(false ,r1.contains(r0));
		CHECK_EQUAL(false ,r0.contains(r1));
		
		CHECK_EQUAL(true  ,r3.intersects(r0));
		CHECK_EQUAL(true  ,r3.intersects(r1));
		CHECK_EQUAL(true  ,r3.intersects(r2));
		CHECK_EQUAL(true  ,r2.intersects(r0));
		CHECK_EQUAL(true  ,r2.intersects(r1));
		CHECK_EQUAL(true  ,r2.intersects(r3));
		CHECK_EQUAL(false ,r1.intersects(r0));
		CHECK_EQUAL(false ,r0.intersects(r1));
		
		r0.shift(3);
		r1.shift(-2);
		CHECK(r0 == r1);
		
		CHECK(r3.mid() == r2.mid());
		
		r0.shift(-3);
		r0.resize(3);
		CHECK_EQUAL(3U, r0.size());
	}
}
