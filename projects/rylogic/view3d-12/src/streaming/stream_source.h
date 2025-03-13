//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct StreamSource
	{
		using Socket = network::Socket;

		Socket       m_socket;     // A non-owning reference to the network connection
		Guid         m_context_id; // Id for the group of files that this object is part of
		ObjectCont   m_objects;    // Objects created by this source
		std::jthread m_thread;     // Thread that receives data from the socket
		std::mutex   m_mutex;      // For synchronising access to 'm_objects'

		StreamSource();
		StreamSource(Renderer* rdr, Socket&& socket, sockaddr_in addr);
		StreamSource(StreamSource&& rhs) noexcept;
		StreamSource(StreamSource const&) = delete;
		StreamSource& operator =(StreamSource&& rhs) noexcept;
		StreamSource& operator =(StreamSource const&) = delete;
		~StreamSource();
	};
}
