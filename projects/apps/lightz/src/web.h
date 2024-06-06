#pragma once
#include "forward.h"
#include "utils/http.h"

namespace lightz
{
	struct Web
	{
		using header_t = std::pair<std::string, std::string>;
		using headers_t = std::vector<header_t>;
		using clients_t = std::deque<WiFiClient>;

		WiFiServer m_wifi_server;
		std::string m_buf;
		bool m_connected;

		Web();
		void Setup();
		void Update();

		void ThreadMain();
		void HandleClient(WiFiClient client);
		void HandleRequest(EMethod method, std::string_view path, headers_t const& headers, std::string_view body, WiFiClient &client);
		void SendResponse(WiFiClient& client, EResponseCode status, std::string_view details = {}, EContentType content_type = {}, std::string_view body = {});
	};
}
