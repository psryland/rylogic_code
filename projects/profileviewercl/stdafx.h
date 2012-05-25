// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows XP or later.                   
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target other versions of Windows.
#endif						

#define PR_PROFILE_ON
#define PR_TIMERS_USE_PERFORMANCE_COUNTER 1

#pragma warning(disable:4355)

#include <stdio.h>
#include <tchar.h>
#include <windows.h>
#include "pr/common/pipe.h"
#include "pr/common/console.h"
#include "pr/common/profile.h"
#include "pr/common/profilemanager.h"
#include "pr/threads/critical_section.h"

using namespace pr;
using namespace pr::profile;
