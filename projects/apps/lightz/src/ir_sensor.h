#pragma once
#include "forward.h"

namespace lightz
{
	class IRSensor
	{
		rmt_channel_t m_channel;
		RingbufHandle_t m_buffer;

	public:

		IRSensor();
		void Setup();
		void Update();

	private:
	
		void IRSensorRecv();

	};
}
