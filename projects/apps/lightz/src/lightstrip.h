#pragma once
#include "forward.h"

namespace lightz
{
	class LightStrip
	{
		std::vector<CRGB> m_leds;
		bool m_on;

	public:

		LightStrip();
		void Setup();

		// Update the state of the light strip
		void Update();

		// Get/Set the colour of the light strip
		CRGB Colour() const;
		void Colour(CRGB colour);

		// Get/Set the light strip on or off
		bool On() const;
		void On(bool on);
	};

	extern LightStrip lightstrip;
}