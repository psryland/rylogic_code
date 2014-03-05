#pragma once
#ifndef PR_NETWORK_TCPIP_H
#define PR_NETWORK_TCPIP_H

#include <string>
#include <vector>
#include <algorithm>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <atomic>
#include <cassert>
#include <exception>

// Thanks microsoft... :-/
#if defined(_INC_WINDOWS) && !defined(_WINSOCK2API_)
#error "winsock2.h must be included before windows.h"
#endif

#include <winsock2.h>
#pragma comment(lib, "ws2_32.lib")

namespace pr
{
	namespace network
	{
		typedef unsigned short uint16;

		// 'client_addr' will be non-null for connections, null for disconnections
		typedef void (*ConnectionCB)(SOCKET socket, sockaddr_in const* client_addr);

		// Network exception
		struct exception : std::exception
		{
			int m_code;

			exception()                         :std::exception("")  ,m_code()     {}
			exception(char const* msg)          :std::exception(msg) ,m_code()     {}
			exception(int code)                 :std::exception("")  ,m_code(code) {}
			exception(char const* msg, int code):std::exception(msg) ,m_code(code) {}
			char const* what() const override { return std::exception::what(); }
			int         code() const          { return m_code; }
		};

		// This is a wrapper of the winsock dll. An instance of this object
		// should have the scope of all network activity
		struct Winsock :WSADATA
		{
			Winsock() :WSADATA()
			{
				if (WSAStartup(MAKEWORD(2, 2) , this) != 0)
					throw exception("WSAStartup failed");
			}
			~Winsock()
			{
				WSACleanup();
			}
		};

		namespace impl
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

			// Convert an ip and port to a socket address
			inline sockaddr_in GetAddress(char const* ip, uint16 port)
			{
				hostent* hp = 0;

				// Convert the ip or dns name to an address
				auto ip_addr = inet_addr(ip);
				if (ip_addr == INADDR_NONE) { hp = ::gethostbyname(ip); }
				else                        { hp = ::gethostbyaddr((char*)&ip_addr, sizeof(ip_addr), AF_INET); }
				if (!hp)
					throw exception("Host address not found");

				sockaddr_in addr = {};
				addr.sin_addr.S_un.S_addr = *((unsigned long*)hp->h_addr_list[0]);
				addr.sin_port             = htons(port);
				addr.sin_family           = AF_INET;
				return addr;
			}

			// Convert a time in milliseconds to a timeval
			inline timeval TimeVal(size_t timeout_ms)
			{
				timeval tv;
				tv.tv_sec  = timeout_ms/1000;
				tv.tv_usec = (timeout_ms - tv.tv_sec*1000)*1000;
				return tv;
			}

			// Send data on 'socket'
			// Returns true if all data was sent
			inline bool Send(SOCKET socket, void const* data, size_t size, size_t max_packet_size, size_t timeout_ms = ~0U)
			{
				timeval timeout = TimeVal(timeout_ms);

				// Send all of the data
				size_t bytes_sent = 0;
				for (auto ptr = static_cast<char const*>(data); size != 0;)
				{
					// Select the socket to check that there's transmit buffer space
					fd_set set = {};
					FD_SET(socket, &set);
					int result = ::select(0, 0, &set, 0, timeout_ms == ~0U ? nullptr : &timeout);
					if (result != 1)
						return false; // no space to send data (timeout) or error

					// Send data
					int sent = ::send(socket, ptr, int(size > max_packet_size ? max_packet_size : size), 0);
					if (sent == SOCKET_ERROR)
						return false;

					ptr += sent;
					bytes_sent += sent;
					size -= sent;
				}
				return true;
			}

			// Receive data on 'socket'
			// Returns true if data was received, false on timeout
			// 'flags' : MSG_PEEK
			inline bool Recv(SOCKET socket, void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				timeval timeout = TimeVal(timeout_ms);
				bytes_read = 0;
				for (auto ptr = static_cast<char*>(data); size != 0;)
				{
					fd_set set = {0};
					FD_SET(socket, &set);
					int result = ::select(0, &set, 0, 0, timeout_ms == ~0U ? nullptr : &timeout);
					if (result == 0)
						return true; // timeout, no more bytes available

					// Read the data
					int read = ::recv(socket, ptr, int(size), flags);
					if (read == 0 || read == SOCKET_ERROR)
						return false; // read zero bytes indicates the socket has been closed

					ptr += read;
					bytes_read += read;
					size -= read;
				}
				return true;
			}

			// Send data to a particular ip using 'socket'.
			// Returns true if all data was sent, false if there was a problem with the connection
			inline bool SendTo(SOCKET socket, char const* host_ip, uint16 host_port, void const* data, size_t size, size_t max_packet_size, size_t timeout_ms = ~0U)
			{
				timeval timeout = impl::TimeVal(timeout_ms);

				// Set the destination address
				sockaddr_in addr = impl::GetAddress(host_ip, host_port);

				// Send all of the data to the host
				size_t bytes_sent = 0;
				for (auto ptr = static_cast<char const*>(data); size != 0;)
				{
					// Select the socket to check that there's transmit buffer space
					fd_set set = {};
					FD_SET(socket, &set);
					int result = ::select(0, 0, &set, 0, timeout_ms == ~0U ? nullptr : &timeout);
					if (result != 1)
						return false; // no space to send data (timeout) or error

					// Send data
					int sent = ::sendto(socket, ptr, int(size <= max_packet_size ? size : max_packet_size), 0, (sockaddr const*)&addr, sizeof(addr));
					if (sent == SOCKET_ERROR)
						return false;

					ptr += sent;
					bytes_sent += sent;
					size -= sent;
				}
				return true;
			}

			// Receive data on 'socket'
			// Returns true if data was received, false on timeout
			// 'flags' : MSG_PEEK
			inline bool RecvFrom(SOCKET socket, char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				timeval timeout = TimeVal(timeout_ms);

				// Set the source address
				sockaddr_in addr = GetAddress(host_ip, host_port);

				bytes_read = 0;
				for (auto ptr = static_cast<char*>(data); size != 0;)
				{
					fd_set set = {};
					FD_SET(socket, &set);
					int result = ::select(0, &set, 0, 0, timeout_ms == ~0U ? nullptr : &timeout);
					if (result == 0)
						return true; // timeout, no more bytes available

					// Read the data
					int addr_size = sizeof(addr);
					int read = ::recvfrom(socket, ptr, int(size), flags, (sockaddr*)&addr, &addr_size);
					if (read == 0 || read == SOCKET_ERROR)
						return false; // read zero bytes indicates the socket has been closed

					ptr += read;
					bytes_read += read;
					size -= read;
				}
				return true;
			}
		}

		// A network socket with server behaviour
		class Server
		{
			typedef std::vector<SOCKET> SocketCont;
			typedef std::unique_lock<std::mutex> Lock;

			Winsock const&           m_winsock;         // The winsock instance we're bound to
			SOCKET                   m_listen_socket;   // The socket we're listen for incoming connections on
			uint16                   m_listen_port;     // The port we're listening on
			size_t                   m_max_connections; // The maximum number of clients we'll accept connections from
			int                      m_protocol;        // TCP or UDP
			size_t                   m_max_packet_size; // The maximum size of a single packet that the underlying provider supports
			SocketCont               m_clients;         // The connected clients
			ConnectionCB             m_connection_cb;   // Callback made when a client connects or disconnects
			bool                     m_run_server;      // True while the server should run
			std::atomic_int          m_client_count;    // The number of connected clients
			std::mutex               m_mutex;           // Synchronise access to the clients list
			std::condition_variable  m_cv;              // Sync
			std::thread              m_listen_thread;   // Thread that listens for incoming connections

			Server(Server const&); // no copying
			Server& operator =(Server const&);

			// Thread for listening for incoming connections
			void ListenThread()
			{
				assert(m_listen_socket != INVALID_SOCKET && "Socket not initialised");

				// Track the number of clients
				size_t client_count = 0;
				{
					Lock lock(m_mutex);
					client_count = m_clients.size();
				}

				// Check for client connections to the server and dump old connections
				for (;!m_run_server;)
				{
					// Wait for new connections
					if (client_count < m_max_connections)
						client_count += WaitForConnections(100);
					else
						std::this_thread::sleep_for(std::chrono::milliseconds(100));

					// Remove dead connections
					client_count -= RemoveDeadConnections();
				}
			}

			// Block for up to 'timeout_ms' waiting for incoming connections
			// Returns the number of new clients added (0 or 1)
			size_t WaitForConnections(size_t timeout_ms)
			{
				auto timeout = impl::TimeVal(timeout_ms);

				fd_set set = {};
				FD_SET(m_listen_socket, &set);
				int result = ::select(0, &set, 0, 0, &timeout);
				if (result == 0)
					return 0; // no incoming connections

				// Someone is trying to connect
				sockaddr_in client_addr;
				int client_addr_size = static_cast<int>(sizeof(client_addr));
				SOCKET client = accept(m_listen_socket, (sockaddr*)&client_addr, &client_addr_size);
				if (client == SOCKET_ERROR)
					return 0;

				// Add 'client'
				Lock lock(m_mutex);
				m_clients.push_back(client);
				//if (m_connection_cb)
				//	m_connection_cb(client, &client_addr);
				return 1;
			}

			// Looks for dead connections and removes them from m_clients
			// Returns the number removed
			size_t RemoveDeadConnections()
			{
				Lock lock(m_mutex); // Lock access to 'clients'
				auto zero_timeout = impl::TimeVal(0);

				// Shutdown closed client sockets
				int dropped = 0;
				for (auto& client : m_clients)
				{
					// Detect shutdown sockets by those that "can be read" but return '0' bytes read
					fd_set set = {};
					FD_SET(client, &set);
					int result = ::select(0, &set, 0, 0, &zero_timeout);
					if (result == 0)
						continue; // socket cannot be read (i.e. no data, that's fine it's not closed)

					char sink_char;
					if (::recv(client, &sink_char, 1, MSG_PEEK) != SOCKET_ERROR)
						continue; // Read is ready, connection still good

					//if (m_connection_cb)
					//	m_connection_cb(client, nullptr);

					// Close the client socket
					::shutdown(client, SD_BOTH);
					::closesocket(client);
					client = INVALID_SOCKET;
					++dropped;
				}

				// Remove dead sockets from the container
				auto end = std::remove_if(std::begin(m_clients), std::end(m_clients), [=](SOCKET s){ return s == INVALID_SOCKET; });
				m_clients.erase(end, std::end(m_clients));
				return dropped;
			}

		public:

			explicit Server(Winsock const& winsock)
				:m_winsock(winsock)
				,m_listen_socket(INVALID_SOCKET)
				,m_listen_port()
				,m_max_connections()
				,m_protocol()
				,m_max_packet_size(~0U)
				,m_connection_cb()
				,m_run_server(false)
				,m_client_count()
				,m_mutex()
				,m_cv()
				,m_listen_thread()
			{}
			~Server()
			{
				StopConnections();
			}

			// Turn on/off the server
			// 'listen_port' is a port number of your choosing
			// 'protocol' is one of 'IPPROTO_TCP', 'IPPROTO_UDP', etc
			void AllowConnections(uint16 listen_port, int protocol = IPPROTO_TCP, int max_connections = 1024, ConnectionCB connection_cb = 0)
			{
				StopConnections();

				m_listen_port     = listen_port;
				m_protocol        = protocol;
				m_max_connections = max_connections;
				m_connection_cb   = connection_cb;

				// Create the listen socket
				switch (m_protocol)
				{
				default: throw exception("Unknown protocol");
				case IPPROTO_TCP: m_listen_socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP); break;
				case IPPROTO_UDP: m_listen_socket = ::socket(AF_INET, SOCK_DGRAM,  IPPROTO_UDP); break;
				}
				if (m_listen_socket == INVALID_SOCKET)
					throw exception("failed to create listen socket", WSAGetLastError());

				// Bind the local address to the socket
				sockaddr_in my_address     = {};
				my_address.sin_family      = AF_INET;
				my_address.sin_port        = htons(m_listen_port);
				my_address.sin_addr.s_addr = INADDR_ANY;
				if (::bind(m_listen_socket, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
					throw exception("Failed to bind listen socket", WSAGetLastError());

				// For message-oriented sockets (i.e UDP) we must not exceed the max packet size of
				// the underlying provider. Assume all clients use the same provider as m_socket
				if (m_protocol == IPPROTO_UDP)
					m_max_packet_size = impl::GetMaxPacketSize(m_listen_socket);

				// Set the socket to listen mode
				if (::listen(m_listen_socket, SOMAXCONN) == SOCKET_ERROR)
					throw exception("Failed to listen on listen socket", WSAGetLastError());

				// Start the thread for incoming connections
				m_run_server = true;
				m_listen_thread = std::thread([this]{ ListenThread(); });
			}

			// Stop accepting incoming connections
			void StopConnections()
			{
				if (m_listen_socket == INVALID_SOCKET)
					return;

				{// Stop the incoming connections thread
					Lock lock(m_mutex);
					m_run_server = false;
					m_cv.notify_all();
				}
				if (m_listen_thread.joinable())
					m_listen_thread.join();

				// No need to lock, because the thread isn't running

				// Shutdown all client connections
				for (auto& client : m_clients)
				{
					::shutdown(client, SD_BOTH);
					::closesocket(client);
				}
				m_clients.resize(0);
				m_client_count = 0;

				// Shutdown the listen socket
				::shutdown(m_listen_socket, SD_BOTH);
				::closesocket(m_listen_socket);
				m_listen_socket = INVALID_SOCKET;
			}

			// Send data to a single client
			// Returns true if all data was sent, false otherwise
			bool Send(SOCKET client, void const* data, size_t size, size_t timeout_ms = ~0U)
			{
				return impl::Send(client, data, size, m_max_packet_size, timeout_ms);
			}

			// Send data to all clients
			bool Send(void const* data, size_t size, size_t timeout_ms = ~0U)
			{
				Lock lock(m_mutex);

				// Send the data to each client
				bool all_sent = true;
				for (auto& client : m_clients)
					all_sent &= impl::Send(client, data, size, m_max_packet_size, timeout_ms);

				return all_sent;
			}

			// Send data to a particular ip using 'socket'.
			// Returns true if all data was sent, false if there was a problem with the connection
			bool SendTo(SOCKET socket, char const* host_ip, uint16 host_port, void const* data, size_t size, size_t timeout_ms = ~0U)
			{
				return impl::SendTo(socket, host_ip, host_port, data, size, m_max_packet_size, timeout_ms);
			}

			// Receive data from 'client'
			// Any received data is from one client only
			// If false is returned the connection to the client was lost or had a problem
			bool Recv(SOCKET client, void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				bytes_read = 0;
				return impl::Recv(client, data, size, bytes_read, timeout_ms, flags);
			}

			// Receive data from any client
			// Returns when data is read from a client, 'out_client' is the client that was read from
			bool Recv(void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0, SOCKET* out_client = nullptr)
			{
				Lock lock(m_mutex);

				// Attempt to read from all clients
				for (auto& client : m_clients)
				{
					// Read data from this client, if data found, then return it
					bytes_read = 0;
					if (impl::Recv(client, data, size, bytes_read, timeout_ms, flags) && bytes_read != 0)
					{
						if (out_client) *out_client = client;
						return true;
					}
				}

				// no data read from any client
				return false;
			}

			// Receive data from client ignoring bytes_read
			bool Recv(void* data, size_t size, size_t timeout_ms = ~0U, int flags = 0, SOCKET* out_client = nullptr)
			{
				size_t bytes_read;
				return Recv(data, size, bytes_read, timeout_ms, flags, out_client);
			}

			// Receive data from a host
			bool RecvFrom(SOCKET socket, char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				return impl::RecvFrom(socket, host_ip, host_port, data, size, bytes_read, timeout_ms, flags);
			}
		};

		// A network socket with client behaviour
		class Client
		{
			Winsock const& m_winsock;         // The winsock instance we're bound to
			SOCKET         m_host_socket;     // The socket we've connected to the host with
			uint16         m_host_port;       // The port we're connected to
			int            m_protocol;        // TCP or UDP
			size_t         m_max_packet_size; // The maximum size of a single packet that the underlying provider supports

			Client(Client const&); // no copying
			Client& operator =(Client const&);

		public:

			explicit Client(Winsock const& winsock)
				:m_winsock(winsock)
				,m_host_socket(INVALID_SOCKET)
				,m_host_port()
				,m_protocol()
				,m_max_packet_size(~0U)
			{}
			~Client()
			{
				Disconnect();
			}

			// For a TCP connections, use IPPROTO_TCP, the ip address and port
			// For a UDP connection with a default ip/port, use IPPROTO_UDP, ip, port
			//  Send/Recv can be used with this type of connection, the UDP packets go
			//  to the specified ip/port. I.e connect sets them as the default ip/port.
			// For a UDP conection without any default ip/port, use 'nullptr, 0, IPPROTO_UDP'
			//  Send/Recv return errors for this connection type, however, SendTo and RecvFrom work.
			// Throws on failure
			void Connect(char const* ip, uint16 port, int protocol = IPPROTO_TCP)
			{
				Disconnect();

				// Create the socket
				m_protocol = protocol;
				m_host_socket = ::socket(AF_INET, m_protocol == IPPROTO_TCP ? SOCK_STREAM : SOCK_DGRAM, m_protocol);
				if (m_host_socket == INVALID_SOCKET)
					throw exception("Failed to create a socket", WSAGetLastError());

				// Explicit binding is "not encouraged" for client connections
				//// Bind the socket to our local address
				//sockaddr_in my_address     = {0};
				//my_address.sin_family      = AF_INET;
				//my_address.sin_port        = htons(listen_port);
				//my_address.sin_addr.s_addr = INADDR_ANY;
				//if (bind(m_host, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
					//throw exception("Bind socket failed");

				// For message-oriented sockets (i.e UDP) we must not exceed
				// the max packet size of the underlying provider.
				if (m_protocol == IPPROTO_UDP)
					m_max_packet_size = impl::GetMaxPacketSize(m_host_socket);

				// If an ip address is given, attempt to connect to it
				if (ip != nullptr)
				{
					sockaddr_in host_addr = impl::GetAddress(ip, port);
					if (::connect(m_host_socket, (sockaddr*)&host_addr, sizeof(host_addr)) == SOCKET_ERROR)
					{
						int error = WSAGetLastError();
						switch (error)
						{
						default:              throw exception("Connect failed for an unknown reason", error);
						case WSAECONNREFUSED: throw exception("Connection refused", error);
						case WSAETIMEDOUT:    throw exception("Connect timed out", error);
						}
					}
				}
			}
			void Disconnect()
			{
				if (m_host_socket == INVALID_SOCKET)
					return;

				::shutdown(m_host_socket, SD_BOTH);
				::closesocket(m_host_socket);
				m_host_socket = INVALID_SOCKET;
			}

			// Send/Recv data to/from the host
			// Returns true if all data was sent/received
			bool Send(void const* data, size_t size, size_t timeout_ms = ~0U)
			{
				return impl::Send(m_host_socket, data, size, m_max_packet_size, timeout_ms);
			}
			bool Recv(void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				return impl::Recv(m_host_port, data, size, bytes_read, timeout_ms, flags);
			}
			bool Recv(void* data, size_t size, size_t timeout_ms = ~0U, int flags = 0)
			{
				size_t bytes_read;
				return impl::Recv(m_host_port, data, size, bytes_read, timeout_ms, flags);
			}

			// Send to a particular host (connection-less sockets)
			// Returns true if all data was sent/received
			bool SendTo(char const* host_ip, uint16 host_port, void const* data, size_t size, size_t timeout_ms = ~0U)
			{
				return impl::SendTo(m_host_port, host_ip, host_port, data, size, m_max_packet_size, timeout_ms);
			}
			bool RecvFrom(char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, size_t timeout_ms = ~0U, int flags = 0)
			{
				return impl::RecvFrom(m_host_socket, host_ip, host_port, data, size, bytes_read, timeout_ms, flags);
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_network_tcpip)
		{
			pr::network::Winsock wsa;
			{
				pr::network::Server svr(wsa);
				pr::network::Client client(wsa);

				svr.AllowConnections(54321, IPPROTO_TCP, 10);
				client.Connect("127.0.0.1", 54321, IPPROTO_TCP);

				char const data[] = "Test data";
				PR_CHECK(svr.Send(data, sizeof(data)), true);

				char result[sizeof(data)] = {};
				PR_CHECK(client.Recv(result, sizeof(result)), true);

				PR_CHECK(data, result);

				client.Disconnect();
				svr.StopConnections();
			}
			{
				pr::network::Server svr(wsa);
				pr::network::Client client(wsa);

				svr.AllowConnections(54321, IPPROTO_TCP, 10);
				client.Connect("127.0.0.1", 54321, IPPROTO_TCP);

				char const data[] = "Test data";
				PR_CHECK(client.Send(data, sizeof(data)), true);

				char result[sizeof(data)] = {};
				PR_CHECK(svr.Recv(result, sizeof(result)), true);

				PR_CHECK(data, result);

				client.Disconnect();
				svr.StopConnections();
			}
		}
	}
}
#endif
#endif
