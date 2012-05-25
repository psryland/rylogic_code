//*****************************************
//	Unit test for Profile.h
//*****************************************
#include "test.h"
#include "pr/common/profile.h"
#include "pr/common/profilemanager.h"
#include "pr/maths/primes.h"

using namespace pr;

namespace TestProfile
{
	void hank()
	{
		PR_DECLARE_PROFILE(1, hank);
		PR_PROFILE_SCOPE(1, hank);
		for( int j = 0, p = 1; j != 20000; ++j )
			p = PrimeGtrEq(p+1);
	}

	void child(int i);

	void childchild()
	{
		PR_DECLARE_PROFILE(1, childchild);
		PR_PROFILE_SCOPE(1, childchild);
		child(2);
	}

	void child(int i)
	{
		PR_DECLARE_PROFILE(1, child);
		PR_PROFILE_SCOPE(1, child);
		if( i == 1 ) childchild();
		if( i == 2 ) return;
		child(i + 1);
	}

	void parent()
	{
		PR_DECLARE_PROFILE(1, parent);
		PR_PROFILE_SCOPE(1, parent);

		PR_DECLARE_PROFILE(1, call_hank);
		PR_PROFILE_START(1, call_hank);
		hank();
		PR_PROFILE_STOP(1, call_hank);
	//	child(0);
	}

	void Run()
	{
		for( int i = 0; i != 10000; ++i )
		{
			PR_PROFILE_FRAME_BEGIN;
			parent();			
			PR_PROFILE_FRAME_END;
			PR_PROFILE_OUTPUT(1);
		}
	}
}//namespace TestProfile
