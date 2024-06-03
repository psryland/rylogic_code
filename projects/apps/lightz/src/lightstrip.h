#pragma once
#include "forward.h"

namespace lightz
{
	class LightStrip
	{
		std::vector<CRGB> m_leds;

	public:

		LightStrip();
		void Setup();
		void Update();
	};
}