#include "forward.h"
#include "console.h"
#include "config.h"
#include "clock.h"
#include "filesys.h"
#include "lightstrip.h"
#include "web.h"

using namespace lightz;

namespace lightz
{
	// Constants
	int const SerialBaudRate = 921600;
	uint8_t const BuiltInLED = 47;

	// Real time clock
	Clock rtc;

	// Access the file system
	FileSys filesys;

	// Serial port console
	Console console;

	// Application config
	Config config;

	// LED light strip controller
	LightStrip lightstrip;

	// Web interface
	Web web;
}

void setup()
{
	try
	{
		Serial.begin(SerialBaudRate);
		Serial.println("\r\n\nStarting...");
		pinMode(BuiltInLED, OUTPUT);

		rtc.Setup();
		filesys.Setup();
		console.Setup();
		config.Setup();
		lightstrip.Setup();
		web.Setup();

		delay(500);
		Serial.println("Setup Complete\n");
	}
	catch(const std::exception& e)
	{
		Serial.printf("Setup Failed: %s\n", e.what());
		for (;; delay(100)){}
	}
}
void loop()
{
	lightstrip.Update();
	web.Update();

	delay(100);
}

#if 0

#define NUM_LEDS 10
#define SPI_DATA 35
#define SPI_CLOCK 47

#include <Arduino.h>
#include <string>
#include <string_view>
#include <algorithm>
#include <fastLED.h>
#include <WiFi.h>
#include <ESP32Console.h>
#include <LittleFS.h>

// Define the array of leds
CRGB leds[NUM_LEDS];

WiFiServer server(80);

ESP32Console::Console console;

fs::LittleFSFS filesys;

void setup()
{
	filesys.begin(true, "/root");
	filesys.mkdir("/config");
	filesys.mkdir("/console");

	//Serial.begin(921600);
	console.begin(921600);
	console.registerSystemCommands();
	console.registerCoreCommands();
	console.registerVFSCommands();
	console.registerNetworkCommands();
	console.registerGPIOCommands();
	console.enablePersistentHistory("/root/console/.history.txt");
	console.setPrompt("%pwd%> ");

	setenv("BOOBS", "80085", 1);
	setenv("HOME", "/root", 1);

	//Serial.println("Starting up...");

	FastLED.addLeds<WS2812B, A1, EOrder::GRB>(leds, NUM_LEDS);

	WiFi.begin(ssid, password);
	server.begin();

	pinMode(RGB_BUILTIN, OUTPUT);

	//Serial.println("Setup Complete");
}

void loop()
{
	static int loopCount = 0;
	static bool connected = false;

	auto avail = Serial.available();
	if (avail != 0)
	{
		static char buf[256];
		auto read = Serial.read(&buf[0], (int)sizeof(buf) - 1);

		buf[read] = 0;
		Serial.print(&buf[0]);
	}

	if (WiFi.status() == wl_status_t::WL_CONNECTED && !connected)
	{
		connected = true;
		Serial.println("WiFi Connected");
		digitalWrite(RGB_BUILTIN, LOW);
	}
	if (WiFi.status() != wl_status_t::WL_CONNECTED)
	{
		connected = false;
		Serial.print(".");
		digitalWrite(RGB_BUILTIN, !digitalRead(RGB_BUILTIN));
		delay(1000);
	}


	++loopCount;
	/*
	if (loopCount % 10000 == 0)
	{
		digitalWrite(RGB_BUILTIN, HIGH);
		Serial.println("LED ON");
	}
	if (loopCount % 10000 == 5000)
	{
		digitalWrite(RGB_BUILTIN, LOW);
		Serial.println("LED OFF");
	}

	//std::printf("LED=%d\n", loopCount % NUM_LEDS);
	//Serial.println("LED=" + (loopCount % NUM_LEDS));
	
	// Turn the LED on, then pause
	leds[loopCount % NUM_LEDS] = 0x0000FF;
	FastLED.show();
	delay(500);

	// Turn the LED on, then pause
	leds[loopCount % NUM_LEDS] = 0x00FF00;
	FastLED.show();
	delay(500);

	// Turn the LED on, then pause
	leds[loopCount % NUM_LEDS] = 0xFF0000;
	FastLED.show();
	delay(500);


	// Now turn the LED off, then pause
	leds[loopCount % NUM_LEDS] = CRGB::Black;
	FastLED.show();
	delay(500);
	*/
}

// https://espressif.github.io/esptool-js/

enum { GPIO_ADC1_0 = 1 };

void setup()
{
	pinMode(RGB_BUILTIN, OUTPUT);
	
	gpio_config_t io_conf = {
		.pin_bit_mask = A1,
		.mode = gpio_mode_t::GPIO_MODE_OUTPUT,
		.pull_up_en = gpio_pullup_t::GPIO_PULLUP_DISABLE,
		.pull_down_en = gpio_pulldown_t ::GPIO_PULLDOWN_DISABLE,
		.intr_type = gpio_int_type_t::GPIO_INTR_DISABLE,
	};
	gpio_config(&io_conf);
	//auto config = gpio_dump_io_configuration();

	auto free_heap = esp_get_minimum_free_heap_size();

	pinMode(A1, OUTPUT);
	Serial.begin(921600);
	Serial.println();
	Serial.println();
	Serial.println("Setup Complete");
	Serial.println("Free heap: " + String(free_heap));
}

void loop_DISABLED1()
{
	delay(100);
	digitalWrite(A1, HIGH);
	delay(100);
	digitalWrite(A1, LOW);
}

void loop_DISABLED()
{
	static int loopCount = 0;
	static char buffer[256];

	++loopCount;
	if (loopCount % 1000 == 0)
	{
		digitalWrite(RGB_BUILTIN, HIGH);
		//Serial.println("LED ON");
	}
	if (loopCount % 1000 == 500)
	{
		digitalWrite(RGB_BUILTIN, LOW);
		//Serial.println("LED OFF");
	}

	auto frac = analogRead(GPIO_ADC1_0) / 4096.0;
	frac = std::max(0.0, std::min(1.0, frac));

	auto ch = static_cast<char>(frac * ('Z' - 'A') + 'A');
	buffer[loopCount % sizeof(buffer)] = ch;

	if (loopCount % sizeof(buffer) == sizeof(buffer) - 1)
	{
		buffer[sizeof(buffer)-1] = 0;
		Serial.println(&buffer[0]);
	}
}


void loop_disabled2() {
  static unsigned long loopCounter = 0;
  static unsigned long lastTime = 0;

  loopCounter++;
  unsigned long currentTime = millis();
  
  if (currentTime - lastTime >= 1000) {  // Print every second
    Serial.print("loop frequency: ");
    Serial.print(loopCounter);
    Serial.println(" Hz");
    loopCounter = 0;  // Reset counter
    lastTime = currentTime;
  }

  // Your loop code goes here
  // Avoid long delays or blocking calls if you want a higher loop frequency
}

#endif