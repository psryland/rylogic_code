//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#pragma once

#include <winsdkver.h>
#include <sdkddkver.h>

#include <stdexcept>
#include <filesystem>
#include <functional>
#include <format>

#include <windows.h>
#include <shlobj_core.h>

#include "pr/common/hresult.h"
#include "pr/common/scope.h"
#include "pr/container/suffix_array.h"
#include "pr/storage/json.h"
#include "pr/gui/wingui.h"
#include "pr/win32/user_directories.h"
#include "pr/win32/windows_com.h"

namespace ui = pr::gui;
namespace blitzsearch
{
	struct MainUI;
}
