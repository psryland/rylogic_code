#include "config.h"
#include "filesys.h"
#include "console.h"
#include "ini_file.h"
#include "util.h"

namespace lightz
{
	char const* Config::FilePath = "/config.ini";

	// Construct the default WiFi configuration
	Config::WiFiConfig::WiFiConfig()
		: SSID("Your-SSID-Here")
		, Password("Your-Password-Here")
	{}

	// Construct the default LED configuration
	Config::LEDConfig::LEDConfig()
		: NumLEDs(1)
		, Colour(0x800000)
	{}

	// Construct the configuration
	Config::Config()
		: WiFi()
		, LED()
	{}

	// Initialise the configuration component
	void Config::Setup()
	{
		if (!filesys.exists(FilePath))
		{
			Serial.println("Configuration file not found, using default configuration");
			return;
		}
		
		auto file = filesys.open(FilePath, "r");
		if (!file)
			throw std::runtime_error("Failed to open the configuration file");

		ini_file::Iterator it(file);
		Load(it);
	}

	// Load the wifi configuration from the file system
	void Config::WiFiConfig::Load(ini_file::Iterator& it)
	{
		for (; !it.AtEnd() && it.IsMatch(ini_file::EElement::KeyValue); it.Next())
		{
			if (it.IsMatch(ini_file::EElement::KeyValue, "ssid"))
			{
				SSID = it.Value();
				continue;
			}
			if (it.IsMatch(ini_file::EElement::KeyValue, "password"))
			{
				Password = it.Value();
				continue;
			}
		}
	}

	// Save the wifi configuration to the file system
	void Config::WiFiConfig::Save(fs::File& file)
	{
		file.printf("ssid=%s\n", SSID.c_str());
		file.printf("password=%s\n", Password.c_str());
	}

	// Print the wifi configuration to the console
	void Config::WiFiConfig::Print()
	{
		printf("ssid=%s\n", SSID.c_str());
		printf("password=%s\n", Password.c_str());
	}

	// Return the value of a configuration key as a string
	String Config::WiFiConfig::Get(String const& key) const
	{
		if (key.equalsIgnoreCase("ssid"))
			return SSID;
		if (key.equalsIgnoreCase("password"))
			return Password;
		return {};
	}
	
	// Set the value of a configuration value
	bool Config::WiFiConfig::Set(String const& key, String const& value)
	{
		if (key.equalsIgnoreCase("ssid"))
		{
			SSID = value;
			return true;
		}
		if (key.equalsIgnoreCase("password"))
		{
			Password = value;
			return true;
		}
		return false;
	}

	// Load the LED configuration from the file system
	void Config::LEDConfig::Load(ini_file::Iterator& it)
	{
		for (; !it.AtEnd() && it.IsMatch(ini_file::EElement::KeyValue); it.Next())
		{
			if (it.IsMatch(ini_file::EElement::KeyValue, "num_leds"))
			{
				NumLEDs = it.Value().toInt();
				continue;
			}
			if (it.IsMatch(ini_file::EElement::KeyValue, "colour"))
			{
				Colour = strtol(it.Value().c_str(), nullptr, 16);
				continue;
			}
		}
	}

	// Save the LED configuration to the file system
	void Config::LEDConfig::Save(fs::File& file)
	{
		file.printf("num_leds=%d\n", NumLEDs);
		file.printf("colour=%d\n", Colour);
	}

	// Print the LED configuration to the console
	void Config::LEDConfig::Print()
	{
		printf("num_leds=%d\n", NumLEDs);
		printf("colour=0x%08X\n", Colour);
	}

	// Return the value of a configuration key as a string
	String Config::LEDConfig::Get(String const& key) const
	{
		if (key.equalsIgnoreCase("num_leds"))
			return String(NumLEDs);
		if (key.equalsIgnoreCase("colour"))
			return String(Colour, 16);
		return {};
	}

	// Set the value of a configuration value
	bool Config::LEDConfig::Set(String const& key, String const& value)
	{
		if (key.equalsIgnoreCase("num_leds"))
		{
			NumLEDs = value.toInt();
			return true;
		}
		if (key.equalsIgnoreCase("colour"))
		{
			Colour = strtol(value.c_str(), nullptr, 16);
			return true;
		}
		return false;
	}

	// Load the configuration from the file system
	void Config::Load(ini_file::Iterator& it)
	{
		for (; !it.AtEnd(); )
		{
			if (it.IsMatch(ini_file::EElement::Section, "wifi"))
			{
				it.Next();
				WiFi.Load(it);
				continue;
			}
			if (it.IsMatch(ini_file::EElement::Section, "led"))
			{
				it.Next();
				LED.Load(it);
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
	}
	void Config::Save(fs::File& file)
	{
		file.println("[wifi]");
		WiFi.Save(file);
		file.println();

		file.println("[led]");
		LED.Save(file);
		file.println();
	}

	// Print the configuration to the console
	void Config::Print()
	{
		printf("[wifi]\n");
		WiFi.Print();
		printf("\n");

		printf("[led]\n");
		LED.Print();
		printf("\n");
	}

	// Return the value of a configuration key as a string
	String Config::Get(String const& key) const
	{
		if (StartsWith(key, "wifi"))
			return WiFi.Get(key.substring(5));
		if (StartsWith(key, "led"))
			return LED.Get(key.substring(4));
		return {};
	}

	// Set the value of a configuration value
	bool Config::Set(String const& key, String const& value)
	{
		if (StartsWith(key, "wifi"))
			return WiFi.Set(key.substring(5), value);
		if (StartsWith(key, "led"))
			return LED.Set(key.substring(4), value);
		return false;
	}
}
