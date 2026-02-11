//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once

// std
#include <string>
#include <filesystem>

// pr - app framework (includes windows, view3d-12, gui, camera, etc.)
#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_ui.h"
#include "pr/app/default_setup.h"
#include "pr/app/skybox.h"
#include "pr/gui/sim_message_loop.h"
#include "pr/geometry/p3d.h"
#include "pr/storage/json.h"
#include "pr/maths/perlin_noise.h"
#include "pr/view3d-12/view3d.h"
#include "pr/win32/win32.h"

namespace las
{
	using namespace pr;
	using namespace pr::rdr12;
	using namespace pr::gui;
	using namespace pr::app;

	struct Main;
	struct MainUI;
	struct Settings;
}