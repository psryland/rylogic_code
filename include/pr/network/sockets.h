//*****************************************
// Sockets
//	Copyright (c) Rylogic 2019
//*****************************************
#pragma once
#include <span>
#include <mutex>
#include <thread>
#include <atomic>
#include <condition_variable>
#include <functional>
#include <optional>
#include <cstdint>
#include <cassert>
#include "winsock.h"

namespace pr::network
{
	// Return the maximum packet size supported by the network
	inline size_t GetMaxPacketSize(SOCKET socket)
	{
		union { unsigned int max_packet_size; char first_byte; } u = {}; int len = sizeof(u);
		Check(::getsockopt(socket, SOL_SOCKET, SO_MAX_MSG_SIZE, &u.first_byte, &len), "Failed to get socket options");
		return u.max_packet_size;
	}

	// Convert a time in milliseconds to a timeval
	inline timeval TimeVal(long timeout_ms)
	{
		return timeval {
			.tv_sec  = timeout_ms/1000,
			.tv_usec = 0,
		};
	}

	// Block up to 'timeout_ms' waiting for 'socket' to be available for sending
	inline bool SelectToSend(SOCKET socket, int timeout_ms = ~0)
	{
		// Select the socket to check that there's transmit buffer space
		fd_set set = {};
		FD_SET(socket, &set);
		auto timeout = TimeVal(timeout_ms);
		auto result = ::select(0, 0, &set, 0, timeout_ms != ~0 ? &timeout : nullptr);
		if (result == 0)
			return false; // timeout, connection still fine but no room to send

		Check(result != SOCKET_ERROR, "Select failed");
		return true;
	}

	// Block up to 'timeout_ms' waiting for 'socket' to be available for receiving
	inline bool SelectToRecv(SOCKET socket, int timeout_ms = ~0)
	{
		// Wait for the socket to be readable
		fd_set set = {0};
		FD_SET(socket, &set);
		auto timeout = TimeVal(timeout_ms);
		auto result = ::select(0, &set, 0, 0, timeout_ms != ~0 ? &timeout : nullptr);
		if (result == 0)
			return false; // timeout, no more bytes available, connection still fine

		Check(result != SOCKET_ERROR, "Select failed");
		return true;
	}

	// Send a datagram.
	// Datagrams are expected to the <= the maximum packet size of the network.
	inline bool SendDatagram(SOCKET socket, std::span<uint8_t const> data, int timeout_ms = ~0, std::optional<SOCKADDR_IN> addr = std::nullopt)
	{
		if (!SelectToSend(socket, timeout_ms))
			return false;

		auto buf = reinterpret_cast<char const*>(data.data());
		auto len = static_cast<int>(data.size());

		// Send data. Datagram sockets should send all the data or fail.
		auto sent = addr
			? ::sendto(socket, buf, len, 0, &reinterpret_cast<sockaddr const&>(*addr), sizeof(*addr))
			: ::send(socket, buf, len, 0);

		Check(sent != SOCKET_ERROR && sent == len, "Send datagram failed");
		return true;
	}
	
	// Send 'data' as a stream (i.e., repeatedly call send until all data is sent)
	inline bool SendStream(SOCKET socket, std::span<uint8_t const> data, int timeout_ms = ~0, std::optional<SOCKADDR_IN> addr = std::nullopt)
	{
		auto buf = reinterpret_cast<char const*>(data.data());
		auto len = static_cast<int>(data.size());

		// Send all of the data
		size_t bytes_sent = 0;
		for (; len != 0;)
		{
			if (!SelectToSend(socket, timeout_ms))
				return false;

			auto sent = addr
				? ::sendto(socket, buf, len, 0, &reinterpret_cast<sockaddr const&>(*addr), sizeof(*addr))
				: ::send(socket, buf, len, 0);

			Check(sent == 0 || sent != SOCKET_ERROR);
			bytes_sent += sent;
			buf += sent;
			len -= sent;
		}
		return true;
	}

	// Receive a datagram
	// Datagrams are expected to the <= the maximum packet size of the network.
	inline bool RecvDatagram(SOCKET socket, std::span<uint8_t> data, size_t& bytes_read, int timeout_ms = ~0, std::optional<SOCKADDR_IN> addr = std::nullopt)
	{
		bytes_read = 0;
		if (!SelectToRecv(socket, timeout_ms))
			return false;

		auto buf = reinterpret_cast<char*>(data.data());
		auto len = static_cast<int>(data.size());
		auto addrlen = addr ? static_cast<int>(sizeof(*addr)) : 0;

		// Read data. Datagram sockets should read a full datagram or fail.
		int read = addr
			? ::recvfrom(socket, buf, len, 0, &reinterpret_cast<sockaddr&>(*addr), &addrlen)
			: ::recv(socket, buf, len, 0);

		Check(read != SOCKET_ERROR && read != 0, "Receive datagram failed");
		bytes_read = read;
		return true;
	}

	// Receive a stream of data (i.e., repeatedly call recv until all data is received)
	inline bool RecvStream(SOCKET socket, std::span<uint8_t> data, size_t& bytes_read, int timeout_ms = ~0, std::optional<SOCKADDR_IN> addr = std::nullopt)
	{
		auto buf = reinterpret_cast<char*>(data.data());
		auto len = static_cast<int>(data.size());
		auto addrlen = addr ? static_cast<int>(sizeof(*addr)) : 0;

		// Read all of the data
		bytes_read = 0;
		for (; len != 0;)
		{
			// Timeout on select means no more data is available. If the caller provided a timeout,
			// then this is expected so return true. If no timeout was provided, then this function
			// won't return until data arrives or the socket is closed.
			if (!SelectToRecv(socket, timeout_ms))
				return timeout_ms != ~0;

			int read = addr
				? ::recvfrom(socket, buf, len, 0, &reinterpret_cast<sockaddr&>(*addr), &addrlen)
				: ::recv(socket, buf, len, 0);

			Check(read == 0 || read != SOCKET_ERROR);
			
			// Reading zero bytes indicates the socket has been closed gracefully
			if (read == 0)
				return false;

			bytes_read += read;
			buf += read;
			len -= read;
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
		std::thread             m_listen_thread;   // Thread that listens for incoming connections
		std::condition_variable m_cv_run_server;   // Sync
		std::condition_variable m_cv_clients;      // Sync
		std::atomic_bool        m_run_server;      // True while the server should run
		mutable std::mutex      m_mutex;           // Synchronise access to the clients list
		SocketCont              m_clients;         // The connected clients

	public:

		ServerSocket(Winsock const& winsock)
			: m_winsock(winsock)
			, m_listen_socket(INVALID_SOCKET)
			, m_listen_port()
			, m_max_connections()
			, m_listen_thread()
			, m_cv_run_server()
			, m_cv_clients()
			, m_run_server(false)
			, m_mutex()
			, m_clients()
		{}
		ServerSocket(ServerSocket&&) = delete;
		ServerSocket(ServerSocket const&) = delete;
		ServerSocket& operator =(ServerSocket&&) = delete;
		ServerSocket& operator =(ServerSocket const&) = delete;
		~ServerSocket()
		{
			StopConnections();
		}

		// True if the server is listening for connections
		bool Listening() const
		{
			return m_run_server;
		}

		// The port we're listening on
		uint16_t ListenPort() const
		{
			return m_listen_port;
		}

		// Turn on/off the server. 'listen_port' is a port number of your choosing.
		void AllowConnections(uint16_t listen_port, ConnectionCB connect_cb, int max_connections = SOMAXCONN)
		{
			StopConnections();

			// If this fails with WSAEACCESS, it's probably because the firewall is blocking it
			m_listen_port = listen_port;
			m_max_connections = max_connections;
			m_listen_socket = CreateListenSocket(m_listen_port);

			// Start the thread for incoming connections
			m_run_server = true;
			m_listen_thread = std::thread([this](ConnectionCB connect_cb)
			{
				try
				{
					ListenThread(connect_cb);
				}
				catch (std::exception const& ex)
				{
					assert(false && ex.what());
					(void)ex;
				}
				catch (...)
				{
					assert(false && "Unhandled exception in tcp listen thread");
				}
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
			return (timeout_ms != ~0)
				? m_cv_clients.wait_for(lock, std::chrono::milliseconds(timeout_ms), [&]{ return m_clients.size() >= client_count; })
				: (m_cv_clients.wait(lock, [&] { return m_clients.size() >= client_count; }), true);
		}

		// Stop accepting incoming connections
		void StopConnections()
		{
			if (m_listen_socket == INVALID_SOCKET)
				return;

			// Stop the incoming connections thread
			m_run_server = false;
			m_cv_run_server.notify_all();
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
		bool SendStream(std::span<uint8_t const> data, int timeout_ms = ~0)
		{
			Lock lock(m_mutex);

			// Send the data to each client
			bool all_sent = true;
			for (auto& client : m_clients)
				all_sent &= network::SendStream(client, data, timeout_ms);

			return all_sent;
		}

		// Receive data from any client.
		// Returns true when data is read from a client, 'out_client' is the client that was read from.
		// Returns false if no data was read from any client.
		// Throws if a connection was aborted, or had a problem.
		bool RecvStream(std::span<uint8_t> data, size_t& bytes_read, int timeout_ms = 0, SOCKET* out_client = nullptr)
		{
			Lock lock(m_mutex);

			// Attempt to read from all clients
			for (auto& client : m_clients)
			{
				// Read data from this client, if data found, then return it
				bytes_read = 0;
				if (!network::RecvStream(client, data, bytes_read, timeout_ms) || bytes_read == 0)
					continue;

				if (out_client) *out_client = client;
				return true;
			}
			return false;
		}

	private:
			
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
				catch (std::exception const&)
				{
					auto code = WSAGetLastError();
					switch (code) {
					case WSAENETDOWN:
					case WSAECONNRESET:
					case WSAEWOULDBLOCK:
						listening = false;
						break; // retry to listen
					default: throw;
					}
				}
			}
		}

		// Block for up to 'timeout_ms' waiting for incoming connections
		// Returns the number of new clients added (0 or 1)
		size_t WaitForConnections(int timeout_ms, ConnectionCB connect_cb)
		{
			// Test for the listen socket being readable (meaning incoming connection request)
			if (!SelectToRecv(m_listen_socket, timeout_ms))
				return 0; // no incoming connections

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
				if (!SelectToRecv(client, 0))
					continue; // socket cannot be read (i.e. no data, that's fine it's not closed)

				char sink_char;
				auto result = ::recv(client, &sink_char, 1, MSG_PEEK);
				if (result != SOCKET_ERROR)
					continue; // read is ready and the connection is still good

				// Check the socket error for dead connection cases
				auto code = WSAGetLastError();
				switch (code)
				{
					case WSAEINTR:
					case WSAEINPROGRESS:
					case WSAEWOULDBLOCK:
					{
						break;
					}
					case WSAENOTCONN:
					case WSAENETDOWN:
					case WSAENETRESET:
					case WSAESHUTDOWN:
					case WSAECONNABORTED:
					case WSAETIMEDOUT:
					case WSAECONNRESET:
					{
						// Notify disconnect
						if (connect_cb)
							connect_cb(client, nullptr);

						// Close the client socket
						::shutdown(client, SD_BOTH);
						::closesocket(client);
						client = INVALID_SOCKET;
						++dropped;
						break;
					}
					default:
					{
						Throw(code);
						break;
					}
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
		ClientSocket(ClientSocket&&) = default;
		ClientSocket(ClientSocket const&) = delete;
		ClientSocket& operator =(ClientSocket&&) = default;
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

		// True if the socket handle "looks" valid
		bool IsValid() const
		{
			return m_socket != INVALID_SOCKET;
		}

		// Send/Recv data to/from the host
		// Returns true if all data was sent/received
		// Returns false if the connection to the client was closed gracefully
		// Throws if the connection was aborted, or had a problem
		bool SendStream(std::span<uint8_t const> data, int timeout_ms = ~0)
		{
			return network::SendStream(m_socket, data, timeout_ms);
		}
		bool RecvStream(std::span<uint8_t> data, size_t& bytes_read, int timeout_ms = ~0)
		{
			return network::RecvStream(m_socket, data, bytes_read, timeout_ms);
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
