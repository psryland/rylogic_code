#pragma once
#include "forward.h"
#include "utils/ini_file.h"

namespace lightz
{
	struct Config
	{
		static char const* FilePath;

		// (name, type, key, default)
		#define LIGHTZ_WIFI_CONFIG(x)\
			x(String, SSID, "ssid", "Your-SSID-Here")\
			x(String, Password, "password", "Your-WiFi-Password-Here")\
			x(bool, ShowWebTrace, "show-web-trace", "true")

		#define LIGHTZ_LED_CONFIG(x)\
			x(int, NumLEDs, "num-leds", 1)\
			x(CRGB, Colour, "colour", 0x101010)

		struct WiFiConfig
		{
			#define x(type, name, key, def) type name = def;
			LIGHTZ_WIFI_CONFIG(x)
			#undef x
		};

		struct LEDConfig
		{
			#define x(type, name, key, def) type name = def;
			LIGHTZ_LED_CONFIG(x)
			#undef x
		};

		bool SavePending; // Need a dummy first member
		WiFiConfig WiFi;
		LEDConfig LED;

		Config();
		void Setup();
		void Load();
		void Load(ini_file::Iterator& it);
		void Save();
		void Save(fs::File& file);
		void Print();

		String Get(std::string_view full_key) const;
		bool Set(std::string_view full_key, std::string_view value);
	};

	// Singleton instance of the configuration
	extern Config config;
}