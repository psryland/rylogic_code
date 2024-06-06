#pragma once
#include "forward.h"
#include "utils/ini_file.h"

namespace lightz
{
	struct Config
	{
		static char const* FilePath;

		struct WiFiConfig
		{
			String SSID;
			String Password;
			WiFiConfig();
			void Load(ini_file::Iterator& file);
			void Save(fs::File& file);
			void Print();

			String Get(String const& key) const;
			bool Set(String const& key, String const& value);
		} WiFi;

		struct LEDConfig
		{
			int NumLEDs;
			uint32_t Colour;
			
			LEDConfig();
			void Load(ini_file::Iterator& file);
			void Save(fs::File& file);
			void Print();

			String Get(String const& key) const;
			bool Set(String const& key, String const& value);

		} LED;

		Config();
		void Setup();
		void Load(ini_file::Iterator& it);
		void Save(fs::File& file);
		void Save();
		void Print();

		String Get(String const& key) const;
		bool Set(String const& key, String const& value);
	};

	// Singleton instance of the configuration
	extern Config config;
}