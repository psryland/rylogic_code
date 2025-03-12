//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "view3d-12/src/streaming/stream_source.h"

namespace pr::rdr12
{
	struct StreamSources
	{
		// Notes:
		//  - A collection of network connections that stream ldraw data/commands.
		//  - This object listens on the configured port for incoming connections.
		//    New connections are assigned a GUID and added as a source.
		//  - A source is bound to a Window using commands sent over the socket.
		//  - Not using 'ServerSocket' because I want to control the client instances
		using SourceCont = std::vector<StreamSource>;

	private:

		Renderer*    m_rdr;           // The owning renderer
		Winsock      m_winsock;       // The 'winsock' instance we're bound to
		SourceCont   m_sources;       // Live connections
		uint16_t     m_listen_port;   // The port we're listening on
		std::jthread m_listen_thread; // Thread that listens for incoming connections
		std::mutex   m_mutex;         // For synchronising access to 'm_sources'

	public:

		explicit StreamSources(Renderer& rdr);
		~StreamSources();

		// Allow connections on 'port'
		void AllowConnections(uint16_t listen_port);

		// Close all connections and stop listening
		void StopConnections();
	};
}
