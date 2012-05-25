//**************************************************************
// GUI template
//  Copyright © Rylogic Ltd 2011
//**************************************************************

#ifndef PR_GUI_TEMPLATE_FORWARD_H
#define PR_GUI_TEMPLATE_FORWARD_H

// Change these values to use different versions
#define  WINVER       0x0600//0x0501//0x0400//
#define _WIN32_WINNT  0x0600//0x0501//0x0400//
#define _WIN32_IE     0x0700//0x0501//0x0400//
#define _RICHEDIT_VER 0x0300//0x0200

#include "pr/common/atl.h"
#include "pr/common/windows_com.h"
#include "pr/common/hresult.h"
#include "pr/common/windowfunctions.h"
#include "pr/gui/misc.h"
#include "pr/renderer/renderer.h"

namespace gui_template
{
	namespace EResult
	{
		enum Type
		{
			Success       = 0,
			StartupFailed = 0x80000000,
		};
	}
	typedef pr::Exception<EResult::Type> Exception;
}

extern CAppModule g_app;

#endif
