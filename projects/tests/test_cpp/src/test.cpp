#include "Test.h"

//template <int S> class C;
//C<sizeof(MIXERCONTROL)> c;

#if GUIAPP
int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
#else
int main(int argc, char* argv[])
#endif
{
	PR_EXPAND(TEST_ARRAY		, TestArray					::Run());
	PR_EXPAND(TEST_CHAIN		, TestChain					::Run());
	PR_EXPAND(TEST_CONSOLE		, TestConsole				::Run());
	PR_EXPAND(TEST_CRYPT		, TestCrypt					::Run());
	PR_EXPAND(TEST_GEOMETRY		, TestGeometry				::Run());
	PR_EXPAND(TEST_IPC			, TestIPC					::Run());
	PR_EXPAND(TEST_IMAGE		, TestImage					::Run());
	PR_EXPAND(TEST_KDTREE		, TestKDTree				::Run());
	PR_EXPAND(TEST_LISTINANARRAY, TestListInAnArray			::Run());
	PR_EXPAND(TEST_LUA			, TestLua					::Run());
	PR_EXPAND(TEST_MATHS		, TestMaths					::Run());
	PR_EXPAND(TEST_MEMPOOL		, TestMemPool				::Run());
	PR_EXPAND(TEST_META			, TestMeta					::Run());
	PR_EXPAND(TEST_MISC			, TestMisc					::Run());
	PR_EXPAND(TEST_NETWORK		, TestNetwork				::Run());
	PR_EXPAND(TEST_NUGFILE		, TestNuggetFile			::Run());
	PR_EXPAND(TEST_OCTTREE		, TestOctTree				::Run());
	PR_EXPAND(TEST_PHYSICS		, TestPhysics				::Run());
	PR_EXPAND(TEST_PIPE			, TestPipe					::Run());
	PR_EXPAND(TEST_PROFILE		, TestProfile				::Run());
	PR_EXPAND(TEST_RENDERER		, TestRenderer				::Run());
	PR_EXPAND(TEST_SCRIPT		, TestScript				::Run());
	PR_EXPAND(TEST_STACKDUMP	, TestStackDump				::Run());
	PR_EXPAND(TEST_STRING		, TestString				::Run());
	PR_EXPAND(TEST_TESTBED3D	, TestTestBed3d				::Run());
	PR_EXPAND(TEST_XFILE		, TestXFile					::Run());
	PR_EXPAND(TEST_XML			, TestXml					::Run());
	PR_EXPAND(TEST_ZIP			, TestZip					::Run());

	#if !GUIAPP
	PR_UNUSED(argc);
	PR_UNUSED(argv);
	#endif
	return 0;
}

