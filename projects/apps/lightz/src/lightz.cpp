#include "forward.h"
#include "console.h"
#include "config.h"
#include "filesys.h"
#include "lightstrip.h"
#include "ir_sensor.h"
#include "web.h"

using namespace lightz;

namespace lightz
{
	// Access the file system
	FileSys filesys;

	// Serial port console
	Console console;

	// Application config
	Config config;

	// LED light strip controller
	LightStrip lightstrip;

	// IR sensor
	IRSensor irsensor;

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

		filesys.Setup();
		console.Setup();
		config.Setup();
		web.Setup();
		lightstrip.Setup();
		//irsensor.Setup();

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
	web.Update();
	//irsensor.Update();
	lightstrip.Update();

	delay(1);
}
