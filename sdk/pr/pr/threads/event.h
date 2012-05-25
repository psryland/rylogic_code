//**********************************
// System event
//  Copyright © Rylogic Ltd 2007
//**********************************
// Notes:
//  An event is 'owned' by the creating thread
//  If the event is released by that thread, any other threads waiting on the event
//  will receive WAIT_ABANDONED. Receiving a 'WAIT_ABANDONED' indicates a design error!
//  On shutdown, threads waiting on the event should be signaled, detect the shutdown
//  condition, then exit gracefully. Only after all threads that can possibly wait on
//  the event have shutdown should the owning thread release the event.
	
#pragma once
#ifndef PR_THREADS_EVENT_H
#define PR_THREADS_EVENT_H
	
#include <windows.h>
#include <exception>
	
//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	namespace threads
	{
		struct Event
		{
			HANDLE m_handle;
			
			Event()
			:m_handle(0)
			{}
			
			Event(BOOL bManualReset, BOOL bInitialState, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpEventAttributes = 0)
			:m_handle(0)
			{
				if (!Initialise(bManualReset, bInitialState, lpName, lpEventAttributes))
					throw std::exception("event creation failed");
			}
			
			~Event()
			{
				Release();
			}
			
			// Create the event
			bool Initialise(BOOL bManualReset, BOOL bInitialState, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpEventAttributes = 0)
			{
				Release();
				m_handle = CreateEvent(lpEventAttributes, bManualReset, bInitialState, lpName);
				return m_handle != 0;
			}
			
			// Close the event handle.
			// After this no other threads should wait on this event, if they do then it's a design error
			void Release()
			{
				if (m_handle != 0) CloseHandle(m_handle);
				m_handle = 0;
			}
			
			// Reset the event to non-signalled
			void Reset()
			{
				PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to Reset() a released event");
				::ResetEvent(m_handle);
			}
			
			// Set the event to the signalled state
			void Signal()
			{
				PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to Signal() a released event");
				::SetEvent(m_handle);
			}
			
			// Wait for this event to become signalled
			bool Wait(DWORD wait_time_ms = INFINITE)
			{
				PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to Wait() on a released event");
				
				// Wait for the event to be signalled
				DWORD res = ::WaitForSingleObject(m_handle, wait_time_ms);
				
				// In theory this can't happen, if 'm_handle' is not null then the event handle
				// should not have been closed. See Notes: above about WAIT_ABANDONED
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Attempt to Wait() on an event that has been externally released");
				return res == WAIT_OBJECT_0;
			}
			
		private:
			Event(Event const&);
			Event operator =(Event const&);
		};
	}
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
