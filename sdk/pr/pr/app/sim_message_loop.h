//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
// Files in the "pr/app/" form a starting point for building line-drawer style graphics
// apps based on WTL and pr::Renderer.
//
#pragma once
#ifndef PR_APP_SIM_MESSAGE_LOOP_H
#define PR_APP_SIM_MESSAGE_LOOP_H

#include <list>
#include <functional>
#include <atlapp.h>
#include "pr/common/timers.h"


namespace pr
{
	// Message loop for simulation applications
	// In a WTL app, replace the CMessageLoop instance for the main thread with one of these
	class SimMsgLoop :public CMessageLoop
	{
	public:
		// The step function for a context.
		// First parameter is the step time in seconds
		typedef std::function<void(double)> StepFunc;
	
	private:
		struct Context
		{
			// The function to call to step the context
			StepFunc m_step;

			// The number of rtc ticks per frame. For fixed step rate contexts, this is the interval
			// used for each step. For non-fixed step rate contexts, this is the minimum time between steps
			pr::rtc::Ticks m_ticks_per_frame;

			// The rtc time last time the context was stepped
			pr::rtc::Ticks m_last_time;

			// True if this context should always be stepped with the same elapsed time
			// Useful for deterministic simulation
			bool m_fixed_step_rate;

			// Drops frames if the simulation gets more than 'm_allowed_frames_behind' steps behind.
			// Use 'INFINITE' to never drop frames, should always be >= 1
			unsigned int m_allowed_frames_behind;

			Context(StepFunc step, float frames_per_second, bool fixed_step_rate, int max_frames_behind)
			:m_step(step)
			,m_ticks_per_frame(static_cast<pr::rtc::Ticks>(pr::rtc::ReadCPUFreq() / frames_per_second))
			,m_last_time(pr::rtc::Read())
			,m_fixed_step_rate(fixed_step_rate)
			,m_allowed_frames_behind(max_frames_behind + (max_frames_behind == 0))
			{}

			// Returns the rtc value of when this context would ideally be stepped next
			pr::rtc::Ticks next_step_time() const { return m_last_time + m_ticks_per_frame; }

			bool operator < (Context const& rhs) { return next_step_time() < rhs.next_step_time(); }
		};

		std::list<Context> m_contexts;

	public:
		SimMsgLoop()
		:m_contexts()
		{}

		// For everything that needs stepping at a particular rate, add a step context
		// e.g. Simulation step and draw are two typical step contexts
		void AddStepContext(StepFunc step, float frames_per_second, bool fixed_step_rate, unsigned int max_frames_behind = 1)
		{
			m_contexts.push_back(Context(step, frames_per_second, fixed_step_rate, max_frames_behind));
			m_contexts.sort();
		}

		// Runs the message loop until WM_QUIT
		virtual int Run()
		{
			// Initialise 'm_msg'
			::PeekMessage(&m_msg, 0, 0, 0, PM_NOREMOVE);
			while (m_msg.message != WM_QUIT)
			{
				// Pumping needed?
				if (::PeekMessage(&m_msg, 0, 0, 0, PM_REMOVE))
				{
					if (!PreTranslateMessage(&m_msg))
					{
						::TranslateMessage(&m_msg);
						::DispatchMessage(&m_msg);
					}
					
					if (IsIdleMessage(&m_msg))
					{
						int idle_count = 0;
						while (OnIdle(idle_count++) && !::PeekMessage(&m_msg, 0, 0, 0, PM_NOREMOVE))
						{}
					}
				}
				else if (!m_contexts.empty())
				{
					// Process all contexts until the front one is no longer due for stepping
					for(;;)
					{
						auto& next = m_contexts.front();

						// See if it's time to step the next context
						auto now = pr::rtc::Read();
						auto elapsed = now - next.m_last_time;
						if (elapsed < next.m_ticks_per_frame)
							break;

						// See if we need to drop frames for this context
						auto frames_behind = next.m_fixed_step_rate ? elapsed / next.m_ticks_per_frame : 1;
						if (frames_behind > next.m_allowed_frames_behind)
						{
							auto time_skip = (frames_behind - 1) * next.m_ticks_per_frame;
							next.m_last_time += time_skip;
							elapsed -= time_skip;
						}

						// 'next' requires stepping
						auto step_interval = next.m_fixed_step_rate ? next.m_ticks_per_frame : elapsed;
						next.m_step(pr::rtc::ToSec(step_interval));
						next.m_last_time += step_interval;

						// Sort the contexts so that the front of the list is the next to be stepped
						m_contexts.sort();
					}
				}
			}
			return (int)m_msg.wParam;
		}
	};
}

#endif
