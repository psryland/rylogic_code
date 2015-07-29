//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#include "lost_at_sea/src/stdafx.h"
#include "lost_at_sea/src/main.h"
//#include "lost_at_sea/src/event.h"
#include "lost_at_sea/src/util.h"

using namespace pr::app;
using namespace pr::gui;

// Create the GUI window
extern std::shared_ptr<IAppMainGui> pr::app::CreateGUI(LPTSTR lpstrCmdLine, int nCmdShow)
{
	return pr::app::CreateGUI<las::MainGUI>(lpstrCmdLine, nCmdShow);
}

namespace las
{
	wchar_t const* AppTitle()     { return L"Lost at Sea"; }
	wchar_t const* AppVersionW()  { return L"v0.00.01"; }
	char const*    AppVersionA()  { return "v0.00.01"; }
	wchar_t const* AppVendor()    { return L"Rylogic Ltd"; }
	wchar_t const* AppCopyright() { return L"Copyright (c) Rylogic Ltd 2011"; }

	struct Setup
	{
		MainGUI* m_gui;

		Setup(MainGUI& gui)
			:m_gui(&gui)
		{}

		std::string UserSettings() const { return ""; }

		// Return configuration settings for the renderer
		pr::rdr::RdrSettings RdrSettings()
		{
			pr::rdr::RdrSettings s(FALSE);
			//s.m_window_handle      = hwnd;
			//s.m_device_config      = settings.m_fullscreen ?
			//	pr::rdr::GetDefaultDeviceConfigFullScreen(settings.m_res_x, settings.m_res_y, D3DDEVTYPE_HAL) :
			//	pr::rdr::GetDefaultDeviceConfigWindowed(D3DDEVTYPE_HAL);
			//s.m_allocator          = &alloc;
			//s.m_client_area        = client_area;
			//s.m_zbuffer_format     = D3DFMT_D24S8;
			//s.m_swap_effect        = D3DSWAPEFFECT_DISCARD;//D3DSWAPEFFECT_COPY;// - have to use discard for antialiasing, but that means no blt during resize
			//s.m_back_buffer_count  = 1;
			//s.m_geometry_quality   = settings.m_geometry_quality;
			//s.m_texture_quality    = settings.m_texture_quality;
			//s.m_background_colour  = pr::Colour32Black;
			//s.m_max_shader_version = pr::rdr::EShaderVersion::v3_0;
			return s;
		}

		// Return settings for the renderer window
		pr::rdr::WndSettings RdrWindowSettings(HWND hwnd, pr::iv2 const& client_area)
		{
			return pr::rdr::WndSettings(hwnd, true, false, client_area);
		}
	};

	// Main ****************************************************
	Main::Main(MainGUI& gui)
		:base(Setup(gui), gui)
		//:m_settings(SettingsPath(), true)
		//,m_gui(gui)
		//,m_rdr(RdrSettings())
		//,m_window(m_rdr, RdrWndSettings(*gui, m_settings, pr::ClientArea(*gui).Size()))
		//,m_scene(m_window,{pr::rdr::ERenderStep::ForwardRender})
//,m_cam(pr::maths::tau_by_8, m_rdr.ClientArea().Aspect())
//,m_cam_ctrl(new las::DevCam(m_cam, gui->m_hInstance, gui->m_hWnd, m_rdr.ClientArea()))
		,m_skybox(m_rdr, DataPath(L"data\\skybox\\SkyBox-Clouds-Few-Noon.png"), Skybox::EStyle::FiveSidedCube)
//,m_ship(m_rdr)
//,m_terrain(m_rdr)
	{
		// Position the camera
		m_cam.LookAt(
			pr::v4::make(0, 0, 10.0f, 1.0f),
			pr::v4Origin, 
			pr::v4YAxis, true);
	//	m_view.CameraToWorld(m_cam.CameraToWorld());
	}
	
	// Advance the game by one frame
	void Main::Step(double /*elapsed_seconds*/)
	{
		//pr::events::Send(las::Evt_Step(elapsed_s));
	}

//// Draw the scene
//void las::Main::Render()
//{
//	// Render the viewports
//	if (pr::Failed(m_rdr.RenderStart()))
//		return;
//	
//	// Set the viewport view
//	m_view.SetView(m_cam);
//	
//	// Add objects to the viewport
//	m_view.ClearDrawlist();
//	pr::events::Send(las::Evt_AddToViewport(m_view, m_cam));
//	
//	// Render the view
//	m_view.Render();
//	m_rdr.RenderEnd();
//	m_rdr.Present();
//}
//
//// The size of the window has changed
//void las::Main::Resize(pr::IRect const& client_area)
//{
//	m_rdr.Resize(client_area);
//	m_cam.Aspect(client_area.Aspect());
//}
//
////		
////	AllocConsole();
////	HWND hwnd = GetConsoleWindow();
////	D3DPtr<IDirectSound8> dsound = pr::sound::InitDSound(hwnd);
////	
////	// Create an ogg data stream
////	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/ship/atmosphere_flying.ogg");
////	pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/ship/ecm.ogg");
////	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/planet/RainForestIntroduced.ogg");
////	//pr::sound::OggDataStream ogg("C:/Users/Paul/Desktop/pioneer-alpha14/data/sounds/planet/river.ogg");
////	
////	// Get the data stream to create an appropriate buffer for us
////	D3DPtr<IDirectSoundBuffer8> dbuf = ogg.CreateBuffer(dsound);
////	
////	// Create a player to play the sample
////	pr::sound::Player player;
////	player.Set(&ogg, dbuf);
////	player.Play(true);
////	
////	// Main loop 
////	for (;!_kbhit(); Sleep(10) ) { pr::events::Send(pr::sound::Evt_SoundUpdate()); }
////	return 0;
////}


	// MainGUI ****************************************************
	MainGUI::MainGUI(LPTSTR lpstrCmdLine, int nCmdShow)
		:pr::app::MainGUI<MainGUI, Main, pr::gui::SimMsgLoop>(AppTitle())
	{
		(void)lpstrCmdLine,nCmdShow;
		m_msg_loop.AddStepContext("render", [this](double)  { m_main->DoRender(true); }, 60.0f, false);
		m_msg_loop.AddStepContext("step"  , [this](double s){ m_main->Step(s); }, 60.0f, true);
	}
}

//// Entry point
//int __stdcall _tWinMain(HINSTANCE, HINSTANCE, LPTSTR, int)
//{
//	int res = 0;
//	try
//	{
//		// CoInitialise
//		pr::InitCom init_com;
//
//		las::MainGUI main;
//		
//		pr::SimMsgLoop loop;
//		loop.AddStepContext("Step", [&](double s){ main.Step(s); }, 30.0f, true);
//		loop.AddStepContext("Render", [&](double){ main.Render(); }, 60.0f, true);
//		
//		return loop.Run();
//	}
//	catch (std::exception const& ex)
//	{
//		DWORD last_error = GetLastError();
//		HRESULT res = HRESULT_FROM_WIN32(last_error);
//		auto err = pr::Fmt("%s\nCode: %X - %s", ex.what(), res, pr::HrMsg(res).c_str());
//		::MessageBoxA(0, err.c_str(), "LAS error", MB_OK|MB_ICONERROR);
//		res = -1;
//	}
//	catch (...)
//	{
//		::MessageBoxA(0, "Shutting down due to an unknown exception", "LAS error", MB_OK|MB_ICONERROR);
//		res = -1;
//	}
//	return res;
//}
