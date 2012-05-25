//*************************************************************
// Unit Test for pr::threads::ThreadPool
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/hash.h"

SUITE(PRHash)
{
	using namespace pr::hash;
	
	TEST(Hash)
	{
		enum
		{
			Blah = -0x7FFFFFFF,
		};
		char const data[] = "Paul was here. CrC this, mofo";
		char const data2[] = "paul was here. crc this, mofo";
		{
			HashValue h0 = HashData(data, sizeof(data));
			CHECK_EQUAL(h0,h0);
			
			HashValue64 h1 = HashData64(data, sizeof(data));
			CHECK_EQUAL(h1,h1);
		}
		{ // Check accumulative hash works
			HashValue h0 = HashData(data, sizeof(data));
			HashValue h1 = HashData(data + 5, sizeof(data) - 5, HashData(data, 5));
			HashValue h2 = HashData(data + 9, sizeof(data) - 9, HashData(data, 9));
			CHECK_EQUAL(h0, h1);
			CHECK_EQUAL(h0, h2);
		}
		{
			HashValue h0 = HashLwr(data);
			HashValue h1 = HashC(data2);
			CHECK_EQUAL(h1, h0);
		}
	}
}
