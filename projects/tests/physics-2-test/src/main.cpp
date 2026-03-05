//************************************
// Physics-2 Sandbox
//  Copyright (c) Rylogic Ltd 2016
//************************************
// Entry point for the physics-2-test application.
// Two modes:
//   -unittest : Allocate a console, run embedded unit tests, and exit.
//   (default) : Launch the interactive physics sandbox window.
//
#include "src/sandbox_ui.h"
#include "src/sandbox_tests.h"

using namespace pr;
using namespace pr::gui;

// Run embedded unit tests to a console window and exit.
// Uses AllocConsole() to create a console for the WinApp process,
// then redirects stdout so PR_EXPECT output is visible.
static int RunUnitTests()
{
	// Attach to parent console (if launched from cmd) or allocate a new one
	if (!AttachConsole(DWORD(-1)))
		AllocConsole();

	// Redirect stdout/stderr to the console
	FILE* fp = nullptr;
	freopen_s(&fp, "CONOUT$", "w", stdout);
	freopen_s(&fp, "CONOUT$", "w", stderr);

	printf("Physics-2 Sandbox: Running unit tests...\n");

	// The PR_UNITTESTS framework collects tests via static initialisation.
	// RunAllTests() executes them and prints results.
	auto failed = pr::unittests::RunAllTests(true);
	return failed > 0 ? 1 : 0;
}

// Entry point
int __stdcall WinMain(HINSTANCE, HINSTANCE, LPTSTR lpCmdLine, int)
{
	// Check for -unittest mode before initialising any GUI resources
	if (lpCmdLine && strstr(lpCmdLine, "-unittest"))
		return RunUnitTests();

	// Interactive sandbox mode
	pr::InitCom com;
	pr::GdiPlus gdi;

	try
	{
		pr::win32::LoadDll<struct View3d>(L"view3d-12.dll");
		InitCtrls();

		// Create the message loop first so the form can reference it for clean shutdown
		WinGuiMsgLoop loop;

		SandboxUI sandbox;
		sandbox.cp().msg_loop(&loop);
		sandbox.Show();

		loop.AddLoop(100.0, false, [&](double dt) { sandbox.Step(dt); });
		loop.AddLoop(60.0, true, [&](double) { sandbox.Render(); });
		loop.AddMessageFilter(sandbox);
		return loop.Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA(ex.what());
		return -1;
	}
}
