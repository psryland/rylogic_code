#include "src/forward.h"
#include "src/about.h"
#include "src/modeless.h"
#include "src/graph.h"
#include "pr/gui/wingui.h"
#include "pr/gui/gdiplus.h"
#include "pr/gui/progress_ui.h"
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

using namespace pr::gui;
using namespace pr::gdi;

// Application window
struct Main :Form
{
	struct Tab :Panel
	{
		Label m_lbl;
		Tab() {}
		Tab(Control* parent, wchar_t const* msg, int id)
			:Panel(Panel::Params<>().id(id).parent(parent).dock(EDock::Fill).style('+',WS_BORDER))
			,m_lbl(Label::Params<>().text(msg).xy(10,10).wh(60,16).parent(this))
		{}
	};

	Label         m_lbl;
	Button        m_btn_progress;
	Button        m_btn_nm_prog;
	Button        m_btn_modeless;
	Button        m_btn_cmenu;
	Button        m_btn;
	Button        m_btn_about;
	Button        m_btn_msgbox;

	TabControl    m_tc;
	Splitter      m_split;
	Tab           m_split_l;
	Tab           m_split_r;
	Tab           m_tab1;
	Tab           m_tab2;
	ScintillaCtrl m_scint;
	ListView      m_lv;

	Modeless      m_modeless;
	ProgressUI    m_nm_progress;

	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	Main()
		:Form(Params<>()
			.name("main")
			.title(L"Pauls Window")
			.icon(IDR_MAINFRAME)
			.xy(1500,100).wh(800,600)
			.menu({{L"&File", Menu(Menu::EKind::Popup, {MenuItem(L"E&xit", IDCLOSE)})}})
			.main_wnd(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<Main>()))
		, m_lbl         (Label::Params<>() .name("m_lbl")         .parent(this_).text(L"hello world")       .xy(10,10)                              .wh(Auto,Auto))
		, m_btn_progress(Button::Params<>().name("m_btn_progress").parent(this_).text(L"progress")          .xy(10,30)                              .wh(100,20) .id(IDC_PROGRESS)   )
		, m_btn_nm_prog (Button::Params<>().name("m_btn_nm_prog") .parent(this_).text(L"non-modal progress").xy(10,Top|BottomOf|m_btn_progress.id()).wh(100,20) .id(IDC_NM_PROGRESS))
		, m_btn_modeless(Button::Params<>().name("m_btn_modeless").parent(this_).text(L"show modeless")     .xy(10,Top|BottomOf|m_btn_nm_prog.id()) .wh(100,20) .id(IDC_MODELESS)   )
		, m_btn_cmenu   (Button::Params<>().name("m_btn_cmenu")   .parent(this_).text(L"context menu")      .xy(10,Top|BottomOf|m_btn_modeless.id()).wh(100,20) .id(IDC_CONTEXTMENU))
		, m_btn         (Button::Params<>().name("btn")           .parent(this_).text(L"BOOBS")             .xy(10,Top|BottomOf|m_btn_cmenu.id())   .wh(100,40) .id(IDC_POSTEST)    .image(L"refresh", pr::gui::Image::EType::Png))
		, m_btn_about   (Button::Params<>().name("m_btn_about")   .parent(this_).text(L"About")             .xy(-10,-10)                            .wh(100,32) .id(IDC_ABOUT)      .anchor(EAnchor::BottomRight)) 
		, m_btn_msgbox  (Button::Params<>().name("m_btn_msgbox")  .parent(this_).text(L"MsgBox")            .xy(-10,Bottom|TopOf|m_btn_about.id())  .wh(100,32) .id(IDC_MSGBOX)     .anchor(EAnchor::BottomRight)) 

		, m_tc     (TabControl::Params<>().name("m_tc").parent(this_).xy(120,10).wh(500,500).id(IDC_TAB).anchor(EAnchor::All).style_ex('=',0).padding(0))
		, m_split  (Splitter  ::Params<>().name("split").parent(&m_tc))
		, m_split_l(&m_split.Pane0, L"LEFT panel" , IDC_SPLITL)
		, m_split_r(&m_split.Pane1, L"RITE panel", IDC_SPLITR)
		, m_tab1   (&m_tc, L"hi from tab1", IDC_TAB1)
		, m_tab2   (&m_tc, L"hi from tab2", IDC_TAB2)
		, m_scint  (ScintillaCtrl::Params<>().name("m_scint").parent(&m_tc).dock(EDock::Fill).id(IDC_SCINT))
		, m_lv     (ListView::Params<>().name("listview").parent(&m_tc).dock(EDock::Fill).columns({L"Name", L"Reason", L"Magnetic Dipole Moment"}))

		,m_modeless    (this_)
		,m_nm_progress (ProgressUI::Params().parent(this_).hide_on_close())
	{
		CreateHandle();

		auto busy_work = [](ProgressUI* dlg)
		{
			for (int i = 0, iend = 500; dlg->Progress(i*1.f/iend) && i != iend; ++i)
				Sleep(100);
			if (dlg->Progress(1.0f))
				Sleep(1000);
		};

		// Assign button handlers
		m_btn_progress.Click += [&](Button&,EmptyArgs const&)
		{
			ProgressUI progress(L"Busy work", L"workin...", busy_work);
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
		m_btn_about.Click += [&](Button&,EmptyArgs const&)
		{
			//About().ShowDialog(this);
			About2().ShowDialog(this);
		};
		m_btn_msgbox.Click += [&](Button&, EmptyArgs const&)
		{
			MsgBox::Show(*this,
				L"This is a test message box. It has loads of text in it to test how the re-flow thing works. "
				L"Hopefully, it will work well, although if it does the first time I try it, I'll be amazed.\r\n"
				L"\r\n"
				L"Here's hoping...",
				L"Message Title", MsgBox::EButtons::YesNo, MsgBox::EIcon::Question);
		};
		m_btn.Click += std::bind(&Main::RunBoobs, this, _1, _2);

		if ((HWND)m_tc != nullptr)
		{
			m_tc.Insert(L"Tab0", m_split);
			m_tc.Insert(L"Tab1", m_tab1);
			m_tc.Insert(L"Tab2", m_tab2);
			m_tc.Insert(L"Tab3", m_scint);
			m_tc.Insert(L"Tab4", m_lv);
			m_tc.SelectedIndex(0);
		}

		if ((HWND)m_scint != nullptr)
		{
			m_scint.InitDefaultStyle();
			m_scint.InitLdrStyle();
		}
	}

	void RunBoobs(Button&, EmptyArgs const&)
	{
		auto sr = ScreenRect();
		auto ar = AdjRect();
		auto cr = ClientRect(true).Shifted(-ar.left, -ar.top);

		auto tsr = m_tc.ScreenRect();
		auto tcr = m_tc.ClientRect(true);
		auto tpr = m_tc.ParentRect();
		m_tc.ParentRect(tpr);
	}
};
struct Test :Form
{
	enum { IDC_SPLIT = 100, IDC_LEFT, IDC_RITE,};
	Panel    m_panel;
	Splitter m_split;
	Test()
		:Form(Params<>()
			.name("test")
			.title(L"Paul's Window")
			.xy(2000,100)
			.wh(800,600)
			.menu({{L"&File", Menu(Menu::EKind::Popup, {MenuItem(L"E&xit", IDCLOSE)})}})
			.main_wnd(true)
			.wndclass(RegisterWndClass<Test>()))
		,m_panel(Panel   ::Params<>().parent(this_   ).xy(50,50).wh(Fill,Fill).anchor(EAnchor::All))
		,m_split(Splitter::Params<>().parent(&m_panel).dock(EDock::Fill))
	{
		m_split.Pane0.Style('+', WS_BORDER);
		m_split.Pane1.Style('+', WS_BORDER);
	}
};
struct Test2 :Form
{
	TabControl m_tc;
	ScintillaCtrl m_scint;

	Test2()
		:Form(Params<>().name("test").title(L"Paul's Window").start_pos(EStartPosition::CentreParent).wh(320,256).wndclass(RegisterWndClass<Test2>()))
		,m_tc(TabControl::Params<>().parent(this_).dock(EDock::Fill))
		,m_scint(ScintillaCtrl::Params<>().parent(&m_tc).dock(EDock::Fill))
	{
		if ((HWND)m_tc != nullptr)
		{
			m_tc.Insert(L"Tab0", m_scint);
			m_tc.SelectedIndex(0);
		}
	}
};

// Entry point
int __stdcall wWinMain(HINSTANCE hInstance, HINSTANCE, LPWSTR, int)
{
	pr::InitCom com;
	pr::GdiPlus gdi;

	pr::win32::LoadDll<struct Scintilla>(L"scintilla.dll");
	InitCtrls();

	try
	{
		(void)hInstance;

		#ifdef USE_ATL
		//WtlMain wtl;
		//wtl.Create(nullptr);
		//wtl.ShowWindow(SW_SHOW);
		CBitmap bm;
		#endif


		//About about1;
		//about1.Show();
		//About2 about2;
		//about2.ShowDialog();
		//return 0;
		//ProgressUI main(ProgressUI::Params<>().title(L"Progress").desc(L"This is not a drill").xy(2000,100).main_wnd());
		//return main.ShowDialog();

		Main main; main.Show();
		//Test test1; auto& main = test1; test1.Show();
		//Test2 test2; auto& main = test2; test2.Show();

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


//hack/workaround for missing function
extern "C"
{
	namespace Gdiplus
	{
		GpStatus GdipGetMetafileHeaderFromWmf(HMETAFILE, GDIPCONST WmfPlaceableFileHeader*, MetafileHeader*)
		{
			return GpStatus::Ok;
		}
	}
}