//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
//#include "src/dll/dll_forward.h"
#include "pr/network/winsock.h"
#include "pr/network/sockets.h"

namespace pr::view3d
{
	// Stream LDraw commands
	// Packet Protocol:
	//   Header:
	//      1b: Command

	class StreamSource
	{
	//	using nw = pr::network::TcpServer;

	//	std::atomic_bool m_shutdown;

	//public:

	//	StreamSource(Winsock const& winsock, uint16_t port)
	//		:m_shutdown(false)
	//		, m_server(winsock, port)
	//	{
	//		m_server.Start();
	//	}
	//	~StreamSource()
	//	{
	//		m_server.Stop();
	//	}
	//	void Shutdown()
	//	{
	//		m_shutdown = true;
	//		m_server.Stop();
	//	}
	};
}
