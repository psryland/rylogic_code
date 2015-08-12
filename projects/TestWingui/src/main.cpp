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
	struct Tab
	{
		Panel m_panel;
		Label m_lbl;
		Tab(wchar_t const* msg, int id, Control* parent)
			:m_panel(L"panel", "tab-panel", 0, 0, 10, 10, id, parent, EAnchor::All)
			,m_lbl  (msg, "tab-lbl", 10, 10, 60, 16, IDC_UNUSED, &m_panel)
		{}
	};

	Label         m_lbl;
	Button        m_btn1;
	Button        m_btn2;
	Button        m_btn3;
	Button        m_btn4;
	ScintillaCtrl m_scint;
	Tab           m_tab1;
	Tab           m_tab2;
	TabControl    m_tc;
	Modeless      m_modeless;

	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_MODELESS, IDC_CONTEXTMENU, IDC_ABOUT, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2 };

	Main()
		:Form   (RegisterWndClass<Main>(), L"Pauls Window", "main", ApplicationMainWindow, 200, 200, 800, 600, DefaultFormStyle, DefaultFormStyleEx, nullptr)
		,m_lbl  (L"hello world", "m_lbl", 10, 10, 60, 16, IDC_UNUSED, this)
		,m_btn1 (L"progress", "m_btn1", 10, 30, 80, 20, IDC_PROGRESS, this)
		,m_btn2 (L"show modeless", "m_btn2", 10, Top|BottomOf|IDC_PROGRESS, 80, 20, IDC_MODELESS, this, EAnchor::TopLeft)
		,m_btn3 (L"context menu", "m_btn3", 10, Top|BottomOf|IDC_MODELESS, 80, 20, IDC_CONTEXTMENU, this, EAnchor::TopLeft)
		,m_btn4 (L"click me!", "m_btn4", -10, -10, 80, 20, IDC_ABOUT, this, EAnchor::BottomRight)
		,m_scint("m_scint", 0, 0, 100, 100, IDC_SCINT, this)
		,m_tab1 (L"hi from tab1", IDC_TAB1, this)
		,m_tab2 (L"hi from tab2", IDC_TAB2, this)
		,m_tc   (L"tabctrl", "m_tc", 120, 10, 500, 500, IDC_TAB, this, EAnchor::All, DefaultControlStyle, 0UL)
		,m_modeless(this)
	{
		MenuStrip file_menu(true);
		file_menu.Insert(L"E&xit", IDCLOSE);
		m_menu = MenuStrip(MenuStrip::Strip);
		m_menu.Insert(file_menu, L"&File");

		m_btn1.Click += [&](Button&,EmptyArgs const&)
			{
				auto task = [](ProgressDlg* dlg)
				{
					for (int i = 0, iend = 50; dlg->Progress(i*1.f/iend) && i != iend; ++i)
					{
						Sleep(100);
					}
					if (dlg->Progress(1.0f))
						Sleep(1000);
				};
				ProgressDlg progress(L"Busy work", L"workin...", task);
				progress.ShowDialog(*this);
			};
		m_btn2.Click += [&](Button&,EmptyArgs const&)
			{
				m_modeless.Show();
			};
		m_btn3.Click += [&](Button&,EmptyArgs const&)
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
				//ECmd cmd = (ECmd)LOWORD(res);
				//int  idx = (int)HIWORD(res);
				
				#if 0
				//menu.AddItem(std::make_shared<ContextMenu::Label>(L"&Reset Zoom", MAKEWPARAM(ECmd::ResetZoom, idx_all)));
				//if (!m_series.empty())
				//{
				//	std::vector<std::wstring> plot_types;
				//	plot_types.push_back(L"Point");
				//	plot_types.push_back(L"Line");
				//	plot_types.push_back(L"Bar");

				//	int vis = 0, invis = 0;
				//	for (auto& series : m_series)
				//		(series->m_opts.Visible ? vis : invis) = 1;

				//	// All series options
				//	auto& series_all = menu.AddItem<ContextMenu>(std::make_shared<ContextMenu>(L"Series: All"));
				//	series_all.AddItem(std::make_shared<ContextMenu::Label>(L"&Visible", MAKEWPARAM(ECmd::Visible, idx_all), vis + invis));

				//	// Specific series options
				//	int idx_series = -1;
				//	for (auto s : m_series)
				//	{
				//		auto& series = *s;
				//		++idx_series;

				//		// Create a sub menu for this series
				//		StylePtr style(new ContextMenuStyle());
				//		style->m_col_text = series.m_opts.color();
				//		auto& series_m = menu.AddItem<ContextMenu>(std::make_shared<ContextMenu>(series.m_name.c_str(), 0, series.m_opts.Visible, style));

				//		// Visibility
				//		series_m.AddItem(std::make_shared<ContextMenu::Label>(L"&Visible"     ,MAKEWPARAM(ECmd::Visible          ,idx_series), series.m_opts.Visible));
				//		series_m.AddItem(std::make_shared<ContextMenu::Label>(L"Series &Data" ,MAKEWPARAM(ECmd::VisibleData      ,idx_series), series.m_opts.DrawData));
				//		series_m.AddItem(std::make_shared<ContextMenu::Label>(L"&Error Bars"  ,MAKEWPARAM(ECmd::VisibleErrorBars ,idx_series), series.m_opts.DrawErrorBars));

				//		// Plot Type
				//		series_m.AddItem(std::make_shared<ContextMenu::Combo>(L"&Plot Type"   ,&plot_types, MAKEWPARAM(ECmd::PlotType ,idx_series)));

				//		{// Appearance menu
				//			auto& appearance = series_m.AddItem<ContextMenu>(std::make_shared<ContextMenu>(L"&Appearance"));
				//			if (series.m_opts.PlotType == Series::RdrOptions::EPlotType::Point ||
				//				series.m_opts.PlotType == Series::RdrOptions::EPlotType::Line)
				//			{
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Point Size:", L"9", MAKEWPARAM(ECmd::PointSize, idx_series)));
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Point Colour:", L"9", MAKEWPARAM(ECmd::PointColour, idx_series)));
				//			}
				//			if (series.m_opts.PlotType == Series::RdrOptions::EPlotType::Line)
				//			{
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Line Width:", L"9", MAKEWPARAM(ECmd::LineWidth, idx_series)));
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Line Colour:", L"9", MAKEWPARAM(ECmd::LineColour, idx_series)));
				//			}
				//			if (series.m_opts.PlotType == Series::RdrOptions::EPlotType::Bar)
				//			{
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Bar Width:", L"9", MAKEWPARAM(ECmd::BarWidth, idx_series)));
				//				appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Bar Colour:", L"9", MAKEWPARAM(ECmd::BarColour, idx_series)));
				//			}
				//		}
				//	}
				//}
				//
				//int res = menu.Show(m_hwnd, int(point.x), int(point.y));
				//ECmd cmd = (ECmd)LOWORD(res);
				//int  idx = (int)HIWORD(res);
				//switch (cmd)
				//{
				//default: break;
				//case ECmd::ShowValues:
				//	m_tt.ShowWindow(m_tt.IsWindowVisible() ? SW_HIDE : SW_SHOW);
				//	break;
				//case ECmd::ResetZoom:
				//	ResetToDefaultRange();
				//	Dirty(true);
				//	break;
				//case ECmd::Visible:
				//	if (idx == idx_all)
				//	{
				//		for (SeriesCont::iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i)
				//		{
				//		}
				//	}
				//	break;
				//}
				//
				////ContextMenu::Label show_values(L"&Show Values", ECmd_ShowValues, 0, m_tt.IsWindowVisible());
				////menu.AddItem(show_values);
				////
				////ContextMenu::Label reset_zoom(L"&Reset Zoom", ECmd_ResetZoom);
				////menu.AddItem(reset_zoom);
				////
				////Series::Menu series_all(L"Series: &All");
				////ContextMenu::Label all_visible(L"&Visible", ECmd_All_Visible);
				////series_all.AddItem(all_visible);
				////if (!m_series.empty()) menu.AddItem(series_all);
				////int invis = 0, vis = 0;
				////for (SeriesCont::const_iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i) (*i)->m_opts.m_visible ? vis : invis) = 1;
				////all_visible.m_check_state = vis + invis;
				////
				////
				////struct Menu :pr::gui::ContextMenu
				////{
				////	pr::gui::ContextMenuStyle m_style;
				////	pr::gui::ContextMenu::Label m_vis;
				////	
				////	Menu()
				////	:pr::gui::ContextMenu(L"", &m_style)
				////	,m_vis(L"&Visible", ECmd_Series)
				////	{}
				////};
				////
				////
				////for (SeriesCont::iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i)
				////{
				////	Series& series = **i;
				////	series.m_menu_style
				////	(*i)->m_opts.m_visible ? vis : invis) = 1;
				////
				////	
				////	series.AppendMenu(menu);

				////		series.m_menu.m_label.m_text      = series.m_name;
				////		series.m_menu.m_style.m_col_text  = series.m_opts.color();
				////		series.m_menu.m_vis.m_check_state = series.m_opts.m_visible;
				////		series.m_menu.AddItem(series.m_menu.m_vis);
				////		menu.AddItem(series.m_menu);
				////	}
				////}
				#endif
			};
		m_btn4.Click += [&](Button&,EmptyArgs const&)
			{
				About about;
				about.ShowDialog(this);
			};

		m_tc.Insert(L"Tab0", m_scint);
		m_tc.Insert(L"Tab1", m_tab1.m_panel);
		m_tc.Insert(L"Tab2", m_tab2.m_panel);
		m_tc.SelectedIndex(0);

		m_scint.InitDefaults();
		m_scint.InitLdrStyle();
	}
};

int __stdcall _tWinMain(HINSTANCE hInstance, HINSTANCE, LPTSTR, int)
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

