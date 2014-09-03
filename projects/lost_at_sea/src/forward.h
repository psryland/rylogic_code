//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#ifndef LAS_FORWARD_H
#define LAS_FORWARD_H
#pragma once

// Change these values to use different versions
#define  WINVER       0x0501
#define _WIN32_WINNT  0x0600
#define _WIN32_IE     0x0501
#define _RICHEDIT_VER 0x0300

// Change these values to use different versions
#define _WTL_NO_CSTRING

#define DIRECTINPUT_VERSION 0x0800

#include "pr/common/min_max_fix.h"
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlcom.h>
#include <atlmisc.h>
#include <atlddx.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>

#include <d3d11.h>
#include <dsound.h>
#include <sstream>
#include <knownfolders.h>

#include "pr/macros/link.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/d3dptr.h"
#include "pr/common/windows_com.h"
#include "pr/common/fmt.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/common/hash.h"
#include "pr/common/stop_watch.h"
#include "pr/maths/maths.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/str/prstring.h"
#include "pr/str/tostring.h"
#include "pr/script/reader.h"
#include "pr/camera/camera.h"
//#include "pr/camera/camctrl_dinput_wasd.h"
#include "pr/input/dinput.h"
#include "pr/renderer11/renderer.h"
#include "pr/sound/sound.h"
#include "pr/sound/player.h"
#include "pr/sound/ogg/ogg_stream.h"
#include "pr/gui/misc.h"

#define DBG PR_DBG_COMMON
extern CAppModule g_app;

namespace las
{
	namespace EResult
	{
		enum Type
		{
			Success = 0,
			Failed  = 0x80000000,
			StartupFailed,
			SettingsPathNotFound,
			SettingsOutOfDate,
		};
	}
	typedef pr::Exception<EResult::Type> Exception;
	typedef pr::string<> string;
	typedef pr::string<wchar_t> wstring;
	
	struct Main;
	struct MainGUI;
}
#endif
