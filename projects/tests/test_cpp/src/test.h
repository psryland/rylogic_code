#define GUIAPP 0

#if !GUIAPP
#define _CONSOLE
#endif

#include "pr/common/min_max_fix.h"
#include <winsock2.h>
#include <windows.h>
#include <stdio.h>
#include <conio.h>
#include <stdlib.h>
#include <string.h>
#include <string>
#include <sstream>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/maths/maths.h"
#include "pr/linedrawer/ldr_helper2.h"

#define TEST_ARRAY			0
#define TEST_ARRAY			0
#define TEST_CHAIN			0
#define TEST_CONSOLE		0
#define TEST_CRYPT			0
#define TEST_EXPR			0
#define TEST_GEOMETRY		0
#define TEST_IPC			0
#define TEST_IMAGE			0
#define TEST_KDTREE			0
#define TEST_LISTINANARRAY	0
#define TEST_LUA			0
#define TEST_MATHS			0
#define TEST_MEMPOOL		0
#define TEST_META			0
#define TEST_MISC			0
#define TEST_NETWORK		0
#define TEST_NUGFILE		0
#define TEST_OCTTREE		0
#define TEST_PHYSICS		0
#define TEST_PIPE			0
#define TEST_PROFILE		0
#define TEST_RENDERER		1
#define TEST_SCRIPT			0
#define TEST_STACKDUMP		0
#define TEST_STRING			0
#define TEST_TESTBED3D		0
#define TEST_XFILE			0
#define TEST_XML			0
#define TEST_ZIP			0

PR_EXPAND(TEST_ARRAY		,namespace TestArray				{ void Run(); })
PR_EXPAND(TEST_ARRAY		,namespace TestArray				{ void Run(); })
PR_EXPAND(TEST_CHAIN		,namespace TestChain				{ void Run(); })
PR_EXPAND(TEST_CONSOLE		,namespace TestConsole				{ void Run(); })
PR_EXPAND(TEST_CRYPT		,namespace TestCrypt				{ void Run(); })
PR_EXPAND(TEST_EXPR			,namespace TestExpressionEvaluator	{ void Run(argc, argv); })
PR_EXPAND(TEST_GEOMETRY		,namespace TestGeometry				{ void Run(); })
PR_EXPAND(TEST_IPC			,namespace TestIPC					{ void Run(); })
PR_EXPAND(TEST_IMAGE		,namespace TestImage				{ void Run(); })
PR_EXPAND(TEST_KDTREE		,namespace TestKDTree				{ void Run(); })
PR_EXPAND(TEST_LISTINANARRAY,namespace TestListInAnArray		{ void Run(); })
PR_EXPAND(TEST_LUA			,namespace TestLua					{ void Run(); })
PR_EXPAND(TEST_MATHS		,namespace TestMaths				{ void Run(); })
PR_EXPAND(TEST_MEMPOOL		,namespace TestMemPool				{ void Run(); })
PR_EXPAND(TEST_META			,namespace TestMeta					{ void Run(); })
PR_EXPAND(TEST_MISC			,namespace TestMisc					{ void Run(); })
PR_EXPAND(TEST_NETWORK		,namespace TestNetwork				{ void Run(); })
PR_EXPAND(TEST_NUGFILE		,namespace TestNuggetFile			{ void Run(); })
PR_EXPAND(TEST_OCTTREE		,namespace TestOctTree				{ void Run(); })
PR_EXPAND(TEST_PHYSICS		,namespace TestPhysics				{ void Run(); })
PR_EXPAND(TEST_PIPE			,namespace TestPipe					{ void Run(); })
PR_EXPAND(TEST_PROFILE		,namespace TestProfile				{ void Run(); })
PR_EXPAND(TEST_RENDERER		,namespace TestRenderer				{ void Run(); })
PR_EXPAND(TEST_SCRIPT		,namespace TestScript				{ void Run(); })
PR_EXPAND(TEST_STACKDUMP	,namespace TestStackDump			{ void Run(); })
PR_EXPAND(TEST_STRING		,namespace TestString				{ void Run(); })
PR_EXPAND(TEST_TESTBED3D	,namespace TestTestBed3d			{ void Run(); })
PR_EXPAND(TEST_XFILE		,namespace TestXFile				{ void Run(); })
PR_EXPAND(TEST_XML			,namespace TestXml					{ void Run(); })
PR_EXPAND(TEST_ZIP			,namespace TestZip					{ void Run(); })
