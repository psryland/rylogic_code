//***********************************************************************************
//
//	UDPTransmitter - A self contained class and thread for sending UDP data
//
//***********************************************************************************
// Implementation of the Sender Thread
//

#include <stdio.h>
#include <process.h>
#include "UDPTransmitter.h"
#include "Common\PRAssert.h"

#ifndef NDEBUG
#define UDP_SENDER_DEBUG(exp) exp
#else//NDEBUG
#define UDP_SENDER_DEBUG(exp)
#endif//NDEBUG

//*****
// This static method is used for creating the instance of the Sender thread.
// It is static because a fixed address must be passed to _beginthread().
void UDPTransmitter::TransmitterThread(UDPTransmitter* transmitter)
{
	UDP_SENDER_DEBUG(printf("*** %s started ***\n", transmitter->m_settings.m_thread_name);)
	
	// Call the main loop for UDPTransmitter
	transmitter->TransmitterMain();
	
	UDP_SENDER_DEBUG(printf("\n*** %s ended ***\n", transmitter->m_settings.m_thread_name);)
	
	transmitter->m_ok_to_delete = true;
	// No other code can be inserted here.
	_endthread();
}

//***********************************************************************************
// Construction and Destruction
// *****
// Construction of the UDPTransmitter object.
// Creates the UDPTransmitter thread.
UDPTransmitter::UDPTransmitter() :
m_thread_handle((HANDLE)-1),
m_socket(INVALID_SOCKET),
m_socket_status(ssIDLE),
m_tx_start(0),
m_tx_end(0),
m_ok_to_delete(true),
m_error_code(udpteSUCCESS)
{}

//*****
// Initialise the UDPTransmitter
bool UDPTransmitter::Initialise(const UDPTransmitterSettings& settings)
{
	// If this thread is already running we need to kill it first
	if( m_thread_handle != (HANDLE)-1 )
		KillAndBlockTillDead();

	m_settings = settings;

	// Setup the IP and PORT numbers
	m_my_addr.sin_family						= AF_INET;
	m_dest_addr.sin_family						= AF_INET;
	if( m_settings.m_my_ip_str[0] != '\0' )		m_my_addr.sin_addr.s_addr	= inet_addr(m_settings.m_my_ip_str);
	else										m_my_addr.sin_addr.s_addr	= m_settings.m_my_ip;
	if( m_settings.m_dest_ip_str[0] != '\0' )	m_dest_addr.sin_addr.s_addr	= inet_addr(m_settings.m_dest_ip_str);
	else										m_dest_addr.sin_addr.s_addr	= m_settings.m_dest_ip;
    m_my_addr.sin_port							= m_settings.m_my_port;
	m_dest_addr.sin_port						= m_settings.m_dest_port;
	
	// Set the blocking time for the select function
	m_block_time.tv_sec = INFINITE;
	if( m_settings.m_milliseconds_to_block != INFINITE )
	{
		m_block_time.tv_sec  = 0;
		m_block_time.tv_usec = m_settings.m_milliseconds_to_block * 100;
	}

	// Create a transmit buffer
	m_transmit_buffer_size	= m_settings.m_buffer_size;
	m_transmit_buffer		= new char[m_settings.m_buffer_size];
	if( !m_transmit_buffer ) { m_error_code = udpteFAILED_TO_ALLOCATE_TRANSMIT_BUFFER; return false; }

	// Create a Semaphore for protecting the start and end pointers to the transmit buffer
	m_tx_semaphore = CreateSemaphore(NULL, 1, 1, NULL);
	if( !m_tx_semaphore )	{ m_error_code = udpteFAILED_TO_CREATE_SEMAPHORE; return false; }

    // Create the Transmitter thread
	m_ok_to_delete = false;
    m_thread_handle = (HANDLE)_beginthread( (void (*)(void *)) &TransmitterThread, 0x2000, this ); // stack size = 4K
	if( m_thread_handle == (HANDLE)-1 )	{ m_error_code = udpteFAILED_TO_CREATE_TRANSMIT_THREAD; m_ok_to_delete = true; return false; }
	SetThreadPriority(m_thread_handle, THREAD_PRIORITY_NORMAL);

	return true;
}

//*****
// Destruction of the Transmitter thread and oject
UDPTransmitter::~UDPTransmitter()
{
	// Wait for the thread to finish before we are destroyed
	KillAndBlockTillDead(); 

	if( m_socket != INVALID_SOCKET ) CloseSocket();
	delete [] m_transmit_buffer;
	m_transmit_buffer = 0;
}

//***********************************************************************************
// Public methods
//*****
// Add data to the transmit buffer.
bool UDPTransmitter::Send(char* buf, DWORD length, long timeout)
{
	// No data to add? returning now will achieve that
	if( length == 0 ) return true;

	// Wait upto the timeout period for access to the transmit buffer pointers
	if( Lock(timeout) )
	{
		DWORD remaining_buffer_space = m_transmit_buffer_size - m_tx_end;
		if( remaining_buffer_space > length )
		{
			memcpy(m_transmit_buffer + m_tx_end, buf, length);
			m_tx_end += length;
			Unlock();
			ResumeThread(m_thread_handle);
			return true;
		}
		else // We have to wrap
		{
			// Make sure there is actually enough room
			if( remaining_buffer_space + m_tx_start < length )
			{
				UDP_SENDER_DEBUG(fprintf(stderr, "UDPTransmitter: Transmit buffer overflow.\n");)
				Unlock();
				return false;
			}
			memcpy(m_transmit_buffer + m_tx_end, buf, remaining_buffer_space);
			memcpy(m_transmit_buffer, buf + remaining_buffer_space, length - remaining_buffer_space);
			m_tx_end = length - remaining_buffer_space;
			Unlock();
			ResumeThread(m_thread_handle);
			return true;			
		}		
	}
	// The WriteSocket method must still be using them
	return false;
}

//*****
// Brute force send
bool UDPTransmitter::SendNow(char* buf, DWORD length, long timeout)
{
	if( Send(buf, length, timeout) )
		return WriteSocket();
	return false;
}

//*****
// Return the maximum number of bytes the transmit buffer has room for
DWORD UDPTransmitter::BytesAvailable()
{
	DWORD bytes_available = 0;
	Lock();

	// If the transmit buffer hasn't wrapped the number of bytes available is easy
	if( m_tx_end >= m_tx_start )	bytes_available = m_tx_end - m_tx_start;
	else							bytes_available = (m_transmit_buffer_size - m_tx_start) + m_tx_end;

	Unlock();
	return bytes_available;
}

//*****
// Set the transmit buffer pointers both equal to zero
void UDPTransmitter::FlushBuffer()
{
	Lock();
	m_tx_start = m_tx_end = 0;
	Unlock();	
}

//*****
// Tell the Sender thread it's time to shutdown
void UDPTransmitter::Kill()
{
	m_socket_status = ssENDING;
	ResumeThread(m_thread_handle);
}

//***********************************************************************************
// Sender Main Loop
//*****
// While the transmitter thread cycles through this loop it will try and maintain
// a connection to the destination ip & port. m_socket_status is used to guide
// the action each time around the loop
void UDPTransmitter::TransmitterMain()
{
	while( m_socket_status != ssENDING )
	{
		switch( m_socket_status )
		{
		case ssIDLE:
			// If the socket status is IDLE then we want to open a socket
			if( OpenSocket() )		m_socket_status = ssCONNECTED;
			else					Sleep(500); // try again in 0.5 a second
			break;

		case ssCONNECTED:
			{
				UDP_SENDER_DEBUG(fprintf(stderr, ".");)

				// If there is data in the transmit buffer then send it
				if( m_tx_start != m_tx_end )
				{
					if( !WriteSocket() ) CloseSocket();
				}
				else
					SuspendThread(m_thread_handle);
			}
			break;

		default:
			PR_ERROR_STR("Unknown socket status");
			break;
		}
    }
}

//***********************************************************************************
// Private methods
//*****
// OpenSocket() creates a non-blocking socket and binds our local address and port to it.
// m_socket holds handle to new socket
// returns true if socket(), ioctlsocket() and bind() succeed.
// returns false otherwise
bool UDPTransmitter::OpenSocket()
{
	// These are the only conditions under which we should be opening a socket
	UDP_SENDER_DEBUG(if( m_socket_status != ssIDLE )  fprintf(stderr, "Sender: OpenSocket called when socket status not idle.\n");)
	UDP_SENDER_DEBUG(if( m_socket != INVALID_SOCKET ) fprintf(stderr, "Sender: OpenSocket called when the socket already exists.\n");)

	WSADATA wsa_data;
	WSAStartup(0x0101, &wsa_data);

    // Create the socket
    m_socket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if( m_socket == INVALID_SOCKET )
    {
		UDP_SENDER_DEBUG(fprintf( stderr, "Sender: 'socket' error. WSAerr: %d\n", WSAGetLastError());)
        return false;
    }

    // Set the socket to non-blocking
    DWORD non_blocking = 1;
    if( ioctlsocket(m_socket, FIONBIO, &non_blocking) == SOCKET_ERROR )
    {
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: 'ioctlsocket' error. WSAerr: %d\n", WSAGetLastError());)
		CloseSocket();
		return false;
    }

    // Bind the local address to the socket
	if( bind(m_socket,(PSOCKADDR)&m_my_addr, sizeof(m_my_addr)) == SOCKET_ERROR )
    {
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: 'bind' failed. WSAerr: %d\n", WSAGetLastError());)
        CloseSocket();
		return false;
    }

	// Success
	UDP_SENDER_DEBUG(printf("Sender: Socket opened.\n");)
    return true;
}

//*****
// Close the socket if it is open
void UDPTransmitter::CloseSocket()
{
	if( m_socket != INVALID_SOCKET )
	{
		shutdown(m_socket, 2);		// SD_BOTH = 2
		closesocket(m_socket);
		m_socket = INVALID_SOCKET;
		m_socket_status = ssIDLE;
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: Socket closed.\n");)
		WSACleanup();
	}
}

//*****
// Sends data to the Receive thread as UDP datagram.
// Should only be called when there is data in the transmit buffer
// Returns:	true if data was sent or select timed out false if there was a socket error
bool UDPTransmitter::WriteSocket()
{
	UDP_SENDER_DEBUG(if( m_socket_status != ssCONNECTED ) fprintf(stderr, "Sender: Attempt to write to a disconnected socket.\n");)
	
	#pragma warning ( disable : 4127 )
	fd_set socket_set;
	FD_ZERO(&socket_set);
	FD_SET(m_socket, &socket_set);
	#pragma warning ( default : 4127 )

	// Check to see if the TCP/IP stack is ready to accept data for sending
	DWORD result = 0;
	if( m_block_time.tv_sec == INFINITE )	result = select(0, NULL, &socket_set, NULL, NULL);
	else									result = select(0, NULL, &socket_set, NULL, &m_block_time);

	// Timeout
	if( result == 0 ) return true;
	
	// Socket error
	else if( result == SOCKET_ERROR )
	{
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: 'select' error. WSAerr: %d\n", WSAGetLastError());)
		return false;
	}

	if( Lock() )
	{
		// Calculate how much data to send
		DWORD length = 0;

		// If the transmit buffer hasn't wrapped then send all available data
		if( m_tx_end > m_tx_start )	length = m_tx_end - m_tx_start;

		// If the transmit buffer has wrapped, send upto the end of the buffer.
		// Next time round the loop we will send the remander.
		else						length = m_transmit_buffer_size - m_tx_start;

		// Send the data
		result = sendto(m_socket, m_transmit_buffer + m_tx_start, length, 0, (PSOCKADDR)&m_dest_addr, sizeof(m_dest_addr));
		if( result == SOCKET_ERROR )
		{
			UDP_SENDER_DEBUG(fprintf(stderr, "Sender: 'sendto' failed. WSAerr: %d\n", WSAGetLastError());)
			Unlock();
			return false;
		}
				
		// Make sure the correct number of bytes were sent
		if( result != length )
		{
			UDP_SENDER_DEBUG(fprintf(stderr, "'sendto' sent %d of %d bytes", result, length);)
			Unlock();
			return true;
		}

		// Adjust the transmit buffer pointers now that data has been sent
		m_tx_start += length;
		if( m_tx_start == m_tx_end )					m_tx_start = m_tx_end = 0;
		if( m_tx_start == m_transmit_buffer_size )		m_tx_start = 0;

		Unlock();
		return true;
	}
	return false;
}

//*****
// Enter a critical section.
// Returns true if the lock was obtained. False otherwise
bool UDPTransmitter::Lock(long timeout)
{
	DWORD WaitResult = WaitForSingleObject( m_tx_semaphore, timeout );
	switch( WaitResult )
	{ 
    case WAIT_OBJECT_0: return true;
    case WAIT_TIMEOUT:	return false;
	default:
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: Error locking semaphore.\n");)
		break;
	}
	return false;	
}

//*****
// Leave a critical section
void UDPTransmitter::Unlock()
{
	if( !ReleaseSemaphore( m_tx_semaphore, 1, NULL ) )
	{
		UDP_SENDER_DEBUG(fprintf(stderr, "Sender: Error releasing semaphore.\n");)
	}
}
