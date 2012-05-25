//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_MISC_H
#define LDR_MISC_H

#include "linedrawer/types/forward.h"
#include "pr/gui/misc.h"
#include "pr/gui/menu_helper.h"

// Status message priority buffer
struct StatusPri
{
	DWORD m_last_update;
	int   m_priority;
	DWORD m_min_display_time_ms;
	CFont m_normal_font;
	CFont m_bold_font;

	StatusPri()
	:m_last_update(0)
	,m_priority(0)
	,m_min_display_time_ms(0)
	{
		m_normal_font.CreatePointFont(80, "Sans Merif");
		m_bold_font.CreatePointFont(80, "Sans Merif", 0, true);
	}
};

#endif
