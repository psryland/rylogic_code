#pragma once

#include "clicket/forward.h"
#include "clicket/user_data.h"

class CMainDlg
	:public CDialogImpl<CMainDlg>
	,public CUpdateUI<CMainDlg>
	,public CWinDataExchange<CMainDlg>
	,public CMessageFilter
	,public CIdleHandler
	,public pr::cmdline::IOptionReceiver
{
	CEdit               m_ctrl_window_title;
	CEdit               m_ctrl_control_type;
	CEdit               m_ctrl_button_text;
	CComboBox           m_ctrl_pol_freq_unit;
	BOOL                m_active;
	pr::PollingToEvent  m_poller;
	CMenu               m_context_menu;
	UserData            m_user_data;
	HCURSOR             m_find_window_cursor;
	bool                m_finding_a_window;
	CInfoDlg            m_info_dlg;
	int                 m_next_level;
	HWND                m_parent_hwnd;

public:
	enum { IDD = IDD_MAINDLG , WM_SYS_TRAY_EVENT = WM_USER + 1 };
	
	CMainDlg();
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	virtual BOOL OnIdle();

	// Handler prototypes (uncomment arguments if needed):
	//LRESULT MessageHandler(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	//LRESULT CommandHandler(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
	//LRESULT NotifyHandler(int idCtrl, LPNMHDR pnmh, BOOL& bHandled)
	LRESULT OnInitDialog(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnContextMenu(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnSysCommand(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnLButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnFileOpen(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnFileActivate(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnAppAbout(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnAppClose(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnBnClickedFindWindow(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnBnClickedCheckActivate(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);

	BEGIN_UPDATE_UI_MAP(CMainDlg)
	END_UPDATE_UI_MAP()
	BEGIN_MSG_MAP(CMainDlg)
		MESSAGE_HANDLER(WM_INITDIALOG           ,OnInitDialog)
		MESSAGE_HANDLER(WM_DESTROY              ,OnDestroy)
		MESSAGE_HANDLER(WM_SYS_TRAY_EVENT       ,OnContextMenu)
		MESSAGE_HANDLER(WM_SYSCOMMAND           ,OnSysCommand)
		MESSAGE_HANDLER(WM_LBUTTONDOWN          ,OnLButtonDown)
		COMMAND_ID_HANDLER(ID_FILE_OPEN         ,OnFileOpen)
		COMMAND_ID_HANDLER(ID_FILE_ACTIVATE     ,OnFileActivate)
		COMMAND_ID_HANDLER(ID_FILE_ABOUT        ,OnAppAbout)
		COMMAND_ID_HANDLER(ID_FILE_EXIT         ,OnAppClose)
		COMMAND_ID_HANDLER(ID_APP_ABOUT         ,OnAppAbout)
		COMMAND_ID_HANDLER(IDCLOSE              ,OnAppClose)
		COMMAND_ID_HANDLER(IDCANCEL             ,OnAppClose)
		COMMAND_HANDLER(IDC_CHECK_FIND_WINDOW   ,BN_CLICKED ,OnBnClickedFindWindow)
		COMMAND_HANDLER(IDC_CHECK_ACTIVATE      ,BN_CLICKED ,OnBnClickedCheckActivate)
	END_MSG_MAP()
	BEGIN_DDX_MAP(CMainDlg)
		DDX_TEXT_LEN(IDC_EDIT_WINDOW_TITLE  ,m_user_data.m_window_title, UserData::WindowTitleLen);
		DDX_TEXT_LEN(IDC_EDIT_CONTROL_TYPE  ,m_user_data.m_control_type, UserData::ControlTypeLen);
		DDX_TEXT_LEN(IDC_EDIT_BUTTON_TEXT   ,m_user_data.m_button_text, UserData::ButtonTextLen);
		DDX_UINT_RANGE(IDC_EDIT_POL_FREQ    ,m_user_data.m_pol_freq, DWORD(1), DWORD(100000));
		DDX_CHECK(IDC_CHECK_ACTIVATE        ,m_active);
		if (bSaveAndValidate)
		{
			m_user_data.m_pol_freq_unit = m_ctrl_pol_freq_unit.GetCurSel();
		}
		else
		{
			m_ctrl_pol_freq_unit.SetCurSel(m_user_data.m_pol_freq_unit);
		}
	END_DDX_MAP()

private:
	bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end);
	void CloseApp(int exit_code);
	void Activate(bool on);
	void UpdateUI();
	void CollectWindowData(HWND hwnd);
	static BOOL CALLBACK EnumWindowItems(HWND hwnd, LPARAM user_data);
	static BOOL CALLBACK EnumChildWindowProc(HWND hwnd, LPARAM user_data);
	static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM user_data);
	static bool LookForButtonsToPress(void* user_data);
};
