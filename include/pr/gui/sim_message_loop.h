//*****************************************************************************************
// Simulation Message Loop
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// In a WTL app, replace the CMessageLoop instance for the main thread with one of these
// You'll need to derive from CMessageLoop
#pragma once
#include <string>
#include <string_view>
#include <vector>
#include <exception>
#include <functional>
#include <algorithm>
#include <memory>
#include <chrono>
#include <thread>
#include <format>

#include <Windows.h>

namespace pr::gui
{
	// PR_CODE_SYNC_BEGIN(SimMessageLoop, source_of_truth)

	// An interface for types that need to handle messages from the message loop before TranslateMessage is called.
	// Typically these are dialog windows or windows with keyboard accelerators that need to call 'IsDialogMessage'
	// or 'TranslateAccelerator'
	struct IMessageFilter
	{
		virtual ~IMessageFilter() = default;

		// Implementers should return true to halt processing of the message.
		// Typically, if you're just observing messages as they go past, return false.
		// If you're a dialog return the result of IsDialogMessage()
		// If you're a window with accelerators, return the result of TranslateAccelerator()
		virtual bool TranslateMessage(MSG&)
		{
			return false;
		}
	};

	// Message loop that also manages and runs a priority queue of simulation loops 
	struct SimMessageLoop :IMessageFilter
	{
		// Notes:
		//  - A message loop designed for simulation applications
		//    This loop sleeps the thread until the next frame is due or until messages arrive.
		using step_func_t = std::function<void(int64_t)>;

	private:
		union buf8
		{
			uint64_t u64;
			uint8_t b[sizeof(uint64_t)];
			void add(uint8_t v)
			{
				u64 <<= 8;
				b[0] = v;
			}
		};

		// A loop represents a process that should be run at a given frame rate
		struct Loop
		{
			step_func_t m_step; // The function to call to step the loop
			int64_t m_clock;    // The time this loop was last stepped (in ms)
			buf8 m_avr;         // Last 8 execution times of the loop (in ms, capped at 255)
			int m_step_rate_ms; // (Minimum) step rate
			bool m_variable;    // Variable step rate

			Loop(step_func_t step, int step_rate_ms, bool variable)
				:m_step(step)
				,m_clock()
				,m_avr()
				,m_step_rate_ms(step_rate_ms)
				,m_variable(variable)
			{}
			int64_t next() const
			{
				return m_clock + m_step_rate_ms;
			}
		};
		using LoopCont = std::vector<Loop>;
		using LoopOrder = std::vector<int>;

		using Filters = std::vector<IMessageFilter*>;

		LoopCont  m_loop;           // The loops to execute
		LoopOrder m_order;          // A priority queue of loops. The loop at position 0 is the next to be stepped
		Filters   m_filters;        // Message filters to process messages before TranslateMessage is called
		int64_t   m_clock0;         // The time when 'Run' was called.
		int64_t   m_clock;          // The last time StepLoops was called.
		int       m_max_loop_steps; // The maximum number of loops to step before checking for messages

	public:

		SimMessageLoop(int max_loop_steps = 10)
			:m_loop()
			,m_order()
			,m_filters()
			,m_clock0()
			,m_clock()
			,m_max_loop_steps(max_loop_steps)
		{
			m_filters.push_back(this);
		}
		virtual ~SimMessageLoop() = default;

		// Add a loop to be stepped by this simulation message pump. if 'variable' is true, 'step_rate_ms' means minimum step rate
		void AddLoop(int step_rate_ms, bool variable, step_func_t step)
		{
			m_loop.push_back(Loop(step, step_rate_ms, variable));
			m_order.push_back(static_cast<int>(m_loop.size()) - 1);
		}
		void AddLoop(double fps, bool variable, step_func_t step)
		{
			AddLoop(static_cast<int>(1000.0 / fps), variable, step);
		}

		// Add/Remove an instance that needs to handle messages before TranslateMessage is called
		void AddMessageFilter(IMessageFilter& filter)
		{
			m_filters.insert(--std::end(m_filters), &filter);
		}
		void RemoveMessageFilter(IMessageFilter& filter)
		{
			m_filters.erase(std::remove(std::begin(m_filters), std::end(m_filters), &filter), std::end(m_filters));
		}

		// Run the thread message pump while maintaining the desired loop rates
		virtual int Run()
		{
			// Set the start time
			m_clock0 = Clock();
			m_clock = 0;

			// Run the message pump loop
			for (;;)
			{
				// Step any pending loops and get the time till the next loop to be stepped.
				auto timeout = StepLoops();

				// Pump any queued messages
				auto exit_code = Pump(timeout);
				if (exit_code)
					return *exit_code;
			}
		}

		// Pump messages. Returns null or an exit code if a WM_QUIT message was pumped
		std::optional<int> Pump(DWORD timeout_ms = 0)
		{
			MSG msg = {};

			// Check for messages and pump any received until
			::MsgWaitForMultipleObjects(0, nullptr, TRUE, timeout_ms, QS_ALLPOSTMESSAGE | QS_ALLINPUT | QS_ALLEVENTS);
			for (int max_messages = 1000; max_messages-- != 0 && ::PeekMessageW(&msg, 0, 0, 0, PM_REMOVE); )
			{
				// Exit the message pump?
				if (msg.message == WM_QUIT)
					return static_cast<int>(msg.wParam);

				// Pump the message
				HandleMessage(msg);
			}
			return std::nullopt;
		}

		// Return the running time since 'Run()' was called
		int64_t Clock()
		{
			return static_cast<int64_t>(GetTickCount64()) - m_clock0;
		}

		// Call 'Step' on all loops that are pending
		// Returns the time in milliseconds until the next loop needs to be stepped
		DWORD StepLoops()
		{
			if (m_loop.empty())
				return INFINITE;

			auto now = Clock();
			auto dt = now - m_clock;
			m_clock = now;

			// Check the StepLoops function is being called frequently enough.
			// If not, it's probably due to a blocking windows message handler
			for (auto const& loop : m_loop)
			{
				if (dt < static_cast<long long>(loop.m_step_rate_ms) * m_max_loop_steps)
					continue;

				//OutputDebugStringA(std::format("SimMessagePump: WARNING - {} ms between StepLoops() calls\n", dt).c_str());
			}

			// Step all loops that are pending
			for (int i = 0; i != m_max_loop_steps; ++i)
			{
				// Sort by soonest to step
				std::sort(m_order.begin(), m_order.end(), [&](int lhs, int rhs)
				{
					// Smaller values need to be stepped sooner
					return m_loop[lhs].next() < m_loop[rhs].next();
				});

				// Get the next due to be stepped
				auto& loop = m_loop[m_order[0]];
				auto time_till_step = loop.next() - m_clock;
				if (time_till_step > 0)
					return static_cast<DWORD>(time_till_step);

				// Elapsed time for the loop step, either a fixed value or the wall time since last stepped
				auto elapsed_ms = loop.m_variable ? m_clock - loop.m_clock : loop.m_step_rate_ms;

				// Step the loop
				auto t0 = Clock();
				loop.m_step(elapsed_ms);
				loop.m_clock += elapsed_ms;
				loop.m_avr.add(static_cast<uint8_t>(std::min(255LL, Clock() - t0)));

				//if (loop.m_avr.b[0] > loop.m_step_rate_ms)
				//	OutputDebugStringA(std::format("SimMessagePump: WARNING - long step: {}% \n", loop.m_avr.b[0] * 100.0 / loop.m_step_rate_ms).c_str());
			}

			// If we get here, the loops are taking too long. Return a timeout of 0 to indicate
			// loops still need stepping. This allows the message queue still to be processed though.
			// Loop at 'm_avr' to see the last 8 loop execution times in ms.
			//OutputDebugStringA("SimMessagePump: WARNING - loops are staving the message queue\n");
			return 0;
		}

	protected:

		// Pass the message to each filter. The last filter is this message loop which always handles the message.
		void HandleMessage(MSG& msg)
		{
			for (auto filter : m_filters)
				if (filter->TranslateMessage(msg))
					break;
		}

		// The message loop is always the last filter in the chain
		virtual bool TranslateMessage(MSG& msg)
		{
			::TranslateMessage(&msg);
			::DispatchMessageW(&msg);
			return true;
		}
	};

	// PR_CODE_SYNC_END()
}


	#if 0

#define PR_LOOP_TIMING 1//PR_DBG
#if PR_LOOP_TIMING
#include "pr/maths/stat.h"
#include "pr/gui/messagemap_dbg.h"
#endif

// Message loop for simulation applications
	struct SimMsgLoop
	{
		// The step function for a context.
		// First parameter is the elapsed time in seconds. For fixed step rate contexts, this is always
		// the 'ticks_per_frame' value. For non-fixed step rates, this is the elapsed real time.
		using StepFunc = std::function<void(double)>; // void Step(double elapsed_s)

	private:

		using rtc_t = std::chrono::high_resolution_clock;
		using duration_t   = rtc_t::duration;
		using time_point_t = rtc_t::time_point;

		// Convert seconds to/from duration_t
		static duration_t FromSec(double sec)
		{
			auto ns = static_cast<std::chrono::nanoseconds::rep>(sec * 1000000000.0);
			return std::chrono::duration_cast<duration_t>(std::chrono::nanoseconds(ns));
		}

		// Message loop timing
		#if PR_LOOP_TIMING
		struct Stats
		{
			size_t m_frame_index;
			size_t m_long_frames;
			maths::Stat<> m_step_time;

			Stats()
				:m_frame_index()
				,m_long_frames()
				,m_step_time()
			{}
			std::string ToString() const
			{
				return std::format(
					"Frame stats:\n"
					"  Long Frames: {}\n"
					"  Avr Frame: {}ms\n"
					"  Min Frame: {}ms\n"
					"  Max Frame: {}ms\n"
					,m_long_frames
					,m_step_time.Mean()
					,m_step_time.Min()
					,m_step_time.Max()
					);
			}
		};
		
		maths::Stat<> m_msg_time;
		#endif

		// A loop context
		struct Context
		{
			// A debugging name for the context
			std::string m_name;

			// The function to call to step the context
			StepFunc m_step;

			// The number of RTC ticks per frame. For fixed step rate contexts, this is the interval
			// used for each step. For non-fixed step rate contexts, this is the minimum time between steps
			duration_t m_ticks_per_frame;

			// The RTC time last time the context was stepped
			time_point_t m_last_time;

			// True if this context should always be stepped with the same elapsed time
			// Useful for deterministic simulation
			bool m_fixed_step_rate;

			// Run stats for this context
			#if PR_LOOP_TIMING
			Stats m_stats;
			#endif

			Context(std::string_view name, StepFunc step, float frames_per_second, bool fixed_step_rate)
				: m_name(name)
				, m_step(step)
				, m_ticks_per_frame(FromSec(1.0 / frames_per_second))
				, m_last_time()
				, m_fixed_step_rate(fixed_step_rate)
			{}
			~Context()
			{
				#if PR_LOOP_TIMING
				OutputDebugStringA(std::format("{}:\n{}", m_name, m_stats.ToString()).c_str());
				#endif
			}

			// Returns the RTC value of when this context would ideally be stepped next
			time_point_t next_step_time() const
			{
				return m_fixed_step_rate ? m_last_time + m_ticks_per_frame : rtc_t::now();
			}

			// A sorting predicate
			static bool Order(Context const* lhs, Context const* rhs)
			{
				return lhs->next_step_time() < rhs->next_step_time();
			}
		};

		// Contexts, stored into next-to-step order
		std::vector<std::unique_ptr<Context>> m_contexts;

	public:
		SimMsgLoop()
			:m_contexts()
		{}
		~SimMsgLoop()
		{
			#if PR_LOOP_TIMING
			OutputDebugStringA(std::format(
				"Msg Queue:\n"
				"  Avr Time: {}ms\n"
				"  Min Time: {}ms\n"
				"  Max Time: {}ms\n"
				,m_msg_time.Mean()
				,m_msg_time.Min()
				,m_msg_time.Max()
				).c_str());
			#endif
		}

		// For everything that needs stepping, add a step context. e.g. Simulation step and draw are two typical step contexts
		void AddStepContext(std::string_view name, StepFunc step, float frames_per_second, bool fixed_step_rate)
		{
			m_contexts.emplace_back(new Context(name, step, frames_per_second, fixed_step_rate));
			std::sort(std::begin(m_contexts), std::end(m_contexts), Context::Order);
		}

		// Remove a step context by name
		void RemoveStepContext(std::string_view name)
		{
			auto new_end = std::remove_if(std::begin(m_contexts), std::end(m_contexts), [=](Context* ctx){ return ctx->m_name == name; });
			m_contexts.erase(new_end, std::end(m_contexts));
		}

		// Runs the message loop until WM_QUIT
		int Run() override
		{
			MSG msg;
			duration_t const MinTimeBetweenFrames = pr::rtc::FromMSec(1.0);

			PR_EXPAND(PR_LOOP_TIMING, pr::rtc::StopWatch sw);
			for (pr::rtc::StopWatch clock(true); ;)
			{
				auto msg_start = clock.now();

				// Pumping needed?
				PR_EXPAND(PR_LOOP_TIMING, sw.start(true));
				int result = ::PeekMessageW(&msg, 0, 0, 0, PM_REMOVE);
				if (result != 0 && msg.message != WM_QUIT)
				{
					Check(result > 0, "PeekMessage failed");

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

				// Process all contexts until the front one is no longer due for stepping
				for (;!m_contexts.empty();)
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
					// For fixed-step contexts, advance from the previous time to allow catch-up.
					// For variable-step contexts, advance to now to prevent accumulation.
					if (ctx.m_fixed_step_rate)
					{
						ctx.m_last_time += ctx.m_ticks_per_frame;

						// Cap catch-up to prevent death spiral. If we're more than a few
						// frames behind, skip ahead rather than trying to catch up.
						auto max_lag = ctx.m_ticks_per_frame * 4;
						if (clock.now() - ctx.m_last_time > max_lag)
							ctx.m_last_time = clock.now() - max_lag;
					}
					else
					{
						ctx.m_last_time = now;
					}

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
					for (size_t i = 0, iend = m_contexts.size() - 1; i != iend; ++i)
						if (!Context::Order(m_contexts[i], m_contexts[i+1]))
							std::swap(m_contexts[i], m_contexts[i+1]);
				}

				// Sleep until the next context is due, yielding CPU time
				if (!m_contexts.empty())
				{
					auto time_until_next = m_contexts.front()->next_step_time() - clock.now();
					if (time_until_next > duration_t::zero())
					{
						auto sleep_ms = pr::rtc::ToMSec(time_until_next);
						if (sleep_ms >= 1.0)
							std::this_thread::sleep_for(std::chrono::milliseconds(static_cast<int>(sleep_ms)));
					}
				}
			}
			return (int)msg.wParam;
		}
	};
	#endif

