//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#include "sol/main/stdafx.h"
#include "sol/main/main_gui.h"
#include "sol/main/main.h"

using namespace sol;

sol::MainGUI::MainGUI()
:m_main(0)
,m_my_thread_id(GetCurrentThreadId())
,m_nav_enabled(false)
,m_resizing(false)
{}

// Create the main window
LRESULT sol::MainGUI::OnCreate(LPCREATESTRUCT create)
{
	SetWindowTextA("Sol");
	
	//// Set icons
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);
	
	// Register this class for message filtering and idle updates
	CMessageLoop* loop = g_app_module.GetMessageLoop();
	loop->AddMessageFilter(this);
	loop->AddIdleHandler(this);
	
	// Create the main app logic
	m_main = new Main(*this);
	return S_OK;
}

// Destroy the window
void MainGUI::OnDestroy()
{
	delete m_main; m_main = 0;
	
	CMessageLoop* loop = g_app_module.GetMessageLoop();
	loop->RemoveMessageFilter(this);
	loop->RemoveIdleHandler(this);
}

// Idle handler
BOOL sol::MainGUI::OnIdle()
{
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

// Handle menu commands
void MainGUI::OnCommand(UINT, INT wID, HWND)
{
	switch (wID)
	{
	default: SetMsgHandled(FALSE); break;
	case IDCLOSE:
		CloseApp(0);
		break;
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
	if (m_main) m_main->Render();
	//SetMsgHandled(FALSE);
}

// Resizing handlers
void MainGUI::OnSizing(UINT, LPRECT)
{
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
		// Find the new client area
		pr::IRect area = pr::ClientArea(m_hWnd);
		area.m_max.y -= pr::WindowBounds(m_hWndStatusBar).SizeY();
		
		//SaveWindowBounds();
		//UpdateUI();
		UpdateLayout(true);
		m_main->Resize(area);
		m_main->Render();
	}
}

// Navigation handlers
void MainGUI::OnMouseDown(UINT flags, CPoint point)
{
	if (pr::AllSet(flags, MK_LBUTTON))
	{
		m_nav_enabled = true;
		m_sol->Nav(pr::NormalisePoint(m_hWnd, point), pr::camera::ENavBtn::Left, true);
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

