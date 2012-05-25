//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************

#include "linedrawer/main/stdafx.h"
#include "linedrawer/types/ldrexception.h"
#include "linedrawer/gui/linedrawergui.h"

CAppModule g_app_module;

int WINAPI _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR /*lpstrCmdLine*/, int nCmdShow)
{
	int nRet;
	try
	{
		// CoInitialise
		pr::InitCom init_com;

		// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
		::DefWindowProc(NULL, 0, 0, 0L);

		//ICC_BAR_CLASSES|ICC_TAB_CLASSES
		AtlInitCommonControls(ICC_STANDARD_CLASSES); // add flags to support other controls

		pr::Throw(g_app_module.Init(NULL, hInstance));

		// Run the app
		LineDrawerGUI ldr;//(lpstrCmdLine);
		g_app_module.AddMessageLoop(&ldr);
		if (ldr.Create(0) == 0) throw LdrException("Main window creation failed");
		ldr.ShowWindow(nCmdShow);
		nRet = ldr.Run();
	}
	catch (LdrException const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		std::string err = ex.m_msg;
		err += pr::FmtS("\nCode: %X - %s", res, pr::HrMsg(res).c_str());
		::MessageBoxA(0, err.c_str(), "LineDrawer error", MB_OK|MB_ICONERROR);
		nRet = -1;
	}
	catch (...)
	{
		::MessageBoxA(0, "Shutting down due to an unknown exception", "LineDrawer error", MB_OK|MB_ICONERROR);
		nRet = -1;
	}
	g_app_module.RemoveMessageLoop();
	g_app_module.Term();
	return nRet;
}
