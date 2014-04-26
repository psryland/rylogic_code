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

#include <string>
#include <functional>
#include <atlbase.h>
#include <atlapp.h>
#include "pr/common/timers.h"
#include "pr/common/fmt.h"
#include "pr/common/array.h"

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
			// A debugging name for the context
			std::string m_name;

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

			// The number of times this frame as been stepped sequentially (without messages being pumped)
			unsigned int m_sequential_step_count;

			Context(char const* name, StepFunc step, float frames_per_second, bool fixed_step_rate, int max_frames_behind)
				:m_name(name)
				,m_step(step)
				,m_ticks_per_frame(static_cast<pr::rtc::Ticks>(pr::rtc::ReadCPUFreq() / frames_per_second))
				,m_last_time(pr::rtc::Read())
				,m_fixed_step_rate(fixed_step_rate)
				,m_allowed_frames_behind(max_frames_behind + (max_frames_behind == 0))
				,m_sequential_step_count(0)
			{}

			// Returns the rtc value of when this context would ideally be stepped next
			pr::rtc::Ticks next_step_time() const { return m_last_time + m_ticks_per_frame; }

			// A sorting predicate
			static bool Order(Context const* lhs, Context const* rhs) { return lhs->next_step_time() < rhs->next_step_time(); }
		};

		pr::Array<Context*> m_contexts;

	public:
		SimMsgLoop()
			:m_contexts()
		{}
		~SimMsgLoop()
		{
			for (auto i = begin(m_contexts), iend = end(m_contexts); i != iend; ++i)
				delete *i;
		}

		// For everything that needs stepping at a particular rate, add a step context
		// e.g. Simulation step and draw are two typical step contexts
		void AddStepContext(char const* name, StepFunc step, float frames_per_second, bool fixed_step_rate, unsigned int max_frames_behind = 1)
		{
			m_contexts.push_back(new Context(name, step, frames_per_second, fixed_step_rate, max_frames_behind));
			std::sort(begin(m_contexts), end(m_contexts), Context::Order);
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
					pr::rtc::StopWatch sw;
					for(;;)
					{
						auto& ctx = *m_contexts.front();

						// See if it's time to step the next context
						auto now = pr::rtc::Read();
						auto elapsed = now - ctx.m_last_time;
						const unsigned int max_sequential_step_count = 10;
						if (elapsed < ctx.m_ticks_per_frame || ++ctx.m_sequential_step_count == max_sequential_step_count)
						{
							ctx.m_sequential_step_count = 0;
							break;
						}

						// See if we need to drop frames for this context
						auto frames_behind = ctx.m_fixed_step_rate ? elapsed / ctx.m_ticks_per_frame : 1;
						if (frames_behind > ctx.m_allowed_frames_behind)
						{
							auto time_skip = (frames_behind - 1) * ctx.m_ticks_per_frame;
							ctx.m_last_time += time_skip;
							elapsed -= time_skip;
							//PR_LOG(m_log, Warn, pr::FmtS("Dropping %d frames for %s", frames_behind - 1, ctx.m_name.c_str()));
						}

						// 'ctx' requires stepping
						auto step_interval = ctx.m_fixed_step_rate ? ctx.m_ticks_per_frame : elapsed;
						{
							sw.start(true);
							ctx.m_step(pr::rtc::ToSec(step_interval));
							sw.stop();
							//if (sw.period() > ctx.m_ticks_per_frame)
								//PR_LOG(m_log, Warn, pr::FmtS("'%s' step() took %3.3fms, frame time: %3.3fms", ctx.m_name.c_str(), sw.period_ms(), pr::rtc::ToMSec(ctx.m_ticks_per_frame)));
						}
						ctx.m_last_time += step_interval;

						// Sort the contexts so that the front of the list is the next to be stepped
						std::sort(begin(m_contexts), end(m_contexts), Context::Order);
					}
				}
			}
			return (int)m_msg.wParam;
		}
	};
}

#endif
