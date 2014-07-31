//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	enum class EMsgLevel {Info, Error};

	// Base class for error/warning/etc messages
	struct Event
	{
		std::string m_msg;
		EMsgLevel   m_level;
		std::exception const* m_except;

		Event(std::string const& msg, EMsgLevel lvl = EMsgLevel::Error, std::exception const* ex = nullptr)
			:m_msg(msg)
			,m_level(lvl)
			,m_except(ex)
		{}
		virtual ~Event() {}

	private:
		Event(Event const&);
		Event& operator=(Event const&);
	};

	// Events containing general information as line drawer runs intended for log files or whatever
	struct Event_Info :Event
	{
		Event_Info(std::string const& msg) :Event(msg) {}
	};

	// Events for conditions that don't need to interrupt the user but are useful for them to know.
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

	// Event raised when the store of ldr objects is added to or removed from
	struct Event_StoreChanged
	{
		// The store that was changed
		pr::ldr::ObjectCont const* m_store;

		Event_StoreChanged(pr::ldr::ObjectCont const& store) :m_store(&store) {}
	};
}
