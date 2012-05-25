//************************************************************************
//
//  Transmitter
//
//************************************************************************

#include <windows.h>
#include "PR/Common/PRAssert.h"
#include "PR/Common/Fmt.h"
#include "PR/Common/ErrorCodes.h"
#include "PR/Network/Transmitter/Transmitter.h"

using namespace pr;

//*****
// Constructor
Transmitter::Transmitter()
:m_socket(INVALID_SOCKET)
,m_status(EStatus_Disconnected)
{
	ZeroMemory(&m_destination, sizeof(m_destination));
}

//*****
// Initialise and reserve resources
HRESULT Transmitter::Initialise(const TransmitterSettings& settings)
{
	PR_ASSERT_STR(m_socket == INVALID_SOCKET, "UnInitialise must be called first");

	m_settings = settings;

	// Start the network
	WSADATA wsa_data;
	int result = WSAStartup(0x0101, &wsa_data);
	if( result != 0 ) { return error::Transmitter_WSASTARTUP_FAILED; }

    // Create the socket
	m_socket = socket(AF_INET, (m_settings.m_protocol == IPPROTO_TCP) ? (SOCK_STREAM) : (SOCK_DGRAM), m_settings.m_protocol);
	if( m_socket == INVALID_SOCKET )
	{
		PR_WARN(Fmt("Transmitter: Failed to create a socket error. WSAerr: %d\n", WSAGetLastError()).c_str());
		return error::Transmitter_FAILED_TO_CREATE_SOCKET;
    }

	// Set non-blocking if requested
	if( !m_settings.m_blocking )
	{
		u_long non_blocking = 1;
		if( ioctlsocket(m_socket, FIONBIO, &non_blocking) == SOCKET_ERROR )
		{
			PR_WARN(Fmt("Transmitter: Failed to set non-blocking. WSAerr: %d\n", WSAGetLastError()).c_str());
			return error::Transmitter_FAILED_TO_SET_NON_BLOCKING;
		}
	}

    // Bind the local address to the socket
	sockaddr_in my_address;
	ZeroMemory(&my_address, sizeof(my_address));
	my_address.sin_family	= AF_INET;
	my_address.sin_port		= htons(m_settings.m_local_port);
	if( !m_settings.m_local_ip.empty() )	my_address.sin_addr.S_un.S_addr = inet_addr(m_settings.m_local_ip.c_str());
	else									my_address.sin_addr.S_un.S_addr = INADDR_ANY;
	if( bind(m_socket,(PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR )
    {
		PR_WARN(Fmt("Transmitter: Failed to bind socket. WSAerr: %d\n", WSAGetLastError()).c_str());
		return error::Transmitter_FAILED_TO_BIND_SOCKET;
    }

	// UDP connections are ready for data now
	if( m_settings.m_protocol == IPPROTO_UDP )
		m_status = EStatus_Connected;

	// Setup the destination address
	SetDestination(m_settings.m_dest_ip.c_str(), m_settings.m_dest_port);
	return S_OK;
}

//*****
// Set the destination address
void Transmitter::SetDestination(const char* ip, uint16 port)
{
	m_settings.m_dest_ip = ip;
	m_settings.m_dest_port = port;

	ZeroMemory(&m_destination, sizeof(m_destination));
	m_destination.sin_family			= AF_INET;
	m_destination.sin_port				= htons(m_settings.m_dest_port);
	if( !m_settings.m_dest_ip.empty() )	m_destination.sin_addr.S_un.S_addr = inet_addr(m_settings.m_dest_ip.c_str());
	else								m_destination.sin_addr.S_un.S_addr = INADDR_ANY;

	// UDP connections are "connectionless" so we don't need to disconnect
	if( m_settings.m_protocol != IPPROTO_UDP ) Disconnect();
}

//*****
// Returns true if there is data to be read
bool Transmitter::IsDataReady(uint* bytes_available)
{
	if( bytes_available ) *bytes_available = 0;
	if( m_status != EStatus_Connected ) return false;

	char data[1];
	uint bytes = Recv(data, 1, MSG_PEEK);
	if( bytes_available ) *bytes_available = bytes;
	return bytes > 0;
}

//*****
// Connect to a host. (TCP only). Returns true if a connection was established
HRESULT Transmitter::Connect()
{
	PR_WARN_EXP(m_settings.m_protocol != IPPROTO_UDP, "UDP connections do not need to connect");

	switch( m_status )
	{
	case EStatus_Connected:
		return S_OK;

	case EStatus_Connecting:
		if( Select() == error::Transmitter_READY )
		{
			m_status = EStatus_Connected;
			return S_OK;
		}
		return error::Transmitter_STILL_CONNECTING;
	
	case EStatus_Disconnected:
		{
			if( connect(m_socket, (PSOCKADDR)&m_destination, sizeof(m_destination)) != SOCKET_ERROR )
			{
				m_status = EStatus_Connected;
				return S_OK;
			}

            int last_error = WSAGetLastError();
			if( !m_settings.m_blocking && (last_error == WSAEWOULDBLOCK || last_error == WSAEALREADY) )
			{
				m_status = EStatus_Connecting;
				return error::Transmitter_STILL_CONNECTING;
			}
			
			PR_WARN(Fmt("Transmitter: Failed to connect. WSAerr: %d\n", last_error).c_str());
			return error::Transmitter_FAILED_TO_CONNECT;
		}

	default:
		PR_ERROR_STR("Unknown transmitter status");
		return E_FAIL;
	}
}

//*****
// See if there are any sockets ready for transmitting on
HRESULT Transmitter::Select()
{
	#pragma warning ( disable : 4127 )
	fd_set write_set;
	FD_ZERO(&write_set);
	FD_SET(m_socket, &write_set);
	#pragma warning ( default : 4127 )

	int result = 0;
	if( m_settings.m_block_time.tv_sec == INFINITE )
		result = select(0, 0, &write_set, 0, 0);
	else
		result = select(0, 0, &write_set, 0, &m_settings.m_block_time);
	
	// Timeout
	if( result == 0 ) return error::Transmitter_TIMEOUT;

	// Socket error
	else if( result == SOCKET_ERROR )
	{
		if( m_settings.m_protocol != IPPROTO_UDP ) Disconnect();
		PR_WARN(Fmt("Transmitter: Failed to connect. WSAerr: %d\n", WSAGetLastError()).c_str());
		return error::Transmitter_SOCKET_ERROR;
	}

	return error::Transmitter_READY;
}

//*****
// Stop communication
void Transmitter::Disconnect()
{
	PR_WARN_EXP(m_settings.m_protocol != IPPROTO_UDP, "UDP connections do not need to disconnect");

	if( m_socket != INVALID_SOCKET ) shutdown(m_socket, 2);	//SD_BOTH = 2
	m_status = EStatus_Disconnected;
}

//*****
// Release the resources
void Transmitter::UnInitialise()
{
	if( m_settings.m_protocol != IPPROTO_UDP ) Disconnect();
	closesocket(m_socket);
	m_socket = INVALID_SOCKET;
	WSACleanup();
}

//*****
// Send data over the socket. Return the number of bytes sent
uint Transmitter::Send(const char* data, uint length)
{
	if( m_status != EStatus_Connected ) return 0;
	
	int result;
	if( m_settings.m_protocol == IPPROTO_UDP )
	{
		if( Select() != error::Transmitter_READY ) return 0;
		result = sendto(m_socket, data, length, 0, (PSOCKADDR)&m_destination, sizeof(m_destination));
	}
	else
	{
		result = send(m_socket, data, length, 0);
	}

	if( result == SOCKET_ERROR )
	{
		PR_WARN(Fmt("Transmitter: Failed to send. WSAerr: %d\n", WSAGetLastError()).c_str());
		return 0;
	}
	return static_cast<uint>(result);
}

//*****
// Read data from the socket. Return the number of bytes received
uint Transmitter::Recv(char* data, uint length, int flags)
{
	if( m_status != EStatus_Connected ) return 0;
	
	int result;
	if( m_settings.m_protocol == IPPROTO_UDP )
	{
		if( Select() != error::Transmitter_READY ) return 0;
		int destination_length = sizeof(m_destination);	
		result = recvfrom(m_socket, data, length, flags, (PSOCKADDR)&m_destination, &destination_length);
	}
	else
	{
		result = recv(m_socket, data, length, flags);
	}

	if( result == SOCKET_ERROR )
	{
		PR_WARN(Fmt("Transmitter: Failed to receive. WSAerr: %d\n", WSAGetLastError()).c_str());
		return 0;
	}
	return static_cast<uint>(result);
}
