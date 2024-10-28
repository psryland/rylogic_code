//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#include "src/forward.h"
#include "src/main.h"
#include "src/util.h"

using namespace pr;
using namespace pr::app;
using namespace pr::gui;
using namespace pr::rdr;

// Create the GUI window
std::unique_ptr<IAppMainUI> pr::app::CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow)
{
	return std::unique_ptr<IAppMainUI>(new las::MainUI(lpstrCmdLine, nCmdShow));
}

namespace las
{
	wchar_t const* AppVersionW()  { return L"v0.00.01"; }
	char const*    AppVersionA()  { return "v0.00.01"; }
	wchar_t const* AppVendor()    { return L"Rylogic Ltd"; }
	wchar_t const* AppCopyright() { return L"Copyright (c) Rylogic Ltd 2011"; }

	struct Setup
	{
		using RSettings = pr::rdr::RdrSettings;
		using WSettings = pr::rdr::WndSettings;

		MainUI* m_ui;

		Setup(MainUI& ui)
			:m_ui(&ui)
		{}

		std::wstring UserSettings() const { return L""; }

		// Return configuration settings for the renderer
		RSettings RdrSettings()
		{
			RSettings s(GetModuleHandleW(nullptr), D3D11_CREATE_DEVICE_FLAG(0));
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
		WSettings RdrWindowSettings(HWND hwnd)
		{
			return WSettings(hwnd);
		}
	};

	// Main ****************************************************

	Main::Main(MainUI& gui)
		:base(Setup(gui), gui)
		,m_skybox(m_rdr, DataPath(L"data\\skybox\\SkyBox-Clouds-Few-Noon.png"), Skybox::EStyle::FiveSidedCube, 100.0f)
		//,m_ship(m_rdr)
		//,m_terrain(m_rdr)
	{
		// Watch for scene drawlist updates
		m_scene.OnUpdateScene += std::bind(&Main::AddToScene, this, _1);
	}
	Main::~Main()
	{
		// Clear the drawlists so that destructing models
		// don't assert because they're still in a drawlist.
		m_scene.ClearDrawlists();
	}
	
	// Advance the game by one frame
	void Main::Step(double elapsed_seconds)
	{
		(void)elapsed_seconds;
	}

	// Add instances to the scene
	void Main::AddToScene(Scene& scene)
	{
		m_skybox.AddToScene(scene);
	}

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
////	for (;!_kbhit(); Sleep(10) ) { player.Update(); }
////	return 0;
////}


	// MainUI ****************************************************

	MainUI::MainUI(wchar_t const*, int)
		:base(Params().title(AppTitle()))
	{
		m_msg_loop.AddStepContext("render", [this](double)  { m_main->DoRender(true); }, 60.0f, false);
		m_msg_loop.AddStepContext("step"  , [this](double s){ m_main->Step(s); }, 60.0f, true);
	}
}

#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
