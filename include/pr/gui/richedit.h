//*****************************************************************************************
// Rich Edit Control
//	Copyright (c) Rylogic 2009
//*****************************************************************************************

#ifndef PR_GUI_RICHEDIT_H
#define PR_GUI_RICHEDIT_H
#pragma once

#include <richedit.h>

namespace pr
{
	// A scoped instance of the richedit dll
	// To use a richedit control in a dialog use:
	//  WTL::CRichEditCtrl m_edit;
	//  CONTROL_CONTROL("", IDC_TEXT, "RICHEDIT50W", WS_HSCROLL|WS_VSCROLL|ES_AUTOHSCROLL|ES_AUTOVSCROLL|ES_MULTILINE|ES_WANTRETURN, 0, 0, 500, 490, WS_EX_STATICEDGE);
	struct RichEdit5
	{
		HINSTANCE m_redit_dll;
		RichEdit5() :m_redit_dll(::LoadLibrary(TEXT("msftedit.dll"))) {}
		~RichEdit5() { if (m_redit_dll) ::FreeLibrary(m_redit_dll); }
	};
}

#endif

