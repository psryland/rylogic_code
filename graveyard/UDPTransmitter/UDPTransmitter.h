//***********************************************************************************
//
//	Sender - A self contained class and thread for sending UDP data
//
//***********************************************************************************
#ifndef UDPTRANSMITTER_H
#define UDPTRANSMITTER_H

#include <windows.h>
#include <winsock.h>

#pragma comment(lib, "Ws2_32.lib")
#ifndef NDEBUG
#pragma comment(lib, "E:/Libs/UDPTransmitterD.lib")
#else//NDEBUG
#pragma comment(lib, "E:/Libs/UDPTransmitter.lib")
#endif//NDEBUG

const int UDPT_MAX_UDPTRANSMITTER_THREAD_NAME	= 50;
const int UDPT_MAX_IP_STRING_LENGTH				= 16;

// UDP socket status
enum TransmitterStatus
{
    ssIDLE,
    ssCONNECTED,
    ssCLOSING,
	ssENDING
};

enum UDPTransmitterError
{
	udpteSUCCESS = S_OK,
	udpteFAILED_TO_ALLOCATE_TRANSMIT_BUFFER,
	udpteFAILED_TO_CREATE_SEMAPHORE,
	udpteFAILED_TO_CREATE_TRANSMIT_THREAD,
};

// Structure used for construction of the Sender object
struct UDPTransmitterSettings
{
	UDPTransmitterSettings() : 
	m_my_ip(0),
	m_my_port(0),
	m_dest_ip(0),
	m_dest_port(6550),
	m_milliseconds_to_block(1000),
	m_buffer_size(1000)
	{
		strncpy(m_thread_name,	"UDP Transmitter Thread",	UDPT_MAX_UDPTRANSMITTER_THREAD_NAME);
		strncpy(m_my_ip_str,	"127.0.0.1",				UDPT_MAX_IP_STRING_LENGTH);
		strncpy(m_dest_ip_str,	"127.0.0.1",				UDPT_MAX_IP_STRING_LENGTH);
	}
	char	m_thread_name[UDPT_MAX_UDPTRANSMITTER_THREAD_NAME];		// The debugging name of the thread (max 50 chars)
	char	m_my_ip_str[UDPT_MAX_IP_STRING_LENGTH];					// Local IP address in string format (m_my_ip is used if this equals "")
	char	m_dest_ip_str[UDPT_MAX_IP_STRING_LENGTH];				// Destination IP address in string format (m_dest_ip is used if this equals "")
	DWORD	m_my_ip;												// Local IP address
	WORD	m_my_port;												// Local PORT number
	DWORD	m_dest_ip;												// Destination IP address
	WORD	m_dest_port;											// Destination PORT number
	DWORD	m_milliseconds_to_block;								// INFINITE = indefinite blocking time
	int		m_buffer_size;											// Transmit buffer size;
};


//***********************************************************************************
// Sender class
class UDPTransmitter
{
public:
	// Create a thread to send data via UDP
	UDPTransmitter();
	~UDPTransmitter();

	bool Initialise(const UDPTransmitterSettings& settings);
	bool IsConnected() const	{ return m_socket_status == ssCONNECTED; }
	DWORD GetBufferSize() const { return m_transmit_buffer_size; }

	// Add data to the send buffer.
	// Returns true if data sent, false if timed out
	bool Send(char* buf, DWORD length, long timeout = INFINITE);
	bool SendNow(char* buf, DWORD length, long timeout = INFINITE);

	// Returns the number of bytes available to write into in the transmit buffer
	DWORD BytesAvailable();

	// Empty the buffer of the current data
	void FlushBuffer();

	// Tell the UDPTransmitter thread to shutdown.
	// The UDPTransmitter object should not be destroyed until OkToDelete returns true
	void KillAndBlockTillDead()					{ Kill(); while( !OkToDelete() ) Sleep(10); }
	void Kill();
	bool OkToDelete()							{ return m_ok_to_delete; }

	// Get the last error that occurred
	UDPTransmitterError GetLastError() const	{ return m_error_code; }

protected:
    static void TransmitterThread(UDPTransmitter*);	// Static method for creating the UDPTransmitter thread
    void TransmitterMain();							// Main loop for the UDPTransmitter thread
	bool OpenSocket();
	void CloseSocket();
	bool WriteSocket();
	bool Lock(long timeout = INFINITE);
	void Unlock();

private:
    // Configuration properties
	UDPTransmitterSettings	m_settings;
	SOCKADDR_IN				m_my_addr;				// Local IP and port
    SOCKADDR_IN				m_dest_addr;			// Remote IP and port
	struct timeval			m_block_time;			// Max 'select' blocking time
	char*					m_transmit_buffer;		// The buffer to send data from
	DWORD					m_transmit_buffer_size;
	DWORD					m_tx_start;
	DWORD					m_tx_end;
	HANDLE					m_thread_handle;
	
	// State properties
	SOCKET					m_socket;
    TransmitterStatus		m_socket_status;		// Current state of the UDP socket
	HANDLE					m_tx_semaphore;			// Used to synchronise access to the transmit buffer start and end pointer
	bool					m_ok_to_delete;
	UDPTransmitterError		m_error_code;
};
#endif//UDPTRANSMITTER_H