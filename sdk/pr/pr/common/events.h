//******************************************
// Event sending/receiving 
//  Copyright © Oct 2003 Paul Ryland
//******************************************

#pragma once
#ifndef PR_COMMON_EVENTS_H
#define PR_COMMON_EVENTS_H

#include "pr/common/chain.h"
#include "pr/threads/atomic.h"

#pragma warning (push)
#pragma warning (disable:4127 4101) // constant conditional, 'lock' unreferenced

namespace pr
{
	namespace events
	{
		// Base class for receiving an event of a specific type
		template <typename EventType> struct IRecv
		{
			typedef pr::chain::Link< IRecv<EventType> > Link;
			Link  m_link;      // The chain link
			DWORD m_thread_id; // The thread that subscribed to the events
			int   m_priority;
			
			// The event handler
			virtual void OnEvent(EventType const& e) = 0;
			
			IRecv(int priority = 0, bool subscribe_immediately = true) :m_thread_id(0) ,m_priority(priority)
			{
				m_link.init(this);
				if (subscribe_immediately) subscribe();
			}
			IRecv(IRecv const& rhs) :m_thread_id(0) ,m_priority(rhs.m_priority)
			{
				m_link.init(this);
				if (!rhs.m_link.empty()) subscribe();
			}
			IRecv& operator = (IRecv const& rhs)
			{
				if (&rhs == this) return *this;
				unsubscribe();
				m_link.init(this);
				if (!rhs.m_link.empty()) subscribe();
			}
			virtual ~IRecv()
			{
				unsubscribe();
			}
			void subscribe()
			{
				unsubscribe();
				pr::threads::Atomic<> lock(Atom());
				m_thread_id = GetCurrentThreadId();
				Link* i; for (i = Chain().begin(); i != Chain().end() && i->m_owner->m_priority > m_priority; i = i->m_next) {}
				pr::chain::Insert(*i, m_link);
			}
			void unsubscribe()
			{
				pr::threads::Atomic<> lock(Atom());
				pr::chain::Remove(m_link);
				m_thread_id = 0;
			}
			bool same_thread() const
			{
				return GetCurrentThreadId() == m_thread_id;
			}
			
			// The EventType receivers chain
			static Link& Chain()
			{
				static Link chain;
				return chain;
			}
			
			// A primitive for synchronising access to the EventType chain
			static pr::threads::Atom1& Atom()
			{
				static pr::threads::Atom1 atom;
				return atom;
			}
		};

		//// Helper functions for making subscribing/unsubscribing syntactically nicer
		//// Use: pr::events::subscribe<Event>(*this);
		//template <typename EventType, typename RecvClass>  inline void subscribe  (RecvClass& orig) { static_cast<IRecv<EventType>&>(orig).subscribe(); }
		//template <typename EventType, typename RecvClass>  inline void unsubscribe(RecvClass& orig) { static_cast<IRecv<EventType>&>(orig).unsubscribe(); }
 
		// Send an event to all receivers of 'EventType'
		// - Sending events is synchronous for each event type
		// - OnEvent handlers will be called in *this* thread context which may
		//   be a different thread context to that which signed up for the events.
		//   Use 'SameThread()' to test for this case if necessary
		template <typename EventType> inline void Send(EventType const& e, bool forward = true)
		{
			// Prevent any re-entrancy or modification to the EventType receivers chain
			pr::threads::Atomic<> lock(IRecv<EventType>::Atom());
			
			// Loop over receivers of this event type
			IRecv<EventType>::Link *i, &head = IRecv<EventType>::Chain();
			if (forward)
			{
				for (i = head.begin(); i != head.end(); i = i->m_next)
					i->m_owner->OnEvent(e);
			}
			else
			{
				for (i = head.end()->m_prev; i != head.end(); i = i->m_prev)
					i->m_owner->OnEvent(e);
			}
		}
	}
}

#pragma warning (pop)

#endif


