//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"
#include "src/core/frame_tasks.h"
#include "src/core/state_snapshot.h"
#include "src/core/sim_state.h"
#include "src/core/day_night_cycle.h"
#include "src/world/sky/procedural_sky.h"
#include "src/world/ocean/ocean.h"
#include "src/world/ocean/distant_ocean.h"
#include "src/world/terrain/height_field.h"
#include "src/world/terrain/terrain.h"
#include "pr/view3d-12/imgui/imgui.h"
#include "pr/common/task_graph.h"

namespace las
{
	// Main application logic
	struct Main :pr::app::Main<Main, MainUI, Settings>
	{
		using base = pr::app::Main<Main, MainUI, Settings>;
		using ImGuiUI = pr::rdr12::imgui::ImGuiUI;
		static char const* AppName() { return "LostAtSea"; }

		ProceduralSky m_sky;
		DayNightCycle m_day_cycle;
		Ocean m_ocean;
		DistantOcean m_distant_ocean;
		Terrain m_terrain;
		HeightField m_height_field; // CPU-side height queries for future physics

		// Simulation state snapshot: Step writes, Render reads
		StateSnapshot<SimState> m_sim_state;

		double m_sim_time;
		float m_move_speed; // World units per second
		int64_t m_render_frame;

		// Task graphs for parallel execution
		pr::task_graph::Graph<StepTaskId> m_step_graph;
		pr::task_graph::Graph<RenderTaskId> m_render_graph;

		// ImGui overlay
		ImGuiUI m_imgui;

		Main(MainUI& ui);
		~Main();

		void Step(double elapsed_seconds);
		void DoRender(bool force = false);
		void RenderUI(Frame& frame);
		void UpdateScene(Scene& scene, UpdateSceneArgs const& args);
	};

	// Main app window
	struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::WinGuiMsgLoop>
	{
		using base = pr::app::MainUI<MainUI, Main, pr::gui::WinGuiMsgLoop>;
		static wchar_t const* AppTitle() { return L"Lost at Sea"; }

		MainUI(wchar_t const* lpstrCmdLine, int nCmdShow);

		// Override WndProc to forward messages to imgui
		bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override;
	};
}

namespace pr::app
{
	std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow);
}