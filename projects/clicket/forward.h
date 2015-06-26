#pragma once

// Change these values to use different versions
#define  WINVER       0x0501//0x0400//
#define _WIN32_IE     0x0501//0x0400//
#define _RICHEDIT_VER 0x0200
#define _UNICODE

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0501
#elif _WIN32_WINNT < 0x0500
#error "_WIN32_WINNT must be >= 0x0500"
#endif

#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlmisc.h>
#include <atlddx.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <strsafe.h>
#include <shellapi.h>

#include "pr/common/min_max_fix.h"
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/common/pollingtoevent.h"
#include "pr/macros/enum.h"
#include "pr/filesys/fileex.h"
#include "pr/str/to_string.h"
#include "pr/gui/windows_com.h"

extern CAppModule _Module;


#define PR_ENUM(x)\
	x(MSec , "msec", = 0)\
	x(Sec  , "sec" ,    )\
	x(Min  , "min" ,    )\
	x(Hr   , "hr"  ,    )
PR_DEFINE_ENUM3(EFreq, PR_ENUM);
#undef PR_ENUM
