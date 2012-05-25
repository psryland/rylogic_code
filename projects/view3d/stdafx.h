//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_VIEW3D_STDAFX_H
#define PR_VIEW3D_STDAFX_H

// Change these values to use different versions
#define  WINVER       0x0501//0x0400//
#define _WIN32_WINNT  0x0501//0x0400//
#define _WIN32_IE     0x0501//0x0400//
#define _RICHEDIT_VER 0x0200

#pragma warning (disable: 4995) // deprecated
#pragma warning (disable: 4996) // warning, you are using conforming code instead of microsoft bollox

#define _WTL_NO_CSTRING

#include <pr/common/min_max_fix.h>
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
//#include <atlmisc.h>
//#include <atlddx.h>
//#include <atlframe.h>
//#include <atldlgs.h>
//#include <windows.h>
//#include <strsafe.h>

#include "pr/common/fmt.h"
#include "pr/maths/maths.h"

#endif
