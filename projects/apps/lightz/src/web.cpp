#include "web.h"
#include "config.h"
#include "lightstrip.h"
#include "utils/json.h"
#include "utils/utils.h"
#include "data/resources.h"
#include "utils/term_colours.h"

namespace lightz
{
	static char const LineEnd[] = "\r\n";
	static char const BlockEnd[] = "\r\n\r\n";
	size_t const ReadTimeoutMS = 1000; // 1 second
	size_t const LimitRequestLine = 8190; // same as Apache default
	size_t const LimitRequestFieldSize = 8190; // same as Apache default
	size_t const LimitRequestBody = 10485760; // same as Apache default
	namespace Col = pr::vt100::colour;

	Web::Web()
		: m_wifi_server(80)
		, m_buf(LimitRequestLine, '\0')
		, m_connected()
	{}

	// Setup the web server
	void Web::Setup()
	{
		// Initialize wifi
		WiFi.begin(config.WiFi.SSID, config.WiFi.Password);
		
		// Start the web server
		m_wifi_server.begin();
	}

	// Update the web server
	void Web::Update()
	{
		// Display connection status on the built-in LED
		if (WiFi.status() != wl_status_t::WL_CONNECTED)
		{
			if (m_connected)
				Serial.printf("WiFi Disconnected\r\n");

			m_connected = false;
			digitalWrite(BuiltInLED, (millis() % 1000) > 500);
			return;
		}
		if (WiFi.status() == wl_status_t::WL_CONNECTED && !m_connected)
		{
			m_connected = true;
			Serial.printf("WiFi Connected\r\n");
			digitalWrite(BuiltInLED, LOW);
		}

		// Listen for incoming clients
		for (;;)
		{
			WiFiClient client = m_wifi_server.available();
			if (!client)
				break;

			HandleClient(client);
		}
	}

	// Read a line of text from the client. 
	EResponseCode ReadLine(WiFiClient& client, std::string& buf, size_t max_size)
	{
		buf.resize(0);
		int prev_byte = 0;

		// Read bytes until a newline is found or timeout
		for (auto start = millis(); client.connected(); )
		{
			// Reading one byte at a time is not that inefficient.
			// The wifi client is doing the buffering for us.
			auto avail = client.available();
			if (avail == 0)
			{
				if (millis() - start > ReadTimeoutMS)
					return EResponseCode::REQUEST_TIMEOUT;

				delay(1);
				continue;
			}

			auto byte = client.read();

			if (byte < 0)
			{
				throw std::runtime_error("Read error");
			}
			if (buf.size() == max_size)
			{
				return EResponseCode::BAD_REQUEST;
			}

			buf.push_back(byte);

			if (prev_byte == '\r' && byte == '\n')
			{
				return EResponseCode::OK;
			}

			prev_byte = byte;
		}
		return EResponseCode::REQUEST_TIMEOUT;
	}

	// Convert a string to a method
	EResponseCode ParseMethod(std::string_view verb, EMethod& method)
	{
		if (verb == "GET") { method = EMethod::GET; return EResponseCode::OK; }
		if (verb == "POST") { method = EMethod::POST; return EResponseCode::OK; }
		if (verb == "PUT") { method = EMethod::PUT; return EResponseCode::OK; }
		if (verb == "DELETE") { method = EMethod::DELETE; return EResponseCode::OK; }
		return EResponseCode::BAD_REQUEST;
	}

	// Convert a string into a 'path' with validation
	EResponseCode ParsePath(std::string_view p, std::string& path)
	{
		if (p.empty() || p[0] != '/')
			return EResponseCode::BAD_REQUEST;

		path = std::string(p);
		return EResponseCode::OK;
	}

	// Check the version of the HTTP request
	EResponseCode ParseVersion(std::string_view version)
	{
		return version == "HTTP/1.1" ? EResponseCode::OK : EResponseCode::BAD_REQUEST;
	}

	// Parse the request line of the HTTP request
	EResponseCode ParseRequestLine(std::string_view request, std::string_view& method, std::string_view& path, std::string_view& version)
	{
		auto end = request.find(' ');
		if (end == std::string::npos)
			return EResponseCode::BAD_REQUEST;

		method = request.substr(0, end);
		request.remove_prefix(end + 1);

		end = request.find(' ');
		if (end == std::string::npos)
			return EResponseCode::BAD_REQUEST;

		path = request.substr(0, end);
		request.remove_prefix(end + 1);

		end = request.find(LineEnd);
		if (end == std::string::npos)
			return EResponseCode::BAD_REQUEST;

		version = request.substr(0, end);
		request.remove_prefix(end + strlen(LineEnd));

		return request.empty() ? EResponseCode::OK : EResponseCode::BAD_REQUEST;
	}

	// Parse the headers of the HTTP request
	EResponseCode ParseHeader(std::string_view request, Web::header_t& header)
	{
		header = {};

		auto endl = request.find(LineEnd);
		if (endl == std::string::npos)
			return EResponseCode::BAD_REQUEST;
		
		if (endl == 0)
			return EResponseCode::OK;

		auto end = request.find(':');
		if (end == std::string::npos)
			return EResponseCode::BAD_REQUEST;

		header.first = request.substr(0, end);
		request.remove_prefix(end + 1);

		header.second = request.substr(0, endl);
		request.remove_prefix(endl + strlen(LineEnd));
		return EResponseCode::OK;
	}

	// Load for a specific header
	std::string const* FindHeader(Web::headers_t const& headers, std::string_view name)
	{
		auto it = std::find_if(headers.begin(), headers.end(), [=](auto& h) { return h.first == name; });
		return it != headers.end() ? &it->second : nullptr;
	}

	// Handle a client connection
	void Web::HandleClient(WiFiClient client)
	{
		using namespace Col;

		// Reuse for performance
		auto& buf = m_buf;

		try
		{
			EMethod method;
			std::string path;
			headers_t headers;
			EResponseCode status;

			// Parse the request line
			std::string_view m,p,v;
			if ((status = ReadLine(client, buf, LimitRequestLine)) != EResponseCode::OK ||
				(status = ParseRequestLine(buf, m, p, v)) != EResponseCode::OK ||
				(status = ParseMethod(m, method)) != EResponseCode::OK ||
				(status = ParsePath(p, path)) != EResponseCode::OK ||
				(status = ParseVersion(v)) != EResponseCode::OK)
			{
				SendResponse(client, status, "Failed to read request line");
				return;
			}

			// Parse the headers
			for (;;)
			{
				header_t header;
				if ((status = ReadLine(client, buf, LimitRequestFieldSize)) != EResponseCode::OK ||
					(status = ParseHeader(buf, header)) != EResponseCode::OK)
				{
					SendResponse(client, status, "Failed to read headers");
					return;
				}
				if (header.first.empty())
				{
					break;
				}
				headers.push_back(header);
			}

			// Parse content
			auto content_length = FindHeader(headers, "Content-Length");
			if (content_length)
			{
				buf.resize(std::min<size_t>(std::stoull(*content_length), LimitRequestBody));
				auto read = client.read(reinterpret_cast<uint8_t*>(buf.data()), buf.size());
				if (read != buf.size())
				{
					SendResponse(client, EResponseCode::BAD_REQUEST, "Failed to read content");
					return;
				}
			}
			else
			{
				buf.resize(0);
			}

			// Dispatch the request
			HandleRequest(method, path, headers, buf, client);
		}
		catch(const std::exception& ex)
		{
			SendResponse(client, EResponseCode::BAD_REQUEST, ex.what());
		}
	}

	// Handle a web request
	void Web::HandleRequest(EMethod method, std::string_view path, headers_t const& headers, std::string_view body, WiFiClient &client)
	{
		if (config.WiFi.ShowWebTrace)
		{
			auto colour = Col::CYAN;
			Serial.printf("[0x%08X] %s%s %.*s" TC_RESET "\r\n", client.fd(), colour, ToString(method), PRINTF_SV(path));
		}

		// Split the path into the path and query string
		std::string_view query;
		auto qp = path.find('?');
		if (qp != std::string::npos)
		{
			query = path.substr(qp + 1);
			path = path.substr(0, qp);
		}

		// Handle the request
		if (method == EMethod::GET && (MatchI(path, "/") || MatchI(path, "/index.html")))
		{
			SendResponse(client, EResponseCode::OK, {}, EContentType::TEXT_HTML, data::index_html);
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/favicon.ico"))
		{
			SendResponse(client, EResponseCode::OK, {}, EContentType::IMAGE_X_ICON, data::favicon_ico);
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/api/state"))
		{
			pr::json::Document doc;
			doc.root()["state"] = lightstrip.On() ? "On" : "Off";
			auto json = pr::json::Serialize(doc, {});
			SendResponse(client, EResponseCode::OK, {}, EContentType::TEXT_JSON, json);
			return;
		}
		if (method == EMethod::POST && MatchI(path, "/api/state"))
		{
			auto jobj = pr::json::Parse(body, {}).to_object();
			auto state = jobj["state"].to<std::string_view>();
			if (MatchI(state, "on"))
			{
				lightstrip.On(true);
				SendResponse(client, EResponseCode::OK);
				return;
			}
			if (MatchI(state, "off"))
			{
				lightstrip.On(false);
				SendResponse(client, EResponseCode::OK);
				return;
			}
			SendResponse(client, EResponseCode::BAD_REQUEST, "Invalid state value");
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/api/color"))
		{
			//pr::json::Object doc;
			//doc["color"] = Fmt("\"#%02X%02X%02X\"}", static_cast<int>(rgb.r), static_cast<int>(rgb.g), static_cast<int>(rgb.b));
			//// Fmt("RGB: #%02X%02X%02X", rgb.r, rgb.g, rgb.b);

			CRGB rgb(config.LED.Colour);

			char buf[64];
			auto n = snprintf(&buf[0], sizeof(buf), "{\"color\": \"#%02X%02X%02X\"}", static_cast<int>(rgb.r), static_cast<int>(rgb.g), static_cast<int>(rgb.b));
			auto color = std::string_view{buf, static_cast<size_t>(n)};
			SendResponse(client, EResponseCode::OK, {}, EContentType::TEXT_JSON, color);
			return;
		}
		if (method == EMethod::POST && MatchI(path, "/api/color"))
		{
			auto jobj = pr::json::Parse(body, {}).to_object();
			auto jcolor = jobj["color"].to<std::string_view>();
			if (jcolor.size() == 7 && jcolor[0] == '#')
			{
				lightstrip.Colour(strtoul(jcolor.data() + 1, nullptr, 16));
				SendResponse(client, EResponseCode::OK);
				return;
			}
			if (jcolor.size() == 4 && jcolor[0] == '#')
			{
				auto rgb = strtoul(jcolor.data() + 1, nullptr, 16);
				lightstrip.Colour(CRGB(
					((rgb >> 8) & 0xF) * 0xff,
					((rgb >> 4) & 0xF) * 0xff,
					((rgb >> 0) & 0xF) * 0xff));
				SendResponse(client, EResponseCode::OK);
				return;
			}
			SendResponse(client, EResponseCode::BAD_REQUEST, "Invalid color value");
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/api/ledcount"))
		{
			int ledcount = config.LED.NumLEDs;
			std::string json = "{}";//pr::json::Serialize();
			SendResponse(client, EResponseCode::OK, {}, EContentType::TEXT_JSON, json);
			return;
		}
		SendResponse(client, EResponseCode::NOT_FOUND, "Unknown endpoint");
	}

	// Send a response to the client
	void Web::SendResponse(WiFiClient& client, EResponseCode status, std::string_view details, EContentType content_type, std::string_view body)
	{
		if (config.WiFi.ShowWebTrace)
		{
			auto colour = status == EResponseCode::OK ? Col::GREEN : Col::RED;
			Serial.printf("[0x%08X] %s%d %s - %.*s" TC_RESET "\r\n", client.fd(), colour, status, ToString(status), PRINTF_SV(details));
		}

		client.printf("HTTP/1.1 %d %s\r\n", status, ToString(status));
		if (!body.empty())
		{
			client.printf("Content-Type: %s\r\n", ToString(content_type));
			client.printf("Content-Length: %d\r\n", static_cast<int>(body.size()));
		}
		client.printf("Connection: close\r\n");
		client.printf("\r\n");
		if (!body.empty())
		{
			client.write(body.data(), body.size());
		}
		client.stop();
	}
}



#if 0
// Load Wi-Fi library
#include <WiFi.h>

// Replace with your network credentials
const char* ssid = "MishMashLabs";
const char* password = "mishmash";

// Set web server port number to 80
WiFiServer server(80);

// Variable to store the HTTP request
String header;

// Auxiliar variables to store the current output state
String output15State = "off";
String output4State = "off";

// Assign output variables to GPIO pins
const int output15 = 15;
const int output4 = 4;

// Current time
unsigned long currentTime = millis();
// Previous time
unsigned long previousTime = 0; 
// Define timeout time in milliseconds (example: 2000ms = 2s)
const long timeoutTime = 2000;

// Define Authentication
const char* base64Encoding = "TWlzaE1hc2hMYWJzOm1pc2htYXNo";  // base64encoding user:pass - "dXNlcjpwYXNz", MishMashLabs:mishmash - "TWlzaE1hc2hMYWJzOm1pc2htYXNo"


void setup() {
  Serial.begin(115200);
  // Initialize the output variables as outputs
  pinMode(output15, OUTPUT);
  pinMode(output4, OUTPUT);
  // Set outputs to LOW
  digitalWrite(output15, LOW);
  digitalWrite(output4, LOW);

  // Connect to Wi-Fi network with SSID and password
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  // Print local IP address and start web server
  Serial.println("");
  Serial.println("WiFi connected.");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
  server.begin();
}

void loop(){
  WiFiClient client = server.available();   // Listen for incoming clients

  if (client) {                             // If a new client connects,
    currentTime = millis();
    previousTime = currentTime;
    Serial.println("New Client.");          // print a message out in the serial port
    String currentLine = "";                // make a String to hold incoming data from the client
    while (client.connected() && currentTime - previousTime <= timeoutTime) {  // loop while the client's connected
      currentTime = millis();
      if (client.available()) {             // if there's bytes to read from the client,
        char c = client.read();             // read a byte, then
        Serial.write(c);                    // print it out the serial monitor
        header += c;
        if (c == '\n') {                    // if the byte is a newline character
          // if the current line is blank, you got two newline characters in a row.
          // that's the end of the client HTTP request, so send a response:
          if (currentLine.length() == 0) {
            // check base64 encode for authentication
            // Finding the right credentials
            if (header.indexOf(base64Encoding)>=0)
            {
            
              // HTTP headers always start with a response code (e.g. HTTP/1.1 200 OK)
              // and a content-type so the client knows what's coming, then a blank line:
              client.println("HTTP/1.1 200 OK");
              client.println("Content-type:text/html");
              client.println("Connection: close");
              client.println();
              
              // turns the GPIOs on and off
              if (header.indexOf("GET /15/on") >= 0) {
                Serial.println("GPIO 15 on");
                output15State = "on";
                digitalWrite(output15, HIGH);
              } else if (header.indexOf("GET /15/off") >= 0) {
                Serial.println("GPIO 15 off");
                output15State = "off";
                digitalWrite(output15, LOW);
              } else if (header.indexOf("GET /4/on") >= 0) {
                Serial.println("GPIO 4 on");
                output4State = "on";
                digitalWrite(output4, HIGH);
              } else if (header.indexOf("GET /4/off") >= 0) {
                Serial.println("GPIO 4 off");
                output4State = "off";
                digitalWrite(output4, LOW);
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
              client.println("<body><h1>ESP32 Web Server</h1>");
              
              // Display current state, and ON/OFF buttons for GPIO 15  
              client.println("<p>GPIO 15 - State " + output15State + "</p>");
              // If the output15State is off, it displays the ON button       
              if (output15State=="off") {
                client.println("<p><a href=\"/15/on\"><button class=\"button\">ON</button></a></p>");
              } else {
                client.println("<p><a href=\"/15/off\"><button class=\"button button2\">OFF</button></a></p>");
              } 
                 
              // Display current state, and ON/OFF buttons for GPIO 4  
              client.println("<p>GPIO 4 - State " + output4State + "</p>");
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
            else{
              client.println("HTTP/1.1 401 Unauthorized");
              client.println("WWW-Authenticate: Basic realm=\"Secure\"");
              client.println("Content-Type: text/html");
              client.println();
              client.println("<html>Authentication failed</html>");
            }
          } else { // if you got a newline, then clear currentLine
            currentLine = "";
          }
        } else if (c != '\r') {  // if you got anything else but a carriage return character,
          currentLine += c;      // add it to the end of the currentLine
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

#endif