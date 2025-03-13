//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/streaming/stream_sources.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"

namespace pr::rdr12
{
	StreamSources::StreamSources(Renderer& rdr)
		: m_rdr(&rdr)
		, m_winsock()
		, m_sources()
		, m_listen_port()
		, m_listen_thread()
		, m_mutex()
	{}
	StreamSources::~StreamSources()
	{
		StopConnections();
	}

	// Allow connections on 'port'
	void StreamSources::AllowConnections(uint16_t listen_port)
	{
		using Socket = network::Socket;

		StopConnections();

		// Start the thread for incoming connections
		m_listen_port = listen_port;
		m_listen_thread = std::jthread([this, listen_port]()
		{
			threads::SetCurrentThreadName("Stream Sources Listen Thread");
			enum class EState { Disconnected, Idle, Listening, Broken } state = EState::Disconnected;

			// Check for client connections to the server and dump old connections
			Socket listen_socket;
			for (;!m_listen_thread.get_stop_token().stop_requested();)
			{
				// Don't exit the thread unless shutting down.
				// Try to handle re-connections, and other errors gracefully.
				try
				{
					switch (state)
					{
						case EState::Disconnected:
						{
							// Create the listen socket. If this fails with WSAEACCESS, it's probably because the firewall is blocking it
							listen_socket = Socket(::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP));
							if (listen_socket == nullptr)
								network::Throw(WSAGetLastError());

							// Bind the local address to the socket
							sockaddr_in my_address = {};
							my_address.sin_family = AF_INET;
							my_address.sin_addr.S_un.S_addr = INADDR_ANY;
							my_address.sin_port = htons(static_cast<u_short>(listen_port));
							auto result = ::bind(listen_socket, (sockaddr const*)&my_address, sizeof(my_address));
							if (result == SOCKET_ERROR)
								network::Throw(WSAGetLastError());

							state = EState::Idle;
							break;
						}
						case EState::Idle:
						{
							// Start listening for incoming connections
							auto result = ::listen(listen_socket, SOMAXCONN);
							if (result != SOCKET_ERROR || WSAGetLastError() == WSAEISCONN) // No error, or already connected
							{
								state = EState::Listening;
								break;
							}

							// Listen failed, check the error code
							auto code = WSAGetLastError();
							switch (code)
							{
								case WSAEINPROGRESS: // A blocking Windows Sockets 1.1 call is in progress, or the service provider is still processing a callback function.
								case WSAENETDOWN:    // The network subsystem has failed.
								case WSAEWOULDBLOCK:
								{
									// Retry after a delay
									std::this_thread::sleep_for(std::chrono::milliseconds(200));
									break;
								}
								default:
								{
									network::Throw(code);
									break;
								}
							}
							break;
						}
						case EState::Listening:
						{
							// Wait for new connections
							if (network::SelectToRecv(listen_socket, 100))
							{
								// Someone is trying to connect
								sockaddr_in client_addr;
								auto client_addr_size = static_cast<int>(sizeof(client_addr));
								auto client = Socket(::accept(listen_socket, (sockaddr*)&client_addr, &client_addr_size));
								network::Check(client != INVALID_SOCKET, "Accepting connection failed");

								// Add this connection as a new source
								std::unique_ptr<StreamSource> source{ new StreamSource(m_rdr, std::move(client), client_addr) };
								{
									std::unique_lock<std::mutex> lock(m_mutex);
									m_sources.push_back(std::move(source));
								}
							}

							// Remove dead connections
							{
								// Remove dead sockets from the container
								std::unique_lock<std::mutex> lock(m_mutex);
								auto end = std::remove_if(std::begin(m_sources), std::end(m_sources), [](auto& s) { return s->m_socket == nullptr; });
								if (end != std::end(m_sources))
								{
									m_sources.erase(end, std::end(m_sources));
								}
							}
							break;
						}
						case EState::Broken:
						{
							std::unique_lock<std::mutex> lock(m_mutex);
							m_sources.resize(0);

							// Clean up the broken socket
							listen_socket = nullptr;
							state = EState::Disconnected;
							break;
						}
						default:
						{
							throw std::runtime_error("Unknown state");
						}
					}
				}
				catch (std::exception const& ex)
				{
					assert(false && ex.what()); (void)ex;
					state = EState::Broken;
				}
				catch (...)
				{
					assert(false && "Unhandled exception in TCP listen thread");
					state = EState::Broken;
				}
			}

			// Clean up
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				m_sources.resize(0);

				// Clean up the broken socket
				listen_socket = nullptr;
				state = EState::Disconnected;
			}
		});
	}

	// Close all connections and stop listening
	void StreamSources::StopConnections()
	{
		// Stop the incoming connections thread
		m_listen_thread.get_stop_source().request_stop();
		if (m_listen_thread.joinable())
			m_listen_thread.join();
	}
}
