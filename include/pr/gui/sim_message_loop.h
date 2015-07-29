//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// In a WTL app, replace the CMessageLoop instance for the main thread with one of these
// You'll need to derive from CMessageLoop

#pragma once

#include <string>
#include <exception>
#include <functional>
#include <thread>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/stop_watch.h"
#include "pr/container/vector.h"
#include "pr/gui/wingui.h"

#define PR_LOOP_TIMING 0//PR_DBG
#if PR_LOOP_TIMING
#include "pr/maths/stat.h"
#include "pr/gui/messagemap_dbg.h"
#endif

namespace pr
{
	namespace gui
	{
		// Message loop for simulation applications
		struct SimMsgLoop :MessageLoop
		{
			// The step function for a context.
			// First parameter is the step time in seconds
			using StepFunc = std::function<void(double)>;

		private:

			using duration_t   = pr::rtc::duration_t;
			using time_point_t = pr::rtc::time_point_t;

			#if PR_LOOP_TIMING
			struct Stats
			{
				size_t m_frame_index;
				size_t m_long_frames;
				pr::Stat<> m_step_time;

				Stats()
					:m_frame_index()
					,m_long_frames()
					,m_step_time()
				{}
				std::string ToString() const
				{
					return pr::Fmt(
						"Frame stats:\n"
						"  Long Frames: %d\n"
						"  Avr Frame: %fms\n"
						"  Min Frame: %fms\n"
						"  Max Frame: %fms\n"
						,m_long_frames
						,m_step_time.Mean()
						,m_step_time.Minimum()
						,m_step_time.Maximum()
						);
				}
			};
		
			// Message loop timing
			pr::Stat<> m_msg_time;
			#endif

			struct Context
			{
				// A debugging name for the context
				std::string m_name;

				// The function to call to step the context
				StepFunc m_step;

				// The number of rtc ticks per frame. For fixed step rate contexts, this is the interval
				// used for each step. For non-fixed step rate contexts, this is the minimum time between steps
				duration_t m_ticks_per_frame;

				// The rtc time last time the context was stepped
				duration_t m_last_time;

				// True if this context should always be stepped with the same elapsed time
				// Useful for deterministic simulation
				bool m_fixed_step_rate;

				// Run stats for this context
				PR_EXPAND(PR_LOOP_TIMING, Stats m_stats;)

				Context(char const* name, StepFunc step, float frames_per_second, bool fixed_step_rate)
					:m_name(name)
					,m_step(step)
					,m_ticks_per_frame(pr::rtc::FromSec(1.0f / frames_per_second))
					,m_last_time()
					,m_fixed_step_rate(fixed_step_rate)
				{}
				~Context()
				{
					PR_INFO(PR_LOOP_TIMING, m_name.c_str());
					PR_INFO(PR_LOOP_TIMING, m_stats.ToString().c_str());
				}

				// Returns the rtc value of when this context would ideally be stepped next
				duration_t next_step_time() const { return m_last_time + m_ticks_per_frame; }

				// A sorting predicate
				static bool Order(Context const* lhs, Context const* rhs) { return lhs->next_step_time() < rhs->next_step_time(); }
			};
			pr::vector<Context*> m_contexts;
			HACCEL m_accel;

		public:
			SimMsgLoop(HACCEL accel = nullptr)
				:m_contexts()
				,m_accel(accel)
			{}
			SimMsgLoop(HINSTANCE hinst, int accel_id)
				:SimMsgLoop(::LoadAccelerators(hinst, MAKEINTRESOURCE(accel_id)))
			{}
			virtual ~SimMsgLoop()
			{
				for (auto ctx : m_contexts)
					delete ctx;
			
				PR_INFO(PR_LOOP_TIMING, pr::FmtS(
					"Msg Queue:\n"
					"  Avr Time: %fms\n"
					"  Min Time: %fms\n"
					"  Max Time: %fms\n"
					,m_msg_time.Mean()
					,m_msg_time.Minimum()
					,m_msg_time.Maximum()
					));
			}

			// For everything that needs stepping at a particular rate, add a step context
			// e.g. Simulation step and draw are two typical step contexts
			void AddStepContext(char const* name, StepFunc step, float frames_per_second, bool fixed_step_rate)
			{
				m_contexts.push_back(new Context(name, step, frames_per_second, fixed_step_rate));
				std::sort(std::begin(m_contexts), std::end(m_contexts), Context::Order);
			}

			// Runs the message loop until WM_QUIT
			int Run() override
			{
				MSG msg;
				duration_t const MinTimeBetweenFrames = pr::rtc::FromMSec(1.0);

				PR_EXPAND(PR_LOOP_TIMING, pr::rtc::StopWatch sw);
				for (pr::rtc::StopWatch clock(true); ; std::this_thread::sleep_for(std::chrono::milliseconds(1)))//yield())
				{
					auto msg_start = clock.now();

					// Pumping needed?
					PR_EXPAND(PR_LOOP_TIMING, sw.start(true));
					int result = ::PeekMessageW(&msg, 0, 0, 0, PM_REMOVE);
					if (result != 0 && msg.message != WM_QUIT)
					{
						Throw(result > 0, "PeekMessage failed");

						// Pass the message to each filter. The last filter is this message loop which always handles the message.
						for (auto filter : m_filters)
							if (filter->TranslateMessage(msg))
								break;
					}
					PR_EXPAND(PR_LOOP_TIMING, sw.stop());
					PR_EXPAND(PR_LOOP_TIMING, m_msg_time.Add(sw.period_ms()));
					PR_INFO_IF(PR_LOOP_TIMING, sw.period_ms() > 20.0, pr::FmtS("%-16s took %fms\n", pr::debug_wm::WMtoString(m_msg.message), sw.period_ms()));

					// Only allow the message handling to consume a fixed amount of sim time
					auto msg_end = clock.now();
					clock.m_start += std::max(duration_t::zero(), (msg_end - msg_start) - MinTimeBetweenFrames);

					// Exit the message pump when WM_QUIT is received
					if (msg.message == WM_QUIT)
						break;

					// No contexts...
					if (m_contexts.empty())
						continue;

					// Process all contexts until the front one is no longer due for stepping
					for(;;)
					{
						auto& ctx    = *m_contexts.front();
						auto now     = clock.now();
						auto elapsed = now - ctx.m_last_time;

						// If the next context is not due for stepping, leave the loop
						if (elapsed < ctx.m_ticks_per_frame)
							break;

						// Find the period of time to step by
						auto step_interval = ctx.m_fixed_step_rate ? ctx.m_ticks_per_frame : elapsed;

						// Perform the step
						PR_EXPAND(PR_LOOP_TIMING, sw.start(true));
						ctx.m_step(pr::rtc::ToSec(step_interval));
						PR_EXPAND(PR_LOOP_TIMING, sw.stop());

						// Advance the context's last time.
						ctx.m_last_time = std::max(now + step_interval, clock.now() - MinTimeBetweenFrames);

						// Frame stats
						#if PR_LOOP_TIMING
						ctx.m_stats.m_frame_index++;
						ctx.m_stats.m_step_time.Add(sw.period_ms());
						if (step_interval < sw.period())
						{
							++ctx.m_stats.m_long_frames;
							PR_INFO(1, pr::FmtS("Long Frame: %-15s (frame: %d) Took: %fms (frame period: %fms)\n", ctx.m_name.c_str(), ctx.m_stats.m_frame_index, sw.period_ms(), pr::rtc::ToMSec(ctx.m_ticks_per_frame)));
						}
						#endif

						// Bubble sort the contexts so that the front of the list is the next to be stepped
						// The list should be in order except for the first so bubble sort is best
						for (size_t i = 0, iend = m_contexts.size() - 1; i < iend; ++i)
							if (!Context::Order(m_contexts[i], m_contexts[i+1]))
								std::swap(m_contexts[i], m_contexts[i+1]);
					}
				}
				return (int)msg.wParam;
			}
		};
	}
}
