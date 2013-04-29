//*********************************************
// Timers.h
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_COMMON_TIMERS_H
#define PR_COMMON_TIMERS_H

#include <windows.h>

namespace pr
{
	namespace rtc
	{
		typedef unsigned __int64 Ticks;

		namespace impl
		{
			inline Ticks ReadCPUFreq()
			{
				LARGE_INTEGER freq;
				if (QueryPerformanceFrequency(&freq)) return static_cast<Ticks>(freq.QuadPart);
				return 1;
			}
		}

		// Return the value of the real time clock (in ticks)
		inline Ticks Read()
		{
			LARGE_INTEGER tick;
			if (QueryPerformanceCounter(&tick)) return static_cast<Ticks>(tick.QuadPart);
			return 0;
		}

		// Return the clock frequency of the CPU (in ticks/second)
		inline Ticks ReadCPUFreq()
		{
			static Ticks cpu_freq = impl::ReadCPUFreq();
			return cpu_freq;
		}

		// Return a number of RTC ticks interpreted as seconds
		inline double ToSec(Ticks ticks)
		{
			return ticks / (double)ReadCPUFreq();
		}

		// Return a number of RTC ticks interpreted as milliseconds
		inline double ToMSec(Ticks ticks)
		{
			return ticks * 1000.0 / (double)ReadCPUFreq();
		}

		// Return the value of the real time clock in seconds
		inline double ReadRTC_sec()
		{
			return Read() / (double)ReadCPUFreq();
		}

		// Helper stopwatch type
		struct StopWatch
		{
			Ticks m_start;
			Ticks m_stop;
			Ticks m_accum;

			void start(bool reset_first)  { if (reset_first) reset(); start(); }
			void start()                  { m_start = Read(); }
			void stop()                   { m_stop  = Read(); m_accum += m_stop - m_start; }
			void reset()                  { m_accum = 0; }
			Ticks period() const          { return m_accum; }
			double period_s() const       { return ToSec(m_accum); }
			double period_ms() const      { return period_s() * 1000.0; }
			StopWatch() :m_start(0) ,m_stop(0) ,m_accum(0) { ReadCPUFreq(); }
		};

		// Not portable enough
		//// Return the value of the real time clock
		//#pragma optimize("", off)
		//inline RTCTick ReadRTC_raw()
		//{
		//	RTCTick time_stamp_counter;
		//	__asm cpuid                                     // Prevent out of order execution of the rdtsc instruction
		//	__asm rdtsc                                     // Read time stamp counter into [eax, edx]
		//	__asm mov dword ptr [time_stamp_counter+0], eax // Low int
		//	__asm mov dword ptr [time_stamp_counter+4], edx // High int
		//	return time_stamp_counter;
		//}
		//#pragma optimize("", on)
	}

	// Set the current thread to run on the first cpu only
	// This solves a bug with some multicore systems that read the
	// performance clock on an arbitrary core causing inconsistent values
	inline void SetAffinityToCPU0()
	{
		SetThreadAffinityMask(GetCurrentThread(), 1);
	}
}

#endif
