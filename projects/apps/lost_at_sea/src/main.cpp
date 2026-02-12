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
	{
		// Position the camera: looking forward (+X) from above the ocean
		m_cam.LookAt(v4(0, 0, 15, 1), v4(50, 0, 0, 1), v4(0, 0, 1, 0));
		m_cam.Align(v4ZAxis);

		m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1, _2);

		// Build initial meshes
		m_ocean.Update(0.0f, m_camera_world_pos);
		m_terrain.Update(m_camera_world_pos);
	}

	Main::~Main()
	{
		m_scene.ClearDrawlists();
	}

	void Main::Step(double elapsed_seconds)
	{
		m_sim_time += elapsed_seconds;

		// Simulation: compute new vertex positions on CPU
		m_ocean.Update(static_cast<float>(m_sim_time), m_camera_world_pos);
		m_terrain.Update(m_camera_world_pos);

		RenderNeeded();
	}

	void Main::UpdateScene(Scene& scene, UpdateSceneArgs const& args)
	{
		// Rendering: upload dirty data to GPU and add instances
		m_skybox.AddToScene(scene);
		m_ocean.AddToScene(scene, args.m_cmd_list, args.m_upload);
		m_terrain.AddToScene(scene, args.m_cmd_list, args.m_upload);
	}

	MainUI::MainUI(wchar_t const*, int)
		:base(Params().title(AppTitle()))
	{
		m_msg_loop.AddStepContext("render", [this](double) { m_main->DoRender(true); }, 60.0f, false);
		m_msg_loop.AddStepContext("step", [this](double s) { m_main->Step(s); }, 60.0f, true);
	}
}

namespace pr::app
{
	std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow)
	{
		return std::unique_ptr<IAppMainUI>(new las::MainUI(lpstrCmdLine, nCmdShow));
	}
}