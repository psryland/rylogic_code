// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently

#pragma once

// Required preprocessor defines:
// ;RI_PLATFORM=PC ;VC_EXTRALEAN ;_WIN32_WINNT=0x0500

#define RYLOGIC_PHYSICS 0
#define REFLECTIONS_PHYSICS 1
#define REFLECTIONS_PHYSICS_OLD 2
//#define PHYSICS_ENGINE REFLECTIONS_PHYSICS

#if PHYSICS_ENGINE==REFLECTIONS_PHYSICS
#	define RI_PLATFORM PC
#	ifdef _DEBUG
#		define WIN32_DEBUG_BUILD
#	else
#		define WIN32_DEVKIT_BUILD
#	endif
#	pragma warning (disable : 4311 4312)
#	include "Shared/Include/BuildConfigs.h"
#endif//USE_REFLECTIONS_ENGINE

//// Modify the following defines if you have to target a platform prior to the ones specified below.
//// Refer to MSDN for the latest info on corresponding values for different platforms.
//#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
//#define WINVER 0x0400		// Change this to the appropriate value to target Windows 98 and Windows 2000 or later.
//#endif
//
//#ifndef _WIN32_WINNT		// Allow use of features specific to Windows NT 4 or later.
//#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target Windows 2000 or later.
//#endif						

//#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
//#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
//#endif
//
//#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
//#define _WIN32_IE 0x0400	// Change this to the appropriate value to target IE 5.0 or later.
//#endif

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxole.h>         // MFC OLE classes
#include <afxodlgs.h>       // MFC OLE dialog classes
#include <afxdisp.h>        // MFC Automation classes
#endif // _AFX_NO_OLE_SUPPORT

#ifndef _AFX_NO_DB_SUPPORT
#include <afxdb.h>			// MFC ODBC database classes
#endif // _AFX_NO_DB_SUPPORT

#ifndef _AFX_NO_DAO_SUPPORT
#include <afxdao.h>			// MFC DAO database classes
#endif // _AFX_NO_DAO_SUPPORT

#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>			// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT

#pragma warning (disable : 4355) // 'this' used in constructor
#pragma warning (disable : 4351) // new behavior: elements of array 'X' will be default initialized

#include <string>
#include <vector>
#include <list>
#include <map>
#include "pr/maths/maths.h"
#include "pr/common/fmt.h"
#include "pr/common/array.h"
#include "pr/common/timers.h"
#include "pr/common/profile.h"
#include "pr/common/profilemanager.h"
#include "pr/common/alloca.h"
#include "pr/common/valuecast.h"
#include "pr/common/colour.h"
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/autofile.h"
#include "pr/linedrawer/ldr_helper.h"
//#include "pr/linedrawer/plugininterface.h"

#ifdef USE_REFLECTIONS_ENGINE
#include "physicstestbed/riheaders.h"
#include "physicstestbed/prtoriconversions.h"
#endif//USE_REFLECTIONS_ENGINE
