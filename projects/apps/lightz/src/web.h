#pragma once
#include "forward.h"

namespace lightz
{
	struct Web
	{
		WiFiServer m_wifi_server;
		std::thread m_web_server_thread;
		std::condition_variable m_cv_clients;
		std::vector<WiFiClient> m_clients;
		std::mutex m_mutex;
		bool m_shutdown;
		double m_elapsed;
		bool m_connected;

		Web();
		~Web();
		void Setup();
		void Update();

		void ThreadMain();
	};
}
