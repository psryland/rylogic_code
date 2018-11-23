//***********************************************************************
// UnitTest
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************
/* Get yer boilerplate here...
#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::unittests::your::namespace
{
	PRUnitTest(TestName)
	{
		using namespace pr;
		PR_CHECK(1+1, 2);
	}
}
#endif
*/

// For faster build times, comment out the 'all headers' include
// and just include the header you care about
#include "pr/common/unittests.h"
//#include "unittests/src/unittests.h" // all tests
#include "pr/maths/maths.h"
#include "pr/maths/spatial.h"
#include "pr/physics2/shape/inertia.h"

// Export a function for executing the tests
extern "C"
{
	__declspec(dllexport) int __stdcall RunAllTests(BOOL wordy)
	{
		return pr::unittests::RunAllTests(wordy != 0);
	}
}

