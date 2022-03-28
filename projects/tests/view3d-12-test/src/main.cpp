#include <stdexcept>
#include <windows.h>
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"

import View3d;

using namespace pr::gui;
using namespace pr::rdr12;

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	Renderer m_rdr;

	Main(HINSTANCE hinstance)
		:Form(Params<>()
			.name("main")
			.title(L"View3d 12 Test")
			.xy(1500,100).wh(800,600)
			.main_wnd(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<Main>()))
		,m_rdr(RdrSettings(hinstance))
	{
		CreateHandle();
	}
	static Settings RdrSettings(HINSTANCE hinstance)
	{
		SystemConfig sys;
		auto& adapter = sys.m_adapters[0];
		auto& output = adapter.m_outputs[0];

		Settings settings(hinstance);
		settings.m_adapter = adapter.m_adapter;
		settings.m_output = output.m_output;
		settings.m_display_mode = output.FindClosestMatchingMode(DisplayMode(800,600));
		return settings;
	}
};

// Entry point
int __stdcall WinMain(HINSTANCE hInstance, HINSTANCE, LPTSTR, int)
{
	pr::InitCom com;

	try
	{
		Main main(hInstance);
		main.Show();

		MessageLoop loop;
		loop.AddMessageFilter(main);
		return loop.Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA("Died: ");
		OutputDebugStringA(ex.what());
		OutputDebugStringA("\n");
		return -1;
	}
}
