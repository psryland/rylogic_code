//************************************************************************
//
//  Receiver
//
//************************************************************************

#include <stdarg.h>
#include <windows.h>
#include "PR/Common/PRAssert.h"
#include "PR/Common/Fmt.h"
#include "PR/Common/ErrorCodes.h"
#include "PR/Network/Receiver/Receiver.h"

//*****
// Constructor
Receiver::Receiver()
:m_socket(INVALID_SOCKET)
,m_accept_socket(INVALID_SOCKET)
,m_status(Disconnected)
{
	ZeroMemory(&m_source, sizeof(m_source));
}

//*****
// Initialise and reserve resources
HRESULT Receiver::Initialise(const ReceiverSettings& settings)
{
	PR_ASSERT_STR(m_socket == INVALID_SOCKET, "UnInitialise must be called first");

	m_settings = settings;

	// Start the network
	WSADATA wsa_data;
	int result = WSAStartup(0x0101, &wsa_data);
	if( result != 0 ) { return PR::Error::Receiver_WSASTARTUP_FAILED; }

    // Create the socket
	m_socket = socket(AF_INET, (m_settings.m_protocol == IPPROTO_TCP) ? (SOCK_STREAM) : (SOCK_DGRAM), m_settings.m_protocol);
	if( m_socket == INVALID_SOCKET )
	{
		PR_WARN(Fmt("Receiver: Failed to create a socket error. WSAerr: %d\n", WSAGetLastError()));
		return PR::Error::Receiver_FAILED_TO_CREATE_SOCKET;
    }

	// Set non-blocking if requested
	if( !m_settings.m_blocking )
	{
		DWORD non_blocking = 1;
		if( ioctlsocket(m_socket, FIONBIO, &non_blocking) == SOCKET_ERROR )
		{
			PR_WARN(Fmt("Receiver: Failed to set non-blocking. WSAerr: %d\n", WSAGetLastError()));
			return PR::Error::Receiver_FAILED_TO_SET_NON_BLOCKING;
		}
	}

    // Bind the local address to the socket
	sockaddr_in my_address;
	ZeroMemory(&my_address, sizeof(my_address));
	my_address.sin_family	= AF_INET;
	my_address.sin_port		= htons(m_settings.m_local_port);
	if( m_settings.m_local_ip[0] != '\0' )	my_address.sin_addr.S_un.S_addr = inet_addr(m_settings.m_local_ip);
	else									my_address.sin_addr.S_un.S_addr = INADDR_ANY;
	if( bind(m_socket,(PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR )
    {
		PR_WARN(Fmt("Receiver: Failed to bind socket. WSAerr: %d\n", WSAGetLastError()));
		return PR::Error::Receiver_FAILED_TO_BIND_SOCKET;
    }

	// UPD connections are "connectionless" so we're ready for data now.
	if( m_settings.m_protocol == IPPROTO_UDP )
		m_status = Connected;

	SetSource(m_settings.m_src_ip, m_settings.m_src_port);
	return S_OK;
}

//*****
// Set the source address to receive from
void Receiver::SetSource(const char* ip, WORD port)
{
	strncpy(m_settings.m_src_ip, ip, RECEIVER_MAX_IP_STRING_LENGTH);
	m_settings.m_src_ip[RECEIVER_MAX_IP_STRING_LENGTH - 1] = '\0';
	m_settings.m_src_port = port;

	ZeroMemory(&m_source, sizeof(m_source));
	m_source.sin_family	= AF_INET;
	m_source.sin_port	= htons(m_settings.m_src_port);
	if( m_settings.m_src_ip[0] != '\0' )	m_source.sin_addr.S_un.S_addr = inet_addr(m_settings.m_src_ip);
	else									m_source.sin_addr.S_un.S_addr = INADDR_ANY;

	// UDP connections are "connectionless" so we don't need to disconnect
	if( m_settings.m_protocol != IPPROTO_UDP ) Disconnect();
}

//*****
// Returns true if there is data to be read
bool Receiver::IsDataReady(DWORD* bytes_available)
{
	if( bytes_available ) *bytes_available = 0;
	if( m_status != Connected ) return false;

	char data[1];
	DWORD bytes = Recv(data, 1, MSG_PEEK);
	if( bytes_available ) *bytes_available = bytes;
	return bytes > 0;
}

//*****
// Wait for incomming connections
HRESULT Receiver::Connect()
{
	PR_WARN_EXP(m_settings.m_protocol != IPPROTO_UDP, "UDP connections do not need to connect\n");
	
	switch( m_status )
	{
	case Connected:
		return S_OK;

	case Disconnected:
	case Connecting:
		{
			if( listen(m_socket, 1) == SOCKET_ERROR )
			{
				PR_WARN(Fmt("Receiver: Failed to set listen. WSAerr: %d\n", WSAGetLastError()));
				return PR::Error::Receiver_FAILED_TO_LISTEN;
			}

			int source_length = static_cast<int>(sizeof(m_source));
			m_accept_socket = accept(m_socket, (PSOCKADDR)&m_source, &source_length);
			if( m_accept_socket != SOCKET_ERROR )
			{
				m_status = Connected;
				return S_OK;
			}

            int last_error = WSAGetLastError();
			if( !m_settings.m_blocking && last_error == WSAEWOULDBLOCK )
			{
				m_status = Connecting;
				return S_OK;
			}
			
			PR_WARN(Fmt("Receiver: Failed to connect. WSAerr: %d\n", last_error));
			return PR::Error::Receiver_FAILED_TO_CONNECT;
		}

	default:
		PR_ERROR_STR("Unknown receiver status");
		return E_FAIL;
	}
}

//*****
// See if there are any sockets ready for receiving
HRESULT Receiver::Select()
{
	#pragma warning ( disable : 4127 )
	fd_set read_set;
	FD_ZERO(&read_set);
	if( m_settings.m_protocol == IPPROTO_UDP )	FD_SET(m_socket, &read_set);
	else										FD_SET(m_accept_socket, &read_set);
	#pragma warning ( default : 4127 )

	int result = 0;
	if( m_settings.m_block_time.tv_sec == INFINITE )
		result = select(0, &read_set, NULL, NULL, NULL);
	else
		result = select(0, &read_set, NULL, NULL, &m_settings.m_block_time);
	
	// Timeout
	if( result == 0 ) return PR::Error::Receiver_TIMEOUT;

	// Socket error
	else if( result == SOCKET_ERROR )
	{
		if( m_settings.m_protocol != IPPROTO_UDP ) Disconnect();
		PR_WARN(Fmt("Transmitter: Failed to connect. WSAerr: %d\n", WSAGetLastError()));
		return PR::Error::Receiver_SOCKET_ERROR;
	}

	return PR::Error::Receiver_READY;
}

//*****
// Stop communication
void Receiver::Disconnect()
{
	// UDP connections do not need to disconnect
	if( m_settings.m_protocol == IPPROTO_UDP ) return;

	if( m_accept_socket != INVALID_SOCKET )
	{
		shutdown(m_accept_socket, 2);	//SD_BOTH = 2
		closesocket(m_accept_socket);
		m_accept_socket = INVALID_SOCKET;
		m_status = Disconnected;
	}
}

//*****
// Free all resources
void Receiver::UnInitialise()
{
	Disconnect();
	if( m_socket != INVALID_SOCKET )
	{
		shutdown(m_socket, 2);
		closesocket(m_socket);
		m_socket = INVALID_SOCKET;
		m_status = Disconnected;
	}
	WSACleanup();
}

//*****
// Send a formatted string
DWORD Receiver::SendFmt(const char* format, ...)
{
	char buffer[1024];
	va_list arglist;
	va_start(arglist, format);
	_vsnprintf(buffer, 1024, format, arglist);
	buffer[1023] = '\0';
	va_end(arglist);
	return Send(buffer, static_cast<DWORD>(strlen(buffer) + 1));
}

//*****
// Send data over the socket. Return the number of bytes sent
DWORD Receiver::Send(const char* data, DWORD length)
{
	if( m_status != Connected ) return 0;
	
	int result;
	if( m_settings.m_protocol == IPPROTO_UDP )
	{
		if( Select() != PR::Error::Receiver_READY ) return 0;
		result = sendto(m_socket, data, length, 0, (PSOCKADDR)&m_source, sizeof(m_source));
	}
	else
	{
		result = send(m_accept_socket, data, length, 0);
	}

	if( result == SOCKET_ERROR )
	{
		PR_WARN(Fmt("Receiver: Failed to send. WSAerr: %d\n", WSAGetLastError()));
		return 0;
	}
	return static_cast<DWORD>(result);
}

//*****
// Read data over the socket. Return the number of bytes received
DWORD Receiver::Recv(char* data, DWORD length, int flags)
{
	if( m_status != Connected ) return 0;
	if( length == 0 ) return 0;
	
	int result;
	if( m_settings.m_protocol == IPPROTO_UDP )
	{
		if( Select() != PR::Error::Receiver_READY ) return 0;
		int source_length = sizeof(m_source);	
		result = recvfrom(m_socket, data, length, flags, (PSOCKADDR)&m_source, &source_length);
	}
	else
	{
		result = recv(m_accept_socket, data, length, flags);
	}

	if( result == SOCKET_ERROR )
	{
		int last_error = WSAGetLastError();
		if( last_error == WSAEWOULDBLOCK )
		{
			return 0;
		}

		PR_WARN(Fmt("Receiver: Failed to receive. WSAerr: %d\n", last_error));
		return 0;
	}
	return static_cast<DWORD>(result);
}

