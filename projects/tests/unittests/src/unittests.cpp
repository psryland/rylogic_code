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
#include "src/unittests.h" // all tests
//#include "pr/common/hash.h"
//#include "pr/common/guid.h"
//#include "pr/common/allocator.h"
//#include "pr/common/event_handler.h"
//#include "pr/common/expr_eval.h"
//#include "pr/common/compress.h"
//#include "pr/container/vector.h"
//#include "pr/container/byte_data.h"
//#include "pr/container/deque.h"
//#include "pr/macros/enum.h"
//#include "pr/maths/maths.h"
//#include "pr/maths/maths_core.h"
//#include "pr/maths/large_int.h"
//#include "pr/maths/spatial.h"
//#include "pr/maths/frustum.h"
//#include "pr/maths/half.h"
//#include "pr/maths/conversion.h"
//#include "pr/gfx/colour.h"
//#include "pr/gfx/onebit.h"
//#include "pr/geometry/intersect.h"
//#include "pr/geometry/point.h"
//#include "pr/geometry/p3d.h"
//#include "pr/hardware/find_bt_device.h"
//#include "pr/filesys/filesys.h"
//#include "pr/str/convert_utf.h"
//#include "pr/str/string_core.h"
//#include "pr/str/string_util.h"
//#include "pr/str/extract.h"
//#include "pr/str/string_filter.h"
//#include "pr/str/string.h"
//#include "pr/str/string2.h"
//#include "pr/str/to_string.h"
//#include "pr/script/buf.h"
//#include "pr/script/location.h"
//#include "pr/script/script_core.h"
//#include "pr/script/filter.h"
//#include "pr/script/token.h"
//#include "pr/script/tokeniser.h"
//#include "pr/script/macros.h"
//#include "pr/script/includes.h"
//#include "pr/script/embedded_lua.h"
//#include "pr/script/src_stack.h"
//#include "pr/script/preprocessor.h"
//#include "pr/script/reader.h"
//#include "pr/physics2/rigid_body/rigid_body.h"

// Export a function for executing the tests
extern "C"
{
	__declspec(dllexport) int __stdcall RunAllTests(int wordy)
	{
		return pr::unittests::RunAllTests(wordy != 0);
	}
}