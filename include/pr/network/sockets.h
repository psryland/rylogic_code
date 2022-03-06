//*****************************************
// Sockets
//	Copyright (c) Rylogic 2019
//*****************************************

#pragma once
#include "pr/network/winsock.h"

namespace pr::network
{
	// Return the maximum packet size supported by the network
	inline size_t GetMaxPacketSize(SOCKET socket)
	{
		size_t max_size;
		int max_size_size = sizeof(max_size);
		if (::getsockopt(socket, SOL_SOCKET, SO_MAX_MSG_SIZE, (char*)&max_size, &max_size_size) == SOCKET_ERROR)
			throw exception("Failed to get socket options", WSAGetLastError());

		return max_size;
	}

	// Convert a time in milliseconds to a timeval
	inline timeval TimeVal(long timeout_ms)
	{
		timeval tv;
		tv.tv_sec  = timeout_ms/1000;
		tv.tv_usec = (timeout_ms - tv.tv_sec*1000)*1000;
		return tv;
	}

	// Send data on 'socket'
	// Returns true if all data was sent
	// Throws if the connection was aborted, or had a problem
	inline bool Send(SOCKET socket, void const* data, size_t size, int timeout_ms = ~0)
	{
		auto max_packet_size = GetMaxPacketSize(socket);

		// Send all of the data
		size_t bytes_sent = 0;
		for (auto ptr = static_cast<char const*>(data); size != 0;)
		{
			// Select the socket to check that there's transmit buffer space
			fd_set set = {};
			FD_SET(socket, &set);
			auto timeout = TimeVal(timeout_ms);
			int result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
			if (result == 0) return false; // timeout, connection still fine but not all bytes sent
			Check(result != SOCKET_ERROR, "Select failed");

			// Send data
			int sent = ::send(socket, ptr, static_cast<int>(std::min(size, max_packet_size)), 0);
			Check(sent == 0 || sent != SOCKET_ERROR);

			ptr += sent;
			bytes_sent += sent;
			size -= sent;
		}
		return true;
	}

	// Receive data on 'socket'
	// Returns false if the connection to the client was closed gracefully
	// Throws if the connection was aborted, or had a problem
	// 'flags' : MSG_PEEK
	inline bool Recv(SOCKET socket, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
	{
		bytes_read = 0;
		for (auto ptr = static_cast<char*>(data); size != 0;)
		{
			// Wait for the socket to be readable
			fd_set set = {0};
			FD_SET(socket, &set);
			auto timeout = TimeVal(timeout_ms);
			int result = ::select(0, &set, 0, 0, timeout_ms == ~0 ? nullptr : &timeout);
			if (result == 0) return true; // timeout, no more bytes available, connection still fine
			Check(result != SOCKET_ERROR, "Select failed");

			// Read the data
			int read = ::recv(socket, ptr, int(size), flags);
			if (read == 0) return false; // read zero bytes indicates the socket has been closed gracefully
			Check(read != SOCKET_ERROR);

			ptr += read;
			bytes_read += read;
			size -= read;
		}
		return true;
	}

	// Send data to a particular ip using 'socket'.
	// Use 'GetAddress' to convert an IP:port into a 'SOCKADDR_IN' end point.
	// Returns true if all data was sent, false if there was a problem with the connection
	// Throws if the connection was aborted, or had a problem
	inline bool SendTo(SOCKET socket, SOCKADDR_IN ep, void const* data, size_t size, int timeout_ms = ~0)
	{
		// Set the destination address
		auto max_packet_size = GetMaxPacketSize(socket);

		// Send all of the data to the host
		size_t bytes_sent = 0;
		for (auto ptr = static_cast<char const*>(data); size != 0;)
		{
			// Select the socket to check that there's transmit buffer space
			fd_set set = {};
			FD_SET(socket, &set);
			auto timeout = TimeVal(timeout_ms);
			int result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
			if (result == 0) return false; // timeout, connection still fine but not all bytes sent
			Check(result != SOCKET_ERROR, "Select failed");

			// Send data
			int sent = ::sendto(socket, ptr, static_cast<int>(std::min(size, max_packet_size)), 0, (sockaddr const*)&ep, sizeof(ep));
			Check(sent == 0 || sent != SOCKET_ERROR);

			ptr += sent;
			bytes_sent += sent;
			size -= sent;
		}
		return true;
	}

	// Receive data on 'socket'
	// 'ep' is an output, containing the end-point that the data was received from.
	// 'data','size' is a buffer to receive data into.
	// 'bytes_read' is the size of the data written to 'data'.
	// 'flags' : MSG_PEEK
	// Returns false if the connection to the client was closed gracefully
	// Throws if the connection was aborted, or had a problem
	inline bool RecvFrom(SOCKET socket, SOCKADDR_IN& ep, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
	{
		bytes_read = 0;
		for (auto ptr = static_cast<char*>(data); size != 0;)
		{
			fd_set set = {};
			FD_SET(socket, &set);
			auto timeout = TimeVal(timeout_ms);
			int result = ::select(0, &set, 0, 0, timeout_ms == ~0 ? nullptr : &timeout);
			if (result == 0) return true; // timeout, no more bytes available, connection still fine
			Check(result != SOCKET_ERROR, "Select failed");

			// Read the data
			int ep_size;
			int read = ::recvfrom(socket, ptr, int(size), flags, (sockaddr*)&ep, &ep_size);
			if (read == 0) return false; // read zero bytes indicates the socket has been closed gracefully
			Check(read != SOCKET_ERROR, "recvform failed");

			ptr += read;
			bytes_read += read;
			size -= read;
		}
		return true;
	}

	// Base class for a socket connection with server behaviour
	class ServerSocket
	{
	protected:

		using SocketCont = std::vector<SOCKET>;
		using Lock = std::unique_lock<std::mutex>;
		using ConnectionCB = std::function<void(SOCKET, sockaddr_in const*)>;

		Winsock const&          m_winsock;         // The winsock instance we're bound to
		SOCKET                  m_listen_socket;   // The socket we're listen for incoming connections on
		uint16_t                m_listen_port;     // The port we're listening on
		int                     m_max_connections; // The maximum number of clients we'll accept connections from
		SocketCont              m_clients;         // The connected clients
		bool                    m_run_server;      // True while the server should run
		mutable std::mutex      m_mutex;           // Synchronise access to the clients list
		std::condition_variable m_cv_run_server;   // Sync
		std::condition_variable m_cv_clients;      // Sync
		std::thread             m_listen_thread;   // Thread that listens for incoming connections

		// Thread for listening for incoming connections
		void ListenThread(ConnectionCB connect_cb)
		{
			assert(m_listen_socket != INVALID_SOCKET && "Socket not initialised");

			// Track the number of clients
			size_t client_count = 0;
			{
				Lock lock(m_mutex);
				client_count = m_clients.size();
			}

			// Check for client connections to the server and dump old connections
			for (bool listening = false; m_run_server;)
			{
				// Set 'm_listen_socket' to listen for incoming connections mode
				if (!listening && ::listen(m_listen_socket, m_max_connections) == SOCKET_ERROR)
				{
					auto code = WSAGetLastError();
					switch (code)
					{
					case WSAEISCONN:     // The socket is already connected.
						break;

					case WSAEINPROGRESS: // A blocking Windows Sockets 1.1 call is in progress, or the service provider is still processing a callback function.
 					case WSAENETDOWN:    // The network subsystem has failed.
					case WSAEWOULDBLOCK:
						std::this_thread::sleep_for(std::chrono::milliseconds(200));
						continue; // retry

					default:
						Throw(code);
						break;
					}
				}
				listening = true;

				try
				{
					// Wait for new connections
					if (client_count < size_t(m_max_connections))
						client_count += WaitForConnections(100, connect_cb);
					else
						std::this_thread::sleep_for(std::chrono::milliseconds(100));

					// Remove dead connections
					client_count -= RemoveDeadConnections(connect_cb);
				}
				catch (exception const& ex)
				{
					switch (ex.code()) {
					default: throw;
					case WSAENETDOWN:
					case WSAECONNRESET:
					case WSAEWOULDBLOCK:
						listening = false;
						break; // retry to listen
					}
				}
			}
		}

		// Block for up to 'timeout_ms' waiting for incoming connections
		// Returns the number of new clients added (0 or 1)
		size_t WaitForConnections(int timeout_ms, ConnectionCB connect_cb)
		{
			// Test for the listen socket being readable (meaning incoming connection request)
			fd_set set = {};
			FD_SET(m_listen_socket, &set);
			auto timeout = TimeVal(timeout_ms);
			int result = ::select(0, &set, 0, 0, &timeout);
			if (result == 0) return 0; // no incoming connections
			Check(result != SOCKET_ERROR, "Select socket failed");

			// Someone is trying to connect
			sockaddr_in client_addr;
			auto client_addr_size = static_cast<int>(sizeof(client_addr));
			auto client = ::accept(m_listen_socket, (sockaddr*)&client_addr, &client_addr_size);
			Check(client != INVALID_SOCKET, "Accepting connection failed");

			// Add 'client'
			Lock lock(m_mutex);
			m_clients.push_back(client);
			m_cv_clients.notify_all();

			// Notify connect
			if (connect_cb)
				connect_cb(client, &client_addr);
			return 1;
		}

		// Looks for dead connections and removes them from m_clients
		// Returns the number removed
		size_t RemoveDeadConnections(ConnectionCB connect_cb)
		{
			Lock lock(m_mutex); // Lock access to 'clients'

			// Shutdown closed client sockets
			int dropped = 0;
			for (auto& client : m_clients)
			{
				// Detect shutdown sockets by those that "can be read" but return '0' bytes read
				fd_set set = {};
				FD_SET(client, &set);
				auto timeout = TimeVal(0);
				int result = ::select(0, &set, 0, 0, &timeout);
				if (result == 0) continue; // socket cannot be read (i.e. no data, that's fine it's not closed)
				Check(result != SOCKET_ERROR, "Select failed");

				char sink_char;
				result = ::recv(client, &sink_char, 1, MSG_PEEK);
				if (result != SOCKET_ERROR)
					continue; // read is ready and the connection is still good

				// Check the socket error for dead connection cases
				auto code = WSAGetLastError();
				switch (code) {
				case WSAEINTR:
				case WSAEINPROGRESS:
				case WSAEWOULDBLOCK:
					break;

				case WSAENOTCONN:
				case WSAENETDOWN:
				case WSAENETRESET:
				case WSAESHUTDOWN:
				case WSAECONNABORTED:
				case WSAETIMEDOUT:
				case WSAECONNRESET:
					// Notify disconnect
					if (connect_cb)
						connect_cb(client, nullptr);

					// Close the client socket
					::shutdown(client, SD_BOTH);
					::closesocket(client);
					client = INVALID_SOCKET;
					++dropped;
					break;

				default:
					Throw(code);
					break;
				}
			}

			// Remove dead sockets from the container
			auto end = std::remove_if(std::begin(m_clients), std::end(m_clients), [=](SOCKET s){ return s == INVALID_SOCKET; });
			m_clients.erase(end, std::end(m_clients));
			m_cv_clients.notify_all();
			return dropped;
		}

		// Create m_listen_socket to use for listening on
		virtual SOCKET CreateListenSocket(int port) = 0;

	public:

		ServerSocket(Winsock const& winsock)
			:m_winsock(winsock)
			,m_listen_socket(INVALID_SOCKET)
			,m_listen_port()
			,m_max_connections()
			,m_run_server(false)
			,m_mutex()
			,m_cv_run_server()
			,m_cv_clients()
			,m_listen_thread()
		{}
		ServerSocket(ServerSocket const&) = delete;
		ServerSocket& operator =(ServerSocket const&) = delete;
		~ServerSocket()
		{
			StopConnections();
		}

		// True if the server is listening for connections
		bool Listening() const
		{
			Lock lock(m_mutex);
			return m_run_server;
		}

		// The port we're listening on
		uint16_t ListenPort() const
		{
			return m_listen_port;
		}

		// Turn on/off the server
		// 'listen_port' is a port number of your choosing
		// 'connect_cb' should have the signature: void ConnectionCB(void* ctx, SOCKET socket, sockaddr_in const* client_addr);
		// 'client_addr' will be non-null for connections, null for disconnections
		void AllowConnections(uint16_t listen_port, ConnectionCB connect_cb, int max_connections = SOMAXCONN)
		{
			StopConnections();

			// If this fails with WSAEACCESS, it's probably because the firewall is blocking it
			m_listen_port     = listen_port;
			m_max_connections = max_connections;
			m_listen_socket   = CreateListenSocket(m_listen_port);

			// Start the thread for incoming connections
			m_run_server = true;
			m_listen_thread = std::thread([this](ConnectionCB connect_cb)
			{
				try                              { ListenThread(connect_cb); }
				catch (std::exception const& ex) { assert(false && ex.what()); (void)ex; }
				catch (...)                      { assert(false && "Unhandled exception in tcp listen thread"); }
			}, connect_cb);
		}
		void AllowConnections(uint16_t listen_port, int max_connections = SOMAXCONN)
		{
			AllowConnections(listen_port, [](SOCKET, sockaddr_in const*){}, max_connections);
		}

		// Block until 'client_count' connections have been made
		bool WaitForClients(size_t client_count, int timeout_ms = ~0)
		{
			Lock lock(m_mutex);
			if (timeout_ms != ~0)
				return m_cv_clients.wait_for(lock, std::chrono::milliseconds(timeout_ms), [&]{ return m_clients.size() >= client_count; });

			m_cv_clients.wait(lock, [&] { return m_clients.size() >= client_count; });
			return true;
		}

		// Stop accepting incoming connections
		void StopConnections()
		{
			if (m_listen_socket == INVALID_SOCKET)
				return;

			{// Stop the incoming connections thread
				Lock lock(m_mutex);
				m_run_server = false;
				m_cv_run_server.notify_all();
			}

			if (m_listen_thread.joinable())
				m_listen_thread.join();

			{// Shutdown the listen socket
				::shutdown(m_listen_socket, SD_BOTH);
				::closesocket(m_listen_socket);
				m_listen_socket = INVALID_SOCKET;
			}

			{// Shutdown all client connections
				Lock lock(m_mutex);
				for (auto& client : m_clients)
				{
					::shutdown(client, SD_BOTH);
					::closesocket(client);
				}
				m_clients.resize(0);
			}
		}

		// Return the number of connected clients
		size_t ClientCount() const
		{
			Lock lock(m_mutex);
			return m_clients.size();
		}

		// Send data to all clients
		bool Send(void const* data, size_t size, int timeout_ms = ~0)
		{
			Lock lock(m_mutex);

			// Send the data to each client
			bool all_sent = true;
			for (auto& client : m_clients)
				all_sent &= network::Send(client, data, size, timeout_ms);

			return all_sent;
		}

		// Receive data from any client
		// Returns true when data is read from a client, 'out_client' is the client that was read from
		// Returns false if no data was read from any client.
		// Throws if a connection was aborted, or had a problem
		bool Recv(void* data, size_t size, size_t& bytes_read, int timeout_ms = 0, int flags = 0, SOCKET* out_client = nullptr)
		{
			Lock lock(m_mutex);

			// Attempt to read from all clients
			for (auto& client : m_clients)
			{
				// Read data from this client, if data found, then return it
				bytes_read = 0;
				if (network::Recv(client, data, size, bytes_read, timeout_ms, flags) && bytes_read != 0)
				{
					if (out_client) *out_client = client;
					return true;
				}
			}

			// no data read from any client
			return false;
		}
	};

	// Base class for a socket connection with client behaviour
	class ClientSocket
	{
	protected:

		Winsock const& m_winsock;         // The winsock instance we're bound to
		SOCKET         m_socket;          // The socket we've connected to the host with
		uint16_t       m_port;            // The port we're connected to

	public:

		explicit ClientSocket(Winsock const& winsock)
			:m_winsock(winsock)
			,m_socket(INVALID_SOCKET)
			,m_port()
		{}
		ClientSocket(ClientSocket const&) = delete;
		ClientSocket& operator =(ClientSocket const&) = delete;
		~ClientSocket()
		{
			Close();
		}

		// Close the socket
		void Close()
		{
			if (m_socket == INVALID_SOCKET)
				return;

			::shutdown(m_socket, SD_BOTH);
			::closesocket(m_socket);
			m_socket = INVALID_SOCKET;
		}

		// True if a the socket is connected to a host
		bool IsConnected() const
		{
			return m_socket != INVALID_SOCKET;
		}

		// Send/Recv data to/from the host
		// Returns true if all data was sent/received
		// Returns false if the connection to the client was closed gracefully
		// Throws if the connection was aborted, or had a problem
		bool Send(void const* data, size_t size, int timeout_ms = ~0)
		{
			return network::Send(m_socket, data, size, timeout_ms);
		}
		bool Recv(void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
		{
			return network::Recv(m_socket, data, size, bytes_read, timeout_ms, flags);
		}

		// Get/Set a socket option
		// 'level' - The level at which the option is defined. Example: SOL_SOCKET.
		// 'optname' - The socket option for which the value is to be retrieved. Example: SO_ACCEPTCONN.
		//  The 'optname' value must be a socket option defined within the specified level, or behaviour is undefined.
		// 'optval' [out] - A pointer to the buffer in which the value for the requested option is to be returned.
		// 'optlen' [in, out] - The size, in bytes, of the 'optval' buffer.
		void GetSocketOption(int level, int optname, char* optval, int& optlen) const
		{
			Check(::getsockopt(m_socket, level, optname, optval, &optlen) == 0, "getsockopt failed");
		}
		void SetSocketOption(int level, int optname, char const* optval, int optlen)
		{
			Check(::setsockopt(m_socket, level, optname, optval, optlen) == 0, "setsockopt failed");
		}
		template <typename OptType> OptType SocketOption(int level, int optname) const
		{
			OptType opt; int len = sizeof(opt);
			GetSocketOption(level, optname, (char*)&opt, len);
			return opt;
		}
		template <typename OptType> void SocketOption(int level, int optname, OptType opt)
		{
			return SetSocketOption(level, optname, (char const*)&opt, sizeof(opt));
		}
	};
}
