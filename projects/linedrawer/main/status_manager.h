//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	// Status message priority buffer
	struct StatusManager
	{
		// Status priorities work like this:
		// Status' without timeouts overwrite other non-timed status'.
		// Status' with timeouts cause the last non-timed status to be saved, then they display for their time period.
		// Successive timed-status' overwrite both timed and non-timed status'.
		// Non-timed status' don't overwrite timed-status.
		Evt_Status   m_curr;
		Evt_Status   m_prev;
		Font         m_font_normal;
		Font         m_font_bold;
		DWORD        m_display_start;
		StatusBar*   m_sb;           // The status bar to apply the status too

		StatusManager(StatusBar& sb)
			:m_curr(L"Idle")
			,m_prev(L"Idle")
			,m_font_normal(Control::DefaultStatusFont())
			,m_font_bold(m_font_normal, FW_BOLD)
			,m_display_start()
			,m_sb(&sb)
		{}

		// Update the status bar with the given status
		void Set(Evt_Status const& status)
		{
			m_sb->Text(0, status.m_msg);
			m_sb->Font(status.m_bold ? m_font_bold : m_font_normal);
			m_sb->ForeColor(status.m_col);
		}

		// Apply 'next' status
		void Apply(Evt_Status const& next)
		{
			// The previous status should always be the last non-timed status.
			// Only replace 'm_prev' with non-timed statuses.
			if (!m_curr.IsTimed())
				m_prev = m_curr;

			// The next status replaces the current status if the current
			// is non-timed or the next status is timed.
			if (!m_curr.IsTimed() || next.IsTimed())
				m_curr = next;

			// Update the status bar with the given status
			Set(m_curr);

			// Record the time that this status was applied
			m_display_start = GetTickCount();
		}

		// Check for timed out timed-statuses and update the status bar as needed
		void Update()
		{
			if (!m_curr.IsTimed())
				return;

			// Restore the prevent status when the current one times out
			if (GetTickCount() - m_display_start > m_curr.m_duration_ms)
			{
				m_curr = m_prev;
				Set(m_prev);
			}
		}
	};
}
