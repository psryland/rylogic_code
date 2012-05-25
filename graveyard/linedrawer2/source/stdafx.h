//*******************************************************************************************
//
// Standard includes for LineDrawer
//
//*******************************************************************************************

#ifndef STDAFX_H
#define STDAFX_H

// Required preprocessor defines:
//	;NOMINMAX ;VC_EXTRALEAN ;WINVER=0x0500 ;_CRT_SECURE_NO_DEPRECATE ;_SCL_SECURE_NO_DEPRECATE

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions
//#include <afxdlgs.h>
//#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
//#ifndef _AFX_NO_AFXCMN_SUPPORT
//#include <afxcmn.h>			// MFC support for Windows Common Controls
//#endif // _AFX_NO_AFXCMN_SUPPORT
//#include <afxsock.h>		// MFC socket extensions

#define USE_NEW_PARSER 0
#define USE_OLD_PARSER 1

#include "pr/common/assert.h"
#include "pr/common/PRString.h"
#include "pr/common/Fmt.h"
#include "pr/common/Console.h"
#include "pr/common/MsgBox.h"
#include "pr/common/ValueCast.h"
#include "pr/maths/maths.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Source/LineDrawerAssertEnable.h"

#pragma warning (disable: 4355) // 'this' used in construction
using namespace pr;

#endif//STDAFX_H
