//******************************************
// Event sending/receiving 
//  Copyright © Oct 2003 Paul Ryland
//******************************************

#pragma once
#ifndef PR_COMMON_EVENTS_H
#define PR_COMMON_EVENTS_H

#include <cassert>
#include <mutex>
#include <thread>
#include <type_traits>
#include "pr/common/chain.h"

#pragma warning (push)
#pragma warning (disable:4127 4101) // constant conditional, 'lock' unreferenced

namespace pr
{
	namespace events
	{
		namespace EControlFlags
		{
			enum
			{
				ChainLocked  = 1 << 0,
				Pending      = 1 << 1,
				Remove       = 1 << 2,
			};
		}

		// Base class for receiving an event of a specific type
		template <typename EventType> struct IRecv
		{
			typedef pr::chain::Link<IRecv<EventType>> Link;

			Link            m_link;      // The chain link
			int             m_priority;  // Event priority
			int             m_control;   // Control flags
			std::thread::id m_thread_id; // The thread that subscribed to the events

			// The event handler
			virtual void OnEvent(EventType const& e) = 0;

			IRecv(int priority = 0, bool subscribe_immediately = true) :m_priority(priority) ,m_control(), m_thread_id()
			{
				m_link.init(this);
				if (subscribe_immediately) subscribe();
			}
			IRecv(IRecv const& rhs) :m_priority(rhs.m_priority) ,m_control() ,m_thread_id()
			{
				m_link.init(this);
				if (rhs.is_subscribed()) subscribe();
			}
			IRecv(IRecv&& rhs) :m_thread_id(rhs.m_thread) ,m_priority(rhs.m_priority) ,m_control(rhs.m_control)
			{
				m_link.init(this);
				swap(m_link, rhs.m_link);
			}
			virtual ~IRecv()
			{
				// Note: if a receiver is added to the pending queue during a
				// Send() call, this unsubscribe call will remove it.
				unsubscribe();
				assert(!is_subscribed() && "can't delete objects in the live event chain while 'Send' is in progress");
			}

			IRecv& operator = (IRecv const& rhs)
			{
				if (&rhs == this) return *this;
				unsubscribe();
				m_link.init(this);
				if (rhs.is_subscribed()) subscribe();
			}
			IRecv& operator = (IRecv&& rhs)
			{
				swap(m_link, rhs.m_link);
			}

			// Returns true if the current thread is the same thread that this receiver was subscribed on
			bool same_thread() const
			{
				return std::this_thread::get_id() == m_thread_id;
			}

			// Returns true if this event receiver is current in an event chain
			bool is_subscribed() const
			{
				std::lock_guard<std::recursive_mutex> lock(Mutex());
				return !m_link.empty();
			}

			// Subscribe or unsubscribe. Use thing.subscribe<EventType>(true), needed for multiple inheritance
			//template <typename T> typename std::enable_if<std::is_same<T,EventType>::value, void>::type
			void subscribe(bool on)
			{
				if (on) subscribe();
				else unsubscribe();
			}

		private:
			template<typename EventType> friend void Send(EventType const& e, bool forward);

			// Sign up to the event chain for this type
			// Note: this can be called during a 'Send' call. In this case the object added to
			// the PendingChain and then later added to the event chain when 'Send' completes
			void subscribe()
			{
				std::lock_guard<std::recursive_mutex> lock(Mutex());
				m_thread_id = std::this_thread::get_id();

				// Unsubscribe first
				unsubscribe();

				// If the chain is locked we have to make sure we don't modify the chain
				if (ChainFlags() & EControlFlags::ChainLocked)
				{
					// If we are currently pending removal from the live chain we can just remove
					// the flag and prevent ourselves being removed
					if (m_control & EControlFlags::Remove)
					{
						m_control &= ~EControlFlags::Remove;
					}
					// Otherwise, we need to add ourselves to the pending chain awaiting subscription
					// once the current 'Send' call completes
					else
					{
						pr::chain::Insert(PendingChain(), m_link);
						m_control |= EControlFlags::Pending;
						ChainFlags() |= EControlFlags::Pending; // Set a bit in the chain flags so we know some additions may be needed
					}
				}
				// If the chain is not locked, we can subscribe now
				else
				{
					// Insert into the chain at the correct priority order
					Link* i; for (i = Chain().begin(); i != Chain().end() && i->m_owner->m_priority > m_priority; i = i->m_next) {}
					pr::chain::Insert(*i, m_link);
					m_control &= ~EControlFlags::Pending;
				}
			}
			void unsubscribe()
			{
				std::lock_guard<std::recursive_mutex> lock(Mutex());
				if (!is_subscribed()) return;

				// If the chain is locked we have to make sure we don't modify the chain
				if (ChainFlags() & EControlFlags::ChainLocked)
				{
					// If this object is current in the pending chain, we can simply remove it
					if (m_control & EControlFlags::Pending)
					{
						pr::chain::Remove(m_link);
						m_control &= ~EControlFlags::Pending;
					}
					// Otherwise, mark it as needing removal from the live chain
					else
					{
						m_control |= EControlFlags::Remove;
						ChainFlags() |= EControlFlags::Remove; // Set a bit in the chain flags so we know some removals may be needed
					}
				}
				// If the chain is not locked, we can just remove it now
				else
				{
					pr::chain::Remove(m_link);
					m_control &= ~EControlFlags::Remove;
				}
			}

			// The EventType receivers chain
			static Link& Chain()
			{
				static Link chain;
				return chain;
			}

			// Event receivers that subscribe during a 'Send' call are linked to this chain until the call is complete
			static Link& PendingChain()
			{
				static Link pending_chain;
				return pending_chain;
			}

			// A mutex to synchronise access to the EventType chain
			static std::recursive_mutex& Mutex()
			{
				static std::recursive_mutex m;
				return m;
			}

			// Control flags
			static int& ChainFlags()
			{
				static int flags;
				return flags;
			}
		};
 
		// Send an event to all receivers of 'EventType'
		// - Sending events is synchronous for each event type
		// - OnEvent handlers will be called in *this* thread context which may
		//   be a different thread context to that which signed up for the events.
		//   Use 'same_thread()' to test for this case if necessary
		template <typename EventType> inline void Send(EventType const& e, bool forward = true)
		{
			// Prevent any re-entrancy or modification to the EventType receivers chain
			std::lock_guard<std::recursive_mutex> lock(IRecv<EventType>::Mutex());
				
			// Scope object for restoring chain flags
			struct ChainLocker
			{
				ChainLocker()  { IRecv<EventType>::ChainFlags() |=  EControlFlags::ChainLocked; }
				~ChainLocker() { IRecv<EventType>::ChainFlags() &= ~EControlFlags::ChainLocked; }
			};

			// Mark the chain as locked
			auto& chain = IRecv<EventType>::Chain();
			auto ibegin = std::begin(chain);
			auto iend   = std::end(chain);

			{
				ChainLocker chain_lock;

				// Loop over receivers of this event type
				if (forward)
				{
					for (auto i = ibegin; i != iend; i = i->m_next)
						i->m_owner->OnEvent(e);
				}
				else
				{
					for (auto i = iend->m_prev; i != iend; i = i->m_prev)
						i->m_owner->OnEvent(e);
				}
			}

			// Unsubscribe any receivers marked as Remove
			if (IRecv<EventType>::ChainFlags() & EControlFlags::Remove)
			{
				IRecv<EventType>::ChainFlags() &= ~EControlFlags::Remove;
				for (auto i = ibegin; i != iend;)
				{
					auto doomed = i; i = i->m_next;
					if (doomed->m_owner->m_control & EControlFlags::Remove)
						doomed->m_owner->subscribe(false);
				}
			}

			// Add any receivers marked as pending
			if (IRecv<EventType>::ChainFlags() & EControlFlags::Pending)
			{
				IRecv<EventType>::ChainFlags() &= ~EControlFlags::Pending;
				auto& pending = IRecv<EventType>::PendingChain();
				while (!pending.empty())
					std::begin(pending)->m_owner->subscribe(true);
			}
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		namespace evt
		{
			inline int& order() { static int order = 0; return order; }

			struct Evt {};

			// Test the priority order of events
			struct Thing0 :pr::events::IRecv<Evt>
			{
				int m_recv;
				int m_order;
				Thing0() :pr::events::IRecv<Evt>(0) ,m_recv() ,m_order() {}
				void OnEvent(Evt const&) { ++m_recv; m_order = ++order(); }
			};
			struct Thing1 :pr::events::IRecv<Evt>
			{
				int m_recv;
				int m_order;
				Thing1() :pr::events::IRecv<Evt>(2) ,m_recv() ,m_order() {}
				void OnEvent(Evt const&) { ++m_recv; m_order = ++order(); }
			};

			// Tests event handler that unsubscribes during the handler
			struct Once :pr::events::IRecv<Evt>
			{
				int m_count;
				Once() :pr::events::IRecv<Evt>() ,m_count(0) {}
				void OnEvent(Evt const&) { subscribe<Evt>(false); ++m_count; }
				void SignUp()            { subscribe<Evt>(true); }
			};

			// Tests changing the event chain during a Send() call
			struct Swapper :pr::events::IRecv<Evt>
			{
				Thing0 m_thing0;
				Thing1 m_thing1;
				int m_subbed;
				Swapper()
					:pr::events::IRecv<Evt>(1) // in between thing0 and thing1
				{
					m_thing0.subscribe<Evt>(true);
					m_thing1.subscribe<Evt>(false);
					m_subbed = 0;
				}
				void OnEvent(Evt const&)
				{
					// Swap subscribed objects on the event
					m_subbed = m_subbed == 0;
					m_thing0.subscribe<Evt>(m_subbed == 0);
					m_thing1.subscribe<Evt>(m_subbed == 1);
				}
			};
		}

		PRUnitTest(pr_common_events)
		{
			using namespace pr::unittests::evt;

			{// priority
				{
					order() = 0;
					Thing0 thing0;  // sign up 0
					Thing1 thing1;  // sign up 1
					pr::events::Send(Evt());
					PR_CHECK(thing1.m_order, 1); // 1 should receive the event first
					PR_CHECK(thing0.m_order, 2);
				}
				{
					order() = 0;
					Thing1 thing1; // sign up 1
					Thing0 thing0; // sign up 0
					pr::events::Send(Evt());
					PR_CHECK(thing1.m_order, 1); // 1 should still receive the event first
					PR_CHECK(thing0.m_order, 2);
				}
				{
					order() = 0;
					Thing0 thing0; // sign up 0
					Thing1 thing1; // sign up 1
					pr::events::Send(Evt(), false);
					PR_CHECK(thing0.m_order, 1); // 0 should receive the event first because the send was in reverse order
					PR_CHECK(thing1.m_order, 2);
				}
			}
			{// Simple self-removing event handlers
				Once once;
				PR_CHECK(once.m_count, 0);
				pr::events::Send(Evt());
				PR_CHECK(once.m_count, 1);
				pr::events::Send(Evt());
				PR_CHECK(once.m_count, 1);
				once.SignUp();
				pr::events::Send(Evt());
				PR_CHECK(once.m_count, 2);
			}
			{// add/remove during Send
				order() = 0;
				Swapper swapper; // Every event, thing0 and thing1 swap their subscriptions
				pr::events::Send(Evt());
				PR_CHECK(swapper.m_thing0.m_recv, 1);
				PR_CHECK(swapper.m_thing1.m_recv, 0);
				pr::events::Send(Evt());
				PR_CHECK(swapper.m_thing0.m_recv, 1);
				PR_CHECK(swapper.m_thing1.m_recv, 1);
				pr::events::Send(Evt());
				PR_CHECK(swapper.m_thing0.m_recv, 2);
				PR_CHECK(swapper.m_thing1.m_recv, 1);
			}
		}
	}
}
#endif

#pragma warning (pop)

#endif


