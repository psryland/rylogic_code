//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"
#include "src/world/ocean.h"

namespace las
{
// Main application logic
struct Main :pr::app::Main<Main, MainUI, Settings>
{
using base = pr::app::Main<Main, MainUI, Settings>;
using Skybox = pr::app::Skybox;

static char const* AppName() { return "LostAtSea"; }

Skybox m_skybox;
Ocean m_ocean;

// Simulation clock (seconds since start)
double m_sim_time;

// Camera world position (for the camera-at-origin pattern)
v4 m_camera_world_pos;

Main(MainUI& ui);
~Main();

// Advance the game by one frame
void Step(double elapsed_seconds);

// Add instances to the scene
void UpdateScene(Scene& scene);
};

// Main app window
struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>
{
using base = pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>;
static wchar_t const* AppTitle() { return L"Lost at Sea"; }

MainUI(wchar_t const* lpstrCmdLine, int nCmdShow);
};

// Inline implementations

inline Main::Main(MainUI& ui)
:base(pr::app::DefaultSetup(), ui)
,m_skybox(m_rdr, L"data\\skybox\\SkyBox-Clouds-Few-Noon.png", Skybox::EStyle::FiveSidedCube, 100.0f)
,m_ocean(m_rdr)
,m_sim_time(0.0)
,m_camera_world_pos(v4(0, 0, 10, 1)) // Start 10m above water
{
// Position camera looking down at ocean
m_cam.LookAt(v4(0, 0, 10, 1), v4(10, 0, 0, 1), v4(0, 0, 1, 0));

// Watch for scene drawlist updates
m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1);

// Build the initial ocean mesh
m_ocean.Update(0.0f, m_camera_world_pos);
}
inline Main::~Main()
{
m_scene.ClearDrawlists();
}

inline void Main::Step(double elapsed_seconds)
{
m_sim_time += elapsed_seconds;
m_ocean.Update(static_cast<float>(m_sim_time), m_camera_world_pos);
RenderNeeded();
}

inline void Main::UpdateScene(Scene& scene)
{
m_skybox.AddToScene(scene);
m_ocean.AddToScene(scene);
}

inline MainUI::MainUI(wchar_t const*, int)
:base(Params().title(AppTitle()))
{
m_msg_loop.AddStepContext("render", [this](double) { m_main->DoRender(true); }, 60.0f, false);
m_msg_loop.AddStepContext("step", [this](double s) { m_main->Step(s); }, 60.0f, true);
}
}

namespace pr::app
{
inline std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow)
{
return std::unique_ptr<IAppMainUI>(new las::MainUI(lpstrCmdLine, nCmdShow));
}
}