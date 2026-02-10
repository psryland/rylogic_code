//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once
#include "src/forward.h"
#include "src/settings.h"

namespace las
{
// Main application logic
struct Main :pr::app::Main<Main, MainUI, Settings>
{
using base = pr::app::Main<Main, MainUI, Settings>;
using Skybox = pr::app::Skybox;

static char const* AppName() { return "LostAtSea"; }

Skybox m_skybox;

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
{
// Watch for scene drawlist updates
m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1);
}
inline Main::~Main()
{
m_scene.ClearDrawlists();
}

inline void Main::Step(double elapsed_seconds)
{
(void)elapsed_seconds;
}

inline void Main::UpdateScene(Scene& scene)
{
m_skybox.AddToScene(scene);
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