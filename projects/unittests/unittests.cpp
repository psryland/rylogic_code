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
#include "pr/common/unittests.h"

// Add includes containing unittests here
#include "pr/common/array.h"
#include "pr/common/repeater.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/macros/enum.h"
#include "pr/maths/maths.h"
#include "pr/renderer11/renderer.h"
#include "pr/script/script.h"
#include "pr/script/reader.h"
#include "pr/storage/sqlite.h"
#include "pr/str/prstring.h"
#include "pr/threads/thread_pool.h"

int _tmain(int argc, _TCHAR* argv[])
{
	if (argc == 2 && _tcscmp(argv[1], _T("runtests")) == 0)
		return pr::unittests::RunAllTests();
	return 0;
}
