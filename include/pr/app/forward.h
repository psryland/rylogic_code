//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// Files in the "pr/app/" form a starting point for building line-drawer style graphics
// apps based on pr::gui::wingui and pr::Renderer.
//
#pragma once

// Change these values to use different versions
#ifndef  WINVER
#define  WINVER       0x0600//0x0501//0x0400//
#endif
#ifndef _WIN32_WINNT
#define _WIN32_WINNT  0x0600//0x0501//0x0400//
#endif
#ifndef _WIN32_IE
#define _WIN32_IE     0x0501//0x0400//0x0700//
#endif
#ifndef _RICHEDIT_VER
#define _RICHEDIT_VER 0x0300//0x0200
#endif

// std
#include <iostream>
#include <string>
#include <sstream>
#include <fstream>
#include <vector>
#include <list>
#include <memory>
#include <filesystem>
#include <guiddef.h>
#include <cguid.h>
#include <thread>

// pr
#include "pr/common/min_max_fix.h"
#include "pr/macros/enum.h"
#include "pr/macros/count_of.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/common/event_handler.h"
#include "pr/common/stop_watch.h"
#include "pr/common/keystate.h"
#include "pr/common/colour.h"
#include "pr/common/log.h"
#include "pr/common/new.h"
#include "pr/common/console.h"
#include "pr/maths/maths.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/gui/wingui.h"
#include "pr/gui/misc.h"
#include "pr/gui/menu_list.h"
#include "pr/gui/recent_files.h"
#include "pr/gui/messagemap_dbg.h"
#include "pr/win32/windows_com.h"
#include "pr/script/forward.h"
#include "pr/view3d/renderer.h"

namespace pr::app
{
	#define PR_ENUM(x)\
		x(Success           ,= 0          )\
		x(Failed            ,= 0x80000000 )\
		x(StartupFailed     ,             )\
		x(SettingsNotFound  ,             )\
		x(SettingsOutOfDate ,             )
	PR_DEFINE_ENUM2_BASE(EResult, PR_ENUM, uint);
	#undef PR_ENUM

	using Exception = pr::Exception<EResult>;

	// App interface
	struct IAppMainUI
	{
		virtual ~IAppMainUI() {}
		virtual int Run() = 0;
	};

	// App main base classes
	template <typename Derived, typename MainUI, typename UserSettings> struct Main;
	template <typename DerivedUI, typename Main, typename MessageLoop> struct MainUI;

	// Forward declaration of the function to create the main window instance. Apps must implement this function.
	std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow);
	//{
	//	return std::unique_ptr<IAppMainUI>(new MyAppMainUI(cmdline, nCmdShow));
	//}
}
