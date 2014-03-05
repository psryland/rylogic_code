//***********************************************************************
// UnitTest
//  Copyright © Rylogic Ltd 2008
//***********************************************************************
/* Get yer boilerplate here...
#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(TestName)
		{
			using namespace pr;
			PR_CHECK(1+1, 2);
		}
	}
}
#endif
*/
#include <tchar.h>
#include <algorithm>
#include "pr/common/unittests.h"
#include "unittests/unittests.h"
//#include "pr/network/tcpip.h"

// For faster build times, comment out the 'all headers' include
// and just include the header you care about

int _tmain(int argc, _TCHAR* argv[])
{
	bool runtests = argc >= 2 && std::any_of(argv + 1, argv + argc, [](_TCHAR* x){ return _tcscmp(x, _T("runtests")) == 0;} );
	bool wordy    = argc >= 2 && std::any_of(argv + 1, argv + argc, [](_TCHAR* x){ return _tcscmp(x, _T("verbose")) == 0;} );
	if (runtests)
		return pr::unittests::RunAllTests(wordy);

	return 0;
}
