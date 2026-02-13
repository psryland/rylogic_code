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
		, m_imgui({
			.m_device = m_rdr.D3DDevice(),
			.m_cmd_queue = m_rdr.GfxQueue(),
			.m_hwnd = HWND(ui),
			.m_rtv_format = m_window.m_rt_props.Format,
			.m_num_frames_in_flight = m_window.BBCount(),
			.m_font_scale = 1.5f,
		})
	{
		// Position the camera: looking forward (+X) from above the ocean
		m_cam.LookAt(v4(0, 0, 15, 1), v4(50, 0, 0, 1), v4(0, 0, 1, 0));
		m_cam.Align(v4ZAxis);

		// Watch for scene renders
		m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1, _2);

		// Build initial meshes
		m_ocean.Update(0.0f, m_camera_world_pos);
		m_terrain.Update(m_camera_world_pos);
	}

	Main::~Main()
	{
		m_scene.ClearDrawlists();
	}

	// Simulation step
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

		// Reset the scene drawlist
		m_scene.ClearDrawlists();

		// Render a frame
		auto& frame = m_window.NewFrame();
		m_scene.Render(frame);

		// Render imgui overlay into the post-resolve back buffer
		RenderUI(frame);

		m_window.Present(frame);
	}

	// Add UI components
	void Main::RenderUI(Frame& frame)
	{
		if (!m_imgui)
			return;

		// Start a new imgui frame
		m_imgui.NewFrame();

		// Build the debug overlay
		m_imgui.SetNextWindowPos(10, 10, 1 /*ImGuiCond_Once*/);
		m_imgui.SetNextWindowBgAlpha(0.5f);
		if (m_imgui.BeginWindow("Debug Info", nullptr, (1 << 0) | (1 << 1) | (1 << 6)))
		{
			// ImGuiWindowFlags_NoTitleBar=1<<0, NoResize=1<<1, AlwaysAutoResize=1<<6
			char buf[128];
			snprintf(buf, sizeof(buf), "Sim Time: %.2f s", m_sim_time);
			m_imgui.Text(buf);

			snprintf(buf, sizeof(buf), "Frame: %lld", m_render_frame);
			m_imgui.Text(buf);

			snprintf(buf, sizeof(buf), "Pos: (%.1f, %.1f, %.1f)", m_camera_world_pos.x, m_camera_world_pos.y, m_camera_world_pos.z);
			m_imgui.Text(buf);
		}
		m_imgui.EndWindow();

		// Set the swap chain back buffer as the render target
		frame.m_resolve.OMSetRenderTargets({ &frame.bb_post().m_rtv, 1 }, FALSE, nullptr);

		// Set viewport and scissor
		auto const& vp = m_scene.m_viewport;
		frame.m_resolve.RSSetViewports({ &vp, 1 });
		frame.m_resolve.RSSetScissorRects(vp.m_clip);

		// Render imgui draw data
		m_imgui.Render(frame.m_resolve.get());
	}

	// Update the scene with things to render
	void Main::UpdateScene(Scene& scene, UpdateSceneArgs const& args)
	{
		m_skybox.AddToScene(scene);
		m_ocean.AddToScene(scene, args.m_cmd_list, args.m_upload);
		//m_terrain.AddToScene(scene, args.m_cmd_list, args.m_upload);
	}

	MainUI::MainUI(wchar_t const*, int)
		:base(Params().title(AppTitle()))
	{
		// Set Windows timer resolution to 1ms for accurate sleep intervals
		::timeBeginPeriod(1);

		// Fixed step simulation at 60Hz, render as fast as possible (capped by vsync/present)
		m_msg_loop.AddLoop(60.0, false, [this](int64_t ms) { m_main->Step(ms * 0.001); });
		m_msg_loop.AddLoop(60.0, true, [this](int64_t) { m_main->DoRender(true); });
	}

	bool MainUI::ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		// Forward to imgui first
		if (m_main && m_main->m_imgui.WndProc(parent_hwnd, message, wparam, lparam))
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