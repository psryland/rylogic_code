//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	// Status message priority buffer
	struct StatusPri
	{
		DWORD         m_last_update;
		int           m_priority;
		DWORD         m_min_display_time_ms;
		pr::gui::Font m_normal_font;
		pr::gui::Font m_bold_font;

		StatusPri()
			:m_last_update(0)
			,m_priority(0)
			,m_min_display_time_ms(0)
			,m_normal_font(L"Sans Merif", 80)
			,m_bold_font(L"Sans Merif", 80, 0, true)
		{}
	};
}
