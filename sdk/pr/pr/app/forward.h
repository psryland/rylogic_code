//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
// Files in the "pr/app/" form a starting point for building line-drawer style graphics
// apps based on WTL and pr::Renderer.
//
#pragma once
#ifndef PR_APP_FORWARD_H
#define PR_APP_FORWARD_H

// Change these values to use different versions
#ifndef  WINVER
#define  WINVER       0x0501//0x0400//0x0600//
#endif
#ifndef _WIN32_WINNT
#define _WIN32_WINNT  0x0501//0x0400//0x0600//
#endif
#ifndef _WIN32_IE
#define _WIN32_IE     0x0501//0x0400//0x0700//
#endif
#ifndef _RICHEDIT_VER
#define _RICHEDIT_VER 0x0300//0x0200
#endif

// Change these values to use different versions
#define _WTL_NO_CSTRING

#include "pr/common/min_max_fix.h"

// std
#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include <list>
#include <memory>
#include <guiddef.h>
#include <cguid.h>

// wtl
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
#include <atlcom.h>
#include <atlmisc.h>
#include <atlddx.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>
#include <shellapi.h>

// pr
#include "pr/macros/enum.h"
#include "pr/macros/count_of.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/common/events.h"
#include "pr/common/timers.h"
#include "pr/common/windows_com.h"
#include "pr/common/keystate.h"
#include "pr/common/colour.h"
#include "pr/common/log.h"
#include "pr/common/console.h"
#include "pr/maths/maths.h"
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/gui/misc.h"
#include "pr/gui/menu_helper.h"
#include "pr/gui/recent_files.h"
#include "pr/script/script_forward.h"
#include "pr/renderer11/renderer.h"
#include "pr/threads/thread.h"
#include "pr/threads/thread_pool.h"

namespace pr
{
	namespace app
	{
		#define PR_ENUM(x)\
			x(Success           ,= 0          )\
			x(Failed            ,= 0x80000000 )\
			x(StartupFailed     ,             )\
			x(SettingsNotFound  ,             )\
			x(SettingsOutOfDate ,             )
		PR_DEFINE_ENUM2(EResult, PR_ENUM);
		#undef PR_ENUM

		typedef pr::Exception<EResult> Exception;
		typedef pr::string<wchar_t> wstring;
		typedef pr::string<char>    string;

		template <typename DerivedGUI, typename Main, typename MessageLoop> struct MainGUI;
		template <typename UserSettings, typename MainGUI> struct Main;
		CAppModule& Module();
	}
}

#endif
