//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/main.h"

namespace las
{
	Main::Main(MainUI& ui)
		:base(pr::app::DefaultSetup(), ui)
		, m_skybox(m_rdr, L"data\\skybox\\SkyBox-Clouds-Few-Noon.png", Skybox::EStyle::FiveSidedCube, 1000.0f, m3x4::Rotation(maths::tau_by_4f, 0, 0))
		, m_height_field(42)
		, m_ocean(m_rdr)
		, m_terrain(m_rdr, m_height_field)
		, m_sim_time(0.0)
		, m_camera_world_pos(v4(0, 0, 15, 1))
		, m_move_speed(20.0f)
		, m_render_frame(0)
		, m_imgui()
		, m_imgui_cmd_list(m_rdr.D3DDevice(), nullptr, "ImGui", Colour32(0xFF00FF00))
	{
		// Position the camera: looking forward (+X) from above the ocean
		m_cam.LookAt(v4(0, 0, 15, 1), v4(50, 0, 0, 1), v4(0, 0, 1, 0));
		m_cam.Align(v4ZAxis);

		m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1, _2);

		// Build initial meshes
		m_ocean.Update(0.0f, m_camera_world_pos);
		m_terrain.Update(m_camera_world_pos);

		// Initialise imgui
		pr::rdr12::imgui::InitArgs imgui_args = {};
		imgui_args.m_device = m_rdr.D3DDevice();
		imgui_args.m_hwnd = HWND(ui);
		imgui_args.m_rtv_format = m_window.m_rt_props.Format;
		imgui_args.m_num_frames_in_flight = m_window.BBCount();
		m_imgui = pr::rdr12::imgui::ImGuiUI(imgui_args);
	}

	Main::~Main()
	{
		m_scene.ClearDrawlists();
	}

	void Main::Step(double elapsed_seconds)
	{
		m_sim_time += elapsed_seconds;
		auto dt = static_cast<float>(elapsed_seconds);

		// WASD movement: move the camera world position (X=forward, Y=right, Z=up)
		auto speed = m_move_speed * (KeyDown(VK_SHIFT) ? 3.0f : 1.0f);
		if (KeyDown('W')) m_camera_world_pos.x += speed * dt;
		if (KeyDown('S')) m_camera_world_pos.x -= speed * dt;
		if (KeyDown('A')) m_camera_world_pos.y -= speed * dt;
		if (KeyDown('D')) m_camera_world_pos.y += speed * dt;

		// Simulation: compute new vertex positions on CPU
		m_ocean.Update(static_cast<float>(m_sim_time), m_camera_world_pos);
		m_terrain.Update(m_camera_world_pos);

		RenderNeeded();
	}

	void Main::DoRender(bool force)
	{
		if (!m_rdr_pending && !force)
			return;

		m_rdr_pending = false;
		++m_render_frame;

		m_scene.ClearDrawlists();
		auto& frame = m_window.NewFrame();
		m_scene.Render(frame);

		// Render imgui overlay into the post-resolve back buffer
		RenderImGui(frame);

		m_window.Present(frame, pr::rdr12::EGpuFlush::Block);
	}

	void Main::UpdateScene(Scene& scene, UpdateSceneArgs const& args)
	{
		// Rendering: upload dirty data to GPU and add instances
		m_skybox.AddToScene(scene);
		m_ocean.AddToScene(scene, args.m_cmd_list, args.m_upload);
		//m_terrain.AddToScene(scene, args.m_cmd_list, args.m_upload);
	}

	void Main::RenderImGui(Frame& frame)
	{
		if (!m_imgui)
			return;

		// Start a new imgui frame
		m_imgui.NewFrame();

		// Build the debug overlay
		m_imgui.SetNextWindowPos(10, 10, 1 /*ImGuiCond_Once*/);
		m_imgui.SetNextWindowBgAlpha(0.5f);
		if (m_imgui.BeginWindow("Debug Info", nullptr, (1 << 0) | (1 << 1) | (1 << 5)))
		{
			// ImGuiWindowFlags_NoTitleBar=1<<0, NoResize=1<<1, AlwaysAutoResize=1<<5
			char buf[128];
			snprintf(buf, sizeof(buf), "Sim Time: %.2f s", m_sim_time);
			m_imgui.Text(buf);

			snprintf(buf, sizeof(buf), "Frame: %lld", m_render_frame);
			m_imgui.Text(buf);

			snprintf(buf, sizeof(buf), "Pos: (%.1f, %.1f, %.1f)", m_camera_world_pos.x, m_camera_world_pos.y, m_camera_world_pos.z);
			m_imgui.Text(buf);
		}
		m_imgui.EndWindow();

		// Record imgui draw commands into a command list for the post-resolve phase
		m_imgui_cmd_list.Reset(frame.m_cmd_alloc_pool.Get());

		// Set the swap chain back buffer as the render target
		auto& bb = frame.bb_post();
		m_imgui_cmd_list.OMSetRenderTargets({ &bb.m_rtv, 1 }, FALSE, nullptr);

		// Set viewport and scissor
		auto const& vp = m_scene.m_viewport;
		m_imgui_cmd_list.RSSetViewports({ &vp, 1 });
		m_imgui_cmd_list.RSSetScissorRects(vp.m_clip);

		// Render imgui draw data
		m_imgui.Render(m_imgui_cmd_list.get());

		m_imgui_cmd_list.Close();
		frame.m_post.push_back(m_imgui_cmd_list);
	}

	bool Main::ImGuiWndProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
	{
		return m_imgui.WndProc(hwnd, msg, wparam, lparam);
	}

	MainUI::MainUI(wchar_t const*, int)
		:base(Params().title(AppTitle()))
	{
		// Set Windows timer resolution to 1ms for accurate sleep intervals
		::timeBeginPeriod(1);

		// Fixed step simulation at 60Hz, render as fast as possible (capped by vsync/present)
		m_msg_loop.AddStepContext("step", [this](double s) { m_main->Step(s); }, 60.0f, true);
		m_msg_loop.AddStepContext("render", [this](double) { m_main->DoRender(true); }, 1000.0f, false);
	}

	bool MainUI::ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		// Forward to imgui first
		if (m_main && m_main->ImGuiWndProc(parent_hwnd, message, wparam, lparam))
		{
			result = 0;
			return true;
		}

		return base::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
	}
}

namespace pr::app
{
	std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow)
	{
		return std::unique_ptr<IAppMainUI>(new las::MainUI(lpstrCmdLine, nCmdShow));
	}
}