//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#include "stdafx.h"
#include "main.h"
#include "event.h"
#include "util.h"
#include "pr/camera/camctrl_dinput_wasd.h"

using namespace las;

CAppModule g_app;

// todo list

int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpstrCmdLine, int nCmdShow)
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
		las::MainGUI gui(hInstance, lpstrCmdLine);
		g_app.AddMessageLoop(&gui);
		if (gui.Create(0) == 0) throw las::Exception(las::EResult::StartupFailed, "Main window creation failed");
		gui.ShowWindow(nCmdShow);
		res = gui.Run();
		SHOULD USE A SIMULATION MESSAGE PUMP
	}
	catch (las::Exception const& ex)
	{
		DWORD last_error = GetLastError();
		HRESULT res = HRESULT_FROM_WIN32(last_error);
		std::string err = ex.m_msg;
		err += pr::FmtS("\nCode: %X - %s", res, pr::HrMsg(res).c_str());
		::MessageBoxA(0, err.c_str(), "LAS error", MB_OK|MB_ICONERROR);
		res = -1;
	}
	catch (...)
	{
		::MessageBoxA(0, "Shutting down due to an unknown exception", "LAS error", MB_OK|MB_ICONERROR);
		res = -1;
	}
	g_app.RemoveMessageLoop();
	g_app.Term();
	return res;
}

// MainGUI ****************************************************

las::MainGUI::MainGUI(HINSTANCE hInstance, LPTSTR lpstrCmdLine)
:m_hInstance(hInstance)
,m_main()
,m_last_time(GetTickCount())
,m_resizing(false)
{
	(void)lpstrCmdLine;
}
las::MainGUI::~MainGUI()
{
	delete m_main;
}

// Idle handler
BOOL MainGUI::OnIdle(int)
{
	if (m_main)
	{
		DWORD now = GetTickCount();
		bool rdr = false;
		
		float const step_rate = 1.0f / 60.0f;
		long const max_steps = 10;
		
		long steps = long(DWORD(now - m_last_time) / (1000.0f * step_rate));
		if (steps > max_steps) steps = max_steps;
		for (;steps--;)
		{
			m_main->Step(step_rate);
			rdr = true;
		}
		
		if (rdr)
		{
			m_main->Render();
			m_last_time = now;
		}
	}
	return TRUE;
}

// Create the main window
LRESULT las::MainGUI::OnCreate(LPCREATESTRUCT /*create*/)
{
	SetWindowText(las::AppTitle());
	
	//// Set icons
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
	//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);
	//m_video_ctrl.Create(m_hWnd);
	//
	//// Create and attached the status bar
	//CreateSimpleStatusBar(IDC_STATUSBAR);
	//m_status.Attach(m_hWndStatusBar);

	//// Create the status pane parts
	//StatusPane::SetWidths(create->cx - 2*::GetSystemMetrics(SM_CXBORDER));
	//m_status.SetParts(PR_COUNTOF(StatusPane::Widths), StatusPane::Widths);
	//
	//// Attach/Create status bar fonts
	//m_font_norm.CreatePointFont(100, _T("Segoe UI"));
	//m_font_bold.CreatePointFont(100, _T("Segoe UI"), 0, true);
	//
	//// Recent files handler
	//m_recent.Attach(pr::gui::GetMenuByName(GetMenu(), _T("&File,&Recent")) ,IDM_RECENT ,10 ,this);
	//
	//// Register this class for message filtering and idle updates
	//CMessageLoop* loop = g_app_module.GetMessageLoop();
	//loop->AddMessageFilter(this);
	//loop->AddIdleHandler(this);
	
	// Create the main app logic
	m_main = new Main(this);
	
	//m_recent.Import(m_img->Settings().m_recent_files);
	//Status("Idle", false);
	return S_OK;
}

// System commands
void las::MainGUI::OnSysCommand(UINT wparam, WTL::CPoint const&)
{
	switch (wparam)
	{
	default: SetMsgHandled(FALSE); break;
	case SC_CLOSE: CloseApp(0); break;
	}
}

// Paint the window
void las::MainGUI::OnPaint(CDCHandle)
{
	SetMsgHandled(FALSE);
	if (m_main && !m_resizing)
		m_main->Render();
}

// Shutdown the app
void las::MainGUI::CloseApp(int exit_code)
{
	DestroyWindow();
	::PostQuitMessage(exit_code);
}

// Main ****************************************************

las::Main::Main(MainGUI* gui)
:m_settings(las::SettingsPath(gui->m_hWnd), true)
,m_alloc()
,m_rdr(las::RdrSettings(gui->m_hWnd, m_settings, m_alloc, pr::ClientArea(gui->m_hWnd)))
,m_view(las::VPSettings(m_rdr, 0))
,m_gui(gui)
,m_cam(pr::maths::tau_by_8, m_rdr.ClientArea().Aspect())
,m_cam_ctrl(new las::DevCam(m_cam, gui->m_hInstance, gui->m_hWnd, m_rdr.ClientArea()))
,m_skybox(m_rdr, las::DataPath("skybox/SkyBox-Clouds-Few-Noon.png"))
,m_ship(m_rdr)
,m_terrain(m_rdr)
{
	// Position the camera
	m_cam.LookAt(
		pr::v4::make(0, 0, 10.0f, 1.0f),
		pr::v4Origin, 
		pr::v4YAxis, true);
	m_view.CameraToWorld(m_cam.CameraToWorld());
}

// Advance the game by one frame
void las::Main::Step(float elapsed_s)
{
	pr::events::Send(las::Evt_Step(elapsed_s));
}

// Draw the scene
void las::Main::Render()
{
	// Render the viewports
	if (pr::Failed(m_rdr.RenderStart()))
		return;
	
	// Set the viewport view
	m_view.SetView(m_cam);
	
	// Add objects to the viewport
	m_view.ClearDrawlist();
	pr::events::Send(las::Evt_AddToViewport(m_view, m_cam));
	
	// Render the view
	m_view.Render();
	m_rdr.RenderEnd();
	m_rdr.Present();
}

// The size of the window has changed
void las::Main::Resize(pr::IRect const& client_area)
{
	m_rdr.Resize(client_area);
	m_cam.Aspect(client_area.Aspect());
}

//		
//	AllocConsole();
//	HWND hwnd = GetConsoleWindow();
//	D3DPtr<IDirectSound8> dsound = pr::sound::InitDSound(hwnd);
//	
//	// Create an ogg data stream
//	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/ship/atmosphere_flying.ogg");
//	pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/ship/ecm.ogg");
//	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/planet/RainForestIntroduced.ogg");
//	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/planet/river.ogg");
//	
//	// Get the data stream to create an appropriate buffer for us
//	D3DPtr<IDirectSoundBuffer8> dbuf = ogg.CreateBuffer(dsound);
//	
//	// Create a player to play the sample
//	pr::sound::Player player;
//	player.Set(&ogg, dbuf);
//	player.Play(true);
//	
//	// Main loop 
//	for (;!_kbhit(); Sleep(10) ) { pr::events::Send(pr::sound::Evt_SoundUpdate()); }
//	return 0;
//}