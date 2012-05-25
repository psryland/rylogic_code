//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************

#include "pr/app/main.h"
#include "pr/app/main_gui.h"

int WINAPI _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR lpstrCmdLine, int nCmdShow)
{
	int nRet;
	try
	{
		// CoInitialise COM
		pr::InitCom init_com;

		// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
		::DefWindowProc(NULL, 0, 0, 0L);

		//ICC_BAR_CLASSES|ICC_TAB_CLASSES
		AtlInitCommonControls(ICC_STANDARD_CLASSES); // add flags to support other controls
		
		// Initialise the WTL module singleton
		pr::Throw(pr::app::Module().Init(NULL, hInstance));
		
		// The main window message loop
		CMessageLoop msg_loop;
		pr::app::Module().AddMessageLoop(&msg_loop);
		
		// Create an instance of the main window
		std::shared_ptr<ATL::CWindow> gui = pr::app::CreateGUI(lpstrCmdLine);
		gui->ShowWindow(nCmdShow);
		gui->UpdateWindow();
		
		// Run the app message pump
		nRet = msg_loop.Run();
	}
	catch (pr::Exception<HRESULT> const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		std::string err = ex.m_msg;
		err += pr::Fmt("\nCode: %X - %s", res, pr::HrMsg(res).c_str());
		::MessageBoxA(0, err.c_str(), "Error During Application Startup", MB_OK|MB_ICONERROR);
		nRet = -1;
	}
	catch (...)
	{
		::MessageBoxA(0, "Shutting down due to an unknown exception", "Application Error", MB_OK|MB_ICONERROR);
		nRet = -1;
	}
	pr::app::Module().RemoveMessageLoop();
	pr::app::Module().Term();
	return nRet;
}

#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
