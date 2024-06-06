#include "web.h"
#include "config.h"
#include "clock.h"
#include "utils/utils.h"
#include "data/resources.h"

namespace lightz
{
	static char const LineEnd[] = "\r\n";
	static char const BlockEnd[] = "\r\n\r\n";

	Web::Web()
		: m_wifi_server(80)
		, m_buf()
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

	// Receive data from 'client'. Returns false on timeout
	EResponseCode ReceiveData(WiFiClient& client, std::string& buf, unsigned long timeout_ms)
	{
		size_t const MaxPacketSize = 1024 * 1024;
		buf.resize(0);

		// Receive data from the client until a double '\r\n' is received
		for (auto start = millis(); client.connected(); )
		{
			auto sofar = buf.size();
			auto avail = client.available();

			if (avail == 0 && millis() - start > timeout_ms)
				return EResponseCode::REQUEST_TIMEOUT;

			if (sofar + avail > MaxPacketSize)
				return EResponseCode::BAD_REQUEST;

			if (avail == 0)
			{
				delay(1);
				continue;
			}

			buf.resize(sofar + avail);
			auto read = client.readBytes(&buf[sofar], avail);
			buf.resize(sofar + read);

			auto end = buf.find(BlockEnd);
			if (end != std::string::npos)
			{
				buf.resize(end + strlen(BlockEnd));
				return EResponseCode::OK;
			}
		}
		return EResponseCode::REQUEST_TIMEOUT;
	}

	// Parse the request line of the HTTP request
	bool ParseRequestLine(std::string_view& request, std::string_view& method, std::string_view& path, std::string_view& version)
	{
		auto delim0 = request.find(' ');
		if (delim0 == std::string::npos)
			return false;

		auto delim1 = request.find(' ', delim0 + 1);
		if (delim1 == std::string::npos)
			return false;

		auto endl = request.find(LineEnd, delim1 + 1);
		if (endl == std::string::npos)
			return false;

		method = request.substr(0, delim0);
		path = request.substr(delim0 + 1, delim1 - (delim0+1));
		version = request.substr(delim1 + 1, endl - (delim1+1));

		request.remove_prefix(endl + strlen(LineEnd));
		return true;
	}

	// Parse the headers of the HTTP request
	bool ParseHeaders(std::string_view& request, std::vector<std::string_view>& headers)
	{
		for (;!request.empty();)
		{
			// Find the end of the line
			auto endl = request.find(LineEnd);
			if (endl == std::string::npos)
				return false;

			// Add the header to the list
			auto header = request.substr(0, endl);
			if (!header.empty())
				headers.push_back(header);
			
			// An empty line signals the end of the headers
			request.remove_prefix(endl + strlen(LineEnd));
			if (header.empty())
				return true;
		}
		return false;
	}

	// Handle a client connection
	void Web::HandleClient(WiFiClient client)
	{
		size_t const TimeoutMS = 1000;

		// Receive data from the client
		auto& buf = m_buf; // Reuse for performance
		switch (ReceiveData(client, buf, TimeoutMS))
		{
			case EResponseCode::OK:
			{
				break;
			}
			case EResponseCode::REQUEST_TIMEOUT:
			{
				Serial.printf("[0x%08X] Timeout waiting for data\r\n", client.fd());
				SendResponse(client, EResponseCode::REQUEST_TIMEOUT);
				return;
			}
			case EResponseCode::BAD_REQUEST:
			{
				Serial.printf("[0x%08X] Buffer overflow\r\n", client.fd());
				SendResponse(client, EResponseCode::BAD_REQUEST);
				return;
			}
			default:
			{
				Serial.printf("[0x%08X] Unknown error\r\n", client.fd());
				SendResponse(client, EResponseCode::INTERNAL_SERVER_ERROR);
				return;
			}
		}

		// Get the request as a string view
		auto request = std::string_view{ buf.data(), buf.size() };

		// Parse the request line
		EMethod method;
		std::string_view verb, path, version;
		if (!ParseRequestLine(request, verb, path, version) || (method = ToMethod(verb)) == EMethod::INVALID || version != "HTTP/1.1")
		{
			SendResponse(client, EResponseCode::BAD_REQUEST);
			return;
		}

		// Parse the headers
		std::vector<std::string_view> headers;
		if (!ParseHeaders(request, headers))
		{
			SendResponse(client, EResponseCode::BAD_REQUEST);
			return;
		}

		// Dispatch to the handler
		HandleRequest(method, path, headers, request, client);
	}

	// Handle a web request
	void Web::HandleRequest(EMethod method, std::string_view path, headers_t const& headers, std::string_view request, WiFiClient &client)
	{
		Serial.printf("[0x%08X] \033[36m%s %.*s\033[0m\r\n", client.fd(), ToString(method), PRINTF_SV(path));

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
			SendResponse(client, EResponseCode::OK, EContentType::TEXT_HTML, data::index_html);
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/favicon.ico"))
		{
			SendResponse(client, EResponseCode::OK, EContentType::IMAGE_X_ICON, data::favicon_ico);
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/api/state"))
		{
			SendResponse(client, EResponseCode::OK, EContentType::TEXT_JSON, {"{\"state\": \"On\"}"});
			return;
		}
		if (method == EMethod::GET && MatchI(path, "/api/color"))
		{
			SendResponse(client, EResponseCode::OK, EContentType::TEXT_JSON, {"{\"color\": \"#00FF00\"}"});
			return;
		}
		
		SendResponse(client, EResponseCode::NOT_FOUND);
	}

	// Send a response to the client
	void Web::SendResponse(WiFiClient& client, EResponseCode status, EContentType content_type, std::string_view body)
	{
		auto colour = status == EResponseCode::OK ? "\033[92m" : "\033[91m";
		Serial.printf("[0x%08X] %s%d %s\033[0m\r\n", client.fd(), colour, status, ToString(status));

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

	// Update the web server
	void Web::Update()
	{
		// Display connection status on the built-in LED
		if (WiFi.status() == wl_status_t::WL_CONNECTED && !m_connected)
		{
			m_connected = true;
			Serial.printf("WiFi Connected\r\n");
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

			HandleClient(client);
		}
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