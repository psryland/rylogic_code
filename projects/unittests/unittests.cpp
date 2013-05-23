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

// Add includes containing unittests here
#include "pr/common/array.h"
#include "pr/common/base64.h"
#include "pr/common/chain.h"
#include "pr/common/expr_eval.h"
#include "pr/common/fmt.h"
#include "pr/common/hash.h"
#include "pr/common/imposter.h"
#include "pr/common/repeater.h"
#include "pr/common/datetime.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/findfiles.h"
#include "pr/filesys/recurse_directory.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/macros/enum.h"
#include "pr/maths/maths.h"
#include "pr/meta/abs.h"
#include "pr/meta/min_max.h"
#include "pr/meta/gcf.h"
#include "pr/renderer11/renderer.h"
#include "pr/script/script.h"
#include "pr/script/reader.h"
#include "pr/storage/csv.h"
#include "pr/storage/sqlite.h"
#include "pr/storage/settings.h"
#include "pr/str/prstring.h"
#include "pr/threads/thread_pool.h"

int _tmain(int argc, _TCHAR* argv[])
{
	bool runtests = argc >= 2 && std::any_of(argv + 1, argv + argc, [](_TCHAR* x){ return _tcscmp(x, _T("runtests")) == 0;} );
	bool wordy    = argc >= 2 && std::any_of(argv + 1, argv + argc, [](_TCHAR* x){ return _tcscmp(x, _T("verbose")) == 0;} );
	if (runtests)
		return pr::unittests::RunAllTests(wordy);
	
	return 0;
}
