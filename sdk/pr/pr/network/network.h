//******************************************************
//	Combined TCP/UDP Transmitter/Receiver
//******************************************************
// Required lib: ws2_32.lib
// Server Usage:
//	Winsock winsock;			// Create an instance to represent the winsock dll
//	Server server(winsock);		// Create an instance of a server (maybe in some other wrapper object)
//	server.AllowConnections();	// Start the thread that handles incoming connections
//	Sleep() or Send()/Recv();	// Do whatever
//	server.StopConnections();	// Close all connections
//
// Client Usage:
//	Winsock winsock;			// Create an instance to represent the winsock dll (only need one per app btw)
//	Client client(winsock);

#ifndef PR_NETWORK_H
#define PR_NETWORK_H
#pragma once

// Thanks microsoft... :-/
#if defined(_INC_WINDOWS) && !defined(_WINSOCK2API_)
#	error "winsock2.h must be included before windows.h"
#endif

#include <winsock2.h>
#include <string>
#include <vector>
#include "pr/threads/thread.h"
#include "pr/threads/mutex.h"

namespace pr
{
	namespace network
	{
		namespace EResult
		{
			enum Type
			{
				Success = 0,
				Failed = 0x80000000,
				WSAStartupFailed,
				InvalidProtocol,
				CreateSocketFailed,
				BindSocketFailed,
				SocketListenFailed,
				GetSockOptFailed,
				HostAddressNotFound,
				ConnectFailed,
				ConnectionRefused,
				ConnectTimeout,
			};
		}

		typedef unsigned short uint16;
		typedef unsigned int   uint;
		
		// 'client_addr' will be non-null for connections, null for disconnections
		typedef void (*ConnectionCB)(SOCKET socket, sockaddr_in const* client_addr);

		// This is a wrapper of the winsock dll. An instance of this object
		// should have the scope of all network activity
		struct Winsock
		{
			WSADATA m_wsa_data;

			Winsock();
			~Winsock();
		};

		// A network socket with server behaviour
		class Server :private pr::threads::Thread<Server>
		{
			typedef std::vector<SOCKET> TSocks;
			Winsock const&	m_winsock;			// The winsock instance we're bound to
			TSocks			m_clients;			// The clients that are connected to the server
			SOCKET			m_socket;			// The socket we're listen for incoming connections on
			uint16			m_listen_port;		// The port we're listening on
			uint			m_max_connections;	// The maximum number of clients we'll accept connections from
			int				m_protocol;			// TCP or UDP
			size_t			m_max_packet_size;	// The maximum size of a single packet that the underlying provider supports
			volatile int	m_client_count;		// The number of connected clients
			pr::Mutex		m_mutex;			// Synchronise access to the clients list
			ConnectionCB	m_connection_cb;	// Callback made when a client connects or disconnects

			Server(Server const&); // no copying
			Server& operator =(Server const&);

			// thread entry point
			void Main(void*);

		public:
			Server(Winsock const& winsock);
			~Server();

			// Turn on/off the server
			// 'listen_port' is a port number of your choosing
			// 'protocol' is one of 'IPPROTO_TCP', 'IPPROTO_UDP', etc
			void AllowConnections(uint16 listen_port, int protocol, int max_connections, ConnectionCB connection_cb = 0);
			void StopConnections();

			// Return the number of currently connected clients
			int ClientCount() const { return m_client_count; }
			uint16 LocalPort() const { return m_listen_port; }
	
			// Send data to a single client
			// If false is returned the connection to the client was lost or had a problem
			bool Send(SOCKET client, void const* data, size_t size, uint timeout_ms);

			// Receive data from 'client'
			// If false is returned the connection to the client was lost or had a problem
			bool Recv(SOCKET client, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags = 0); // MSG_PEEK

			// Duplex communication
			// 'Send' sends to all connected clients
			// 'Recv' can receive from any connected client. Returns true if data was received
			bool Send(const void* data, size_t size, uint timeout_ms);
			bool Recv(void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags = 0, SOCKET* client = 0);
		};


		// A network socket with client behaviour
		class Client
		{
			Winsock const&	m_winsock;			// The winsock instance we're bound to
			SOCKET			m_host;				// The socket we've connected to the host with
			uint16			m_port;				// The port we're connected to
			int				m_protocol;			// TCP or UDP
			size_t			m_max_packet_size;	// The maximum size of a single packet that the underlying provider supports

			Client(Server const&); // no copying
			Client& operator =(Client const&);

		public:
			Client(Winsock const& winsock);
			~Client();

			// For a TCP connections, use IPPROTO_TCP, the ip address and port
			// For a UDP connection with a default ip/port, use IPPROTO_UDP, ip, port
			//	Send/Recv can be used with this type of connection, the UDP packets go
			//	to the specified ip/port. I.e connect sets them as the default ip/port
			// For a UDP conection without any default ip/port, use IPPROTO_UDP, 0, 0
			//	Send/Recv return errors for this connection type, however, SendTo and
			//	RecvFrom should work
			EResult::Type Connect(int protocol, char const* ip, uint16 port);
			void Disconnect();

			// Duplex communication
			bool Send(void const* data, size_t size, uint timeout_ms);
			bool Recv(void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags);	// flags = MSG_PEEK
			bool Recv(void* data, size_t size, size_t& bytes_read, uint timeout_ms) { return Recv(data, size, bytes_read, timeout_ms, 0); }

			// Send to a particular host (connection-less sockets)
			bool SendTo  (char const* host_ip, uint16 host_port, void const* data, size_t size, uint timeout_ms);
			bool RecvFrom(char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags = 0);
		};
	}

	// Result testing
	inline bool Failed        (network::EResult::Type result) { return result  < 0; }
	inline bool Succeeded     (network::EResult::Type result) { return result >= 0; }
	void        Verify        (network::EResult::Type result);
	char const* GetErrorString(network::EResult::Type result);
}
#endif
