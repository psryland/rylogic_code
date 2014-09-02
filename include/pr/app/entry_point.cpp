//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************

#include "pr/app/main.h"
#include "pr/app/main_gui.h"

int WINAPI _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR lpstrCmdLine, int nCmdShow)
{
	int nRet;
	{
		std::string err_msg;
		std::shared_ptr<ATL::CWindow> gui;
		try
		{
			// CoInitialise COM
			pr::InitCom init_com;

			// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
			::DefWindowProc(NULL, 0, 0, 0L);

			// Initialise the WTL module singleton
			pr::Throw(pr::app::Module().Init(NULL, hInstance));

			// Create an instance of the main window and start it running
			gui = pr::app::CreateGUI(lpstrCmdLine);
			gui->ShowWindow(nCmdShow);
			gui->UpdateWindow();
			nRet = pr::app::Module().GetMessageLoop()->Run();
		}
		catch (std::exception const& ex)
		{
			DWORD last_error = GetLastError();
			HRESULT res = HRESULT_FROM_WIN32(last_error);
			std::string ex_msg(ex.what()); ex_msg.substr(0, ex_msg.find_last_not_of(" \t\r\n")).swap(ex_msg);
			err_msg = pr::Fmt("Application shutdown due to unhandled error:\r\nError Message: '%s'", ex_msg.c_str());
			if (res != S_OK) err_msg += pr::Fmt("\r\nLast Error Code: %X - %s", res, pr::HrMsg(res).c_str());
			nRet = -1;
		}
		catch (...)
		{
			err_msg = "Shutting down due to an unknown exception";
			nRet = -1;
		}

		// Attempt to shut the window down gracefully
		if (gui && gui->IsWindow())
		{
			try
			{
				gui->DestroyWindow();
				pr::app::Module().GetMessageLoop()->Run();
			}
			catch (...) {}
		}

		if (nRet == -1)
		{
			std::thread([&] { ::MessageBoxA(0, err_msg.c_str(), "Application Error", MB_OK|MB_ICONERROR); }).join();
		}
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
