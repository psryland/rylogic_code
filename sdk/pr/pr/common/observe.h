//***********************************************************************************************
//
//	Structs that implement the observer pattern
//
//***********************************************************************************************
// Usage:
//	1) Inherit Observee / Observer
//	2) Register the Observer with the Observee by passing an SObserver struct. e.g. SObserver(this, some_data)
//	3) The Observee calls NotifyObservers with the observer data (some_data) and data relevant to the notify (something's happen)

#ifndef PR_OBSERVE_H
#define PR_OBSERVE_H

#include "pr/common/assert.h"
#include "pr/common/StdVector.h"

namespace pr
{
	struct IObserver;
	namespace impl
	{
		struct ObserverData
		{
			IObserver*	m_observer;
			void*		m_user_data;
		};
		inline bool operator == (const ObserverData& lhs, const ObserverData& rhs) const { return lhs.m_observer == rhs.m_observer; }
	}//namespace impl

	struct Observee
	{
		void RegisterObserver  (const Observer* observer, void* user_data);
		void UnRegisterObserver(const Observer* observer);
		void SendEvent         (void* event_data);
		
		typedef std::vector<impl::ObserverData>	TObserverContainer;
		TObserverContainer m_observer;	// Those watching us
	};

	struct IObserver
	{
		virtual void OnEvent(void* event_data, void* user_data) = 0;
	};

	//******************************************************************************
	// Implementation
	//*****
	// Register someone as an observer
	inline void Observee::RegisterObserver(const IObserver* observer, void* user_data)
	{
		impl::ObserverData obs = {observer, user_data};
		TObserverContainer::iterator iter = std::find(m_observer.begin(), m_observer.end(), obs);
		if( iter != m_observer.end() )	{ iter->m_user_data = user_data; }
		else							{ m_observer.push_back(obs); }
	}

	//*****
	// Unregister someone as an observer
	inline void Observee::UnRegisterObserver(const IObserver* observer)
	{
		impl::ObserverData obs = {observer, 0};
		TObserverContainer::iterator iter = std::find(m_observer.begin(), m_observer.end(), obs);
		if( iter != m_observer.end() ) { m_observer.erase(iter); }
	}

	//*****
	// Tell anyone watching that we've changed
	inline void	Observee::SendEvent(void* event_data)
	{
		for( TObserverContainer::iterator obs = m_observer.begin(), obs_end = m_observer.end(); obs != obs_end; ++obs )
		{
			obs->OnEvent(event_data, obs->m_user_data);
		}
	}
}//namespace pr

#endif//PR_OBSERVE_H
