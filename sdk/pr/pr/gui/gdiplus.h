//*******************************************************
// GDI+ helpers
//  Copyright © Rylogic Limited 2009
//*******************************************************

#ifndef PR_GUI_GDIPLUS_H
#define PR_GUI_GDIPLUS_H
#pragma once

#include <windows.h>
#include <gdiplus.h> // remember to link gdiplus.lib
#pragma comment(lib, "gdiplus.lib")

namespace pr
{
	// RAII object for initialised/shutting down the gdiplus framework
	struct GdiPlus
	{
		ULONG_PTR m_token;
		Gdiplus::GdiplusStartupInput  m_startup_input;
		Gdiplus::GdiplusStartupOutput m_startup_output;
		
		GdiPlus()  { Gdiplus::GdiplusStartup(&m_token, &m_startup_input, &m_startup_output); }
		~GdiPlus() { Gdiplus::GdiplusShutdown(m_token); }
	};
	
}

#endif
