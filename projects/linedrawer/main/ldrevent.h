//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_EVENT_H
#define LDR_EVENT_H

#include "linedrawer/main/forward.h"

namespace ldr
{
	// Ldr event base type
	struct Event
	{
		std::string m_msg;
		Event(std::string const& msg) :m_msg(msg) {}
	};

	// Events containing general information as line drawer runs
	// intended for log files or whatever
	struct Event_Info :Event
	{
		Event_Info(std::string const& msg) :Event(msg) {}
	};

	// Events for conditions that don't need to interrupt the user
	// but are useful for them to know.
	struct Event_Warn :Event
	{
		Event_Warn(std::string const& msg) :Event(msg) {}
	};

	// Events that should reported to the user
	struct Event_Error :Event
	{
		Event_Error(std::string const& msg) :Event(msg) {}
	};

	// Status bar text
	struct Event_Status :Event
	{
		bool  m_bold;
		int   m_priority;
		DWORD m_min_display_time_ms;

		Event_Status(char const* msg, bool bold = false, int priority = 0, DWORD min_display_time_ms = 200)
		:Event(msg)
		,m_bold(bold)
		,m_priority(priority)
		,m_min_display_time_ms(min_display_time_ms)
		{}
	};

	// Event to signal a refresh of the display
	struct Event_Refresh
	{
	};
}

#endif
