//*****************************************
// ScriptParser test
//*****************************************
#include "test.h"
#include "pr/common/scriptreader.h"
#include "pr/filesys/fileex.h"

namespace TestScript
{
	using namespace pr;

	char const* src1  = "#define TEST{0.3} *Test { #def{TEST} -10.9 }";
	char const* src2  = "#include \"script_test1.pr_script\"";
	char const* file1 = "D:/deleteme/script_test1.pr_script";
	char const* file2 = "D:/deleteme/script_test2.pr_script";

	void Run()
	{
		using namespace pr::script;
	}
}
