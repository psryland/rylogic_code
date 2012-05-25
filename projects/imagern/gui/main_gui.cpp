//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#include "imagern/main/stdafx.h"
#include "imagern/gui/main_gui.h"
#include "imagern/main/imager.h"
#include "imagern/media/media_file.h"
#include "imagern/main/events.h"

namespace StatusPane
{
	enum Type
	{
		Message,   // The general purpose status message
		ImageDim,  // The dimensions of the currently displayed media file
		ZoomFill,  // The state of "zoom in to fill the window"
		ZoomFit,   // The state of "zoom out to fit the window"
		NumberOf,
	};
	int Widths[] = {400, 100, 20, 20, -1};
	void SetWidths(int client_width)
	{
		int cx = client_width;
		for (int i = 1; i != PR_COUNTOF(Widths); ++i) cx -= Widths[i];
		Widths[0] = pr::Max(0, cx);
	}
}

MainGUI::MainGUI()
:m_recent()
,m_status()
,m_img(0)
,m_video_ctrl()
,m_font_norm()
,m_font_bold()
,m_my_thread_id(GetCurrentThreadId())
,m_nav_enabled(false)
,m_resizing(false)
{}

// Create the main window
LRESULT MainGUI::OnCreate(LPCREATESTRUCT create)
{
	SetWindowTextA("Imager");
	
	//// Set icons
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);
	m_video_ctrl.Create(m_hWnd);
	
	// Create and attached the status bar
	CreateSimpleStatusBar(IDC_STATUSBAR);
	m_status.Attach(m_hWndStatusBar);

	// Create the status pane parts
	StatusPane::SetWidths(create->cx - 2*::GetSystemMetrics(SM_CXBORDER));
	m_status.SetParts(PR_COUNTOF(StatusPane::Widths), StatusPane::Widths);
	
	// Attach/Create status bar fonts
	m_font_norm.CreatePointFont(100, _T("Segoe UI"));
	m_font_bold.CreatePointFont(100, _T("Segoe UI"), 0, true);
	
	// Recent files handler
	m_recent.Attach(pr::gui::GetMenuByName(GetMenu(), _T("&File,&Recent")) ,IDM_RECENT ,10 ,this);
	
	// Register this class for message filtering and idle updates
	CMessageLoop* loop = g_app_module.GetMessageLoop();
	loop->AddMessageFilter(this);
	loop->AddIdleHandler(this);
	
	// Create the main app logic
	m_img = new Imager(*this);
	
	m_recent.Import(m_img->Settings().m_recent_files);
	Status("Idle", false);
	return S_OK;
}

// Destroy the window
void MainGUI::OnDestroy()
{
	delete m_img; m_img = 0;
	
	m_status.Detach();
	CMessageLoop* loop = g_app_module.GetMessageLoop();
	loop->RemoveMessageFilter(this);
	loop->RemoveIdleHandler(this);
}

// Idle handler
BOOL MainGUI::OnIdle()
{
	//DWORD now = GetTickCount();
	
	// Fade the video control out if visible
	if (m_video_ctrl.IsWindowVisible())
	{
		
		//FadeVideoCtrl();

	}
	return FALSE;
}

// PreTranslate msg
BOOL MainGUI::PreTranslateMessage(MSG*)
{
	return FALSE;
}

// System commands
void MainGUI::OnSysCommand(UINT wparam, WTL::CPoint const&)
{
	switch (wparam)
	{
	default: SetMsgHandled(FALSE); break;
	case SC_CLOSE: CloseApp(0); break;
	}
}

// Populate 'filter' with the file type filters for image/video/audio files as given in 'settings'
struct MediaFileFilter
{
	typedef std::vector<COMDLG_FILTERSPEC> FilterCont;
	
	FilterCont m_filter;
	std::wstring m_image_extns;
	std::wstring m_video_extns;
	std::wstring m_audio_extns;
	std::wstring m_all_extns;
	
	MediaFileFilter(UserSettings const& settings)
	:m_filter(4)
	,m_image_extns(pr::str::ToWString<std::wstring>(settings.m_image_extns))
	,m_video_extns(pr::str::ToWString<std::wstring>(settings.m_video_extns))
	,m_audio_extns(pr::str::ToWString<std::wstring>(settings.m_audio_extns))
	,m_all_extns()
	{
		pr::str::Replace(m_image_extns, L"+", L"*."); pr::str::Replace(m_image_extns, L"-", L"*.");
		pr::str::Replace(m_video_extns, L"+", L"*."); pr::str::Replace(m_video_extns, L"-", L"*.");
		pr::str::Replace(m_audio_extns, L"+", L"*."); pr::str::Replace(m_audio_extns, L"-", L"*.");
		m_all_extns.append(m_image_extns).append(L";").append(m_video_extns).append(L";").append(m_audio_extns).append(L";");
		m_filter[0].pszName = L"All Media Files";
		m_filter[0].pszSpec = m_all_extns.c_str();
		m_filter[1].pszName = L"Image Files";
		m_filter[1].pszSpec = m_image_extns.c_str();
		m_filter[2].pszName = L"Video Files";
		m_filter[2].pszSpec = m_video_extns.c_str();
		m_filter[3].pszName = L"Audio Files";
		m_filter[3].pszSpec = m_audio_extns.c_str();
	}
};

// Handle menu commands
void MainGUI::OnCommand(UINT, INT wID, HWND)
{
	switch (wID)
	{
	default: SetMsgHandled(FALSE); break;
	case IDCLOSE:
	case IDM_EXIT:
		CloseApp(0);
		break;
	case IDM_OPEN_FILE:
		{
			MediaFileFilter mff(m_img->Settings());
			
			WTL::CShellFileOpenDialog dlg;
			dlg.GetPtr()->SetTitle(L"Open a Media File");
			dlg.GetPtr()->SetFileTypes((UINT)mff.m_filter.size(), &mff.m_filter[0]);
			if (dlg.DoModal() != IDOK) break;
			
			WCHAR path[1024];
			if (pr::Failed(dlg.GetFilePath(path, PR_COUNTOF(path))))
			{
				::MessageBox(m_hWnd, pr::FmtS("Failed to open file\nReason: %s", pr::Reason().c_str()), "File Open Failed", MB_OK);
				break;
			}
			
			// Open the file
			string filepath = pr::str::ToAString<string>(path);
			m_recent.Add(filepath.c_str(), true);
			m_img->SetMedia(MediaFile(filepath));
		}break;
	case IDM_FILE_DIRECTORIES:
		{
			// Query the database for the list of search directories

			// Display UI for modifying this list

			// Update the database with the new list of directories

			// Signal the crawler thread

		}break;
	case IDM__OPTIONS___1:
		{
		}break;
	}
}

// Clear the background during resize
LRESULT MainGUI::OnEraseBkGnd(HDC hdc)
{
	(void)hdc;
	//if (m_sizing)
	//{
	//	HBRUSH hbrush = CreateSolidBrush(m_ldr->Settings().m_background_colour.GetColorRef());
	//	CRect r; GetClientRect(&r);
	//	CPoint ctr = r.CenterPoint();
	//	CDCHandle dc(GetDC());
	//	dc.FillRect(&r, hbrush);
	//	dc.SetTextAlign(TA_CENTER|TA_BASELINE);
	//	dc.SetBkMode(TRANSPARENT);
	//	dc.TextOutA(ctr.x, ctr.y, "...resizing...");
	//	DeleteObject(hbrush);
	//}
	return S_OK;
}
	
// Paint the window
void MainGUI::OnPaint(HDC hdc)
{
//	SetMsgHandled(FALSE);
	(void)hdc;
	//{// Test the video stuff
	//	pr::rdr::Video vid;
	//	vid.LoadFile("C:/deleteme/vid.avi");
	//	vid.AttachToWindow(m_hWnd);
	//	vid.Play();
	//	Sleep(10000);
	//	vid.Stop();
	//}
	CPaintDC dc(m_hWnd);
	if (m_img) m_img->Render();
	//SetMsgHandled(FALSE);
}

// Resizing handlers
void MainGUI::OnSizing(UINT, LPRECT)
{
	// If the video controls are visible, reposition them
	if (m_video_ctrl.IsWindowVisible())
		m_video_ctrl.ResizeToParent();
	
	SetMsgHandled(FALSE);
	m_resizing = true;
}
void MainGUI::OnExitSizeMove()
{
	SetMsgHandled(FALSE);
	m_resizing = false;
	OnSize(0, CSize());
}
void MainGUI::OnSize(UINT type, CSize)
{
	SetMsgHandled(FALSE);
	if (m_resizing) return;
	if (type != SIZE_MINIMIZED)
	{
		// If the video controls are visible, reposition them
		if (m_video_ctrl.IsWindowVisible())
			m_video_ctrl.ResizeToParent();
		
		// Find the new client area
		pr::IRect area = pr::ClientArea(m_hWnd);
		area.m_max.y -= pr::WindowBounds(m_hWndStatusBar).SizeY();
		
		// Update the status bar
		StatusPane::SetWidths(area.SizeX());
		m_status.SetParts(PR_COUNTOF(StatusPane::Widths), StatusPane::Widths);

		//SaveWindowBounds();
		//UpdateUI();
		UpdateLayout(true);
		m_img->Resize(area);
		m_img->Render();
	}
}

// Navigation handlers
void MainGUI::OnMouseDown(UINT flags, CPoint point)
{
	if (pr::AllSet(flags, MK_LBUTTON))
	{
		m_nav_enabled = true;
		m_img->Nav(pr::NormalisePoint(m_hWnd, point), pr::camera::ENavBtn::Left, true);
		//Cursor = Cursors.SizeAll;
	}
}
void MainGUI::OnMouseUp(UINT, CPoint point)
{
	m_nav_enabled = false;
	m_img->Nav(pr::NormalisePoint(m_hWnd, point), 0, true);
	//Cursor = Cursors.Default;
}
void MainGUI::OnMouseMove(UINT, CPoint point)
{
	// If navigation is enabled, forward mouse movements to the main app
	if (m_nav_enabled)
		m_img->Nav(pr::NormalisePoint(m_hWnd, point), pr::camera::ENavBtn::Left, false);
	
	// If a video is currently displayed, show the video control panel
	if (m_img->CurrentPhoto().MediaType() == EMedia::Video)
	{
		m_video_ctrl.ShowWindow(SW_SHOW);
	}
	
	// Refresh the display of the next/prev control panel
}
BOOL MainGUI::OnMouseWheel(UINT, short delta, CPoint)
{
	m_img->NavZ(delta / (float)WHEEL_DELTA);
	return FALSE; // ie. we handled this wheel message
}

// Recent files onclick
void MainGUI::MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item)
{
	if (sender == static_cast<pr::gui::MenuList*>(&m_recent))
	{
		m_img->SetMedia(MediaFile(item.m_name));
	}
}

// When the recent files list changes, save the settings
void MainGUI::MenuList_ListChanged(pr::gui::MenuList* sender)
{
	if (sender == static_cast<pr::gui::MenuList*>(&m_recent))
	{
		m_img->Settings().m_recent_files = m_recent.Export();
		m_img->Settings().Save();
	}
}

// Update the status text
void MainGUI::Status(char const* msg, bool bold)
{
	PR_ASSERT(DBG, GetCurrentThreadId() == m_my_thread_id, "Cross thread call to Status()");
	m_status.SetText(StatusPane::Message, msg);
	m_status.SetFont(bold ? m_font_bold : m_font_norm);
}

// Shutdown the app
void MainGUI::CloseApp(int exit_code)
{
	DestroyWindow();
	::PostQuitMessage(exit_code);
}

// A new media file has been displayed
void MainGUI::OnEvent(Event_MediaSet const& e)
{
	// Update the title bar
	SetWindowText(pr::FmtS("ImagerN - %s", e.m_mf->m_path.c_str()));
	
	// Update the image info on the status bar
	Status(pr::FmtS("Loaded %s", e.m_mf->m_path.c_str()), false);
	m_status.SetText(StatusPane::ImageDim, pr::FmtS("%d x %d", e.m_width, e.m_height));
	
	// If the media type is video, show the video controls, otherwise hide them
	m_video_ctrl.ResizeToParent();
	m_video_ctrl.ShowWindow(m_img->CurrentPhoto().MediaType() == EMedia::Video ? SW_SHOW : SW_HIDE);
}

// Display an error or status message
void MainGUI::OnEvent(Event_Message const& e)
{
	switch (e.m_lvl)
	{
	case Event_Message::Error:
		::MessageBox(m_hWnd, e.m_msg, "ImagerN Error", MB_OK);
		break;
	case Event_Message::Warning:
		Status(e.m_msg, true);
		break;
	case Event_Message::Info:
		Status(e.m_msg, false);
		break;
	}
}

