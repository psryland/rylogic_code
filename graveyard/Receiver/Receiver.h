//************************************************************************
//
//  Receiver
//
//************************************************************************
#ifndef PR_RECEIVER_H
#define PR_RECEIVER_H

#include <Winsock.h>
#include "PR/Common/StdString.h"
#include "PR/Common/PRTypes.h"

#pragma comment(lib, "ws2_32.lib")
#ifndef NDEBUG
#pragma comment(lib, "ReceiverD.lib")
#else//NDEBUG
#pragma comment(lib, "Receiver.lib")
#endif//NDEBUG

namespace pr
{
	// Configuration
	struct ReceiverSettings
{
	ReceiverSettings()
	{
		m_protocol			= IPPROTO_TCP; //IPPROTO_UDP
		
		m_local_ip[0]		= '\0';							// 0 = ADDR_ANY 
		m_local_port		= RECEIVER_DEFAULT_PORT;	
		_snprintf(m_src_ip, RECEIVER_MAX_IP_STRING_LENGTH, "127.000.000.001");
		m_src_port			= 0;							// 0 = Don't care

		m_blocking			= true;
		m_block_time.tv_sec = 0;
		m_block_time.tv_usec = INFINITE;
	}
	
	int		m_protocol;
	char	m_local_ip[RECEIVER_MAX_IP_STRING_LENGTH];
	WORD	m_local_port;
	char	m_src_ip[RECEIVER_MAX_IP_STRING_LENGTH];
	WORD	m_src_port;
	bool	m_blocking;
	struct timeval m_block_time;			// Max 'select' blocking time
};

//*****
// Network Host
class Receiver
{
public:
	enum Status { Disconnected, Connecting, Connected };
	Receiver();
	HRESULT	Initialise(const ReceiverSettings& settings);
	void	UnInitialise();

	Receiver::Status State() const	{ return m_status; }
	bool	IsConnected() const		{ return m_status == Connected; }
	void	SetSource(const char* ip, WORD port);
	bool	IsDataReady(DWORD* bytes_available = NULL);
	HRESULT	Connect();
	void	Disconnect();

	// Send/Recv 
	DWORD	SendFmt(const char* format, ...);
	DWORD	Send(const char* data, DWORD length);
	DWORD	Recv(char* data, DWORD length, int flags = 0);	// MSG_PEEK
	
private:
	HRESULT	Select();

private:
	ReceiverSettings		m_settings;
	SOCKET					m_socket;
	SOCKET					m_accept_socket;
	sockaddr_in				m_source;
	Status					m_status;
};

#endif//PR_RECEIVER_H
