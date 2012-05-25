//***********************************************************************************
//
//	UDPReceiver - A self contained class and thread for receiving UDP data
//
//***********************************************************************************
// Implementation of the Receiver Thread
//

#include <stdio.h>
#include <process.h>
#include "UDPReceiver.h"
#include "Common\PRAssert.h"

#ifndef NDEBUG
#define UDP_RECEIVER_DEBUG(exp) exp
#else//NDEBUG
#define UDP_RECEIVER_DEBUG(exp)
#endif//NDEBUG

//*****
// This static method is used for creating the instance of the Receiver thread.
// It is static because a fixed address must be passed to _beginthread().
void UDPReceiver::ReceiverThread(UDPReceiver *receiver)
{
	UDP_RECEIVER_DEBUG(printf("*** %s started ***\n", receiver->m_settings.m_thread_name);)
	
	// Call the main loop for UDPReceiver
	receiver->ReceiverMain();
	
	UDP_RECEIVER_DEBUG(printf("\n*** %s ended ***\n", receiver->m_settings.m_thread_name);)
	
	receiver->m_ok_to_delete = true;
	// No other code can be inserted here.
	_endthread();
}

//***********************************************************************************
// Construction and Destruction
// *****
// Construction of the UDPReceiver object.
// Creates the UDPReceiver thread.
UDPReceiver::UDPReceiver() :
m_thread_handle((HANDLE)-1),
m_socket(INVALID_SOCKET),
m_socket_status(rsIDLE),
m_rx_start(0),
m_rx_end(0),
m_ok_to_delete(true),
m_error_code(udpreSUCCESS)
{}

//*****
// Initialise the UDPReceiver
bool UDPReceiver::Initialise(const UDPReceiverSettings& settings)
{
	// If this thread is already running we need to kill it first
	if( m_thread_handle != (HANDLE)-1 )
		KillAndBlockTillDead();

	m_settings = settings;

	// Setup the IP and PORT numbers
	m_my_addr.sin_family						= AF_INET;
	m_src_addr.sin_family						= AF_INET;
	if( m_settings.m_my_ip_str[0] != '\0' )		m_my_addr.sin_addr.s_addr	= inet_addr(m_settings.m_my_ip_str);
	else										m_my_addr.sin_addr.s_addr	= m_settings.m_my_ip;
	if( m_settings.m_src_ip_str[0] != '\0' )	m_src_addr.sin_addr.s_addr	= inet_addr(m_settings.m_src_ip_str);
	else										m_src_addr.sin_addr.s_addr	= m_settings.m_src_ip;
    m_my_addr.sin_port							= m_settings.m_my_port;
	m_src_addr.sin_port							= m_settings.m_src_port;
	
	// Set the blocking time for the select function
	m_block_time.tv_sec = INFINITE;
	if( m_settings.m_milliseconds_to_block != INFINITE )
	{
		m_block_time.tv_sec  = 0;
		m_block_time.tv_usec = m_settings.m_milliseconds_to_block * 1000;
	}

	// Create a receive buffer
	m_receive_buffer_size	= m_settings.m_buffer_size;
	m_receive_buffer		= new char[m_settings.m_buffer_size];
	if( !m_receive_buffer )	{ m_error_code = udpreFAILED_TO_ALLOCATE_RECEIVE_BUFFER; return false; }

	// Create a Semaphore for protecting the start and end pointers to the receive buffer
	m_rx_semaphore = CreateSemaphore(NULL, 1, 1, NULL);
	if( !m_rx_semaphore )	{ m_error_code = udpreFAILED_TO_CREATE_SEMAPHORE; return false; }

    // Create the Receiver thread
    m_ok_to_delete = false;
	m_thread_handle = (HANDLE)_beginthread( (void (*)(void *)) &ReceiverThread, 0x2000, this ); // stack size = 4K
	if( m_thread_handle == (HANDLE)-1 )	{ m_error_code = udpreFAILED_TO_CREATE_RECEIVE_THREAD; m_ok_to_delete = true; return false; }
	SetThreadPriority(m_thread_handle, THREAD_PRIORITY_NORMAL);
	
	return true;
}

//*****
// Destruction of the Receiver thread and oject
UDPReceiver::~UDPReceiver()
{
	// Wait for the thread to finish before we are destroyed
	KillAndBlockTillDead();

	if( m_socket != INVALID_SOCKET ) CloseSocket();
	delete [] m_receive_buffer;
	m_receive_buffer = 0;
}

//***********************************************************************************
// Public methods
//*****
// Read data from the receive buffer. Return the length of data received
DWORD UDPReceiver::Receive(char* buf, DWORD length, bool must_be_full)
{
	// No bytes wanted? returning now will achieve that
	if( length == 0 ) return 0;

	// We want access to the receive buffer pointers now or not at all
	if( Lock(0) )
	{
		// No data to receive
		if( m_rx_start == m_rx_end ) { Unlock(); return 0; }

		// If the receive buffer hasn't wrapped we can do one copy
		if( m_rx_end > m_rx_start )
		{
			DWORD bytes_copied = (m_rx_end - m_rx_start < length) ? (m_rx_end - m_rx_start) : (length);
			if( must_be_full && bytes_copied < length ) { Unlock(); return 0; }
			memcpy(buf, m_receive_buffer + m_rx_start, bytes_copied);
			m_rx_start += bytes_copied;
			if( m_rx_start == m_rx_end ) m_rx_start = m_rx_end = 0;
			Unlock();
			return bytes_copied;
		}
		else // We have to wrap
		{
			// Find out how much data there actually is and, if they are asking for
			// more, give them what we have
			DWORD bytes_available = (m_receive_buffer_size - m_rx_start) + m_rx_end;
			if( bytes_available < length ) length = bytes_available;
			DWORD bytes_copied = 0;

			// If less than to the end of the buffer is wanted
			if( length < m_receive_buffer_size - m_rx_start )
			{
				memcpy(buf, m_receive_buffer + m_rx_start, length);
				m_rx_start += length;
				Unlock();
				return length;
			}
			
			// Otherwise, copy up to the end
			else
			{
				bytes_copied = m_receive_buffer_size - m_rx_start;
				memcpy(buf, m_receive_buffer + m_rx_start, bytes_copied); 
			}
			
			// Copy the remaining number of bytes wanted (or number of bytes available
			// whichever is smaller)
			DWORD bytes_to_copy = (m_rx_end < length - bytes_copied) ? (m_rx_end) : (length - bytes_copied);
			if( must_be_full && (bytes_copied + bytes_to_copy < length) ) { Unlock(); return 0; }
			memcpy(buf + bytes_copied, m_receive_buffer, bytes_to_copy);
			bytes_copied += bytes_to_copy;
			m_rx_start = bytes_to_copy;
			if( m_rx_start == m_rx_end ) m_rx_start = m_rx_end = 0;
			Unlock();
			return bytes_copied;
		}		
	}
	// The ReadSocket method must still be using them
	return 0;
}

//*****
// Return the maximum number of bytes the Receive function could return
DWORD  UDPReceiver::BytesAvailable()
{
	DWORD bytes_available = 0;
	Lock();

	// If the receive buffer hasn't wrapped the number of bytes available is easy
	if( m_rx_end >= m_rx_start )	bytes_available = m_rx_end - m_rx_start;
	else							bytes_available = (m_receive_buffer_size - m_rx_start) + m_rx_end;

	Unlock();
	return bytes_available;
}

//*****
// Set the receive buffer pointers both equal to zero
void UDPReceiver::FlushBuffer()
{
	Lock();
	m_rx_start = m_rx_end = 0;
	Unlock();	
}

//*****
// Tell the UDPReceiver thread it's time to shutdown
void UDPReceiver::Kill()
{
	m_socket_status = rsENDING;
}


//***********************************************************************************
// UDPReceiver Main Loop
//*****
// While the Receiver thread cycles through this loop it will try and maintain
// a connection to the destination ip & port. m_socket_status is used to guide
// the action each time around the loop
void UDPReceiver::ReceiverMain()
{
	while( m_socket_status != rsENDING )
	{
		switch( m_socket_status )
		{
		case rsIDLE:
			// If the socket status is IDLE then we want to open a socket
			if( OpenSocket() )		m_socket_status = rsCONNECTED;
			else					Sleep(500); // try again in 0.5 a second
			break;

		case rsCONNECTED:
			{
				// Try and receive some data
				if( !ReadSocket() )		CloseSocket();

				// If there's data available call the call back
				DWORD bytes_available = BytesAvailable();
				if( bytes_available > 0 && m_settings.m_signal_callback )
				{
					m_settings.m_signal_callback(bytes_available, m_settings.m_user_data);
				}
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
bool UDPReceiver::OpenSocket()
{
	// These are the only conditions under which we should be opening a socket
	UDP_RECEIVER_DEBUG(if( m_socket_status != rsIDLE )	fprintf(stderr, "UDPReceiver: OpenSocket called when socket status not idle.\n");)
	UDP_RECEIVER_DEBUG(if( m_socket != INVALID_SOCKET ) fprintf(stderr, "UDPReceiver: OpenSocket called when the socket already exists.\n");)

	WSADATA wsa_data;
	WSAStartup(0x0101, &wsa_data);

    // Create the socket
    m_socket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if( m_socket == INVALID_SOCKET )
    {
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: 'socket' error. WSAerr: %d\n", WSAGetLastError());)
        return false;
    }

    // Set the socket to non-blocking
    DWORD non_blocking = 1;
    if( ioctlsocket(m_socket, FIONBIO, &non_blocking) == SOCKET_ERROR )
    {
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: 'ioctlsocket' error. WSAerr: %d\n", WSAGetLastError());)
		CloseSocket();
		return false;
    }

    // Bind the local address to the socket
	if( bind(m_socket,(PSOCKADDR)&m_my_addr, sizeof(m_my_addr)) == SOCKET_ERROR )
    {
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: 'bind' failed. WSAerr: %d\n", WSAGetLastError());)
        CloseSocket();
		return false;
    }

	// Success
	UDP_RECEIVER_DEBUG(printf("UDPReceiver: Socket opened.\n");)
    return true;
}

//*****
// Close the socket if it is open
void UDPReceiver::CloseSocket()
{
	if( m_socket != INVALID_SOCKET )
	{
		shutdown(m_socket, 2);		  // SD_BOTH = 2
		closesocket(m_socket);
		m_socket = INVALID_SOCKET;
		m_socket_status = rsIDLE;
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: Socket closed.\n");)
		WSACleanup();
	}
}

//*****
// Receive UDP datagrams.
// Wait up till the block time for data to arrive.
// Returns:	true if data was received or select timed out false if there was a socket error
bool UDPReceiver::ReadSocket()
{
	UDP_RECEIVER_DEBUG(if( m_socket_status != rsCONNECTED ) fprintf(stderr, "UDPReceiver: Attempt to read from a disconnected socket.\n");)
	
	#pragma warning ( disable : 4127 )
	fd_set socket_set;
	FD_ZERO(&socket_set);
	FD_SET(m_socket, &socket_set);
	#pragma warning ( default : 4127 )

	// Check to see if the TCP/IP stack is ready to have data read from it
	int result = 0;
	if( m_block_time.tv_sec == INFINITE )	result = select(0, &socket_set, NULL, NULL, NULL);
	else									result = select(0, &socket_set, NULL, NULL, &m_block_time);

	// Timeout
	if( result == 0 ) return true;
	
	// Socket error
	else if( result == SOCKET_ERROR )
	{
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: 'select' error. WSAerr: %d\n", WSAGetLastError());)
		return false;
	}

	// We'll wait forever for access to the receive pointers once we know that there is data available
	if( Lock() )
	{
		// We should always be able to read upto the end of the receive buffer
		DWORD space_available = m_receive_buffer_size - m_rx_end;
		SOCKADDR_IN source_addr;
		int sock_len = sizeof(source_addr);
		memset( &source_addr, 0, sock_len );
		int bytes_read = recvfrom( m_socket, m_receive_buffer + m_rx_end, space_available, 0, (PSOCKADDR)&source_addr, &sock_len );
		if( bytes_read == SOCKET_ERROR )
		{
			UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: 'recvfrom' failed. WSAerr: %d\n", WSAGetLastError());)
			Unlock();
			return false;
		}
		
		// Only adjust the pointers if the data is from who we want to receive from
		if( m_src_addr.sin_addr.s_addr == 0 || m_src_addr.sin_addr.s_addr == source_addr.sin_addr.s_addr &&
			m_src_addr.sin_port		   == 0 || m_src_addr.sin_port		   == source_addr.sin_port )
		{
			// Adjust the receive pointers for the number of bytes actually read
			m_rx_end += bytes_read;
			if( m_rx_end == m_receive_buffer_size ) m_rx_end = 0;
		}

		Unlock();
	}
    return true;
}

//*****
// Enter a critical section.
// Returns true if the lock was obtained. False otherwise
bool UDPReceiver::Lock(long timeout)
{
	DWORD WaitResult = WaitForSingleObject( m_rx_semaphore, timeout );
	switch( WaitResult )
	{ 
    case WAIT_OBJECT_0: return true;
    case WAIT_TIMEOUT:	return false;
	default:
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: Error locking semaphore.\n");)
		break;
	}
	return false;	
}

//*****
// Leave a critical section
void UDPReceiver::Unlock()
{
	if( !ReleaseSemaphore( m_rx_semaphore, 1, NULL ) )
	{
		UDP_RECEIVER_DEBUG(fprintf(stderr, "UDPReceiver: Error releasing semaphore.\n");)
	}
}
