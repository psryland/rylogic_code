// MainDlg.cpp : implementation of the CMainDlg class
//
/////////////////////////////////////////////////////////////////////////////

#include "clicket/stdafx.h"
#include "clicket/resource.h"
#include "clicket/aboutdlg.h"
#include "clicket/maindlg.h"

namespace EButton
{
	enum Id
	{
		Ok,
		Cancel,
		Abort,
		Retry,
		Ignore,
		Yes,
		No,
		#if(WINVER >= 0x0400)
		Close,
		Help,
		#endif
		#if(WINVER >= 0x0500)
		TryAgain,
		Continue,
		#endif
		#if(WINVER >= 0x0501)
		Timeout,
		#endif
		NumberOf
	};
	TCHAR const* Str(int id)
	{
		switch (id)
		{
		case Ok:        return TEXT("Ok");
		case Cancel:    return TEXT("Cancel");
		case Abort:     return TEXT("Abort");
		case Retry:     return TEXT("Retry");
		case Ignore:    return TEXT("Ignore");
		case Yes:       return TEXT("Yes");
		case No:        return TEXT("No");
		#if(WINVER >= 0x0400)
		case Close:     return TEXT("Close");
		case Help:      return TEXT("Help");
		#endif
		#if(WINVER >= 0x0500)
		case TryAgain:  return TEXT("TryAgain");
		case Continue:  return TEXT("Continue");
		#endif
		#if(WINVER >= 0x0501)
		case Timeout:   return TEXT("Timeout");
		#endif
		default: PR_ASSERT(PR_DBG, false, ""); return TEXT("");
		}
	}
	DWORD WinId(int id)
	{
		switch (id)
		{
		case Ok:        return IDOK;
		case Cancel:    return IDCANCEL;
		case Abort:     return IDABORT;
		case Retry:     return IDRETRY;
		case Ignore:    return IDIGNORE;
		case Yes:       return IDYES;
		case No:        return IDNO;
		#if(WINVER >= 0x0400)
		case Close:     return IDCLOSE;
		case Help:      return IDHELP;
		#endif
		#if(WINVER >= 0x0500)
		case TryAgain:  return IDTRYAGAIN;
		case Continue:  return IDCONTINUE;
		#endif
		#if(WINVER >= 0x0501)
		case Timeout:   return IDTIMEOUT;
		#endif
		default: PR_ASSERT(PR_DBG, false, ""); return 0;
		}
	}
}
namespace EFreq
{
	enum
	{
		MSec,
		Sec,
		Min,
		Hr,
		NumberOf
	};
	wchar_t const* Str(int id)
	{
		switch (id)
		{
		default: PR_ASSERT(PR_DBG, false, ""); return L"";
		case MSec: return L"msec";
		case Sec:  return L"sec";
		case Min:  return L"min";
		case Hr:   return L"hr";
		}
	}
}
UserData::UserData()
{
	_snwprintf_s(m_window_title, WindowTitleLen, L"<title of window to look for>");
	_snwprintf_s(m_control_type, ControlTypeLen, L"<type of control to look for>");
	_snwprintf_s(m_button_text,  ButtonTextLen,  L"<control text>");
	m_pol_freq = 1;
	m_pol_freq_unit = 1;
}
void UserData::Load()
{
	std::wstring path(MAX_PATH, 0);
	GetModuleFileNameW(0, &path[0], static_cast<DWORD>(path.size()));
	path = path.c_str();
	path += L".user_data";
	
	UserData copy;
	pr::Handle file = pr::FileOpen(path.c_str(), pr::EFileOpen::Reading);
	if (!pr::FileRead(file, &copy, sizeof(copy))) return;
	
	copy.Validate();
	*this = copy;
}
void UserData::Save()
{
	std::wstring path(MAX_PATH, 0);
	GetModuleFileNameW(0, &path[0], static_cast<DWORD>(path.size()));
	path = path.c_str();
	path += L".user_data";
	
	pr::Handle file = pr::FileOpen(path.c_str(), pr::EFileOpen::Writing);
	pr::FileWrite(file, this, sizeof(*this));
}
void UserData::Validate()
{
	m_window_title[WindowTitleLen - 1] = 0;
	m_control_type[ControlTypeLen - 1] = 0;
	m_button_text[ButtonTextLen - 1] = 0;
	m_pol_freq      = (m_pol_freq       >= 1 && m_pol_freq      <= 100000)              ? m_pol_freq : 1;
	m_pol_freq_unit = (m_pol_freq_unit  >= 0 && m_pol_freq_unit < EFreq::NumberOf)      ? m_pol_freq_unit : 1;
}

CMainDlg::CMainDlg()
:m_poller(pr::PollingToEventSettings(CMainDlg::LookForButtonsToPress, 0, this))
,m_finding_a_window(false)
{}

BOOL CMainDlg::PreTranslateMessage(MSG* pMsg)
{
	return CWindow::IsDialogMessage(pMsg);
}

BOOL CMainDlg::OnIdle()
{
	return FALSE;
}

LRESULT CMainDlg::OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
{
	// center the dialog on the screen
	CenterWindow();
	
	// set icons
	HICON hIcon      = (HICON)::LoadImage(_Module.GetResourceInstance(), MAKEINTRESOURCE(IDR_MAINFRAME), IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR);
	HICON hIconSmall = (HICON)::LoadImage(_Module.GetResourceInstance(), MAKEINTRESOURCE(IDR_MAINFRAME), IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR);
	SetIcon(hIcon, TRUE);
	SetIcon(hIconSmall, FALSE);
	
	// register object for message filtering and idle updates
	CMessageLoop* pLoop = _Module.GetMessageLoop(); ATLASSERT(pLoop != NULL);
	pLoop->AddMessageFilter(this);
	pLoop->AddIdleHandler(this);
	
	m_find_window_cursor = LoadCursor(NULL, IDC_CROSS);
	
	UIAddChildWindowContainer(m_hWnd);
	
	// Add system tray icon
	NOTIFYICONDATAW nidata;
	nidata.cbSize = sizeof(nidata);
	nidata.hWnd = m_hWnd;
	nidata.uID = 0;
	nidata.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;// | NIF_INFO;
	nidata.uCallbackMessage = WM_SYS_TRAY_EVENT;
	nidata.hIcon = hIconSmall;
	wchar_t const tip_text[] = L"Clicket";
	StringCbCopyNW(nidata.szTip, sizeof(nidata.szTip), tip_text, sizeof(tip_text));
	#if(_WIN32_IE >= 0x0500)
	nidata.dwState = 0;
	nidata.dwStateMask = 0;
	wchar_t const info_text[] = L"Automatic dialog box button clicker";
	StringCbCopyNW(nidata.szInfo, sizeof(nidata.szInfo), info_text, sizeof(info_text));
	nidata.uTimeout = 20000;
	wchar_t const info_title[] = L"Clicket";
	StringCbCopyNW(nidata.szInfoTitle, sizeof(nidata.szInfoTitle), info_title, sizeof(info_title));
	nidata.dwInfoFlags = NIIF_NONE;
	#endif
	#if(_WIN32_IE >= 0x0600)
	nidata.guidItem = 0;
	#endif
	Shell_NotifyIconW(NIM_ADD, &nidata);
	
	// Read saved data
	m_user_data.Load();
	
	// Attach and initialise controls
	m_ctrl_window_title.Attach(GetDlgItem(IDC_EDIT_WINDOW_TITLE));
	m_ctrl_window_title.SetWindowText(m_user_data.m_window_title);
	
	m_ctrl_control_type.Attach(GetDlgItem(IDC_EDIT_CONTROL_TYPE));
	m_ctrl_control_type.SetWindowTextW(m_user_data.m_control_type);
	
	m_ctrl_button_text.Attach(GetDlgItem(IDC_EDIT_BUTTON_TEXT));
	m_ctrl_button_text.SetWindowText(m_user_data.m_button_text);
	
	m_ctrl_pol_freq_unit.Attach(GetDlgItem(IDC_COMBO_TIME));
	for (int i = 0; i != EFreq::NumberOf; ++i)
		m_ctrl_pol_freq_unit.AddString(EFreq::Str(i));
	m_ctrl_pol_freq_unit.SetCurSel(m_user_data.m_pol_freq_unit);
	
	m_active = FALSE;
	
	m_context_menu.LoadMenu(IDR_MENU1);
	m_context_menu.GetSubMenu(0).SetMenuDefaultItem(0, TRUE);
	DoDataExchange(FALSE);
	
	m_info_dlg.Create(m_hWnd);
	
	UpdateUI();
	
	std::string cmdline = GetCommandLineA();
	pr::EnumCommandLine(cmdline.c_str(), *this);
	return TRUE;
}

LRESULT CMainDlg::OnDestroy(UINT, WPARAM, LPARAM, BOOL&)
{
	DoDataExchange(TRUE);
	m_user_data.Save();
	
	// Delete the system tray icon
	NOTIFYICONDATA nidata;
	nidata.cbSize = sizeof(NOTIFYICONDATA);
	nidata.hWnd = m_hWnd;
	nidata.uID = 0;
	Shell_NotifyIcon(NIM_DELETE, &nidata);
	
	// unregister message filtering and idle updates
	CMessageLoop* pLoop = _Module.GetMessageLoop(); ATLASSERT(pLoop != NULL);
	pLoop->RemoveMessageFilter(this);
	pLoop->RemoveIdleHandler(this);
	
	return 0;
}

// Catch minimise events, and minimise to the tray
LRESULT CMainDlg::OnSysCommand(UINT uMsg, WPARAM wparam, LPARAM, BOOL& handled)
{
	if (uMsg == WM_SYSCOMMAND)
	{
		handled = true;
		switch (wparam)
		{
		case SC_MINIMIZE:
			ShowWindow(SW_HIDE);
			m_info_dlg.ShowWindow(SW_HIDE);
			return 0;
		case SC_CLOSE:      CloseApp(0); return 0;
		}
	}
	handled = false;
	return 0;
}

// Handle left clicks after the find window button has been pressed
LRESULT CMainDlg::OnLButtonDown(UINT, WPARAM wParam, LPARAM lParam, BOOL& handled)
{
	if (wParam == MK_LBUTTON)
	{
		if (m_finding_a_window)
		{
			//handled = true;
			m_finding_a_window = false;
			//m_find_window_cursor = SetCursor(m_find_window_cursor);
			CheckDlgButton(IDC_CHECK_FIND_WINDOW, FALSE);
			ReleaseCapture();
			
			POINT point = {GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)};
			ClientToScreen(&point);
			HWND target = WindowFromPoint(point);
			if (target)
			{
				m_info_dlg.m_info.Empty();
				CollectWindowData(target);
				CRect rect; GetWindowRect(&rect);
				m_info_dlg.Show(true, rect.right, rect.top);
			}
		}
	}
	handled = false;
	return 0;
}

// Display the system tray context menu
LRESULT CMainDlg::OnContextMenu(UINT uMsg, WPARAM, LPARAM lparam, BOOL&)
{
	if (uMsg != WM_SYS_TRAY_EVENT)
		return 0;
		
	CPoint pt;
	switch (lparam)
	{
	case WM_LBUTTONDOWN:    break;
	case WM_RBUTTONDOWN:
		GetCursorPos(&pt);
		m_context_menu.GetSubMenu(0).TrackPopupMenu(TPM_BOTTOMALIGN|TPM_LEFTBUTTON|TPM_RIGHTBUTTON, pt.x, pt.y, m_hWnd);
		break;
	case WM_LBUTTONDBLCLK:
		SendMessage(WM_SYSCOMMAND, SC_RESTORE);
		break;
	case WM_MOUSEMOVE:      break;
	case WM_RBUTTONDBLCLK:  break;
	case WM_CONTEXTMENU:    break;
	}
	return 0;
}

// Display the window
LRESULT CMainDlg::OnFileOpen(WORD, WORD, HWND, BOOL&)
{
	SendMessage(WM_SYSCOMMAND, SC_RESTORE);
	return 0;
}

// Activate the window checking
LRESULT CMainDlg::OnFileActivate(WORD, WORD, HWND, BOOL&)
{
	Activate(!m_active);
	return 0;
}

// Show the about dialog box
LRESULT CMainDlg::OnAppAbout(WORD, WORD, HWND, BOOL&)
{
	CAboutDlg dlg;
	dlg.DoModal();
	return 0;
}

// Close the dialog event
LRESULT CMainDlg::OnAppClose(WORD, WORD wID, HWND, BOOL&)
{
	CloseApp(wID);
	return 0;
}

// The find a window button has been clicked
LRESULT CMainDlg::OnBnClickedFindWindow(WORD, WORD, HWND, BOOL&)
{
	if (!m_finding_a_window)
	{
		SetCapture();
		//m_find_window_cursor = SetCursor(m_find_window_cursor);
		m_finding_a_window = true;
	}
	return 0;
}

// Activate button pressed
LRESULT CMainDlg::OnBnClickedCheckActivate(WORD, WORD, HWND, BOOL&)
{
	Activate(!m_active);
	return 0;
}

// Handle command line arguments
bool CMainDlg::CmdLineOption(std::string const& option, pr::cmdline::TArgIter&, pr::cmdline::TArgIter)
{
	if (pr::str::EqualI(option, "-activate"))
	{
		Activate(true);
		PostMessage(WM_SYSCOMMAND, SC_MINIMIZE, 0);
	}
	return true;
}

// Shut the app down
void CMainDlg::CloseApp(int)
{
	m_poller.Stop();
	m_poller.BlockTillDead();
	::PostMessage(m_hWnd, WM_CLOSE, 0, 0);
}

// Activate/Deactivate the poller
void CMainDlg::Activate(bool on)
{
	if (on == m_poller.Running())
		return;
		
	if (on)
	{
		DoDataExchange(TRUE);
		m_user_data.Save();
		
		float period_s = float(m_user_data.m_pol_freq);
		switch (m_user_data.m_pol_freq_unit)
		{
		case 3: period_s *= 60.0f * 60.0f;  break; // hours
		case 2: period_s *= 60.0f;          break; // minutes
		case 1: period_s *= 1.0f;           break; // seconds
		case 0: period_s *= 0.001f;         break; // milli_seconds
			break;
		default: PR_ASSERT(PR_DBG, false, "Unknown time unit");
		}
		m_poller.SetFrequency(1.0f / period_s);
		m_poller.Start();
	}
	else
	{
		m_poller.Stop();
	}
	
	m_active = on;
	DoDataExchange(FALSE);
	UpdateUI();
}

// Enable/disable controls, update texts, check marks etc
void CMainDlg::UpdateUI()
{
	if (m_active)
	{
		GetDlgItem(IDC_EDIT_WINDOW_TITLE).EnableWindow(FALSE);
		GetDlgItem(IDC_EDIT_CONTROL_TYPE).EnableWindow(FALSE);
		GetDlgItem(IDC_EDIT_BUTTON_TEXT).EnableWindow(FALSE);
		GetDlgItem(IDC_EDIT_POL_FREQ).EnableWindow(FALSE);
		GetDlgItem(IDC_COMBO_TIME).EnableWindow(FALSE);
		GetDlgItem(IDC_CHECK_FIND_WINDOW).EnableWindow(FALSE);
		GetDlgItem(IDC_CHECK_ACTIVATE).SetWindowText(TEXT("Deactivate"));
		m_context_menu.GetSubMenu(0).CheckMenuItem(1, MF_BYPOSITION|MF_CHECKED);
	}
	else
	{
		GetDlgItem(IDC_EDIT_WINDOW_TITLE).EnableWindow(TRUE);
		GetDlgItem(IDC_EDIT_CONTROL_TYPE).EnableWindow(TRUE);
		GetDlgItem(IDC_EDIT_BUTTON_TEXT).EnableWindow(TRUE);
		GetDlgItem(IDC_EDIT_POL_FREQ).EnableWindow(TRUE);
		GetDlgItem(IDC_COMBO_TIME).EnableWindow(TRUE);
		GetDlgItem(IDC_CHECK_FIND_WINDOW).EnableWindow(TRUE);
		GetDlgItem(IDC_CHECK_ACTIVATE).SetWindowText(TEXT("Activate"));
		m_context_menu.GetSubMenu(0).CheckMenuItem(1, MF_BYPOSITION|MF_UNCHECKED);
	}
}

// Remove unexpected stuff from a button string
void Standardise(TCHAR* str)
{
	TCHAR* in = str, *out = str;
	for (; *in != 0; ++in)
	{
		if (*in == TEXT('&')) continue;
		*out++ = *in;
	}
	*out = 0;
}

// Build a string of the ctrls on a window
BOOL CALLBACK CMainDlg::EnumWindowItems(HWND hwnd, LPARAM user_data)
{
	CMainDlg* This = reinterpret_cast<CMainDlg*>(user_data);
	++This->m_next_level;
	
	TCHAR type[MAX_PATH];
	::GetClassName(hwnd, type, MAX_PATH);
	
	TCHAR title[MAX_PATH];
	::GetWindowText(hwnd, title, MAX_PATH);
	Standardise(title);
	
	TCHAR text[MAX_PATH];
	::SendMessage(hwnd, WM_GETTEXT, (WPARAM)MAX_PATH, (LPARAM)&text[0]);
	
	CRect parent_rect;  ::GetWindowRect(::GetParent(hwnd), &parent_rect);
	CRect rect;         ::GetWindowRect(hwnd, &rect);
	rect.left   -= parent_rect.left;
	rect.top    -= parent_rect.top ;
	rect.right  -= parent_rect.left;
	rect.bottom -= parent_rect.top ;
	
	TCHAR tabs[20] = {'\r','\n','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t','\t'};
	tabs[This->m_next_level < 19 ? This->m_next_level+1 : 19] = 0;
	
	// All info:
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%sControl:"), tabs).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tType:\t%s"), tabs, type).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tTitle:\t%s"), tabs, title).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tText:\t%s"), tabs, text).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tCtrl ID:\t%d"), tabs, ::GetDlgCtrlID(hwnd)).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tPos:\tx=%d y=%d"), tabs, rect.left, rect.top).c_str();
	This->m_info_dlg.m_info += pr::Fmt(TEXT("%s\tDim:\tw=%d h=%d"), tabs, rect.Width(), rect.Height()).c_str();
	
	EnumChildWindows(hwnd, CMainDlg::EnumWindowItems, (LPARAM)This);
	--This->m_next_level;
	return TRUE;
}

// Build up a string containing formation about a window
void CMainDlg::CollectWindowData(HWND hwnd)
{
	TCHAR title[MAX_PATH];
	::GetWindowText(hwnd, title, MAX_PATH);
	_tcsncpy(m_user_data.m_window_title, title, UserData::WindowTitleLen);
	
	CRect rect; ::GetWindowRect(hwnd, &rect);
	m_info_dlg.m_info  = pr::Fmt(TEXT("Window Title:\t%s"), title).c_str();
	m_info_dlg.m_info += pr::Fmt(TEXT("\r\nScreen Pos:\tx=%d y=%d"), rect.left, rect.top).c_str();
	m_info_dlg.m_info += pr::Fmt(TEXT("\r\nScreen Dim:\tw=%d h=%d"), rect.Width(), rect.Height()).c_str();
	m_info_dlg.m_info += TEXT("\r\nContents:");
	m_next_level = 0;
	EnumChildWindows(hwnd, CMainDlg::EnumWindowItems, (LPARAM)this);
	DoDataExchange(FALSE);
}

// Call back for enumerating the child windows of a window
BOOL CALLBACK CMainDlg::EnumChildWindowProc(HWND hwnd, LPARAM user_data)
{
	CMainDlg* This = reinterpret_cast<CMainDlg*>(user_data);
	++This->m_next_level;
	EnumChildWindows(hwnd, CMainDlg::EnumChildWindowProc, user_data);
	--This->m_next_level;
	
	// Look for the wanted control type
	TCHAR type[MAX_PATH];
	::GetClassName(hwnd, type, MAX_PATH);
	if (_tcscmp(type, This->m_user_data.m_control_type) != 0)
		return TRUE;
		
	// Match the window text
	TCHAR text[MAX_PATH];
	::SendMessage(hwnd, WM_GETTEXT, (WPARAM)MAX_PATH, (LPARAM)&text[0]);
	if (_tcscmp(text, This->m_user_data.m_button_text) != 0)
		return TRUE;
		
	CRect rect; ::GetClientRect(hwnd, &rect);
	rect.left += rect.Width() / 2;
	rect.top  += rect.Height() / 2;
	::PostMessage(hwnd, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(rect.left, rect.top));
	::PostMessage(hwnd, WM_LBUTTONUP  , MK_LBUTTON, MAKELPARAM(rect.left, rect.top));
	
	//CWindow wnd(hwnd);
	//DWORD thread_id = wnd.GetWindowThreadID();
	//int id = wnd.GetDlgCtrlID();
	//int id = ::GetDlgCtrlID(hwnd);
	//if( ::PostMessage(This->m_parent_hwnd, WM_COMMAND, MAKEWPARAM(id, BN_CLICKED), (LPARAM)hwnd) == FALSE )
	//if( ::PostMessage(hwnd, WM_COMMAND, MAKEWPARAM(id, BN_CLICKED), (LPARAM)hwnd) == FALSE )
	//if( ::PostThreadMessage(thread_id, WM_COMMAND, MAKEWPARAM(id, BN_CLICKED), (LPARAM)hwnd) == FALSE )
	{}
	
	return FALSE;
}

// Windows call back function, called for every window
BOOL CALLBACK CMainDlg::EnumWindowsProc(HWND hwnd, LPARAM user_data)
{
	CMainDlg* This = reinterpret_cast<CMainDlg*>(user_data);
	CWindow wnd(hwnd);
	
	// Get the name of the window
	TCHAR window_name[MAX_PATH];
	wnd.GetWindowText(window_name, MAX_PATH);
	
	// Match the window title
	if (_tcscmp(window_name, This->m_user_data.m_window_title) == 0)
	{
		This->m_next_level = 0;
		This->m_parent_hwnd = hwnd;
		EnumChildWindows(hwnd, CMainDlg::EnumChildWindowProc, user_data);
		return FALSE;
	}
	return TRUE;
}

// Polling function
bool CMainDlg::LookForButtonsToPress(void* user_data)
{
	EnumWindows(CMainDlg::EnumWindowsProc, (LPARAM)user_data);
	return true;
}
