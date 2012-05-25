//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/stackdump.h"

namespace TestStackDump
{
	using namespace pr;

	//typedef stack_dump::OutputDbgStr	OutType;
	typedef stack_dump::Printf			OutType;

	void Func0()
	{
		printf("Func0 at %p called: \n", Func0);
		OutputDebugString(Fmt("Func0 at %p called: \n", Func0).c_str());
		OutType out;
		StackDump(out);
	}
	void Func1()
	{
		printf("Func1 at %p called: \n", Func1);
		OutputDebugString(Fmt("Func1 at %p called: \n", Func1).c_str());
		OutType out;
		StackDump(out);

		Func0();
	}
	void Func2()
	{
		printf("Func2 at %p called: \n", Func2);
		OutputDebugString(Fmt("Func2 at %p called: \n", Func2).c_str());
		OutType out;
		StackDump(out);

		Func1();
	}

	void Run()
	{
		Func2();
	}
}//namespace TestZip
