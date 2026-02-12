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
#include "pr/view3d-12/imgui/imgui.h"

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
		int64_t m_render_frame;

		// ImGui overlay
		pr::rdr12::imgui::ImGuiUI m_imgui;
		GfxCmdList m_imgui_cmd_list;

		Main(MainUI& ui);
		~Main();

		void Step(double elapsed_seconds);
		void DoRender(bool force = false);
		void UpdateScene(Scene& scene, UpdateSceneArgs const& args);
		void RenderImGui(Frame& frame);

		// Forward WndProc to imgui for input handling
		bool ImGuiWndProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);
	};

	// Main app window
	struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>
	{
		using base = pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>;
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