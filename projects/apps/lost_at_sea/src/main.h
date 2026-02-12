//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"
#include "src/world/ocean.h"
#include "src/world/height_field.h"
#include "src/world/terrain.h"

namespace las
{
	// Main application logic
	struct Main :pr::app::Main<Main, MainUI, Settings>
	{
		using base = pr::app::Main<Main, MainUI, Settings>;
		using Skybox = pr::app::Skybox;

		static char const* AppName() { return "LostAtSea"; }

		Skybox m_skybox;
		HeightField m_height_field;
		Ocean m_ocean;
		Terrain m_terrain;

		double m_sim_time;
		v4 m_camera_world_pos;
		float m_move_speed; // World units per second

		Main(MainUI& ui);
		~Main();

		void Step(double elapsed_seconds);
		void UpdateScene(Scene& scene, UpdateSceneArgs const& args);
	};

	// Main app window
	struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>
	{
		using base = pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>;
		static wchar_t const* AppTitle() { return L"Lost at Sea"; }

		MainUI(wchar_t const* lpstrCmdLine, int nCmdShow);
	};
}

namespace pr::app
{
	std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow);
}