#pragma once
#include "forward.h"
#include "utils/http.h"

namespace lightz
{
	struct Web
	{
		using headers_t = std::vector<std::string_view>;
		using clients_t = std::deque<WiFiClient>;

		WiFiServer m_wifi_server;
		std::string m_buf;
		bool m_connected;

		Web();
		void Setup();
		void Update();

		void ThreadMain();
		void HandleClient(WiFiClient client);
		void HandleRequest(EMethod method, std::string_view path, headers_t const& headers, std::string_view request, WiFiClient &client);
		void SendResponse(WiFiClient& client, EResponseCode status, EContentType content_type = {}, std::string_view body = {});
	};
}
