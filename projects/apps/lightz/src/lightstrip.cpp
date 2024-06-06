#include "lightstrip.h"
#include "config.h"

namespace lightz
{
	LightStrip::LightStrip()
		: m_leds()
		, m_on(true)
	{}

	void LightStrip::Setup()
	{
		m_leds.resize(config.LED.NumLEDs);
		for (auto& led : m_leds)
			led = config.LED.Colour;

		FastLED.addLeds<WS2812B, A1, EOrder::GRB>(m_leds.data(), m_leds.size());
	}

	// Update the state of the light strip
	void LightStrip::Update()
	{
		auto colour = m_on ? config.LED.Colour : CRGB::Black;
		for (auto& led : m_leds)
			led = colour;

		FastLED.show();
	}

	// Get/Set the colour of the light strip
	CRGB LightStrip::Colour() const
	{
		return config.LED.Colour;
	}
	void LightStrip::Colour(CRGB colour)
	{
		if (colour == Colour())
			return;

		config.LED.Colour = static_cast<uint32_t>(colour);
		Update();
	}

	// Get/Set the light strip on or off
	bool LightStrip::On() const
	{
		return m_on;
	}
	void LightStrip::On(bool on)
	{
		if (on == On())
			return;

		m_on = on;
		Update();
	}
}