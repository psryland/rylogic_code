//*********************************************
// PollingToEvent
//  Copyright (c) Rylogic Ltd 2007
//*********************************************
// This is a self contained thread for polling something
// and calling an event function whenever the polling
// function returns true
//
// Usage:
//  Create a static polling function and event function. The event function
//  is called when the polling function returns true. A good practice is to
//  have these functions post messages to the main thread

#ifndef PR_POLLING_TO_EVENT_H
#define PR_POLLING_TO_EVENT_H

#include <windows.h>
#include <process.h>
#include "pr/common/assert.h"
#include "pr/common/Fmt.h"

namespace pr
{
	typedef bool (*PollingFunction)(void*);
	typedef void (*EventFunction)(void*);
			
	struct PollingToEventSettings
	{
		PollingToEventSettings(
			PollingFunction polling_function = 0,
			EventFunction event_function = 0,
			void* user_data = 0,
			unsigned int polling_frequency = 1000,
			unsigned int stack_size = 0x2000)
		:m_polling_function(polling_function)
		,m_event_function(event_function)
		,m_user_data(user_data)
		,m_polling_frequency(polling_frequency)
		,m_stack_size(stack_size)
		{}
		PollingFunction	m_polling_function;
		EventFunction	m_event_function;
		void*			m_user_data;
		unsigned int	m_polling_frequency;
		unsigned int	m_stack_size;
	};

	//*****
	// Class to contain the polling thread
	class PollingToEvent
	{
	public:
		PollingToEvent(const PollingToEventSettings& settings);
		~PollingToEvent()						{ BlockTillDead(); }
		bool Start();
		void Stop();
		bool Running() const					{ return m_ref_count != 0; }
		void SetFrequency(float step_rate_hz)	{ m_settings.m_polling_frequency = unsigned int(1000.0f / step_rate_hz); }
		bool OkToDelete()						{ return m_thread_handle == 0; }
		void BlockTillDead(int max_loops = 10, DWORD sleep_time = 100)
		{
			PR_ASSERT(PR_DBG, m_ref_count == 0, "This should only be used after the poller has been stopped");
			while( !OkToDelete() && max_loops > 0 ) { Sleep(sleep_time); --max_loops; }
			PR_ASSERT(PR_DBG, max_loops > 0, "Unable to shut down polling thread");
		}

	private:
		static DWORD WINAPI PollingToEventThread(void* parameter);
		void Main();

	private:
		PollingToEventSettings	m_settings;
		HANDLE					m_thread_handle;
		HANDLE					m_terminate_event;
		int						m_ref_count;
	};

	// Implementation ******************************
	
	// Constructor
	inline PollingToEvent::PollingToEvent(const PollingToEventSettings& settings)
	:m_settings(settings)
	,m_thread_handle(0)
	,m_terminate_event(0)
	,m_ref_count(0)
	{}

	// Initialise and start the polling thread
	inline bool PollingToEvent::Start()
	{
		++m_ref_count;
		if( m_ref_count == 1 )
		{
			PR_ASSERT(PR_DBG, m_thread_handle == 0, "");

			// Create an event to terminate
			m_terminate_event = CreateEvent(0, TRUE, FALSE, Fmt(_T("PollingToEvent_%p"), this).c_str());
			if( m_terminate_event == 0 )
			{
				PR_INFO(PR_DBG, "Failed to create terminate event");
				Stop();
				return false;
			}

			// Create the polling thread
			m_thread_handle = CreateThread(0, m_settings.m_stack_size, PollingToEventThread, this, 0, 0);
			if( m_thread_handle == 0 )
			{
				PR_INFO(PR_DBG, "Failed to create thread");
				Stop();
				return false;
			}
			SetThreadPriority(m_thread_handle, THREAD_PRIORITY_BELOW_NORMAL);
		}
		return true;
	}

	// Stop the polling thread
	inline void PollingToEvent::Stop()
	{
		if( m_ref_count > 0 )
		{
			if( --m_ref_count == 0 )
			{
				if( m_terminate_event )
				{
					SetEvent(m_terminate_event);
				}
			}
		}
	}
		
	// Static thread start point
	inline DWORD WINAPI PollingToEvent::PollingToEventThread(void* parameter)
	{
		PollingToEvent* poller = static_cast<PollingToEvent*>(parameter);

		poller->Main();
		CloseHandle(poller->m_terminate_event);
		poller->m_terminate_event = 0;
		poller->m_thread_handle = 0;
		return 0;
	}

	// Main loop for polling
	inline void PollingToEvent::Main()
	{
		while( WaitForSingleObject(m_terminate_event, m_settings.m_polling_frequency) == WAIT_TIMEOUT )
		{
			// If the polling function returns true, Call the event function
			if( m_settings.m_polling_function && m_settings.m_polling_function(m_settings.m_user_data) )
			{
				if( m_settings.m_event_function ) m_settings.m_event_function(m_settings.m_user_data);
			}
		}
	}
}//namespace pr

#endif//PR_POLLING_TO_EVENT_H
