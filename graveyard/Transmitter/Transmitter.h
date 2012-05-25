//************************************************************************
//
//  Transmitter
//
//************************************************************************
#ifndef PR_TRANSMITTER_H
#define PR_TRANSMITTER_H

#include <Winsock.h>
#include "PR/Common/StdString.h"
#include "PR/Common/PRTypes.h"

#pragma comment(lib, "ws2_32.lib")
#ifndef NDEBUG
#pragma comment(lib, "TransmitterD.lib")
#else//NDEBUG
#pragma comment(lib, "Transmitter.lib")
#endif//NDEBUG

namespace pr
{
	// Configuration
	struct TransmitterSettings
	{
		TransmitterSettings()
		{
			m_protocol				= IPPROTO_TCP;			// IPPROTO_TCP or IPPROTO_UDP
			m_local_ip				= "";					// 0 = ADDR_ANY 
			m_local_port			= 0;					// 0 = Don't care
			m_dest_ip				= "127.000.000.001";	// Localhost
			m_dest_port				= 6550;					// Default port
			m_blocking				= true;
			m_block_time.tv_sec		= 0;
			m_block_time.tv_usec	= INFINITE;
		}
		
		int			m_protocol;
		std::string	m_local_ip;
		uint16		m_local_port;
		std::string	m_dest_ip;
		uint16		m_dest_port;
		bool		m_blocking;
		timeval		m_block_time;	// Max 'select' blocking time
	};

	// Transmitter
	class Transmitter
	{
	public:
		enum EStatus  { EStatus_Disconnected, EStatus_Connecting, EStatus_Connected };
		Transmitter();
		HRESULT	Initialise(const TransmitterSettings& settings);
		void	UnInitialise();

		EStatus State() const				{ return m_status; }
		bool	IsConnected() const			{ return m_status == EStatus_Connected; }
		void	SetDestination(const char* ip, uint16 port);
		bool	IsDataReady(uint* bytes_available);
		bool	IsDataReady()				{ return IsDataReady(0); }
		HRESULT	Connect();
		void	Disconnect();

		// Send/Recv 
		uint	Send(const char* data, uint length);
		uint	Recv(char* data, uint length, int flags = 0);	// flags = MSG_PEEK
		uint	Recv(char* data, uint length) { return Recv(data, length, 0); }

	private:
		HRESULT	Select();

	private:
		TransmitterSettings		m_settings;
		SOCKET					m_socket;
		EStatus					m_status;
		sockaddr_in				m_destination;
	};
}//nammespace pr

#endif//PR_TRANSMITTER_H
