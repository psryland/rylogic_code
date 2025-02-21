//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/view3d-dll.h"
#include "view3d-12/src/dll/streaming.h"
#include "pr/network/tcpip.h"

namespace pr::view3d
{
	class LDrawServer
	{
		struct Connection
		{
			Guid m_context;
		};

		network::Winsock m_winsock;
		network::TcpServer m_server;
		//std::vector<uint8_t> m_buffer;
		//std::mutex m_mutex;
		//std::condition_variable m_cv;
		//std::atomic<bool> m_running;
		//std::thread m_thread;

	public:

		LDrawServer()
			: m_winsock()
			, m_server(m_winsock)
		{
		}

		void Run()
		{
			while (m_running)
			{
				// Wait for a connection
				if (!m_server.Accept())
					continue;
				// Receive data
				size_t bytes_read = 0;
				while (m_server.Recv(m_buffer, bytes_read))
				{
					// Process the data
					{
						std::lock_guard lock(m_mutex);
						// Process the data
					}
				}
			}
		}
	};
}
