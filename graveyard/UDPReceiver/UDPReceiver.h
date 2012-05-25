//***********************************************************************************
//
//	UDP Receiver - A self contained class and thread for receiving UDP data
//
//***********************************************************************************
#ifndef UDPRECEIVER_H
#define UDPRECEIVER_H

#include <windows.h>
#include <winsock.h>

#pragma comment(lib, "Ws2_32.lib")
#ifndef NDEBUG
#pragma comment(lib, "UDPReceiverD.lib")
#else//NDEBUG
#pragma comment(lib, "UDPReceiver.lib")
#endif//NDEBUG

const int UDPR_MAX_UDPRECEIVER_THREAD_NAME	= 50;
const int UDPR_MAX_IP_STRING_LENGTH			= 16;

// UDP socket status
enum ReceiverStatus
{
    rsIDLE,
    rsCONNECTED,
    rsCLOSING,
	rsENDING
};

enum UDPReceiverError
{
	udpreSUCCESS = S_OK,
	udpreFAILED_TO_ALLOCATE_RECEIVE_BUFFER,
	udpreFAILED_TO_CREATE_SEMAPHORE,
	udpreFAILED_TO_CREATE_RECEIVE_THREAD,
};

// Structure used for construction of the Receiver object
struct UDPReceiverSettings
{
	UDPReceiverSettings() :
	m_my_ip(0),
	m_my_port(6550),
	m_src_ip(0),
	m_src_port(0),
	m_milliseconds_to_block(1000),
	m_buffer_size(1000),
	m_signal_callback(NULL)
	{
		strncpy(m_thread_name,	"UDP Receiver Thread",	UDPR_MAX_UDPRECEIVER_THREAD_NAME);
		strncpy(m_my_ip_str,	"127.0.0.1",			UDPR_MAX_IP_STRING_LENGTH);
		strncpy(m_src_ip_str,	"127.0.0.1",			UDPR_MAX_IP_STRING_LENGTH);
	}
	char		m_thread_name[UDPR_MAX_UDPRECEIVER_THREAD_NAME];	// The debugging name of the thread (max 50 chars)
	char		m_my_ip_str[UDPR_MAX_IP_STRING_LENGTH];				// Local IP address in string format (m_my_ip is used if this equals "")
	char		m_src_ip_str[UDPR_MAX_IP_STRING_LENGTH];			// Destination IP address in string format (m_src_ip is used if this equals "")
	DWORD		m_my_ip;											// Local IP address
	DWORD		m_src_ip;											// Source IP address. If '0' any IP is accepted
	WORD		m_my_port;											// Local PORT number
	WORD		m_src_port;											// Source PORT number. If '0' and PORT is accepted
	DWORD		m_milliseconds_to_block;							// INFINITE = indefinite blocking time
	DWORD		m_buffer_size;										// Transmit buffer size;
	void*		m_user_data;
	void		(*m_signal_callback)(DWORD bytes_available, void* user);	// The callback function to called when we've received data
};

//***********************************************************************************
// Receiver class
class UDPReceiver
{
public:
	// Create a thread to receive data via UDP
	UDPReceiver();
	~UDPReceiver();

	bool Initialise(const UDPReceiverSettings& settings);
	bool IsConnected() const				{ return m_socket_status == rsCONNECTED; }

	// Read data from the receive buffer.
	// Returns true if buf has data added to it, false if no data ready
	// It is assumed that buf is big enough to fit length bytes
	DWORD Receive( char* buf, DWORD length, bool must_be_full = false );

	// Returns the number of bytes that can be read from the receive buffer
	DWORD BytesAvailable();

	// Empty the buffer of the current data
	void FlushBuffer();

	// Tell the Receiver thread to shutdown.
	// The UDPReceiver object should not be destroyed until OkToDelete returns true
	void KillAndBlockTillDead()				{ Kill(); while( !OkToDelete() ) Sleep(10); }
	void Kill();
	bool OkToDelete()						{ return m_ok_to_delete; }

	// Get the last error that occurred
	UDPReceiverError GetLastError() const	{ return m_error_code; }

protected:
    static void ReceiverThread(UDPReceiver*);		// Static method for creating the UDPReceiver thread
    void ReceiverMain();							// Main loop for the Receiver thread
	bool OpenSocket();
	void CloseSocket();
	bool ReadSocket();
	bool Lock(long timeout = INFINITE);
	void Unlock();

private:
    // Configuration properties
	UDPReceiverSettings	m_settings;
	SOCKADDR_IN			m_my_addr;					// Local IP and port
    SOCKADDR_IN			m_src_addr;					// Remote IP and port
	struct timeval		m_block_time;				// Max 'select' blocking time
	char*				m_receive_buffer;			// The buffer to put received data into
	DWORD				m_receive_buffer_size;
	DWORD				m_rx_start;
	DWORD				m_rx_end;
	HANDLE			    m_thread_handle;
	
	// State properties
	SOCKET				m_socket;
    ReceiverStatus		m_socket_status;			// Current state of the UDP socket
	HANDLE				m_rx_semaphore;				// Used to synchronise access to the transmit buffer start and end pointer
	bool				m_ok_to_delete;
	UDPReceiverError	m_error_code;
};
#endif//UDPRECEIVER_H
