//**************************************************************
// Copyright © Rex Bionics 2011
//**************************************************************

#include "stdafx.h"
#include "forward.h"
#include "settings.h"
#include "pr/gui/gdiplus.h"
#include "pr/gui/grid_ctrl.h"
#include "pr/gui/graph_ctrl.h"

using namespace gui_template;

// Declare the atl app module
CAppModule g_app;

struct MainGUI
	:WTL::CFrameWindowImpl<MainGUI>
	,WTL::CMessageLoop
	,WTL::CDialogResize<MainGUI>
{
	gui_template::Settings        m_settings;
	pr::GdiPlus                   m_gdiplus;
	WTL::CFont                    m_font_norm;
	WTL::CFont                    m_font_bold;
	WTL::CStatusBarCtrl           m_status;
	pr::gui::CGridCtrl            m_grid;
	pr::gui::CGraphCtrl<>         m_graph;
	pr::gui::CGraphCtrl<>::Series m_series;
	bool m_resizing;
	
	MainGUI(HINSTANCE hInstance, LPSTR lpstrCmdLine)
	:m_settings()
	,m_font_norm()
	,m_font_bold()
	,m_status()
	,m_grid()
	,m_graph()
	,m_series()
	,m_resizing(false)
	{
		(void)hInstance; (void)lpstrCmdLine;
	}
	~MainGUI()
	{}
	
	enum { IDR_MAINFRAME = 100, IDC_STATUSBAR = 100, IDC_TOOLBAR = 100, IDC_GRID = 1000, IDC_GRAPH = 1001 };
	DECLARE_FRAME_WND_CLASS(_T("prGUITemplateWinClass"), IDR_MAINFRAME);
	
	BEGIN_MSG_MAP(MainGUI)
		MSG_WM_CREATE(OnCreate)
		MSG_WM_DESTROY(OnDestroy)
		MSG_WM_SYSCOMMAND(OnSysCommand)
		MSG_WM_COMMAND(OnCommand)
		MSG_WM_PAINT(OnPaint)
		MSG_WM_SIZING(OnSizing)
		MSG_WM_EXITSIZEMOVE(OnExitSizeMove)
		MSG_WM_SIZE(OnSize)
		CHAIN_MSG_MAP(CFrameWindowImpl<MainGUI>)
		CHAIN_MSG_MAP(CDialogResize<MainGUI>)
	END_MSG_MAP()
	BEGIN_DLGRESIZE_MAP(MainGUI)
		DLGRESIZE_CONTROL(IDC_GRID, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
		DLGRESIZE_CONTROL(IDC_GRAPH, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
	END_DLGRESIZE_MAP()
	
private:
	
	enum ECmd
	{
		ECmd_Reset,
	};
	
	virtual BOOL PreTranslateMessage(MSG* pMsg)
	{
		if (pMsg->message == WM_MOUSEWHEEL) return pr::HoverScroll(pMsg);
		return FALSE;
	}
	
	// Idle handler
	BOOL OnIdle(int)
	{
		// Update the display
		//View3D_Render(m_drawset);
		//Sleep(1); // free some cpu
		//return TRUE;
		return FALSE;
	}
	
	// Create the main window
	LRESULT OnCreate(LPCREATESTRUCT)
	{
		// Attach/Create status bar fonts
		m_font_norm.CreatePointFont(100, _T("Segoe UI"));
		m_font_bold.CreatePointFont(100, _T("Segoe UI"), 0, true);
		
		// Create the menu
		CMenu menu_file; menu_file.CreatePopupMenu();
		menu_file.AppendMenu(MF_SEPARATOR, 0U, (LPCTSTR)0);
		menu_file.AppendMenu(MF_STRING, ID_APP_EXIT, _T("E&xit"));
		
		CMenu menu_help; menu_help.CreatePopupMenu();
		menu_help.AppendMenu(MF_SEPARATOR, 0U, (LPCTSTR)0);
		menu_help.AppendMenu(MF_STRING, ID_APP_ABOUT, _T("&About"));
		
		CMenu menu; menu.CreateMenu();
		menu.AppendMenu(MF_POPUP, menu_file, _T("&File"));
		menu.AppendMenu(MF_POPUP, menu_help, _T("&Help"));
		SetMenu(menu);
		
		// Create and attached the status bar
		CreateSimpleStatusBar();
		m_status.Attach(m_hWndStatusBar);
		int widths[] = {-1}; m_status.SetParts(sizeof(widths)/sizeof(widths[0]), widths);
		
		// Load the app settings
		if (!m_settings.Load(pr::GetAppSettingsFilepath<Settings::string>(m_hWnd, true)))
		{
			Status(_T("Default settings used. Could not load user settings file"), true);
			m_settings.Save();
		}
		
		CRect area; GetClientRect(&area);
		area.bottom -= pr::WindowBounds(m_status.m_hWnd).SizeY();
		int h = area.Height()/2;
		area.bottom = area.top + h;
		
		area.MoveToY(0);
		m_grid.Create(m_hWnd, area, NULL, WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN|WS_TABSTOP|WS_BORDER, WS_EX_STATICEDGE, IDC_GRID);
		m_grid.AddColumn(_T("Col1"), 80);
		m_grid.AddColumn(_T("Col2"), 80);
		m_grid.AddColumn(_T("Col3"), 80);
		m_grid.AddColumn(_T("Col4"), 80);
		m_grid.AddColumn(_T("Col5"), 80);
		for (int i = 0; i != 10; ++i) m_grid.AddRow();
		
		area.MoveToY(h);
		m_graph.Create(m_hWnd, area, NULL, WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN|WS_BORDER, WS_EX_STATICEDGE, IDC_GRAPH);
		m_graph.m_title = _T("My Graph");
		m_graph.m_xaxis.m_label = _T("X Axis");
		m_graph.m_yaxis.m_label = _T("Y Axis");
		
		// Can use the graph data object directly since it's a user provided type
		m_series.m_name = _T("Pauls Data");
		m_series.m_opts.m_point_size = 0;
		for (float x = -pr::maths::tau; x <= pr::maths::tau; x += 0.00001f)
			m_series.m_values.m_data.push_back(pr::gui::CGraphData::Elem(x, sinf(x)));
		
		m_graph.m_series.push_back(&m_series);
		m_graph.FindDefaultRange();
		m_graph.ResetToDefaultRange();
		
		DlgResize_Init(false, false);
		return S_OK;
	}
	
	// Main window destroyed
	void OnDestroy()
	{
	}
	
	// System commands
	void OnSysCommand(UINT wparam, WTL::CPoint const&)
	{
		switch (wparam)
		{
		default: SetMsgHandled(FALSE); break;
		case SC_CLOSE: CloseApp(0); break;
		}
	}
	
	// Menu commands
	void OnCommand(UINT, int nID, CWindow)
	{
		switch (nID)
		{
		default: SetMsgHandled(FALSE); break;
		case ID_APP_EXIT: CloseApp(0); break;
		case ID_APP_ABOUT: break;
		}
	}
	
	// Resizing handlers
	void OnSizing(UINT, LPRECT)
	{
		SetMsgHandled(FALSE);
		m_resizing = true;
	}
	void OnExitSizeMove()
	{
		SetMsgHandled(FALSE);
		m_resizing = false;
		OnSize(0, CSize());
	}
	void OnSize(UINT type, CSize)
	{
		SetMsgHandled(FALSE);
		if (m_resizing) return;
		if (type != SIZE_MINIMIZED)
		{
			// Find the new client area
			pr::IRect area = pr::ClientArea(m_hWnd);
			//if (m_menu.m_hMenu)  area.m_min.y += m_menu.m_hw
			if (m_hWndToolBar)   area.m_min.y += pr::WindowBounds(m_hWndToolBar).SizeY();
			if (m_hWndStatusBar) area.m_max.y -= pr::WindowBounds(m_hWndStatusBar).SizeY();
			
			UpdateLayout(true);
		}
	}
	
	// Paint the window
	void OnPaint(CDCHandle)
	{
		SetMsgHandled(FALSE);
		//View3D_Render(m_drawset);
		//if (m_main && !m_resizing)
		//	m_main->Render();
	}
	
	// Update the status text
	void Status(TCHAR const* msg, bool bold)
	{
		m_status.SetText(0, msg);
		m_status.SetFont(bold ? m_font_bold : m_font_norm);
	}
	
	// Shutdown the app
	void CloseApp(int exit_code)
	{
		DestroyWindow();
		::PostQuitMessage(exit_code);
	}
};

// Entry point ***********************************************

int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpstrCmdLine, int nCmdShow)
{
	(void)(hInstance); (void)(hPrevInstance); (void)(lpstrCmdLine); (void)(nCmdShow);
	
	int res = 0;
	try
	{
		// CoInitialise
		pr::InitCom init_com;
		
		// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
		::DefWindowProc(NULL, 0, 0, 0L);

		//ICC_BAR_CLASSES|ICC_TAB_CLASSES
		AtlInitCommonControls(ICC_STANDARD_CLASSES); // add flags to support other controls

		pr::Throw(g_app.Init(NULL, hInstance));

		// Run the app
		MainGUI gui(hInstance, lpstrCmdLine);
		g_app.AddMessageLoop(&gui);
		if (gui.Create(0) == 0) throw Exception(EResult::StartupFailed, "Main window creation failed");
		gui.ShowWindow(nCmdShow);
		res = gui.Run();
	}
	catch (Exception const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		std::string err = ex.m_msg;
		err += pr::FmtS("\nCode: %X - %s", res, pr::HrMsg(res).c_str());
		::MessageBoxA(0, err.c_str(), "Error", MB_OK|MB_ICONERROR);
		res = -1;
	}
	catch (...)
	{
		::MessageBoxA(0, "Shutting down due to an unknown exception", "Error", MB_OK|MB_ICONERROR);
		res = -1;
	}
	g_app.RemoveMessageLoop();
	g_app.Term();
	return res;
}

// Add common controls 6 to the manifest
#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif



//struct Dialog
//	:public CIndirectDialogImpl<Dialog>
//	,public CDialogResize<Dialog>
//{
//	pr::gui::CGridCtrl  m_grid;
//	pr::gui::CGraphCtrl m_graph;
//	
//	Dialog()  {}
//	~Dialog() { if (IsWindow()) DestroyWindow(); }
//	
//	enum { IDC_TEXT = 1000, IDC_GRID = 1001, IDC_GRAPH = 1002 };
//	BEGIN_DIALOG_EX(0, 0, 500, 480, 0)
//		DIALOG_STYLE(DS_CENTER|WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX|WS_SYSMENU)
//		DIALOG_CAPTION("Test Dialog")
//		DIALOG_FONT(8, TEXT("MS Shell Dlg"))
//	END_DIALOG()
//	BEGIN_CONTROLS_MAP()
//		//CONTROL_CONTROL("", IDC_GRID, "BHWTLGRID", WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN, 0, 0, 500, 490, WS_EX_STATICEDGE);
//		//CONTROL_CONTROL("", IDC_TEXT, "RICHEDIT50W", WS_HSCROLL|WS_VSCROLL|ES_AUTOHSCROLL|ES_AUTOVSCROLL|ES_MULTILINE|ES_WANTRETURN, 0, 0, 500, 490, WS_EX_STATICEDGE);
//		//CONTROL_EDITTEXT(IDC_TEXT, 0, 0, 500, 490, WS_HSCROLL|WS_VSCROLL|ES_AUTOHSCROLL|ES_AUTOVSCROLL|ES_MULTILINE|ES_WANTRETURN, WS_EX_STATICEDGE)//NOT WS_BORDER|
//	END_CONTROLS_MAP()
//	BEGIN_DLGRESIZE_MAP(Dialog)
//		DLGRESIZE_CONTROL(IDC_GRID, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
//		//DLGRESIZE_CONTROL(IDC_TEXT, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
//	END_DLGRESIZE_MAP()
//	BEGIN_MSG_MAP(Dialog)
//		MSG_WM_INITDIALOG(OnInitDialog)
//		MSG_WM_SYSCOMMAND(OnSysCommand)
//		MSG_WM_COMMAND(OnCommand)
//		CHAIN_MSG_MAP(CDialogResize<Dialog>)
//	END_MSG_MAP()
//	
//	// Handler methods
//	BOOL OnInitDialog(CWindow, LPARAM)
//	{
//		CenterWindow(GetParent());
//		
//		// Create the menu
//		CMenu menu_file; menu_file.CreatePopupMenu();
//		menu_file.AppendMenuA(MF_SEPARATOR, 0U, (LPCTSTR)0);
//		menu_file.AppendMenuA(MF_STRING, ID_APP_EXIT, "E&xit");
//		CMenu menu; menu.CreateMenu();
//		menu.AppendMenuA(MF_POPUP, menu_file, "&File");
//		SetMenu(menu);
//		
//		//m_grid.Attach(GetDlgItem(IDC_GRID));
//		CRect client1(0,0,300,200);
//		m_grid.Create(m_hWnd, client1, NULL, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN, WS_EX_STATICEDGE, IDC_GRID);
//		m_grid.AddColumn("Col1", 80);
//		m_grid.AddColumn("Col2", 80);
//		m_grid.AddColumn("Col3", 80);
//		m_grid.AddColumn("Col4", 80);
//		m_grid.AddColumn("Col5", 80);
//		for (int i = 0; i != 10; ++i) m_grid.AddRow();
//		
//		CRect client2(0,201,300,200);
//		m_graph.Create(m_hWnd, client2, NULL, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN, WS_EX_STATICEDGE, IDC_GRAPH);
//		
//		DlgResize_Init(true, false);
//		return TRUE;
//	}
//	void OnSysCommand(UINT wparam, WTL::CPoint const&)
//	{
//		switch (wparam)
//		{
//		default: SetMsgHandled(FALSE); break;
//		case SC_CLOSE: EndDialog(0); break;
//		}
//	}
//	void OnCommand(UINT, int nID, CWindow)
//	{
//		switch (nID)
//		{
//		default: SetMsgHandled(FALSE); break;
//		case ID_APP_EXIT: EndDialog(0); break;
//		}
//	}
//}