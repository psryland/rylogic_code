//*********************************************
// Stop Watch
//  Copyright (c) Rylogic Ltd 2014
//*********************************************
#pragma once
#include <chrono>

namespace pr::rtc
{
	using RTC = std::chrono::high_resolution_clock;
	using time_point_t = std::chrono::high_resolution_clock::time_point;
	using duration_t = std::chrono::high_resolution_clock::duration;

	// Seconds to/from duration_t
	inline double ToSec(duration_t ticks)
	{
		auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(ticks);
		return ns.count() * 0.000000001;
	}
	inline duration_t FromSec(double sec)
	{
		auto ns = static_cast<std::chrono::nanoseconds::rep>(sec * 1000000000.0);
		return std::chrono::duration_cast<duration_t>(std::chrono::nanoseconds(ns));
	}

	// Milliseconds to/from duration_t
	inline double ToMSec(duration_t ticks)
	{
		return ToSec(ticks) * 1000.0;
	}
	inline duration_t FromMSec(double ms)
	{
		return FromSec(ms / 1000.0);
	}

	// Microseconds to/from duration_t
	inline double ToUSec(duration_t ticks)
	{
		return ToSec(ticks) * 1000000.0;
	}
	inline duration_t FromUSec(double us)
	{
		return FromSec(us / 1000000.0);
	}

	// Helper stopwatch
	struct StopWatch
	{
		time_point_t m_start;
		time_point_t m_stop;
		duration_t   m_accum;

		StopWatch()
			:m_start()
			,m_stop()
			,m_accum()
		{}
		explicit StopWatch(bool start_)
			:StopWatch()
		{
			if (start_)
				start();
		}

		// Reset the stop watch to 00:00
		void reset()
		{
			m_accum = duration_t::zero();
		}

		// Start the stop watch
		void start(bool reset_first)
		{
			if (reset_first) reset();
			start();
		}
		void start()
		{
			m_start = RTC::now();
		}

		// Stop the stop watch. Measure time given by 'period' methods
		void stop()
		{
			m_stop  = RTC::now();
			m_accum += m_stop - m_start;
		}

		// Return the value recorded when 'stop()' was last called
		duration_t period() const
		{
			return m_accum;
		}
		double period_s() const
		{
			return ToSec(period());
		}
		double period_ms() const
		{
			return ToMSec(period());
		}

		// Return the current 'running' time of the stop watch without stopping
		duration_t now() const
		{
			return RTC::now() - m_start;
		}

		// Return a 'lap' time, resetting the stop watch to zero but leaving it running
		duration_t lap()
		{
			stop();
			auto p = period();
			start(true);
			return p;
		}
		double lap_s()
		{
			return ToSec(lap());
		}
		double lap_ms()
		{
			return ToMSec(lap());
		}
	};
}
