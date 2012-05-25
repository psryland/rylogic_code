//*******************************************************************************************
//
//	A header file for sending data to LineDrawer
//
//*******************************************************************************************
#include "E:\Network\UDPTransmitter\UDPTransmitter.h"

class LDSend
{
public:
	bool IsConnected() const { return m_tx.IsConnected(); }
	bool TryConnect();
	bool Send(char* str);

private:
	UDPTransmitter m_tx;
};

//*******************************************************************************************
// Implementation
//*****
// Basic send
inline bool LDSend::Send(char* str)
{
	DWORD length = (DWORD)strlen(str) + 1;
	if( length >= m_tx.GetBufferSize() ) length = m_tx.GetBufferSize() - 1;
	if( m_tx.IsConnected() )
	{
		m_tx.SendNow(str, length);
		return true;
	}
	return false;
}


//*****
// Constructor
inline bool LDSend::TryConnect()
{
	if( m_tx.IsConnected() ) return true;

	UDPTransmitterSettings settings;
	settings.m_milliseconds_to_block = 0;
	settings.m_buffer_size		= 10000;
	m_tx.Initialise(settings);
	for( int i = 0; i < 3; ++i )
	{
        if( m_tx.IsConnected() ) break;
		Sleep(100);
	}

	return m_tx.IsConnected();
}

