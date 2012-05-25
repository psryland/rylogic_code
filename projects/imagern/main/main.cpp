//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************

#include "imagern/main/stdafx.h"
#include "imagern/gui/main_gui.h"

CAppModule g_app_module;

int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR /*lpstrCmdLine*/, int nCmdShow)
{
	HRESULT hRes = ::CoInitialize(NULL);
	
	// If you are running on NT 4.0 or higher you can use the following call instead to 
	// make the EXE free threaded. This means that calls come in on a random RPC thread.
	//HRESULT hRes = ::CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);// COINIT_MULTITHREADED);
	ATLASSERT(SUCCEEDED(hRes));

	// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
	::DefWindowProc(NULL, 0, 0, 0L);

	//ICC_BAR_CLASSES|ICC_TAB_CLASSES
	AtlInitCommonControls(ICC_STANDARD_CLASSES); // add flags to support other controls

	hRes = g_app_module.Init(0, hInstance);
	ATLASSERT(SUCCEEDED(hRes));

	// Run the app
	CMessageLoop msg_loop;
	g_app_module.AddMessageLoop(&msg_loop);

	int nRet;
	try
	{
		MainGUI gui;//(lpstrCmdLine);
		if (gui.CreateEx() == 0) throw std::exception("Main window creation failed");
		gui.ShowWindow(nCmdShow);
		gui.UpdateWindow();
		nRet = msg_loop.Run();
	}
	catch (std::exception const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		std::string err = ex.what();
		err += pr::FmtS("\nCode: %X - %s", res, pr::HrMsg(res).c_str());
		::MessageBoxA(0, err.c_str(), "LineDrawer error", MB_OK|MB_ICONERROR);
		nRet = 0;
	}
	g_app_module.RemoveMessageLoop();
	g_app_module.Term();
	::CoUninitialize();
	return nRet;
}
