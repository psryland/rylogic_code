#pragma once

#include <string>
#include <sstream>
#include <vector>
#include <algorithm>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <atomic>
#include <cassert>
#include <exception>
#include "pr/network/sockets.h"

namespace pr
{
	namespace network
	{
		// A network socket with server behaviour
		class TCPServer :public ServerSocket
		{
			int m_protocol; // TCP or UDP

			TCPServer(TCPServer const&); // no copying
			TCPServer& operator =(TCPServer const&);

			// Create m_listen_socket to use for listening on
			virtual void CreateListenSocket() override
			{
				// Create the listen socket
				switch (m_protocol)
				{
				default: throw exception("Unknown protocol");
				case IPPROTO_TCP: m_listen_socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP); break;
				case IPPROTO_UDP: m_listen_socket = ::socket(AF_INET, SOCK_DGRAM,  IPPROTO_UDP); break;
				}
				if (m_listen_socket == INVALID_SOCKET)
					ThrowSocketError(WSAGetLastError());

				// Bind the local address to the socket
				sockaddr_in my_address          = {};
				my_address.sin_family           = AF_INET;
				my_address.sin_port             = htons(m_listen_port);
				my_address.sin_addr.S_un.S_addr = INADDR_ANY;
				int result = ::bind(m_listen_socket, (sockaddr const*)&my_address, sizeof(my_address));
				if (result == SOCKET_ERROR)
					ThrowSocketError(WSAGetLastError());

				// For message-oriented sockets (i.e UDP) we must not exceed the max packet size of
				// the underlying provider. Assume all clients use the same provider as m_socket
				if (m_protocol == IPPROTO_UDP)
					m_max_packet_size = pr::network::GetMaxPacketSize(m_listen_socket);
			}

		public:

			explicit TCPServer(Winsock const& winsock, int protocol = IPPROTO_TCP)
				:ServerSocket(winsock)
				,m_protocol(protocol)
			{}
		};

		// A network socket with client behaviour
		class TCPClient :public ClientSocket
		{
			IPPROTO m_protocol;  // TCP or UDP
			int     m_sock_type; // Socket type

			TCPClient(TCPClient const&); // no copying
			TCPClient& operator =(TCPClient const&);

		public:

			explicit TCPClient(Winsock const& winsock, IPPROTO protocol = IPPROTO::IPPROTO_TCP, int sock_type = SOCK_STREAM)
				:ClientSocket(winsock)
				,m_protocol(protocol)
				,m_sock_type(sock_type)
			{}

			// (Re)create 'm_socket'
			// Typically applications can just call 'Connect', however some socket options
			// need to be set after creating the socket but before connecting, in this case
			// applications can call 'CreateSocket' first, then connect.
			void CreateSocket()
			{
				Disconnect();

				// Create the socket
				m_socket = ::socket(AF_INET, m_sock_type, m_protocol);
				if (m_socket == INVALID_SOCKET)
					pr::network::ThrowSocketError(WSAGetLastError());
			}

			// For a TCP connections, use IPPROTO_TCP, the ip address and port
			// For a UDP connection with a default ip/port, use IPPROTO_UDP, ip, port
			//  Send/Recv can be used with this type of connection, the UDP packets go
			//  to the specified ip/port. I.e connect sets them as the default ip/port.
			// For a UDP conection without any default ip/port, use 'nullptr, 0, IPPROTO_UDP'
			//  Send/Recv return errors for this connection type, however, SendTo and RecvFrom work.
			// Returns true if the connection is established, false on timeout. Throws on failure
			bool Connect(char const* ip, uint16 port, int timeout_ms = ~0)
			{
				if (m_socket == INVALID_SOCKET)
					CreateSocket();

				// Explicit binding is "not encouraged" for client connections
				//// Bind the socket to our local address
				//sockaddr_in my_address     = {0};
				//my_address.sin_family      = AF_INET;
				//my_address.sin_port        = htons(listen_port);
				//my_address.sin_addr.s_addr = INADDR_ANY;
				//if (bind(m_host, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
					//throw exception("Bind socket failed");

				// For message-oriented sockets (e.g UDP) we must not exceed
				// the max packet size of the underlying provider.
				if (m_sock_type == SOCK_DGRAM)
					m_max_packet_size = pr::network::GetMaxPacketSize(m_socket);

				// If an ip address is given, attempt to connect to it
				// It won't be given for UDP connections
				if (ip != nullptr)
				{
					sockaddr_in host_addr = pr::network::GetAddress(ip, port);
					int result = ::connect(m_socket, (sockaddr*)&host_addr, sizeof(host_addr));
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Wait for the socket to say it's writable (meaning it's connected)
					fd_set set = {};
					FD_SET(m_socket, &set);
					auto timeout = pr::network::TimeVal(timeout_ms);
					result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return false;
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());
				}
				return true;
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
				bool connected = false;

				pr::network::TCPServer svr(wsa);
				svr.AllowConnections(54321, [&](SOCKET, sockaddr_in const* client_addr)
					{
						connected = client_addr != nullptr;
					});

				pr::network::TCPClient client(wsa);
				client.Connect("127.0.0.1", 54321);

				PR_CHECK(svr.WaitForClients(1), true);
				PR_CHECK(connected, true);

				char const data[] = "Test data";
				PR_CHECK(svr.Send(data, sizeof(data)), true);

				char result[sizeof(data)] = {};
				PR_CHECK(client.Recv(result, sizeof(result)), true);

				PR_CHECK(data, result);
				svr.StopConnections();
			}
			{
				pr::network::TCPServer svr(wsa);
				svr.AllowConnections(54321, 10);

				pr::network::TCPClient client(wsa);
				client.Connect("127.0.0.1", 54321);

				PR_CHECK(svr.WaitForClients(1), true);

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

