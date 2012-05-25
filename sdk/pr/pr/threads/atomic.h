//**********************************
// Atomic
//  Copyright © Rylogic Ltd 2011
//**********************************
// A lightweight "busy-wait" critical section
// Use:
//  pr::threads::Atom the_right_to_speak;
//  {//in parallel:
//    pr::threads::Atomic lock(the_right_to_speak); // only one thread can get past here at a time
//    printf("Only one in here at a time\n");
//  }
#pragma once
#ifndef PR_THREADS_ATOMIC_H
#define PR_THREADS_ATOMIC_H

#include <windows.h>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	namespace threads
	{
		// This atom allows one thread past once and blocks all else (including the same thread on reentrancy)
		struct Atom0
		{
			Atom0() :m_lock(0) {}
			bool lock()
			{
				return InterlockedCompareExchange(&m_lock, 1, 0) == 0;
			}
			void unlock()
			{
				PR_ASSERT(PR_DBG, m_lock > 0, "pr::thread::Atom0: mismatched lock()/unlock() calls");
				InterlockedDecrement(&m_lock);
			}
			long count() const
			{
				return m_lock;
			}
		private:
			volatile long m_lock;
		};

		// This atom is basically a critical section, it blocks different
		// threads but allows one thread to lock multiple times
		struct Atom1
		{
			Atom1() :m_lock(0) ,m_thread_id(0) {}
			bool lock()
			{
				// Try to acquire the lock first (this is the any thread case)
				if (InterlockedCompareExchange(&m_lock, 1, 0) == 0)
				{
					m_thread_id = ::GetCurrentThreadId(); // save the thread that's got the lock
					return true;
				}
				// If not acquired, check if this is the same thread that currently holds the lock
				if (m_thread_id == ::GetCurrentThreadId())
				{
					InterlockedIncrement(&m_lock); // keep a nesting count
					return true;
				}
				return false;
			}
			void unlock()
			{
				PR_ASSERT(PR_DBG, m_lock > 0, "pr::thread::Atom: mismatched lock()/unlock() calls");
				if (InterlockedDecrement(&m_lock) == 0)
					m_thread_id = 0;
			}
			long count() const
			{
				return m_lock;
			}
		private:
			volatile long m_lock;
			DWORD m_thread_id; // the id of the thread that holds the lock
		};
		
		// A scope for locking an 'atom'
		template <typename AtomType = pr::threads::Atom1> struct Atomic
		{
			AtomType& m_atom;
			
			Atomic(AtomType& atom, DWORD spin_ms = 0)
			:m_atom(atom)
			{
				while (!m_atom.lock())
					Sleep(spin_ms);
			}
			~Atomic()
			{
				m_atom.unlock();
			}
			
		private:
			Atomic(Atomic const&); // no copying
			Atomic& operator =(Atomic const&);
		};
	}
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
