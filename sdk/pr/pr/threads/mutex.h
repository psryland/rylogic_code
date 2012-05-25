//**********************************
// Mutex
//  Copyright © Rylogic Ltd 2007
//**********************************
// Notes:
//  A mutex is 'owned' by the creating thread
//  If the mutex is released by that thread, any other threads waiting on the mutex
//  will receive WAIT_ABANDONED. Receiving a 'WAIT_ABANDONED' indicates a design error!
//  On shutdown, threads should acquire the mutex, detect the shutdown condition, then
//  exit gracefully. Only after all threads that can possibly wait on the mutex have
//  shutdown should the owning thread release the mutex.
#pragma once
#ifndef PR_THREADS_MUTEX_H
#define PR_THREADS_MUTEX_H

#include <windows.h>
#include <exception>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)	
#endif

namespace pr
{
	struct Mutex
	{
		HANDLE m_handle;
		
		Mutex()
		:m_handle(0)
		{}

		Mutex(BOOL bInitialOwner, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpMutexAttributes = 0)
		:m_handle(0)
		{
			if (!Initialise(bInitialOwner, lpName, lpMutexAttributes))
				throw std::exception("mutex creation failed");
		}
		
		~Mutex()
		{
			Release();
		}
		
		// Create the mutex. The calling thread is the owner of this mutex
		bool Initialise(BOOL bInitialOwner, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpMutexAttributes = 0)
		{
			Release();
			m_handle = CreateMutex(lpMutexAttributes, bInitialOwner, lpName);
			return m_handle != 0;
		}
		
		// Close the mutex handle.
		// After this no other threads should wait on this mutex, if they do then it's a design error
		void Release()
		{
			if (m_handle != 0) CloseHandle(m_handle);
			m_handle = 0;
		}
		
		// Returns true if the mutex was acquired, false if timed out
		bool Acquire(DWORD wait_time_ms = INFINITE)
		{
			PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to Acquire() a released mutex");
			
			// Wait on the mutex
			DWORD res = WaitForSingleObject(m_handle, wait_time_ms);
			
			// In theory this can't happen, if 'm_handle' is not null then the mutex handle
			// should not have been closed. See Notes: above about WAIT_ABANDONED
			PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Attempt to Acquire() a mutex that has been externally released");
			return res == WAIT_OBJECT_0;
		}
		
		// Relinquish the lock on a mutex
		void UnAcquire()
		{
			PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to UnAcquire() a released mutex");
			ReleaseMutex(m_handle);
		}
		
	private:
		Mutex(Mutex const&);
		Mutex operator =(Mutex const&);
	};
	
	// Scoping object for acquiring a mutex
	struct MutexLock
	{
		Mutex* m_mutex;
		
		MutexLock()
		:m_mutex(0)
		{}
		
		MutexLock(Mutex& mutex)
		:m_mutex(&mutex)
		{
			if (!m_mutex->Acquire())
				throw std::exception("attempt to lock an invalid mutex");
		}
		
		~MutexLock()
		{
			if (m_mutex)
				m_mutex->UnAcquire();
		}
		
		// Acquire / Re-Acquire a lock on 'mutex'
		bool Lock(Mutex& mutex, DWORD wait_time_ms = INFINITE)
		{
			if (m_mutex) m_mutex->UnAcquire();
			m_mutex = &mutex;
			return m_mutex->Acquire(wait_time_ms);
		}
	};
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
