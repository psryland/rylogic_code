//*****************************************************************************************
// Message Loop
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
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
	// PR_CODE_SYNC_BEGIN(MessageLoop, source_of_truth)

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

	// Message loop that also manages and runs a priority queue of loops 
	struct MessageLoop :IMessageFilter
	{
		// Notes:
		//  - For event driven applications, don't add any loops. Then the message loop will just pump messages as normal.
		//  - Fixed step loops run at exactly the requested rate, accumulating time and catching up if behind.
		//  - Variable step loops run whenever possible, receiving the actual wall-clock elapsed time.
		//  - Fixed step loops have priority over variable step loops when both are due.

		using clock_t = std::chrono::steady_clock;
		using time_point_t = clock_t::time_point;
		using duration_t = clock_t::duration;
		using step_func_t = std::function<void(double)>; // receives elapsed time in seconds

		// The maximum number of fixed-step catch-up iterations before skipping ahead (death spiral protection)
		static constexpr int MaxCatchUpSteps = 4;

	private:

		// A loop represents a process that should be run at a given rate
		struct Loop
		{
			step_func_t m_step;       // The function to call to step the loop
			duration_t m_interval;    // The (minimum) time between steps
			time_point_t m_last_time; // The time this loop was last stepped
			time_point_t m_next_due;  // When this loop is next due to be stepped
			bool m_variable;          // Variable step rate (true = run as fast as possible, false = fixed step)

			Loop(step_func_t step, duration_t interval, bool variable)
				: m_step(step)
				, m_interval(interval)
				, m_last_time()
				, m_next_due()
				, m_variable(variable)
			{}
		};

		using LoopCont = std::vector<Loop>;
		using Filters = std::vector<IMessageFilter*>;

		LoopCont m_loop;          // The loops to execute
		Filters m_filters;        // Message filters to process messages before TranslateMessage is called
		time_point_t m_clock0;    // The time when 'Run' was called

	public:

		MessageLoop()
			: m_loop()
			, m_filters()
			, m_clock0()
		{
			m_filters.push_back(this);
		}
		virtual ~MessageLoop() = default;

		// Return the running time since 'Run()' was called (in seconds)
		double Clock() const
		{
			return std::chrono::duration<double>(clock_t::now() - m_clock0).count();
		}

		// Add a loop to be stepped by this message pump.
		// 'fps' is the target frame rate. 'variable' true means run as fast as possible (fps is minimum rate).
		void AddLoop(double fps, bool variable, step_func_t step)
		{
			auto interval = std::chrono::duration_cast<duration_t>(std::chrono::duration<double>(1.0 / fps));
			m_loop.emplace_back(std::move(step), interval, variable);
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
			// Initialise clocks
			m_clock0 = clock_t::now();
			auto now = m_clock0;
			for (auto& loop : m_loop)
			{
				loop.m_last_time = now;
				loop.m_next_due = now;
			}

			// Run the message pump loop
			for (;;)
			{
				// Step any pending loops and get the time till the next loop is due
				auto timeout = StepLoops();

				// Pump any queued messages, waiting up to 'timeout' for new ones
				auto exit_code = Pump(timeout);
				if (exit_code)
					return *exit_code;
			}
		}

		// Pump messages. Returns null or an exit code if a WM_QUIT message was pumped
		std::optional<int> Pump(DWORD timeout_ms = 0)
		{
			MSG msg = {};

			// Wait for messages or until timeout (efficient idle, no busy-spin)
			::MsgWaitForMultipleObjects(0, nullptr, FALSE, timeout_ms, QS_ALLPOSTMESSAGE | QS_ALLINPUT | QS_ALLEVENTS);
			for (int max_messages = 1000; max_messages-- != 0 && ::PeekMessageW(&msg, 0, 0, 0, PM_REMOVE); )
			{
				if (msg.message == WM_QUIT)
					return static_cast<int>(msg.wParam);

				HandleMessage(msg);
			}
			return std::nullopt;
		}

		// Call 'Step' on all loops that are pending. Returns the time in milliseconds until the next loop is due.
		DWORD StepLoops()
		{
			if (m_loop.empty())
				return INFINITE;

			auto now = clock_t::now();

			// Step fixed-rate loops first (they have priority), then variable-rate loops
			for (int pass = 0; pass != 2; ++pass)
			{
				for (auto& loop : m_loop)
				{
					// Pass 0 = fixed loops, Pass 1 = variable loops
					if (loop.m_variable != (pass == 1))
						continue;

					if (loop.m_variable)
					{
						// Variable step: run whenever due, with the actual elapsed wall-clock time
						if (now < loop.m_next_due)
							continue;

						auto elapsed = std::chrono::duration<double>(now - loop.m_last_time).count();
						loop.m_step(elapsed);
						loop.m_last_time = now;
						loop.m_next_due = now + loop.m_interval;
					}
					else
					{
						// Fixed step: run at exactly the requested rate, catching up if behind
						for (int catch_up = 0; catch_up != MaxCatchUpSteps && loop.m_next_due <= now; ++catch_up)
						{
							auto dt = std::chrono::duration<double>(loop.m_interval).count();
							loop.m_step(dt);
							loop.m_next_due += loop.m_interval;
						}

						// Death spiral protection: if still behind, skip ahead
						if (loop.m_next_due < now)
							loop.m_next_due = now;

						loop.m_last_time = now;
					}
				}
			}

			// Calculate time until the next loop is due
			auto next_due = m_loop[0].m_next_due;
			for (auto const& loop : m_loop)
			{
				if (loop.m_next_due < next_due)
					next_due = loop.m_next_due;
			}

			now = clock_t::now();
			if (next_due <= now)
				return 0;

			auto wait = std::chrono::duration_cast<std::chrono::milliseconds>(next_due - now).count();
			return static_cast<DWORD>(std::max(0LL, wait));
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
		virtual bool TranslateMessage(MSG& msg) override
		{
			::TranslateMessage(&msg);
			::DispatchMessageW(&msg);
			return true;
		}
	};

	// PR_CODE_SYNC_END()
}
