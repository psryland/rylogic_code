//***********************************************************************************************
//
//***********************************************************************************************
// Usage:
//	1) Inherit Observee / Observer
//	2) Register the Observer with the Observee by passing an SObserver struct. e.g. SObserver(this, some_data)
//	3) The Observee calls NotifyObservers with the observer data (some_data) and data relevant to the notify (something's happen)

#pragma once
#ifndef PR_COMMON_OBSERVE_H
#define PR_COMMON_OBSERVE_H

#include <vector>
#include <algorithm>
#include "pr/common/assert.h"

namespace pr
{
	struct IObserver
	{
		virtual void OnNotification(void* event_data, void* user_data) = 0;
	};

	namespace impl
	{
		struct ObserverData
		{
			IObserver* m_observer;
			void*      m_user_data;
			static ObserverData make(IObserver* observer, void* user_data)
			{
				ObserverData obs;
				obs.m_observer = observer;
				obs.m_user_data = user_data;
				return obs;
			}
		};
		inline bool operator == (const ObserverData& lhs, const ObserverData& rhs) { return lhs.m_observer == rhs.m_observer; }
	}

	struct Observee
	{
		typedef std::vector<impl::ObserverData>	TObserverContainer;
		TObserverContainer m_observer; // Those watching us

		// Register an observer
		void RegisterObserver(IObserver* observer, void* user_data)
		{
			auto obs = impl::ObserverData::make(observer, user_data);
			auto iter = std::find(begin(m_observer), end(m_observer), obs);
			if (iter != m_observer.end()) iter->m_user_data = user_data;
			else                          m_observer.push_back(obs);
		}

		// Unregister someone as an observer
		void UnRegisterObserver(IObserver* observer)
		{
			auto obs = impl::ObserverData::make(observer, 0);
			auto iter = std::find(begin(m_observer), end(m_observer), obs);
			if (iter != end(m_observer)) m_observer.erase(iter);
		}

		// Send a message to observers
		void NotifyObservers(void* event_data)
		{
			for (auto obs : m_observer)
				obs.m_observer->OnNotification(event_data, obs.m_user_data);
		}
	};
}

#endif
