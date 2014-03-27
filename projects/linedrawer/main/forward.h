//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_FORWARD_H
#define LDR_FORWARD_H

#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_gui.h"
#include "pr/app/sim_message_loop.h"

// pr
#include "pr/macros/count_of.h"
#include "pr/macros/enum.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/common/events.h"
#include "pr/common/new.h"
#include "pr/common/timers.h"
#include "pr/common/windows_com.h"
#include "pr/common/keystate.h"
#include "pr/common/colour.h"
#include "pr/maths/maths.h"
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/gui/messagemap_dbg.h"
#include "pr/gui/menu_helper.h"
#include "pr/gui/recent_files.h"
#include "pr/gui/progress_dlg.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/lights/light_dlg.h"
#include "pr/script/script_forward.h"
#include "pr/linedrawer/ldr_forward.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/linedrawer/ldr_tools.h"
#include "pr/network/web_get.h"
#include "pr/storage/xml.h"

#define LDR_EXPORTS 1
#include "pr/linedrawer/ldr_plugin_interface.h"

#define PR_ENUM(x)\
	x(NotSpecified       )\
	x(FileNotFound       )\
	x(FailedToLoad       )\
	x(IncorrectVersion   )\
	x(InvalidUserSettings)\
	x(SourceScriptError  )\
	x(OperationCancelled )
PR_DEFINE_ENUM1(ELdrException, PR_ENUM);
#undef PR_ENUM

#define PR_ENUM(x)\
	x(Solid)\
	x(Wireframe)\
	x(SolidAndWire)
PR_DEFINE_ENUM1(EGlobalRenderMode, PR_ENUM);
#undef PR_ENUM

#define PR_ENUM(x)\
	x(Left   ,= 1 << 0)\
	x(Right  ,= 1 << 1)\
	x(Middle ,= 1 << 2)
PR_DEFINE_ENUM2_FLAGS(EMouseButton, PR_ENUM);
#undef PR_ENUM

#define PR_ENUM(x)\
	x(Navigation  )\
	x(Manipulation)
PR_DEFINE_ENUM1(ENavMode, PR_ENUM);
#undef PR_ENUM

#define PR_ENUM(x)\
	x(Default)\
	x(Stereo)
PR_DEFINE_ENUM1(EScreenView, PR_ENUM);
#undef PR_ENUM

typedef pr::Exception<ELdrException> LdrException;

typedef std::vector<pr::ldr::ContextId> ContextIdCont;

namespace ldr
{
	wchar_t const* AppTitleW();
	char const* AppTitleA();
	char const* AppString();
	char const* AppStringLine();

	typedef pr::string<> string;
	typedef std::istream istream;
	typedef std::ostream ostream;
	typedef std::stringstream sstream;

	struct MainGUI;
	struct Main;
	struct UserSettings;
	class  PluginManager;
	struct Plugin;
	struct NavManager;
}

//hack
typedef ldr::Main LineDrawer;
typedef ldr::MainGUI LineDrawerGUI;

#endif
