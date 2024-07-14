#include "config.h"
#include "filesys.h"
#include "console.h"
#include "utils/ini_file.h"
#include "utils/convert.h"
#include "utils/utils.h"
#include "utils/term_colours.h"

namespace lightz
{
	char const* Config::FilePath = "/config.ini";

	// Construct the configuration
	Config::Config()
		: SavePending()
		, WiFi()
		, LED()
	{}

	// Initialise the configuration component
	void Config::Setup()
	{
		Load();
	}

	// Load the configuration from the file system
	void Config::Load()
	{
		if (!filesys.exists(FilePath))
		{
			printf("Configuration file not found, using default configuration\n");
			return;
		}
		
		auto file = filesys.open(FilePath, "r");
		if (!file)
			throw std::runtime_error("Failed to open the configuration file");

		ini_file::Iterator it(file);
		Load(it);
	}
	void Config::Load(ini_file::Iterator& it)
	{
		for (; !it.AtEnd(); )
		{
			if (it.IsMatch(ini_file::EElement::Section, "wifi"))
			{
				for (it.Next(); !it.AtEnd() && it.IsMatch(ini_file::EElement::KeyValue); it.Next())
				{
					#define x(type, name, key, def)\
					if (it.IsMatch(ini_file::EElement::KeyValue, key))\
					{\
						WiFi.name = ConvertTo<type>(it.Value());\
						continue;\
					}
					LIGHTZ_WIFI_CONFIG(x)
					#undef x
				}
				continue;
			}
			if (it.IsMatch(ini_file::EElement::Section, "led"))
			{
				for (it.Next(); !it.AtEnd() && it.IsMatch(ini_file::EElement::KeyValue); it.Next())
				{
					#define x(type, name, key, def)\
					if (it.IsMatch(ini_file::EElement::KeyValue, key))\
					{\
						LED.name = ConvertTo<type>(it.Value());\
						continue;\
					}
					LIGHTZ_LED_CONFIG(x)
					#undef x
				}
				continue;
			}

			// Ignore unknown sections or key/value pairs
			it.Next();
		}
	}

	// Serialize the configuration to the file system
	void Config::Save()
	{
		auto file = filesys.open(FilePath, "w");
		Save(file);
		SavePending = false;
		printf("Configuration saved to %s\n", FilePath);
	}
	void Config::Save(fs::File& file)
	{
		file.println("[wifi]");
		#define x(type, name, key, def)\
		file.printf(key "=%s\n", ToString<type>(WiFi.name).c_str());
		LIGHTZ_WIFI_CONFIG(x)
		#undef x
		file.println();

		file.println("[led]");
		#define x(type, name, key, def)\
		file.printf(key "=%s\n", ToString<type>(LED.name).c_str());
		LIGHTZ_LED_CONFIG(x)
		#undef x
		file.println();
	}

	// Print the configuration to the console
	void Config::Print()
	{
		printf("" TC_CYAN "[wifi]" TC_RESET "\n");
		#define x(type, name, key, def)\
		printf(TC_GREEN key TC_RESET "=%s\n", ToString<type>(WiFi.name).c_str());
		LIGHTZ_WIFI_CONFIG(x)
		#undef x
		printf("\n");

		printf("" TC_CYAN "[led]" TC_RESET "\n");
		#define x(type, name, key, def)\
		printf(TC_GREEN key TC_RESET "=%s\n", ToString<type>(LED.name).c_str());
		LIGHTZ_LED_CONFIG(x)
		#undef x
		printf("\n");

		if (SavePending)
			printf(" *** Save pending ***\n\n");
	}

	// Return the value of a configuration key as a string
	String Config::Get(std::string_view full_key) const
	{
		if (MatchI(full_key, "wifi.", 5))
		{
			auto sub_key = full_key.substr(5);
			#define x(type, name, key, def)\
			if (MatchI(sub_key, key))\
				return ToString<type>(WiFi.name);
			LIGHTZ_WIFI_CONFIG(x)
			#undef x
		}
		if (MatchI(full_key, "led.", 4))
		{
			auto sub_key = full_key.substr(4);
			#define x(type, name, key, def)\
			if (MatchI(sub_key, key))\
				return ToString<type>(LED.name);
			LIGHTZ_LED_CONFIG(x)
			#undef x
		}
		return {};
	}

	// Set the value of a configuration value
	bool Config::Set(std::string_view full_key, std::string_view value)
	{
		// Update the configuration value if changed
		auto DoSet = [this](auto& prop, std::string_view sv_value)
		{
			auto value = ConvertTo<std::decay_t<decltype(prop)>>(sv_value);
			if (prop == value)
				return true;

			prop = value;
			SavePending = true;
			return true;
		};

		if (MatchI(full_key, "wifi.", 5))
		{
			auto sub_key = full_key.substr(5);
			#define x(type, name, key, def)\
			if (MatchI(sub_key, key))\
				return DoSet(WiFi.name, value);
			LIGHTZ_WIFI_CONFIG(x)
			#undef x
		}
		if (MatchI(full_key, "led.", 4))
		{
			auto sub_key = full_key.substr(4);
			#define x(type, name, key, def)\
			if (MatchI(sub_key, key))\
				return DoSet(LED.name, value);
			LIGHTZ_LED_CONFIG(x)
			#undef x
		}
		return false;
	}
}
