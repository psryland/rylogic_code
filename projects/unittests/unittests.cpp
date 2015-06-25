//***********************************************************************
// UnitTest
//  Copyright (c) Rylogic Ltd 2008
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
#include <algorithm>
#include "pr/common/unittests.h"
//#include "unittests/unittests.h" // all tests
#include "pr/str/string_core.h"
#include "pr/str/string_util.h"
//#include "pr/str/extract.h"
//#include "pr/common/expr_eval.h"
//#include "pr/crypt/rijndael.h"
//#include "pr/common/datetime.h"

// For faster build times, comment out the 'all headers' include
// and just include the header you care about
using namespace std;

int main(int argc, char const* argv[])
//int _tmain(int argc, _TCHAR* argv[])
{
	bool runtests = argc >= 2 && std::any_of(argv + 1, argv + argc, [](char const* x){ return strcmp(x, "runtests") == 0;} );
	bool wordy    = argc >= 2 && std::any_of(argv + 1, argv + argc, [](char const* x){ return strcmp(x, "verbose") == 0;} );
	if (runtests)
		return pr::unittests::RunAllTests(wordy);
	
	printf("*** Unit Tests Not Run ***\n");
	return 0;
}
