//*********************************************
// Stop Watch
//  Copyright (c) Rylogic Ltd 2014
//*********************************************

#pragma once

#include <chrono>

namespace pr
{
	namespace rtc
	{
		typedef std::chrono::high_resolution_clock RTC;
		typedef std::chrono::high_resolution_clock::time_point time_point_t;
		typedef std::chrono::high_resolution_clock::duration   duration_t;

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

			void start(bool reset_first)  { if (reset_first) reset(); start(); }
			void start()                  { m_start = RTC::now(); }
			void stop()                   { m_stop  = RTC::now(); m_accum += m_stop - m_start; }
			void reset()                  { m_accum = duration_t::zero(); }
			duration_t now() const        { return RTC::now() - m_start; }
			duration_t period() const     { return m_accum; }
			double period_s() const       { return ToSec(period()); }
			double period_ms() const      { return ToMSec(period()); }
		};
	}
}
