//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"
//#include "cam/cam.h"
//#include "world/terrain.h"
//#include "ship/ship.h"

namespace las
{
	// Main application logic container
	struct Main :pr::app::Main<Main, MainUI, Settings>
	{
		using base = pr::app::Main<Main, MainUI, Settings>;
		using Skybox = pr::app::Skybox;

		static char const* AppName() { return "LostAtSea"; }

		Skybox     m_skybox;
		//Ship       m_ship;
		//Terrain    m_terrain;

		Main(MainUI& gui);
		~Main();

		// Advance the game by one frame
		void Step(double elapsed_seconds);

		// Add instances to the scene
		void AddToScene(Scene& scene);
	};

	// Main app window
	struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>
	{
		using base = pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>;
		static wchar_t const* AppTitle() { return L"Lost at Sea"; }

		MainUI(wchar_t const* lpstrCmdLine, int nCmdShow);
	};
}
