//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************

#include "pr/app/main.h"
#include "pr/app/main_gui.h"

int WINAPI _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR lpstrCmdLine, int nCmdShow)
{
	int nRet;
	std::string err_msg;
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

		// Create an instance of the main window and start it running
		// The gui should create it's own message loop and register it
		// with the app module (and clean it up on destruction)
		std::shared_ptr<ATL::CWindow> gui = pr::app::CreateGUI(lpstrCmdLine);
		gui->ShowWindow(nCmdShow);
		gui->UpdateWindow();
		nRet = pr::app::Module().GetMessageLoop()->Run();
	}
	catch (std::exception const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		err_msg = ex.what();
		if (res != S_OK) err_msg += pr::Fmt("\nLast Error Code: %X - %s", res, pr::HrMsg(res).c_str());
		nRet = -1;
	}
	catch (...)
	{
		err_msg = "Shutting down due to an unknown exception";
		nRet = -1;
	}

	if (nRet == -1)
	{
		struct ShowMB :pr::threads::Thread<ShowMB>
		{
			void Main(void* ctx) { ::MessageBoxA(0, static_cast<char const*>(ctx), "Application Error", MB_OK|MB_ICONERROR); }
			using base::Start;
			using base::Join;
		};
		ShowMB show_mb;
		show_mb.Start((void*)err_msg.c_str());
		show_mb.Join();
	}

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
