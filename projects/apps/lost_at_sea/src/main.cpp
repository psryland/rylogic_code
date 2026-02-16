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
		, m_skybox(m_rdr, L"data\\skybox\\SkyBox-Clouds-Few-Noon.png", Skybox::EStyle::FiveSidedCube, 5000.0f, m3x4::Rotation(maths::tau_by_4f, 0, 0))
		, m_ocean(m_rdr)
		, m_distant_ocean(m_rdr)
		, m_terrain(m_rdr)
		, m_height_field(42)
		, m_sim_state()
		, m_sim_time(0.0)
		, m_move_speed(20.0f)
		, m_render_frame(0)
		, m_step_graph(2)   // Step graph: small thread pool (input-heavy, will grow with physics/AI)
		, m_render_graph(4) // Render graph: larger pool for parallel CB prep
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
	}

	Main::~Main()
	{
		m_scene.ClearDrawlists();

		// Ensure the GPU has finished all in-flight frames before destroying
		// pipeline state objects (shaders) owned by ocean/terrain models.
		m_window.m_gsync.Wait();
	}

	// Simulation step — builds and runs the step task graph
	void Main::Step(double elapsed_seconds)
	{
		m_sim_time += elapsed_seconds;
		auto dt = static_cast<float>(elapsed_seconds);

		// Input task: process WASD movement
		m_step_graph.Add(StepTaskId::Input, [&](auto&) -> pr::task_graph::Task {
			auto speed = m_move_speed * (KeyDown(VK_SHIFT) ? 3.0f : 1.0f);
			auto c2w = m_cam.CameraToWorld();
			auto forward = -c2w.z;
			auto right = c2w.x;
			auto move = v4::Zero();
			if (KeyDown('W')) move += forward;
			if (KeyDown('S')) move -= forward;
			if (KeyDown('D')) move += right;
			if (KeyDown('A')) move -= right;
			if (LengthSq(move) > 0)
			{
				move = Normalise(move) * speed * dt;
				move.z = 0;
				c2w.pos += move;
				m_cam.CameraToWorld(c2w);
			}
			co_return;
		});

		// Finalise task: commit state snapshot for the render graph
		m_step_graph.Add(StepTaskId::Finalise, [&](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(StepTaskId::Input);

			auto lock = m_sim_state.Lock();
			lock->m_camera_pos = m_cam.CameraToWorld().pos;
			lock->m_sim_time = m_sim_time;
			co_return;
		});

		m_step_graph.Run();
		m_step_graph.Reset();

		RenderNeeded();
	}

	// Update the scene with things to render (called from render graph's Submit task)
	void Main::UpdateScene(Scene& scene, UpdateSceneArgs const&)
	{
		// AddInstance is not thread-safe, so all scene population happens here serially
		m_skybox.AddToScene(scene);
		m_ocean.AddToScene(scene);
		m_distant_ocean.AddToScene(scene);
		m_terrain.AddToScene(scene);
	}

	// Render step — builds and runs the render task graph
	void Main::DoRender(bool force)
	{
		if (!m_rdr_pending && !force)
			return;

		m_rdr_pending = false;
		++m_render_frame;

		// Read the latest simulation state snapshot
		auto const& sim = m_sim_state.Read();
		auto cam_pos = sim.m_camera_pos;
		auto time = static_cast<float>(sim.m_sim_time);

		// PrepareFrame task: set up the frame (must be serial, touches GPU resources)
		m_render_graph.Add(RenderTaskId::PrepareFrame, [&](auto&) -> pr::task_graph::Task {
			m_scene.ClearDrawlists();
			co_return;
		});

		// Per-system tasks: prepare shader constant buffers (thread-safe, parallel)
		m_render_graph.Add(RenderTaskId::Skybox, [&](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(RenderTaskId::PrepareFrame);
			// Skybox has no per-frame CB update
			co_return;
		});
		m_render_graph.Add(RenderTaskId::Ocean, [&, cam_pos, time](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(RenderTaskId::PrepareFrame);
			m_ocean.PrepareRender(cam_pos, time);
			co_return;
		});
		m_render_graph.Add(RenderTaskId::DistantOcean, [&, cam_pos](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(RenderTaskId::PrepareFrame);
			m_distant_ocean.PrepareRender(cam_pos);
			co_return;
		});
		m_render_graph.Add(RenderTaskId::Terrain, [&, cam_pos](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(RenderTaskId::PrepareFrame);
			m_terrain.PrepareRender(cam_pos);
			co_return;
		});

		// Submit task: populate scene and present (serial, after all CB prep is done)
		m_render_graph.Add(RenderTaskId::Submit, [&](auto ctx) -> pr::task_graph::Task {
			co_await ctx.Wait(RenderTaskId::Skybox);
			co_await ctx.Wait(RenderTaskId::Ocean);
			co_await ctx.Wait(RenderTaskId::DistantOcean);
			co_await ctx.Wait(RenderTaskId::Terrain);
			co_return;
		});

		m_render_graph.Run();
		m_render_graph.Reset();

		// Scene population and presentation happen on the main thread after the graph completes
		auto& frame = m_window.NewFrame();
		m_scene.Render(frame);
		RenderUI(frame);
		m_window.Present(frame);
	}

	// Render imgui overlay into the post-resolve back buffer
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
			*std::format_to(buf, "Sim Time: {:.2f} s", m_sim_time) = 0;
			m_imgui.Text(buf);
			*std::format_to(buf, "Frame: {}", m_render_frame) = 0;
			m_imgui.Text(buf);
			auto cam_pos = m_cam.CameraToWorld().pos;
			*std::format_to(buf, "Pos: ({:.1f}, {:.1f}, {:.1f}", cam_pos.x, cam_pos.y, cam_pos.z) = 0;
			m_imgui.Text(buf);

			m_imgui.Separator();

			// Task graph thread counts
			*std::format_to(buf, "Step Threads: {}", m_step_graph.ThreadCount()) = 0;
			m_imgui.Text(buf);
			*std::format_to(buf, "Render Threads: {}", m_render_graph.ThreadCount()) = 0;
			m_imgui.Text(buf);
			*std::format_to(buf, "Terrain Patches: {}", m_terrain.PatchCount()) = 0;
			m_imgui.Text(buf);
			*std::format_to(buf, "Ocean Patches: {}", m_distant_ocean.PatchCount()) = 0;
			m_imgui.Text(buf);

			m_imgui.Separator();

			// Render loop FPS (loop index 1 = variable-step render loop)
			float fps_history[WinGuiMsgLoop::FpsHistory::Length];
			m_ui.m_msg_loop.LoopFps(1).Fps<float>(fps_history);
			*std::format_to(buf, "Render: {:.0f} fps", fps_history[0]) = 0;
			m_imgui.PlotLines(buf, &fps_history[0], _countof(fps_history), 0, nullptr, 0.0f, 120.0f, 200, 40);

			// Sim loop FPS (loop index 0 = fixed-step sim loop)
			m_ui.m_msg_loop.LoopFps(0).Fps<float>(fps_history);
			*std::format_to(buf, "Sim: {:.0f} fps", fps_history[0]) = 0;
			m_imgui.PlotLines(buf, &fps_history[0], _countof(fps_history), 0, nullptr, 0.0f, 120.0f, 200, 40);
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

	// --------------------------------------------------------------------------------------------

	MainUI::MainUI(wchar_t const*, int)
		:base(Params().title(AppTitle()))
	{
		m_msg_loop.m_config.msgs_per_loop = 1;

		// Fixed step simulation at 60Hz, render as fast as possible (capped by vsync/present)
		m_msg_loop.AddLoop(60.0, false, [this](double dt) { if (m_main) m_main->Step(dt); });
		m_msg_loop.AddLoop(120.0, true, [this](double) { if (m_main) m_main->DoRender(true); });
	}

	bool MainUI::ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		// Forward to imgui first
		if (m_main && m_main->m_imgui.WndProc(parent_hwnd, message, wparam, lparam))
		{
			result = 0;
			return true;
		}

		// On WM_CLOSE, tear down the renderer before the HWND is destroyed.
		// DXGI's swap chain can post internal messages that starve WM_QUIT
		// (which has the lowest priority). Destroying 'm_main' releases the
		// swap chain so that PostQuitMessage's WM_QUIT actually gets dequeued.
		if (message == WM_CLOSE)
			m_main.reset();

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