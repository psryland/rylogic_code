//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_LINEDRAWER_GUI_H
#define LDR_LINEDRAWER_GUI_H

#include "linedrawer/main/forward.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "linedrawer/utility/misc.h"

namespace ldr
{
	struct MainGUI
		:pr::app::MainGUI<ldr::MainGUI, ldr::Main, pr::SimMsgLoop>
		,pr::cmdline::IOptionReceiver
		,pr::gui::RecentFiles::IHandler
		,pr::events::IRecv<ldr::Event_Info>
		,pr::events::IRecv<ldr::Event_Warn>
		,pr::events::IRecv<ldr::Event_Error>
		,pr::events::IRecv<ldr::Event_Status>
		,pr::events::IRecv<ldr::Event_Refresh>
		,pr::events::IRecv<pr::rdr::Evt_SceneRender>
		,pr::events::IRecv<pr::ldr::Evt_Refresh>
		,pr::events::IRecv<pr::ldr::Evt_LdrMeasureCloseWindow>
		,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>
		,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgCloseWindow>
		,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>
		,pr::events::IRecv<pr::ldr::Evt_AddBegin>
		,pr::events::IRecv<pr::ldr::Evt_AddEnd>
		,pr::events::IRecv<pr::settings::Evt<UserSettings>>
		,pr::AlignTo<16>
	{
		enum
		{
			IDD                = IDD_DIALOG_MAIN,
			IDC_STATUSBAR_MAIN = 200,
			ID_MAIN_TIMER      = 2000,
		};

		WTL::CMenuHandle          m_menu;                 // The menu
		WTL::CStatusBarCtrl       m_status;               // The status bar
		pr::gui::RecentFiles      m_recent_files;         // The recent files
		pr::gui::MenuList         m_saved_views;          // A list of camera snapshots
		pr::ldr::ObjectManagerDlg m_store_ui;             // GUI window for manipulating ldr object properties
		pr::ldr::MeasureDlg       m_measure_tool_ui;      // The UI for the measuring tool
		pr::ldr::AngleDlg         m_angle_tool_ui;        // The UI for the angle measuring tool
		bool                      m_mouse_status_updates; // Whether to show mouse position in the status bar (todo: more general system for this)
		bool                      m_suspend_render;       // True to prevent rendering
		StatusPri                 m_status_pri;           // Status priority buffer

		static char const* AppName() { return ldr::AppTitleA(); }

		MainGUI(LPTSTR cmdline);

		BEGIN_MSG_MAP(x)
			MSG_WM_SYSKEYDOWN(OnSysKeyDown)
			MSG_WM_LBUTTONDBLCLK(OnMouseDblClk)
			MSG_WM_RBUTTONDBLCLK(OnMouseDblClk)
			MSG_WM_MBUTTONDBLCLK(OnMouseDblClk)

			COMMAND_ID_HANDLER(ID_ACCELERATOR_FILENEW               ,OnFileNew)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_FILENEWSCRIPT         ,OnFileNewScript)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_FILEOPEN              ,OnFileOpen)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_FILEOPEN_ADDITIVE     ,OnFileOpenAdditive)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_WIREFRAME             ,OnToggleFillMode)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_EDITOR                ,OnEditSourceFiles)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_PLUGINMGR             ,OnFilePluginMgr)
			COMMAND_ID_HANDLER(ID_ACCELERATOR_LIGHTING_DLG          ,OnShowLightingDlg)
			COMMAND_ID_HANDLER(ID_FILE_NEW1                         ,OnFileNew)
			COMMAND_ID_HANDLER(ID_FILE_NEWSCRIPT                    ,OnFileNewScript)
			COMMAND_ID_HANDLER(ID_FILE_OPEN1                        ,OnFileOpen)
			COMMAND_ID_HANDLER(ID_FILE_ADDITIVEOPEN                 ,OnFileOpenAdditive)
			COMMAND_ID_HANDLER(ID_FILE_OPTIONS                      ,OnFileShowOptions)
			COMMAND_ID_HANDLER(ID_FILE_PLUGINMGR                    ,OnFilePluginMgr)
			COMMAND_ID_HANDLER(ID_FILE_EXIT                         ,OnAppClose)
			COMMAND_ID_HANDLER(IDCLOSE                              ,OnAppClose)
			COMMAND_ID_HANDLER(ID_NAV_RESETVIEW_ALL                 ,OnResetView)
			COMMAND_ID_HANDLER(ID_NAV_RESETVIEW_SELECTED            ,OnResetView)
			COMMAND_ID_HANDLER(ID_NAV_RESETVIEW_VISIBLE             ,OnResetView)
			COMMAND_ID_HANDLER(ID_NAV_ALIGN_X                       ,OnNavAlign)
			COMMAND_ID_HANDLER(ID_NAV_ALIGN_Y                       ,OnNavAlign)
			COMMAND_ID_HANDLER(ID_NAV_ALIGN_Z                       ,OnNavAlign)
			COMMAND_ID_HANDLER(ID_NAV_ALIGN_CURRENT                 ,OnNavAlign)
			COMMAND_ID_HANDLER(ID_NAV_ALIGN_NONE                    ,OnNavAlign)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_POSX                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_NEGX                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_POSY                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_NEGY                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_POSZ                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_NEGZ                    ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_VIEW_AXIS_POSXYZ                  ,OnViewAxis)
			COMMAND_ID_HANDLER(ID_NAVIGATION_CLEARSAVEDVIEWS        ,OnSaveView)
			COMMAND_ID_HANDLER(ID_NAVIGATION_SAVEVIEW               ,OnSaveView)
			COMMAND_ID_HANDLER(ID_NAVIGATION_SETFOCUSPOSITION       ,OnSetFocusPosition)
			COMMAND_ID_HANDLER(ID_NAVIGATION_ORBIT                  ,OnOrbit)
			COMMAND_ID_HANDLER(ID_DATA_OBJECTMANAGER                ,OnShowObjectManagerUI)
			COMMAND_ID_HANDLER(ID_DATA_EDITSOURCEFILES              ,OnEditSourceFiles)
			COMMAND_ID_HANDLER(ID_DATA_CLEARSCENE                   ,OnDataClearScene)
			COMMAND_ID_HANDLER(ID_DATA_AUTOREFRESH                  ,OnDataAutoRefresh)
			COMMAND_ID_HANDLER(ID_DATA_CREATE_DEMO_SCENE            ,OnCreateDemoScene)
			COMMAND_ID_HANDLER(ID_RENDERING_SHOWFOCUS               ,OnShowFocus)
			COMMAND_ID_HANDLER(ID_RENDERING_SHOWORIGIN              ,OnShowOrigin)
			COMMAND_ID_HANDLER(ID_RENDERING_SHOWSELECTION           ,OnShowSelection)
			COMMAND_ID_HANDLER(ID_RENDERING_SHOWOBJECTBBOXES        ,OnShowObjBBoxes)
			COMMAND_ID_HANDLER(ID_RENDERING_WIREFRAME               ,OnToggleFillMode)
			COMMAND_ID_HANDLER(ID_RENDERING_RENDER2D                ,OnRender2D)
			COMMAND_ID_HANDLER(ID_RENDERING_LIGHTING                ,OnShowLightingDlg)
			COMMAND_ID_HANDLER(ID_TOOLS_MEASURE                     ,OnShowToolDlg)
			COMMAND_ID_HANDLER(ID_TOOLS_ANGLE                       ,OnShowToolDlg)
			COMMAND_ID_HANDLER(ID_WINDOW_ALWAYSONTOP                ,OnWindowAlwaysOnTop)
			COMMAND_ID_HANDLER(ID_WINDOW_BACKGROUNDCOLOUR           ,OnWindowBackgroundColour)
			COMMAND_ID_HANDLER(ID_WINDOW_EXAMPLESCRIPT              ,OnWindowExampleScript)
			COMMAND_ID_HANDLER(ID_WINDOW_CHECKFORUPDATES            ,OnCheckForUpdates)
			COMMAND_ID_HANDLER(ID_WINDOW_ABOUTLINEDRAWER            ,OnWindowShowAboutBox)

			CHAIN_MSG_MAP_MEMBER(m_recent_files)
			CHAIN_MSG_MAP_MEMBER(m_saved_views)
			CHAIN_MSG_MAP(base)
		END_MSG_MAP()

	private:
		typedef std::list<std::string> StrList;

		LRESULT OnCreate(LPCREATESTRUCT create);
		void OnTimer(UINT_PTR nIDEvent);
		void OnPaint(HDC hDC);
		void OnGetMinMaxInfo(LPMINMAXINFO lpMMI);
		void OnDropFiles(HDROP hDropInfo);

		void OnSysKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
		void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);

		void OnMouseDown(UINT flags, CPoint point);
		void OnMouseUp(UINT, CPoint point);
		void OnMouseMove(UINT flags, CPoint point);
		BOOL OnMouseWheel(UINT, short delta, CPoint);
		void OnMouseDblClk(UINT nFlags, CPoint point);

		LRESULT OnFileNew               (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnFileNewScript         (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnFileOpen              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnFileOpenAdditive      (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnFileShowOptions       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnFilePluginMgr         (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnAppClose              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnResetView             (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnViewAxis              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnSetFocusPosition      (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnNavAlign              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnSaveView              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnOrbit                 (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowObjectManagerUI   (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnEditSourceFiles       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnDataClearScene        (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnDataAutoRefresh       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnCreateDemoScene       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowFocus             (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowOrigin            (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowSelection         (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowObjBBoxes         (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnToggleFillMode        (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnRender2D              (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowLightingDlg       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnShowToolDlg           (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnWindowAlwaysOnTop     (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnWindowBackgroundColour(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnWindowExampleScript   (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnCheckForUpdates       (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
		LRESULT OnWindowShowAboutBox    (WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);

		void CloseApp(int exit_code);
		void FileNew(char const* filepath);
		void FileOpen(char const* filepath, bool additive);
		void OpenTextEditor(StrList const& files);
		void UpdateUI();
		void MouseStatusUpdate(pr::v2 const& mouse_location);
		void ShowAbout() const;

		// Recent files callbacks
		void MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item);
		void MenuList_ListChanged(pr::gui::MenuList* sender);

		// Ldr event handlers
		void OnEvent(ldr::Event_Info const& e);
		void OnEvent(ldr::Event_Warn const& e);
		void OnEvent(ldr::Event_Error const& e);
		void OnEvent(ldr::Event_Status const& e);
		void OnEvent(ldr::Event_Refresh const& e);
		void OnEvent(pr::rdr::Evt_SceneRender const&);
		void OnEvent(pr::ldr::Evt_Refresh const& e);
		void OnEvent(pr::ldr::Evt_LdrMeasureCloseWindow const&);
		void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&);
		void OnEvent(pr::ldr::Evt_LdrAngleDlgCloseWindow const&);
		void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&);
		void OnEvent(pr::ldr::Evt_AddBegin const&);
		void OnEvent(pr::ldr::Evt_AddEnd const&);
		void OnEvent(pr::settings::Evt<UserSettings> const&);

		// Command line
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end);
	};
}

/*

//#include "pr/gui/messagemap_dbg.h"

class LineDrawerGUI
	:public CDialogImpl<LineDrawerGUI>
	,public CUpdateUI<LineDrawerGUI>
	,public CWinDataExchange<LineDrawerGUI>
	,public CMessageLoop
	,public CMessageFilter
	,public CDialogResize<LineDrawerGUI>
{
public:
	BOOL PreTranslateMessage(MSG* pMsg);
	BOOL OnIdle(int);

	BEGIN_UPDATE_UI_MAP(LineDrawerGUI)
	END_UPDATE_UI_MAP()
	BEGIN_DLGRESIZE_MAP(LineDrawerGUI)
		DLGRESIZE_CONTROL(IDC_STATUSBAR_MAIN, DLSZ_MOVE_Y|DLSZ_SIZE_X|DLSZ_REPAINT)
	END_DLGRESIZE_MAP()
	BEGIN_MSG_MAP(LineDrawerGUI)
		MSG_WM_INITDIALOG(       OnInitDialog      )
		MSG_WM_TIMER(            OnTimerTick       )
		MSG_WM_SYSCOMMAND(       OnSysCommand      )
		MSG_WM_ENTERSIZEMOVE(    OnEnterSizeMove   )
		MSG_WM_SIZE(             OnSize            )
		MSG_WM_EXITSIZEMOVE(     OnExitSizeMove    )
		MSG_WM_ERASEBKGND(       OnEraseBkgnd      )
		MSG_WM_PAINT(            OnPaint           )
		MESSAGE_HANDLER(WM_KEYDOWN               ,OnKeyDown)
		MESSAGE_HANDLER(WM_SYSCHAR               ,OnKeyDown)
		MESSAGE_HANDLER(WM_MOUSEMOVE             ,OnMouseMove)
		MESSAGE_HANDLER(WM_MOUSEWHEEL            ,OnMouseWheel)
		MESSAGE_HANDLER(WM_LBUTTONDOWN           ,OnMouseButton)
		MESSAGE_HANDLER(WM_RBUTTONDOWN           ,OnMouseButton)
		MESSAGE_HANDLER(WM_MBUTTONDOWN           ,OnMouseButton)
		MESSAGE_HANDLER(WM_LBUTTONUP             ,OnMouseButton)
		MESSAGE_HANDLER(WM_RBUTTONUP             ,OnMouseButton)
		MESSAGE_HANDLER(WM_MBUTTONUP             ,OnMouseButton)
		MESSAGE_HANDLER(WM_LBUTTONDBLCLK         ,OnMouseDblClick)
		MESSAGE_HANDLER(WM_MBUTTONDBLCLK         ,OnMouseDblClick)
		MESSAGE_HANDLER(WM_RBUTTONDBLCLK         ,OnMouseDblClick)
		CHAIN_MSG_MAP(CDialogResize<LineDrawerGUI>) // note this sets bHandled to true... might be better to use OnSize and DlgResize_UpdateLayout

	END_MSG_MAP()
	BEGIN_DDX_MAP(LineDrawerGUI)
		//DDX_TEXT_LEN(IDC_EDIT_WINDOW_TITLE	,m_user_data.m_window_title, UserData::WindowTitleLen);
		//DDX_TEXT_LEN(IDC_EDIT_CONTROL_TYPE	,m_user_data.m_control_type, UserData::ControlTypeLen);
		//DDX_TEXT_LEN(IDC_EDIT_BUTTON_TEXT	,m_user_data.m_button_text, UserData::ButtonTextLen);
		//DDX_UINT_RANGE(IDC_EDIT_POL_FREQ	,m_user_data.m_pol_freq, DWORD(1), DWORD(100000));
		//DDX_CHECK(IDC_CHECK_ACTIVATE		,m_active);
		//if( bSaveAndValidate )
		//{
		//	m_user_data.m_pol_freq_unit	= m_ctrl_pol_freq_unit.GetCurSel();
		//}
		//else
		//{
		//	m_ctrl_pol_freq_unit.SetCurSel(m_user_data.m_pol_freq_unit);
		//}
	END_DDX_MAP()

	BOOL    OnInitDialog            (CWindow wndFocus, LPARAM lInitParam);
	void    OnEnterSizeMove         ();
	void    OnSize                  (UINT nType, CSize size);
	void    OnExitSizeMove          ();
	BOOL    OnEraseBkgnd            (CDCHandle dc);
	void    OnPaint                 (CDCHandle dc);
};
*/
#endif
