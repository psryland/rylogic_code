//******************************************************
//	Combined TCP/UDP Transmitter/Receiver
//******************************************************

#include <algorithm>
#include <functional>
#include "network.h"

using namespace pr;
using namespace pr::network;

#ifndef PR_ASSERT
#	define PR_ASSERT_DEFINED
#	define PR_ASSERT(grp, exp, str)
#	define PR_INFO(grp, str)
#else
#	include "pr/common/fmt.h"
#	ifndef PR_DBG_NETWORK
#		define PR_DBG_NETWORK  PR_DBG
#	endif
#endif

#pragma warning(push)
#pragma warning(disable: 4127)

// Result testing
void pr::Verify(pr::network::EResult::Type result)
{
	sizeof(result);
	PR_ASSERT(PR_DBG_NETWORK, Succeeded(result), "network verify failure");
}
char const* pr::GetErrorString(pr::network::EResult::Type result)
{
	using namespace pr::network::EResult;

	switch (result)
	{
	default: PR_ASSERT(PR_DBG_NETWORK, false, "unknown error code"); return "unknown error code";
	case EResult::Success:    return "success";
	case EResult::Failed:     return "unspecified failure";
	case WSAStartupFailed:    return "wsa startup failed";
	case InvalidProtocol:     return "invalid protocol";
	case CreateSocketFailed:  return "create socket failed";
	case BindSocketFailed:    return "bind socket failed";
	case SocketListenFailed:  return "failed to set socket to listen mode";
	case GetSockOptFailed:    return "failed to read socket options";
	case HostAddressNotFound: return "host address not found";
	case ConnectFailed:       return "connect failed";
	case ConnectionRefused:   return "connection refused";
	case ConnectTimeout:      return "connect timeout";
	}
}

inline bool IsInvalidSocket(SOCKET socket)
{
	return socket == INVALID_SOCKET;
}

inline timeval TimeVal(uint timeout_ms)
{
	timeval tv;
	tv.tv_sec	= timeout_ms/1000;
	tv.tv_usec	= (timeout_ms - tv.tv_sec*1000)*1000;
	return tv;
} 

inline sockaddr_in GetAddress(char const* ip, uint16 port)
{
	hostent* hp = 0;

	// Convert the ip or dns name to an address
	uint ip_addr = inet_addr(ip);
	if (ip_addr == INADDR_NONE)	{ hp = gethostbyname(ip); }
	else						{ hp = gethostbyaddr((char*)&ip_addr, sizeof(ip_addr), AF_INET); }
	if (!hp) throw EResult::HostAddressNotFound;

	sockaddr_in addr = {0};
	addr.sin_addr.s_addr	= *((unsigned long*)hp->h_addr);
	addr.sin_port			= htons(port);
	addr.sin_family			= AF_INET;
	return addr;
}

// Read the maximum packet size that the underlying provider of 'socket' supports
inline size_t GetMaxPacketSize(SOCKET socket)
{
	unsigned int max_size;
	int max_size_size = sizeof(max_size);
	if (getsockopt(socket, SOL_SOCKET, SO_MAX_MSG_SIZE, (char*)&max_size, &max_size_size) == SOCKET_ERROR)
		throw EResult::GetSockOptFailed;
	return size_t(max_size);
}

// Send data on 'socket'
// Returns true if all data was sent
bool Send(SOCKET socket, void const* data, size_t size, size_t max_packet_size, uint timeout_ms)
{
	timeval timeout = TimeVal(timeout_ms);

	// Send all of the data to the host
	size_t bytes_sent = 0;
	do
	{
		// Select the socket to check that there's transmit buffer space
		fd_set set = {0};
		FD_SET(socket, &set);
		int result = select(0, 0, &set, 0, timeout_ms == INFINITE ? 0 : &timeout);
		if (result != 1) return false; // no space to send data (timeout) or error
	
		// Send data
		int sent = send(socket, (char const*)data, int(size <= max_packet_size ? size : max_packet_size), 0);
		if (sent == SOCKET_ERROR) return false;
		reinterpret_cast<byte const*&>(data) += sent;
		bytes_sent += sent;
		size -= sent;
	}
	while (size != 0);
	return true;
}

// Send data to a particular ip using 'socket'.
// Returns true if all data was sent, false if there was a problem with the connection
bool SendTo(SOCKET socket, char const* host_ip, uint16 host_port, void const* data, size_t size, size_t max_packet_size, uint timeout_ms)
{
	timeval timeout = TimeVal(timeout_ms);

	// Set the destination address
	sockaddr_in addr = GetAddress(host_ip, host_port);

	// Send all of the data to the host
	size_t bytes_sent = 0;
	do
	{
		// Select the socket to check that there's transmit buffer space
		fd_set set = {0};
		FD_SET(socket, &set);
		int result = select(0, 0, &set, 0, timeout_ms == INFINITE ? 0 : &timeout);
		if (result != 1) return false; // no space to send data (timeout) or error
	
		// Send data
		int sent = sendto(socket, (char const*)data, int(size <= max_packet_size ? size : max_packet_size), 0, (sockaddr const*)&addr, sizeof(addr));
		if (sent == SOCKET_ERROR) return false;
		reinterpret_cast<byte const*&>(data) += sent;
		bytes_sent += sent;
		size -= sent;
	}
	while (size != 0);
	return true;
}


// Receive data on 'socket'
// Returns true if data was received, false on timeout
// 'flags' : MSG_PEEK
bool Recv(SOCKET socket, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags)
{
	timeval timeout = TimeVal(timeout_ms);
	bytes_read = 0;
	do
	{
		fd_set set = {0};
		FD_SET(socket, &set);
		int result = select(0, &set, 0, 0, timeout_ms == INFINITE ? 0 : &timeout);
		if (result == 0) return true; // timeout, no more bytes available

		// Read the data
		int read = recv(socket, (char*)data, int(size), flags);
		if (read == 0 || read == SOCKET_ERROR) return false; // read zero bytes indicates the socket has been closed
		reinterpret_cast<char*&>(data) += read;
		bytes_read += read;
		size -= read;
	}
	while (size != 0);
	return true;
}

// Receive data on 'socket'
// Returns true if data was received, false on timeout
// 'flags' : MSG_PEEK
bool RecvFrom(SOCKET socket, char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags)
{
	timeval timeout = TimeVal(timeout_ms);

	// Set the source address
	sockaddr_in addr = GetAddress(host_ip, host_port);
	int addr_size = sizeof(addr);

	bytes_read = 0;
	do
	{
		fd_set set = {0};
		FD_SET(socket, &set);
		int result = select(0, &set, 0, 0, timeout_ms == INFINITE ? 0 : &timeout);
		if (result == 0) return true; // timeout, no more bytes available

		// Read the data
		int read = recvfrom(socket, (char*)data, int(size), flags, (sockaddr*)&addr, &addr_size);
		if (read == 0 || read == SOCKET_ERROR) return false; // read zero bytes indicates the socket has been closed
		reinterpret_cast<char*&>(data) += read;
		bytes_read += read;
		size -= read;
	}
	while (size != 0);
	return true;
}

// Winsock dll wrapper ***********************************

Winsock::Winsock()
:m_wsa_data()
{
	// Start the network
	if (WSAStartup(MAKEWORD(2, 2) , &m_wsa_data) != 0)
		throw pr::network::EResult::WSAStartupFailed;
}
Winsock::~Winsock()
{
	WSACleanup();
}

// Server ************************************************
Server::Server(Winsock const& winsock)
:m_winsock(winsock)
,m_clients()
,m_socket(INVALID_SOCKET)
,m_listen_port(0)
,m_protocol(0)
,m_max_packet_size(INFINITE)
,m_client_count(0)
,m_mutex(0)
{}

Server::~Server()
{
	StopConnections();
}

// Turn on the server
void Server::AllowConnections(uint16 listen_port, int protocol, int max_connections, ConnectionCB connection_cb)
{
	StopConnections();

	m_listen_port = listen_port;
	m_protocol = protocol;
	m_max_connections = max_connections;
	m_connection_cb = connection_cb;
	
	// Create the socket
	switch (m_protocol)
	{
	default: throw EResult::InvalidProtocol;
	case IPPROTO_TCP:	m_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP); break;
	case IPPROTO_UDP:	m_socket = socket(AF_INET, SOCK_DGRAM,  IPPROTO_UDP); break;
	}
	if (m_socket == INVALID_SOCKET)
	{
		PR_INFO(PR_DBG_NETWORK, Fmt("Network: Failed to create a socket. WSAerr: %d\n", WSAGetLastError()).c_str());
		throw EResult::CreateSocketFailed;
    }

    // Bind the local address to the socket
	sockaddr_in my_address		= {0};
	my_address.sin_family		= AF_INET;
	my_address.sin_port			= htons(m_listen_port);
	my_address.sin_addr.s_addr	= INADDR_ANY;
	if (bind(m_socket, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
    {
		PR_INFO(PR_DBG_NETWORK, Fmt("Network: Failed to bind socket. WSAerr: %d\n", WSAGetLastError()).c_str());
		throw EResult::BindSocketFailed;
    }

	// For message-oriented sockets (i.e UDP) we must not exceed the max packet size of
	// the underlying provider. Assume all clients use the same provider as m_socket
	if (m_protocol == IPPROTO_UDP)
		m_max_packet_size = GetMaxPacketSize(m_socket);

	// Set the socket to listen mode
	if (listen(m_socket, SOMAXCONN) == SOCKET_ERROR)
	{
		PR_INFO(PR_DBG_NETWORK, Fmt("Network: Failed to listen on socket. WSAerr: %d\n", WSAGetLastError()).c_str());
		throw EResult::SocketListenFailed;
	}

	// Start the thread for incoming connections
	Thread<Server>::Start();
}

// Turn off the server
void Server::StopConnections()
{
	if (m_socket == INVALID_SOCKET) return;
	
	// Stop the incoming connections thread
	Thread<Server>::Stop();

	// Shutdown all client connections
	for (TSocks::iterator i = m_clients.begin(), i_end = m_clients.end(); i != i_end; ++i)
	{
		shutdown(*i, SD_BOTH);
		closesocket(*i);
	}
	m_clients.resize(0);
	m_client_count = 0;

	// Shutdown the listen socket
	shutdown(m_socket, SD_BOTH);
	closesocket(m_socket);
	m_socket = INVALID_SOCKET;
}

// Thread entry point for checking for client connections
void Server::Main(void*)
{
	PR_ASSERT(PR_DBG_NETWORK, m_socket != INVALID_SOCKET, "Socket not initialised");

	char sink_char;
	timeval no_timeout = TimeVal(0);

	// Check for client connections to the server and dump old connections
	while (!Cancelled(1000))
	{
		pr::MutexLock mutex;
		if (mutex.Lock(m_mutex, 100))
		{
			// Shutdown closed client socket
			bool clients_dropped = false;
			for (TSocks::iterator i = m_clients.begin(), i_end = m_clients.end(); i != i_end; ++i)
			{
				// Detect shutdown sockets by those that "can be read"
				// but return '0' bytes read
				fd_set set = {0};
				FD_SET(*i, &set);
				int result = select(0, &set, 0, 0, &no_timeout);
				if (result == 0) continue; // socket cannot be read (i.e. no data, that's fine it's not closed)
				if (recv(*i, &sink_char, 1, MSG_PEEK) != SOCKET_ERROR) continue; // Read is ready, connection still good
				if (m_connection_cb) m_connection_cb(*i, 0);
				shutdown(*i, SD_BOTH);
				closesocket(*i);
				*i = INVALID_SOCKET;
				clients_dropped = true;
			}

			// Remove dead sockets
			if (clients_dropped)
			{
				TSocks::iterator end = std::remove_if(m_clients.begin(), m_clients.end(), IsInvalidSocket);
				m_clients.erase(end, m_clients.end());
			}

			// Look for new connections
			while (m_clients.size() < m_max_connections)
			{
				fd_set set = {0};
				FD_SET(m_socket, &set);
				int result = select(0, &set, 0, 0, &no_timeout);
				if (result == 0) break; // listen socket is not readable, therefore no incoming connections

				// The listen socket is readable meaning someone is trying to connect
				sockaddr_in client_addr;
				int client_addr_size = static_cast<int>(sizeof(client_addr));
				SOCKET client = accept(m_socket, (sockaddr*)&client_addr, &client_addr_size);
				if (client != SOCKET_ERROR)
				{
					// Add 'client'
					m_clients.push_back(client);
					if (m_connection_cb) m_connection_cb(client, &client_addr);
				}
			}

			m_client_count = int(m_clients.size());
		}
	}
}

// Send data to a client.
// Returns true if all data was sent, false otherwise
bool Server::Send(SOCKET client, void const* data, size_t size, uint timeout_ms)
{
	pr::MutexLock mutex;
	if (!mutex.Lock(m_mutex, timeout_ms)) return false;
	return ::Send(client, data, size, m_max_packet_size, timeout_ms);
}
bool Server::Send(void const* data, size_t size, uint timeout_ms)
{
	pr::MutexLock mutex;
	if (!mutex.Lock(m_mutex, timeout_ms)) return false;

	// Send the data to each client
	bool all_sent = true;
	for (TSocks::iterator i = m_clients.begin(), i_end = m_clients.end(); i != i_end; ++i)
	{
		// Send the data to this client
		all_sent &= ::Send(*i, data, size, m_max_packet_size, timeout_ms);
	}
	return all_sent;
}

// Receive data from 'client'
// Any received data is from one client only
// If false is returned the connection to the client was lost or had a problem
bool Server::Recv(SOCKET client, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags)
{
	bytes_read = 0;
	pr::MutexLock mutex;
	if (!mutex.Lock(m_mutex, timeout_ms)) return false;
	return ::Recv(client, data, size, bytes_read, timeout_ms, flags);
}
bool Server::Recv(void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags, SOCKET* client)
{
	bytes_read = 0;
	pr::MutexLock mutex;
	if (!mutex.Lock(m_mutex, timeout_ms)) return false;

	// Attempt to read from all clients
	for (TSocks::iterator i = m_clients.begin(), i_end = m_clients.end(); i != i_end; ++i)
	{
		// Read data from this client, if data found, then return it
		if (::Recv(*i, data, size, bytes_read, timeout_ms, flags) && bytes_read != 0)
		{
			if (client) *client = *i;
			return true;
		}
	}

	// no data read from any client
	return false;
}


// Client ************************************************

Client::Client(Winsock const& winsock)
:m_winsock(winsock)
,m_host(INVALID_SOCKET)
,m_port(0)
,m_protocol(0)
,m_max_packet_size(INFINITE)
{}

Client::~Client()
{
	Disconnect();
}

// For a TCP connections, use IPPROTO_TCP, the ip address and port
// For a UDP connection with a default ip/port, use IPPROTO_UDP, ip, port
//	Send/Recv can be used with this type of connection, the UDP packets go
//	to the specified ip/port. I.e connect sets them as the default ip/port
// For a UDP conection without any default ip/port, use IPPROTO_UDP, 0, 0
//	Send/Recv return errors for this connection type, however, sendto and
//	recvfrom should work
EResult::Type Client::Connect(int protocol, char const* host_ip, uint16 host_port)
{
	Disconnect();

	// Create the socket
	m_protocol = protocol;
	m_host = socket(AF_INET, m_protocol == IPPROTO_TCP ? SOCK_STREAM : SOCK_DGRAM, m_protocol);
	if (m_host == INVALID_SOCKET)
	{
		PR_INFO(PR_DBG_NETWORK, FmtS("Network: Failed to create a socket. ESA error %d\n", WSAGetLastError()));
		throw EResult::CreateSocketFailed;
	}

	// Explicit binding is "not encouraged" for client connections
	//// Bind the socket to our local address
	//sockaddr_in my_address		= {0};
	//my_address.sin_family			= AF_INET;
	//my_address.sin_port			= htons(listen_port);
	//my_address.sin_addr.s_addr	= INADDR_ANY;
	//if (bind(m_host, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
	//{
	//	PR_INFO(PR_DBG_NETWORK, Fmt("Network: Failed to bind socket. WSAerr: %d\n", WSAGetLastError()).c_str());
	//	throw EResult::BindSocketFailed;
	//}
	
	// For message-oriented sockets (i.e UDP) we must not exceed
	// the max packet size of the underlying provider.
	if (m_protocol == IPPROTO_UDP)
		m_max_packet_size = GetMaxPacketSize(m_host);

	// If an ip address is given, attempt to connect to it
	if (host_ip != 0)
	{
		sockaddr_in host_addr = GetAddress(host_ip, host_port);
		if (connect(m_host, (sockaddr*)&host_addr, sizeof(host_addr)) == SOCKET_ERROR)
		{
			int error = WSAGetLastError();
			switch (error)
			{
			default:						return EResult::ConnectFailed;
			case WSAECONNREFUSED:			return EResult::ConnectionRefused;
			case WSAETIMEDOUT:				return EResult::ConnectTimeout;
			}
		}
	}
	return EResult::Success;
}

// Disconnect from the host
void Client::Disconnect()
{
	if (m_host == INVALID_SOCKET) return;
	shutdown(m_host, SD_BOTH);
	closesocket(m_host);
	m_host = INVALID_SOCKET;
}

// Send/Recv data to/from the host
// Returns true if all data was sent/received
bool Client::Send(void const* data, size_t size, uint timeout_ms)
{
	return ::Send(m_host, data, size, m_max_packet_size, timeout_ms);
}
bool Client::Recv(void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags)
{
	return ::Recv(m_host, data, size, bytes_read, timeout_ms, flags);
}

// Send to a particular host (connection-less sockets)
// Returns true if all data was sent/received
bool Client::SendTo(char const* host_ip, uint16 host_port, void const* data, size_t size, uint timeout_ms)
{
	return ::SendTo(m_host, host_ip, host_port, data, size, m_max_packet_size, timeout_ms);
}
bool Client::RecvFrom(char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, uint timeout_ms, int flags)
{
	return ::RecvFrom(m_host, host_ip, host_port, data, size, bytes_read, timeout_ms, flags);
}

#pragma warning(pop)

#ifdef PR_ASSERT_DEFINED
#	undef PR_ASSERT_DEFINED
#	undef PR_ASSERT
#	undef PR_INFO
#endif
