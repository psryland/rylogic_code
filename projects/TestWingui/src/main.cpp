#include "testwingui/src/forward.h"
#include "testwingui/src/about.h"
#include "testwingui/src/modeless.h"
#include "testwingui/src/graph.h"
#include "pr/gui/wingui.h"
#include "pr/gui/progress_dlg.h"
#include "pr/gui/context_menu.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/win32/win32.h"

//#define USE_ATL
#ifdef USE_ATL
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
#endif

// Application window
struct Main :Form
{
	struct Tab :Panel
	{
		Label m_lbl;
		Tab(){}
		Tab(wchar_t const* msg, int id, Control* parent)
			:Panel(Panel::Params().id(id).parent(parent).dock(EDock::Fill).style(Panel::DefaultStyle | WS_BORDER))
			,m_lbl(Label::Params().text(msg).xy(10,10).wh(60,16).parent(this))
		{}
	};

	Label         m_lbl;
	Button        m_btn_progress;
	Button        m_btn_nm_prog;
	Button        m_btn_modeless;
	Button        m_btn_cmenu;
	Button        m_btn;
	Button        m_btn_about;
	ScintillaCtrl m_scint;
	Tab           m_tab1;
	Tab           m_tab2;
	Splitter      m_split;
	Tab           m_split_l;
	Tab           m_split_r;
	TabControl    m_tc;
	Modeless      m_modeless;
	ProgressDlg   m_nm_progress;

	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	struct Params :FormParams
	{
		Params()
		{
			wndclass(RegisterWndClass<Main>())
			.name("main")
			.title(L"Pauls Window")
			.xy(0,0)
			.wh(800,600)
			.menu({{L"&File", Menu(Menu::EKind::Popup, {MenuItem(L"E&xit", IDCLOSE)})}})
			.main_wnd(true);
		}
	};

	Main()
		:Form          (Params())
		,m_lbl         (Label::Params()        .name("m_lbl")         .text(L"hello world")       .xy(10,10)                          .wh(60,16)                      .parent(this))
		,m_btn_progress(Button::Params()       .name("m_btn_progress").text(L"progress")          .xy(10,30)                          .wh(100,20) .id(IDC_PROGRESS)   .parent(this))
		,m_btn_nm_prog (Button::Params()       .name("m_btn_nm_prog") .text(L"non-modal progress").xy(10,Top|BottomOf|IDC_PROGRESS)   .wh(100,20) .id(IDC_NM_PROGRESS).parent(this))
		,m_btn_modeless(Button::Params()       .name("m_btn_modeless").text(L"show modeless")     .xy(10,Top|BottomOf|IDC_NM_PROGRESS).wh(100,20) .id(IDC_MODELESS)   .parent(this))
		,m_btn_cmenu   (Button::Params()       .name("m_btn_cmenu")   .text(L"context menu")      .xy(10,Top|BottomOf|IDC_MODELESS)   .wh(100,20) .id(IDC_CONTEXTMENU).parent(this))
		,m_btn         (Button::Params()       .name("btn")           .text(L"BOOBS")             .xy(10,Top|BottomOf|IDC_CONTEXTMENU).wh(100,20) .id(IDC_POSTEST)    .parent(this))
		,m_btn_about   (Button::Params()       .name("m_btn_about")   .text(L"click me!")         .xy(-10,-10)                        .wh(100,20) .id(IDC_ABOUT)      .parent(this).anchor(EAnchor::BottomRight)) 
		,m_scint       (ScintillaCtrl::Params().name("m_scint")                                   .xy(0,0)                            .wh(100,100).id(IDC_SCINT)      .parent(this))
		,m_tab1        (L"hi from tab1", IDC_TAB1, this)
		,m_tab2        (L"hi from tab2", IDC_TAB2, this)
		,m_split       (Splitter::Params().vertical().name("split").parent(this))
		,m_split_l     (L"Left panel" , IDC_SPLITL, &m_split.Pane0)
		,m_split_r     (L"Right panel", IDC_SPLITR, &m_split.Pane1)
		,m_tc          (TabControl::Params().name("m_tc").text(L"tabctrl").xy(120,10).wh(500,500).id(IDC_TAB).parent(this).anchor(EAnchor::All).style_ex(0UL).padding(0))
		,m_modeless    (this)
		,m_nm_progress ()
	{
		m_nm_progress.Create(ProgressDlg::Params().parent(this).hide_on_close(true));
		auto busy_work = [](ProgressDlg* dlg)
			{
				for (int i = 0, iend = 500; dlg->Progress(i*1.f/iend) && i != iend; ++i)
					Sleep(100);
				if (dlg->Progress(1.0f))
					Sleep(1000);
			};
		m_btn_progress.Click += [&](Button&,EmptyArgs const&)
			{
				ProgressDlg progress(L"Busy work", L"workin...", busy_work);
				progress.ShowDialog(this);
			};
		m_btn_nm_prog.Click += [&](Button&,EmptyArgs const&)
			{
				m_nm_progress.Show(L"Busy work", L"workin hard or hardly workin?", busy_work);
			};
		m_btn_modeless.Click += [&](Button&,EmptyArgs const&)
			{
				m_modeless.Show();
			};
		m_btn_cmenu.Click += [&](Button&,EmptyArgs const&)
			{
				enum class ECmd { Label, Label2, TextBox };

				auto pt = MousePosition();
				if (KeyState(VK_SHIFT))
				{
					auto m = ::CreatePopupMenu();
					::AppendMenuW(m, MF_SEPARATOR, 0, nullptr);
					::TrackPopupMenu(m, ::GetSystemMetrics(SM_MENUDROPALIGNMENT)|TPM_LEFTBUTTON, pt.x, pt.y, 0, *this, nullptr);
				}
				else
				{
					// Construct the menu
					ContextMenu menu;
					ContextMenu::Label     lbl1(menu, L"&Label1", 0);
					ContextMenu::Separator sep1(menu);
					ContextMenu::Label     lbl2(menu, L"&Label2", 2);
					ContextMenu::Label     lbl3(menu, L"&Label3", 3);
					ContextMenu::Separator sep3(menu);
					ContextMenu::Label     lbl4(menu, L"&Label4", 5);
					ContextMenu::TextBox   tb  (menu, L"&Text Box1", L"xox", 6);
					ContextMenu::Label     lbl5(menu, L"&Label5", 7);
					ContextMenu::Label     lbl6(menu, L"&Label6", 8);
					
					//ContextMenu submenu(&menu, _T("Sub Menu"));
					//ContextMenu::Label     lbl3(submenu, _T("&Label3"), (int)ECmd::Label);
					pt = PointToClient(pt);
					menu.Show(this, pt.x, pt.y);
				}
			};
		m_btn.Click += std::bind(&Main::RunBoobs, this, _1, _2);
		m_btn_about.Click += [&](Button&,EmptyArgs const&)
			{
				About about;
				about.ShowDialog(this);
			};

		m_tc.Insert(L"Tab0", m_split);
		m_tc.Insert(L"Tab1", m_tab1);
		m_tc.Insert(L"Tab2", m_scint);
		m_tc.Insert(L"Tab3", m_tab2);
		m_tc.SelectedIndex(0);

		m_scint.InitDefaults();
		m_scint.InitLdrStyle();
	}

	void RunBoobs(Button&, EmptyArgs const&)
	{
		auto sr = ScreenRect();
		auto ar = AdjRect();
		auto cr = ClientRect().Shifted(-ar.left, -ar.top);

		auto tsr = m_tc.ScreenRect();
		auto tcr = m_tc.ClientRect();
		auto tpr = m_tc.ParentRect();
		m_tc.ParentRect(tpr);
	}
};

int __stdcall WinMain(HINSTANCE hInstance, HINSTANCE, LPTSTR, int)
{
	pr::InitCom com;
	pr::GdiPlus gdi;

	InitCtrls();

	try
	{
		(void)hInstance;

		#ifdef USE_ATL
		//WtlMain wtl;
		//wtl.Create(nullptr);
		//wtl.ShowWindow(SW_SHOW);
		#endif

		pr::win32::LoadDll<struct Scintilla>(L"scintilla.dll");

		Main main;
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

