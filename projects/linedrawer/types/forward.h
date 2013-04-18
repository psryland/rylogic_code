//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_FORWARD_H
#define LDR_FORWARD_H

// Change these values to use different versions
#define  WINVER       0x0501//0x0400//0x0600//
#define _WIN32_WINNT  0x0501//0x0400//0x0600//
#define _WIN32_IE     0x0501//0x0400//0x0700//
#define _RICHEDIT_VER 0x0300//0x0200

// Change these values to use different versions
#define _WTL_NO_CSTRING

#include "pr/common/min_max_fix.h"

// std
#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include <list>

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

// d3d
#include <d3d9.h>
#include <d3dx9.h>

// pr
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
#include "pr/maths/maths.h"
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/camera/camera.h"
#include "pr/gui/menu_helper.h"
#include "pr/gui/recent_files.h"
#include "pr/renderer/renderer.h"
#include "pr/script/script_forward.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_forward.h"

#define LDR_EXPORTS 1
#include "pr/linedrawer/ldr_plugin_interface.h"

namespace ELdrException
{
	enum Type
	{
		NotSpecified,
		FileNotFound,
		FailedToLoad,
		IncorrectVersion,
		InvalidUserSettings,
		SourceScriptError,
		OperationCancelled,
	};
}
typedef pr::Exception<ELdrException::Type> LdrException;
namespace EScreenView
{
	enum Type
	{
		Default,
		Stereo
	};
}
namespace EGlobalRenderMode
{
	enum Type
	{
		Solid,
		Wireframe,
		SolidAndWire,
		NumberOf
	};
}
namespace EMouseButton
{
	enum Type
	{
		Left   = 1 << 0,
		Right  = 1 << 1,
		Middle = 1 << 2
	};
}
namespace ENavMode
{
	enum Type
	{
		Navigation,
		Manipulation
	};
}

// Main app
class  LineDrawerGUI;
class  LineDrawer;
struct UserSettings;
class  PluginManager;
struct Plugin;
typedef std::vector<pr::ldr::ContextId> ContextIdCont;

namespace ldr
{
	char const* AppTitle();
	char const* AppString();
	char const* AppStringLine();
	typedef pr::string<> string;
	typedef std::istream istream;
	typedef std::ostream ostream;
	typedef std::stringstream sstream;
}

#endif
