//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once

// std
#include <string>
#include <filesystem>

// Windows
#include <windows.h>
#include <mmsystem.h>
#pragma comment(lib, "winmm.lib")

// pr - app framework (includes windows, view3d-12, gui, camera, etc.)
#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_ui.h"
#include "pr/app/default_setup.h"
#include "pr/app/skybox.h"
#include "pr/common/keystate.h"
#include "pr/common/resource.h"
#include "pr/geometry/p3d.h"
#include "pr/storage/json.h"
#include "pr/maths/perlin_noise.h"
#include "pr/view3d-12/view3d.h"
#include "pr/physics-2/physics.h"
#include "pr/win32/win32.h"

using namespace pr;

namespace las
{
	using namespace pr;
	using namespace pr::rdr12;
	using namespace pr::physics;
	using namespace pr::collision;
	using namespace pr::gui;
	using namespace pr::app;

	struct Main;
	struct MainUI;
	struct Settings;
}