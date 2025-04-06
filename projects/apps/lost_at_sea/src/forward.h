//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#pragma once

#include <sstream>
#include <filesystem>

#include <sdkddkver.h>
#include <windows.h>
#include <knownfolders.h>
#include <mmreg.h>
#include <dsound.h>
#include <d3d11.h>

#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_ui.h"
#include "pr/app/skybox.h"
#include "pr/gui/wingui.h"
#include "pr/gui/sim_message_loop.h"
//#include "pr/macros/link.h"
#include "pr/macros/enum.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
//#include "pr/common/hresult.h"
//#include "pr/common/d3dptr.h"
#include "pr/common/fmt.h"
#include "pr/common/event_handler.h"
#include "pr/str/to_string.h"
//#include "pr/common/refcount.h"
//#include "pr/common/refptr.h"
//#include "pr/common/hash.h"
//#include "pr/common/stop_watch.h"
#include "pr/maths/maths.h"
//#include "pr/filesys/filesys.h"
//#include "pr/str/prstring.h"
//#include "pr/str/tostring.h"
//#include "pr/script/reader.h"
//#include "pr/camera/camera.h"
////#include "pr/camera/camctrl_dinput_wasd.h"
//#include "pr/input/dinput.h"
#include "pr/view3d/renderer.h"
#include "pr/storage/settings.h"
#include "pr/audio/directsound/sound.h"
#include "pr/audio/directsound/player.h"
#include "pr/audio/ogg/ogg_stream.h"
//#include "pr/gui/misc.h"
#include "pr/win32/windows_com.h"
#include "pr/win32/win32.h"

#define DBG PR_DBG_COMMON

namespace las
{
	using namespace pr;
	using namespace pr::rdr;
	using namespace pr::gui;
	using namespace pr::app;

	enum class EResult : UINT
	{
		#define PR_ENUM(x) \
		x(Success             ,= 0         )\
		x(Failed              ,= 0x80000000)\
		x(StartupFailed       ,            )\
		x(SettingsPathNotFound,            )\
		x(SettingsOutOfDate   ,            )
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EResult, PR_ENUM);
	#undef PR_ENUM

	using Exception = pr::Exception<EResult>;

	struct Main;
	struct MainUI;

	wchar_t const* AppVersionW();
	char const*    AppVersionA();
	wchar_t const* AppVendor();
	wchar_t const* AppCopyright();
}
