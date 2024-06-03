#include "lightstrip.h"
#include "config.h"

namespace lightz
{
	LightStrip::LightStrip()
		:m_leds()
	{}

	void LightStrip::Setup()
	{
		m_leds.resize(config.LED.NumLEDs);
		for (auto& led : m_leds)
			led = config.LED.Colour;

		FastLED.addLeds<WS2812B, A1, EOrder::GRB>(m_leds.data(), m_leds.size());
	}

	void LightStrip::Update()
	{
		for (auto& led : m_leds)
			led = config.LED.Colour;

		FastLED.show();
	}
}