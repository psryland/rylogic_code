#include "web.h"
#include "config.h"
#include "clock.h"

namespace lightz
{
	Web::Web()
		: m_wifi_server(80)
		, m_web_server_thread()
		, m_cv_clients()
		, m_clients()
		, m_mutex()
		, m_shutdown()
		, m_elapsed()
		, m_connected(false)
	{}
	Web::~Web()
	{
		m_shutdown = true;
		m_cv_clients.notify_all();
		if (m_web_server_thread.joinable())
			m_web_server_thread.join();
	}

	// Setup the web server
	void Web::Setup()
	{
		// Initialize wifi
		WiFi.begin(config.WiFi.SSID, config.WiFi.Password);
		
		// Start the web server
		m_wifi_server.begin();

		// Start the web server handling thread
		m_web_server_thread = std::thread([this]()
		{
			try { ThreadMain(); }
			catch (const std::exception& e) { Serial.printf("Web Thread Failed: %s\n", e.what()); }
		});
	}
	
	// Web server thread
	void Web::ThreadMain()
	{
		for (;!m_shutdown;)
		{
			// Wait for a client
			WiFiClient client;
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				m_cv_clients.wait(lock, [this]() { return m_shutdown || !m_clients.empty(); });
				if (m_shutdown)
					break;

				client = m_clients.back();
				m_clients.pop_back();
			}

			printf("Web Client connected\n");
			static auto currentTime = millis();
			static auto previousTime = currentTime;
			static auto timeoutTime = 10000;
			static String header;
			static String currentLine;
			static String output15State = "off";
			static String output4State = "off";

			// loop while the client's connected
			while (client.connected())// && currentTime - previousTime <= timeoutTime)
			{
				currentTime = millis();
					
				// if there's bytes to read from the client,
				if (client.available())
				{
					// read a byte, then print it out the serial monitor
					char c = client.read();
					Serial.write(c);

					header += c;
					if (c == '\n')
					{
						// if the current line is blank, you got two newline characters in a row.
						// that's the end of the client HTTP request, so send a response:
						if (currentLine.length() == 0)
						{
							// HTTP headers always start with a response code (e.g. HTTP/1.1 200 OK)
							// and a content-type so the client knows what's coming, then a blank line:
							client.println("HTTP/1.1 200 OK");
							client.println("Content-type:text/html");
							client.println("Connection: close");
							client.println();

							// turns the GPIOs on and off
							if (header.indexOf("GET /15/on") >= 0)
							{
								Serial.println("GPIO 15 On");
								output15State = "on";
								//digitalWrite(output15, HIGH);
							}
							else if (header.indexOf("GET /15/off") >= 0)
							{
								Serial.println("GPIO 15 Off");
								output15State = "off";
								//digitalWrite(output15, LOW);
							}
							else if (header.indexOf("GET /4/on") >= 0)
							{
								Serial.println("GPIO 4 on");
								output4State = "on";
								//digitalWrite(output4, HIGH);
							}
							else if (header.indexOf("GET /4/off") >= 0)
							{
								Serial.println("GPIO 4 off");
								output4State = "off";
								//digitalWrite(output4, LOW);
							}
							
							// Display the HTML web page
							client.println("<!DOCTYPE html><html>");
							client.println("<head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
							client.println("<link rel=\"icon\" href=\"data:,\">");

							// CSS to style the on/off buttons 
							// Feel free to change the background-color and font-size attributes to fit your preferences
							client.println("<style>html { font-family: Helvetica; display: inline-block; margin: 0px auto; text-align: center;}");
							client.println(".button { background-color: #4CAF50; border: none; color: white; padding: 16px 40px;");
							client.println("text-decoration: none; font-size: 30px; margin: 2px; cursor: pointer;}");
							client.println(".button2 {background-color: #555555;}</style></head>");
							
							// Web Page Heading
							client.println("<body><h1>Mish Mash Labs</h1>");
							client.println("<body><h1>ESP32 Web Server</h1>");
							
							// Display current state, and ON/OFF buttons for GPIO 15  
							client.println("<p>GPIO 15 - Currently " + output15State + "</p>");

							// If the output15State is off, it displays the ON button       
							if (output15State=="off") {
							client.println("<p><a href=\"/15/on\"><button class=\"button\">ON</button></a></p>");
							} else {
							client.println("<p><a href=\"/15/off\"><button class=\"button button2\">OFF</button></a></p>");
							} 
							
							// Display current state, and ON/OFF buttons for GPIO 4  
							client.println("<p>GPIO 4 - Currently" + output4State + "</p>");

							// If the output4State is off, it displays the ON button       
							if (output4State=="off") {
							client.println("<p><a href=\"/4/on\"><button class=\"button\">ON</button></a></p>");
							} else {
							client.println("<p><a href=\"/4/off\"><button class=\"button button2\">OFF</button></a></p>");
							}
							client.println("</body></html>");

							// The HTTP response ends with another blank line
							client.println();

							// Break out of the while loop
							break;
						}
						// if you got a newline, then clear currentLine
						else
						{
							currentLine = "";
						}
					}
					// if you got anything else but a carriage return character,
					else if (c != '\r')
					{
						// add it to the end of the currentLine
						currentLine += c;
					}
				}
			}

			// Clear the header variable
			header = "";

			// Close the connection
			client.stop();
			Serial.println("Client disconnected.");
			Serial.println("");
		}
	}

	// Update the web server
	void Web::Update()
	{
		// Display connection status on the built-in LED
		if (WiFi.status() == wl_status_t::WL_CONNECTED && !m_connected)
		{
			m_connected = true;
			printf("WiFi Connected\n");
			digitalWrite(BuiltInLED, LOW);
		}
		if (WiFi.status() != wl_status_t::WL_CONNECTED)
		{
			m_connected = false;
			digitalWrite(BuiltInLED, fmod(rtc.Seconds(), 1.0) > 0.5);
		}

		// Listen for incoming clients
		for (;;)
		{
			WiFiClient client = m_wifi_server.available();
			if (!client)
				break;

		 	std::unique_lock<std::mutex> lock(m_mutex);
		 	m_clients.push_back(client);
		 	m_cv_clients.notify_all();
		}
	}
}
