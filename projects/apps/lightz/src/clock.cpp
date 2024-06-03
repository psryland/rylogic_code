#include "clock.h"

namespace lightz
{
	Clock::Clock()
		: m_loop_start()
	{}

	// Setup the clock
	void Clock::Setup()
	{
	}

	// Return the running time in microseconds
	uint64_t Clock::Ticks() const
	{
		return esp_rtc_get_time_us();
	}

	// Return the running time in seconds
	double Clock::Seconds() const
	{
		return Ticks() / 1000000.0;
	}

	// Register the start of the next loop and return the elapsed time in seconds
	void Clock::LoopStart()
	{
		m_loop_start = Ticks();
	}
}