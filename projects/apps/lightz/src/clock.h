#pragma once
#include "forward.h"

namespace lightz
{
	struct Clock
	{
		float m_loop_start;

		Clock();
		void Setup();
		uint64_t Ticks() const;
		double Seconds() const;
		void LoopStart();
	};

	extern Clock rtc;
}