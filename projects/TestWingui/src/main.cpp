// Win32Project1.cpp : Defines the entry point for the application.
//

#include <sdkddkver.h>
#include "testwingui/res/resource.h"
#include "pr/common/min_max_fix.h"
#include "pr/gui/wingui.h"
#include "pr/gui/graph_ctrl.h"
#include "pr/gui/windows_com.h"

#include <atlapp.h>
//#include <atldwm.h>
#include <atlwin.h>
#include <atlctrls.h>
//#include <atlcom.h>
//#include <atlmisc.h>
//#include <atlddx.h>
#include <atlframe.h>
#include <atlctrls.h>
//#include <atldlgs.h>
#include <atlcrack.h>
//#include <shellapi.h>
//#include <atlctrlx.h>

using namespace pr::gui;

// About dialog
struct About :Form<About>
{
	Button m_btn_ok;

	About()
		:Form<About>(IDD_ABOUTBOX, "about")
		,m_btn_ok(IDOK,this,"btn_ok", EAnchor::Bottom)
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

// Modeless dialog
struct Modeless :Form<Modeless>
{
	Button m_btn_ok;

	Modeless()
		:Form<Modeless>(IDD_DLG, "modeless")
		,m_btn_ok(IDOK, this, "ok_btn", EAnchor::Bottom|EAnchor::Right)
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

// Application window
struct Main :Form<Main>
{
	Label m_lbl;
	Button m_btn1;
	Button m_btn2;
	Modeless m_modeless;
	GraphCtrl<> m_graph;
	GraphCtrl<>::Series m_series0;
	GraphCtrl<>::Series m_series1;

	enum { IDC_BTN1 = 100, IDC_BTN2 };
	Main()
		:Form<Main>(_T("Pauls Window"), ApplicationMainWindow, CW_USEDEFAULT, CW_USEDEFAULT, 320, 200)
		,m_lbl(_T("hello world"), 80, 20, 100, 16, -1, m_hwnd, this)
		,m_btn1(_T("click me!"), 200, 130, 80, 20, IDC_BTN1, m_hwnd, this, EAnchor::Right|EAnchor::Bottom)
		,m_btn2(_T("show modeless"), 10, 130, 80, 20, IDC_BTN2, m_hwnd, this, EAnchor::Left|EAnchor::Bottom)
		,m_graph(10, 40, 280, 80, -1, m_hwnd, this, EAnchor::All)
		,m_series0(L"Sin")
		,m_series1(L"Cos")
		,m_modeless()
	{
		float j = 0.0f;
		for (int i = 0; i != 3600; ++i, j += 0.1f)
		{
			m_series0.m_values.push_back(GraphDatum(j, sinf(j/pr::maths::tau)));
			m_series1.m_values.push_back(GraphDatum(j, cosf(j/pr::maths::tau)));
		}
		m_graph.m_series.push_back(&m_series0);
		m_graph.m_series.push_back(&m_series1);

		m_graph.m_opts.Border = GraphCtrl<>::RdrOptions::EBorder::Single;
		m_graph.FindDefaultRange();
		m_graph.ResetToDefaultRange();

		m_btn1.Click += [&](Button&,EmptyArgs const&)
			{
				About about;
				about.ShowDialog(this);
			};
		m_btn2.Click += [&](Button&,EmptyArgs const&)
			{
				m_modeless.Show();
			};
	}
};

struct WtlMain :WTL::CFrameWindowImpl<WtlMain>
{
	typedef WTL::CFrameWindowImpl<WtlMain> base;

	BEGIN_MSG_MAP(WtlMain)
		MSG_WM_CREATE(OnCreate)
	END_MSG_MAP()

	DECLARE_FRAME_WND_CLASS(_T("PR_APP_MAIN_GUI"), IDR_MAINFRAME);

	int OnCreate(LPCREATESTRUCT)
	{
		return S_OK;
	}
};

int __stdcall _tWinMain(HINSTANCE hInstance, HINSTANCE, LPTSTR, int)
{
	pr::InitCom com;
	InitCtrls();

	try
	{
		//WtlMain wtl;
		//wtl.Create(nullptr);
		//wtl.ShowWindow(SW_SHOW);

		Main main;
		main.Show();
		MessageLoop loop(hInstance, IDC_WIN32PROJECT1);
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

