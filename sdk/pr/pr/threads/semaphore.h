//**********************************
// Semaphre
//  Copyright © Rylogic Ltd 2007
//**********************************
// Notes:
//  A semaphore does not have an owning thread.
//  However, once the semaphore handle has been closed other threads must not wait
//  on the semaphore. It's not possible to receive a WAIT_ABANDONED because the OS does
//  not signal waiting threads when the handle is closed. This means waiting threads will
//  wait forever or timeout. The correct process for shutting down is for the semaphore to
//  be unacquired repeatedly, revived threads to detect the shutdown condition and shutdown
//  gracefully. Once no more threads can wait on the semaphore it's handle can be closed.
//  
//  The state of a semaphore is 'signaled' when its count is greater than zero, and 'nonsignaled'
//  when its count is equal to zero.
//  
//  To temporarily block or reduces access to resources controlled by the semaphore use:
//  for (int i = 0; i != reduce_count; i += sema.Acquire(0)) {}
//  
//  Once done use:
//  sema.UnAcquire(reduce_count);

#pragma once
#ifndef PR_THREADS_SEMAPHORE_H
#define PR_THREADS_SEMAPHORE_H

#include <windows.h>
#include <exception>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)	
#endif

namespace pr
{
	struct Semaphore
	{
		HANDLE m_handle;
		
		Semaphore()
		:m_handle(0)
		{}

		Semaphore(LONG lInitialCount, LONG lMaximumCount, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpSemaphoreAttributes = 0)
		:m_handle(0)
		{
			if (!Initialise(lInitialCount, lMaximumCount, lpName, lpSemaphoreAttributes))
				throw std::exception("semaphore creation failed");
		}
		
		~Semaphore()
		{
			Release();
		}
		
		// Create the semaphore
		bool Initialise(LONG lInitialCount, LONG lMaximumCount, LPCTSTR lpName = 0, LPSECURITY_ATTRIBUTES lpSemaphoreAttributes = 0)
		{
			Release();
			m_handle = CreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName);
			return m_handle != 0;
		}
		
		// Close the semaphore handle.
		// After this no other threads should wait on this semaphore, if they do then it's a design error
		void Release()
		{
			if (m_handle != 0) CloseHandle(m_handle);
			m_handle = 0;
		}
		
		// Returns true if the semaphore was acquired, false if not.
		bool Acquire(DWORD wait_time_ms = INFINITE)
		{
			PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to Acquire() a released semaphore");

			// Wait to acquire a semaphore count
			DWORD res = WaitForSingleObject(m_handle, wait_time_ms);

			// In theory this can't happen, the OS doesn't signal closed semaphore handles
			PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Attempt to Acquire() a semaphore that has been externally released");
			return res == WAIT_OBJECT_0;
		}
		
		// Relinquish a count on a semaphore
		int UnAcquire(int count = 1)
		{
			PR_ASSERT(PR_DBG, m_handle != 0, "Attempt to UnAcquire() a released semaphore");
			LONG prev; ReleaseSemaphore(m_handle, LONG(count), &prev);
			return int(prev);
		}

	private:
		Semaphore(Semaphore const&);
		Semaphore operator =(Semaphore const&);
	};

	// Scoping object for acquiring a count on a semaphore
	struct SemaLock
	{
		Semaphore* m_sema;

		SemaLock()
		:m_sema(0)
		{}
		
		SemaLock(Semaphore& sema)
		:m_sema(&sema)
		{
			if (!m_sema->Acquire())
				throw std::exception("attempt to lock an invalid semaphore");
		}
		
		~SemaLock()
		{
			if (m_sema)
				m_sema->UnAcquire();
		}
		
		// Acquire / Re-Acquire a count on 'sema'
		bool Lock(Semaphore& sema, DWORD wait_time_ms = INFINITE)
		{
			if (m_sema) m_sema->UnAcquire();
			m_sema = &sema;
			return m_sema->Acquire(wait_time_ms);
		}
	};
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
