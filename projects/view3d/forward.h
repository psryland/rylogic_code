//*********************************************
// View3D
//  Copyright � Rylogic Ltd 2012
//*********************************************
#pragma once

// Change these values to use different versions
#define  WINVER       0x0501//0x0400//
#define _WIN32_WINNT  0x0501//0x0400//
#define _WIN32_IE     0x0501//0x0400//
#define _RICHEDIT_VER 0x0200

#pragma warning (disable: 4995) // deprecated
#pragma warning (disable: 4996) // warning, you are using conforming code instead of microsoft bollox

#define _WTL_NO_CSTRING

#include <exception>
#include <set>
#include <mutex>
#include <thread>

#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>

#include <windows.h>
#include <d3d11.h>

#include "pr/common/min_max_fix.h"
#include "pr/common/fmt.h"
#include "pr/common/log.h"
#include "pr/common/hresult.h"
#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/common/new.h"
#include "pr/filesys/fileex.h"
#include "pr/script/reader.h"
#include "pr/script/embedded_lua.h"
#include "pr/macros/count_of.h"
#include "pr/meta/alignment_of.h"
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/lights/light_dlg.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/linedrawer/ldr_tools.h"