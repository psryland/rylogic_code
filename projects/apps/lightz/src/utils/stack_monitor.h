#pragma once
#include "forward.h"

namespace lightz
{
	struct StackMonitor
	{
		char const* m_name;
		uint32_t m_max_stack;
		uint32_t m_high_tide;

		StackMonitor(char const* name, uint32_t max_stack)
			: m_name(name)
			, m_max_stack(max_stack)
			, m_high_tide(max_stack * 3 / 4)
		{}
		void operator()()
		{
			auto tide = uxTaskGetStackHighWaterMark(nullptr);
			auto used = m_max_stack - tide;
			if (used > m_high_tide)
			{
				Serial.printf("%s stack used = %d words, %d remaining\r\n", m_name, used, tide);
				m_high_tide = used;
			}
		}
	};
}
