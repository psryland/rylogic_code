//*********************************************************
//*********************************************************
#pragma once
#ifndef PR_COMMON_SERIAL_PORT_IO_H
#define PR_COMMON_SERIAL_PORT_IO_H

// WIP

#include <windows.h>
#include "PRAssert.h"

namespace pr
{
	namespace impl
	{
		template <typename T>
		class SerialPortIO
		{
			HANDLE	m_handle;			// File handle to the serial port
			HANDLE	m_ovrlap_evt;		// Event handle for overlapped calls
			DWORD	m_baud;				// Baud rate
			BYTE	m_data_bits;		// Valid values are 5,6,7,8 (8 is default)
			BYTE	m_parity;			// Parity
			BYTE	m_stop_bits;		// Stop bits
		//	EEvent	m_eEvent;			// Event type
		//	DWORD	m_dwEventMask;		// Event mask
		//	LONG	m_lLastError;		// Last serial error

		//	// CancelIo wrapper (for Win95 compatibility)
		//	BOOL CancelCommIo (void);
		//
		public:

			enum EReadMode { EReadMode_Blocking, EReadMode_NonBlocking };

			// Constructor
			SerialPortIO::SerialPortIO(DWORD baud = CBR_9600, BYTE data_bits = 8, BYTE parity = NOPARITY, BYTE stop_bits = ONESTOPBIT)
			:m_handle(INVALID_HANDLE_VALUE)
			,m_ovrlap_evt(0)
			,m_baud(baud)
			,m_data_bits(data_bits)
			,m_parity(parity)
			,m_stop_bits(stop_bits)
			//,m_lLastError(ERROR_SUCCESS)
			//,m_eEvent(EEventNone)
			//,m_dwEventMask(0)
			{}

			SerialPortIO::~SerialPortIO()
			{
				if (IsOpen()) Close();
			}

			// Returns true if the port is already opened
			bool IsOpen() const { return m_handle != INVALID_HANDLE_VALUE; }

			// Configure the port once it is open
			bool Config(DWORD baud, BYTE data_bits, BYTE parity, BYTE stop_bits)
			{
				PR_ASSERT(PR_DBG, IsOpen(), "Serial port should be open before calling this method");
				if (!IsOpen()) return false;

				// Read the comm state, update the data, then set it again
				DCB comm_state; comm_state.DCBlength = sizeof(DCB);
				if (!::GetCommState(m_handle, &comm_state)) return false;
				comm_state.BaudRate = m_baud		= baud;
				comm_state.ByteSize = m_data_bits	= data_bits;
				comm_state.Parity   = m_parity		= parity;
				comm_state.StopBits = m_stop_bits	= stop_bits;
				comm_state.fParity  = parity != NOPARITY;
				return ::SetCommState(m_handle, &comm_state) == TRUE;
			}

			// Set the read mode for the port, either blocking or non-blocking
			void SetReadMode(EReadMode read_mode)
			{
				PR_ASSERT(PR_DBG, IsOpen(), "Serial port should be open before calling this method");
				if (!IsOpen()) return false;

				// Read the timeouts, update, then set them
				COMMTIMEOUTS cto;
				if (!::GetCommTimeouts(m_handle, &cto)) return false;
				switch (read_mode)
				{
				default: PR_ERROR(PR_DBG); break;
				case EReadMode_Blocking:
					cto.ReadIntervalTimeout = 0;
					cto.ReadTotalTimeoutConstant = 0;
					cto.ReadTotalTimeoutMultiplier = 0;
					break;
				case EReadMode_NonBlocking:
					cto.ReadIntervalTimeout = MAXDWORD;
					cto.ReadTotalTimeoutConstant = 0;
					cto.ReadTotalTimeoutMultiplier = 0;
					break;
				}
				return ::SetCommTimeouts(m_handle, &cto) == TRUE;
			}

			// Open a serial port
			// 'port_name' is something like "COM1"
			// 'in_buf_size' and 'out_buf_size' are the size of the local buffers
			// 'overlapped' allows the read and writes to be asynchronous
			bool SerialPortIO::Open(char const* port_name)											{ return Open(port_name, 0, 0, true); }
			bool SerialPortIO::Open(char const* port_name, DWORD in_buf_size, DWORD out_buf_size)	{ return Open(port_name, in_buf_size, out_buf_size, true); }
			bool SerialPortIO::Open(char const* port_name, DWORD in_buf_size, DWORD out_buf_size, bool overlapped)
			{
				PR_ASSERT(PR_DBG, !IsOpen(), "Serial port already open");
				if (!IsOpen()) Close();

				m_handle = ::CreateFile(port_name, GENERIC_READ|GENERIC_WRITE, 0, 0, OPEN_EXISTING, overlapped ? FILE_FLAG_OVERLAPPED : 0, 0);
				if (m_handle == INVALID_HANDLE_VALUE) return false;

				// Create the event handle for overlapped calls
				if (overlapped)
				{
					m_ovrlap_evt = ::CreateEvent(0, true, false, 0);
					if (m_ovrlap_evt == 0) { Close(); return false; }
				}
			
				// Setup buffering
				if (in_buf_size != 0 || out_buf_size != 0)
				{
					if (in_buf_size  < 16) in_buf_size  = 16;
					if (out_buf_size < 16) out_buf_size = 16;
					if (!::SetupComm(m_handle, in_buf_size, out_buf_size)) { Close(); return false; }
				}

				// Setup the default communication mask
				SetMask();

				// Non-blocking reads by default
				SetReadMode(EReadMode_NonBlocking);

				// Setup the device for default settings
 				COMMCONFIG config = {0};
				config.dwSize = sizeof(config);
				if (::GetDefaultCommConfig(port_name, &config, config.dwSize))
				{
					// Set the default communication configuration
					if (!::SetCommConfig(m_handle,&config,dwSize))
					{
						// Display a warning
						_RPTF0(_CRT_WARN,"CSerial::Open - Unable to set default communication configuration.\n");
					}
				}
				else
				{
					// Display a warning
					_RPTF0(_CRT_WARN,"CSerial::Open - Unable to obtain default communication configuration.\n");
				}

				// Return successful
				return m_lLastError;
			}

			// Close the serial port.
			void Close();
			{
				if (!IsOpen()) return;
				if (m_ovrlap_evt) ::CloseHandle(m_ovrlap_evt); m_ovrlap_evt = 0;
				::CloseHandle(m_handle); m_handle = INVALID_HANDLE_VALUE;
			}

			// Read data from the serial port. Refer to the description of
			// the 'SetupReadTimeouts' for an explanation about (non) blocking
			// reads and how to use this.
			bool Read(void* data, size_t size, DWORD& bytes_read)										{ return Read(data, size, bytes_read, INFINITE, 0); }
			bool Read(void* data, size_t size, DWORD& bytes_read, DWORD timeout)						{ return Read(data, size, bytes_read, timeout, 0); }
			bool Read(void* data, size_t size, DWORD& bytes_read, DWORD timeout, LPOVERLAPPED overlap)
			{
				PR_ASSERT(PR_DBG, IsOpen(), "Serial port should be open before calling this method");
				if (!IsOpen()) return false;

				// Use a local overlap structure if one is not provided
				OVERLAPPED local_overlap;
				if (overlap == 0)
				{
					memset(&local_overlap, 0, sizeof(local_overlap));
					local_overlap.hEvent = m_ovrlap_evt;
					overlap = &local_overlap;
				}

				PR_EXPAND(PR_DBG, memset(data, 0xcc, size));
				//PR_ASSERT(PR_DBG, m_ovrlap_evt || (!overlap && (timeout == INFINITE)));

				//// Check if an overlapped structure has been specified
				//if (!m_ovrlap_evt && (overlap || (timeout != INFINITE)))
				//{
				//	// Set the internal error code
				//	m_lLastError = ERROR_INVALID_FUNCTION;

				//	// Issue an error and quit
				//	_RPTF0(_CRT_WARN,"CSerial::Read - Overlapped I/O is disabled, specified parameters are illegal.\n");
				//	return m_lLastError;
				//}

				// Make sure the overlapped structure isn't busy
				PR_ASSERT(PR_DBG, !m_ovrlap_evt || HasOverlappedIoCompleted(overlap));
				
				// Read the data
				if (!::ReadFile(m_handle, data, size, bytes_read, overlap))
				{
					// Set the internal error code
					long lLastError = ::GetLastError();

					// Overlapped operation in progress is not an actual error
					if (lLastError != ERROR_IO_PENDING)
					{
						// Save the error
						m_lLastError = lLastError;

						// Issue an error and quit
						_RPTF0(_CRT_WARN,"CSerial::Read - Unable to read the data\n");
						return m_lLastError;
					}

					// We need to block if the client didn't specify an overlapped structure
					if (overlap == &ovInternal)
					{
						// Wait for the overlapped operation to complete
						switch (::WaitForSingleObject(overlap->hEvent,timeout))
						{
						case WAIT_OBJECT_0:
							// The overlapped operation has completed
							if (!::GetOverlappedResult(m_handle,overlap,bytes_read,FALSE))
							{
								// Set the internal error code
								m_lLastError = ::GetLastError();

								_RPTF0(_CRT_WARN,"CSerial::Read - Overlapped completed without result\n");
								return m_lLastError;
							}
							break;

						case WAIT_TIMEOUT:
							// Cancel the I/O operation
							CancelCommIo();

							// The operation timed out. Set the internal error code and quit
							m_lLastError = ERROR_TIMEOUT;
							return m_lLastError;

						default:
							// Set the internal error code
							m_lLastError = ::GetLastError();

							// Issue an error and quit
							_RPTF0(_CRT_WARN,"CSerial::Read - Unable to wait until data has been read\n");
							return m_lLastError;
						}
					}
				}
				else
				{
					// The operation completed immediatly. Just to be sure
					// we'll set the overlapped structure's event handle.
					if (overlap)
						::SetEvent(overlap->hEvent);
				}
			
				// Return successfully
				return m_lLastError;
			}

			//// Communication event
			//typedef enum
			//{
			//	EEventUnknown  	   = -1,			// Unknown event
			//	EEventNone  	   = 0,				// Event trigged without cause
			//	EEventBreak 	   = EV_BREAK,		// A break was detected on input
			//	EEventCTS   	   = EV_CTS,		// The CTS signal changed state
			//	EEventDSR   	   = EV_DSR,		// The DSR signal changed state
			//	EEventError 	   = EV_ERR,		// A line-status error occurred
			//	EEventRing  	   = EV_RING,		// A ring indicator was detected
			//	EEventRLSD  	   = EV_RLSD,		// The RLSD signal changed state
			//	EEventRecv  	   = EV_RXCHAR,		// Data is received on input
			//	EEventRcvEv 	   = EV_RXFLAG,		// Event character was received on input
			//	EEventSend		   = EV_TXEMPTY,	// Last character on output was sent
			//	EEventPrinterError = EV_PERR,		// Printer error occured
			//	EEventRx80Full	   = EV_RX80FULL,	// Receive buffer is 80 percent full
			//	EEventProviderEvt1 = EV_EVENT1,		// Provider specific event 1
			//	EEventProviderEvt2 = EV_EVENT2,		// Provider specific event 2
			//} 
			//EEvent;

			//// Handshaking
			//typedef enum
			//{
			//	EHandshakeUnknown		= -1,	// Unknown
			//	EHandshakeOff			=  0,	// No handshaking
			//	EHandshakeHardware		=  1,	// Hardware handshaking (RTS/CTS)
			//	EHandshakeSoftware		=  2	// Software handshaking (XON/XOFF)
			//} 
			//EHandshake;

			//// Timeout settings
			//typedef enum
			//{
			//	EReadTimeoutUnknown		= -1,	// Unknown
			//	EReadTimeoutNonblocking	=  0,	// Always return immediately
			//	EReadTimeoutBlocking	=  1	// Block until everything is retrieved
			//}
			//EReadTimeout;

			//// Communication errors
			//typedef enum
			//{
			//	EErrorUnknown = 0,			// Unknown
			//	EErrorBreak   = CE_BREAK,	// Break condition detected
			//	EErrorFrame   = CE_FRAME,	// Framing error
			//	EErrorIOE     = CE_IOE,		// I/O device error
			//	EErrorMode    = CE_MODE,	// Unsupported mode
			//	EErrorOverrun = CE_OVERRUN,	// Character buffer overrun, next byte is lost
			//	EErrorRxOver  = CE_RXOVER,	// Input buffer overflow, byte lost
			//	EErrorParity  = CE_RXPARITY,// Input parity error
			//	EErrorTxFull  = CE_TXFULL	// Output buffer full
			//}
			//EError;

			//// Port availability
			//typedef enum
			//{
			//	EPortUnknownError = -1,		// Unknown error occurred
			//	EPortAvailable    =  0,		// Port is available
			//	EPortNotAvailable =  1,		// Port is not present
			//	EPortInUse        =  2		// Port is in use

			//} 
			//EPort;

		// Operations
		public:
			// Check if particular COM-port is available (static method).
			static EPort CheckPort (LPCTSTR lpszDevice);

			// Set/clear the event character. When this byte is being received
			// on the serial port then the EEventRcvEv event is signalled,
			// when the mask has been set appropriately. If the fAdjustMask flag
			// has been set, then the event mask is automatically adjusted.
			virtual LONG SetEventChar (BYTE bEventChar, bool fAdjustMask = true);

			// Set the event mask, which indicates what events should be
			// monitored. The WaitEvent method can only monitor events that
			// have been enabled. The default setting only monitors the
			// error events and data events. An application may choose to
			// monitor CTS. DSR, RLSD, etc as well.
			virtual LONG SetMask (DWORD dwMask = EEventBreak|EEventError|EEventRecv);

			// The WaitEvent method waits for one of the events that are
			// enabled (see SetMask).
			virtual LONG WaitEvent (LPOVERLAPPED lpOverlapped = 0, DWORD timeout = INFINITE);

			// Setup the handshaking protocol. There are three forms of
			// handshaking:
			//
			// 1) No handshaking, so data is always send even if the receiver
			//    cannot handle the data anymore. This can lead to data loss,
			//    when the sender is able to transmit data faster then the
			//    receiver can handle.
			// 2) Hardware handshaking, where the RTS/CTS lines are used to
			//    indicate if data can be sent. This mode requires that both
			//    ports and the cable support hardware handshaking. Hardware
			//    handshaking is the most reliable and efficient form of
			//    handshaking available, but is hardware dependant.
			// 3) Software handshaking, where the XON/XOFF characters are used
			//    to throttle the data. A major drawback of this method is that
			//    these characters cannot be used for data anymore.
			virtual LONG SetupHandshaking (EHandshake eHandshake);
			
			// Obtain communication settings
			virtual EBaudrate  GetBaudrate    (void);
			virtual EDataBits  GetDataBits    (void);
			virtual EParity    GetParity      (void);
			virtual EStopBits  GetStopBits    (void);
			virtual EHandshake GetHandshaking (void);
			virtual DWORD      GetEventMask   (void);
			virtual BYTE       GetEventChar   (void);

			// Write data to the serial port. Note that we are only able to
			// send ANSI strings, because it probably doesn't make sense to
			// transmit Unicode strings to an application.
			virtual LONG Write (const void* pData, size_t size, DWORD* pdwWritten = 0, LPOVERLAPPED lpOverlapped = 0, DWORD timeout = INFINITE);
			virtual LONG Write (LPCSTR pString, DWORD* pdwWritten = 0, LPOVERLAPPED lpOverlapped = 0, DWORD timeout = INFINITE);

			// Send a break
			LONG Break (void);

			// Determine what caused the event to trigger
			EEvent GetEventType (void);

			// Obtain the error
			EError GetError (void);

			// Obtain the COMM and event handle
			HANDLE GetCommHandle (void)		{ return m_handle; }

			// Obtain last error status
			LONG GetLastError (void) const	{ return m_lLastError; }

			// Obtain CTS/DSR/RING/RLSD settings
			bool GetCTS (void);
			bool GetDSR (void);
			bool GetRing (void);
			bool GetRLSD (void);

			// Purge all buffers
			LONG Purge (void);

		protected:
			// Internal helper class which wraps DCB structure
			class CDCB : public DCB
			{
			public:
				CDCB() { DCBlength = sizeof(DCB); }
			};
		};
	}
	typedef impl::SerialPortIO<void> SerialPortIO;
}

#endif//PR_COMMON_SERIAL_PORT_IO_H


//////////////////////////////////////////////////////////////////////
// Helper methods

inline BOOL CSerial::CancelCommIo (void)
{
#ifdef SERIAL_NO_CANCELIO
	// CancelIo shouldn't have been called at this point
	::DebugBreak();
	return FALSE;
#else

	// Cancel the I/O request
	return ::CancelIo(m_handle);

#endif	// SERIAL_NO_CANCELIO
}


//////////////////////////////////////////////////////////////////////
// Code


CSerial::EPort CSerial::CheckPort (LPCTSTR lpszDevice)
{
	// Try to open the device
	HANDLE hFile = ::CreateFile(lpszDevice, 
						   GENERIC_READ|GENERIC_WRITE, 
						   0, 
						   0, 
						   OPEN_EXISTING, 
						   0,
						   0);

	// Check if we could open the device
	if (hFile == INVALID_HANDLE_VALUE)
	{
		// Display error
		switch (::GetLastError())
		{
		case ERROR_FILE_NOT_FOUND:
			// The specified COM-port does not exist
			return EPortNotAvailable;

		case ERROR_ACCESS_DENIED:
			// The specified COM-port is in use
			return EPortInUse;

		default:
			// Something else is wrong
			return EPortUnknownError;
		}
	}

	// Close handle
	::CloseHandle(hFile);

	// Port is available
	return EPortAvailable;
}




LONG CSerial::SetEventChar (BYTE bEventChar, bool fAdjustMask)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::SetEventChar - Device is not opened\n");
		return m_lLastError;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::SetEventChar - Unable to obtain DCB information\n");
		return m_lLastError;
	}

	// Set the new event character
	dcb.EvtChar = char(bEventChar);

	// Adjust the event mask, to make sure the event will be received
	if (fAdjustMask)
	{
		// Enable 'receive event character' event.  Note that this
		// will generate an EEventNone if there is an asynchronous
		// WaitCommEvent pending.
		SetMask(GetEventMask() | EEventRcvEv);
	}

	// Set the new DCB structure
	if (!::SetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::SetEventChar - Unable to set DCB information\n");
		return m_lLastError;
	}

	// Return successful
	return m_lLastError;
}

LONG CSerial::SetMask (DWORD dwEventMask)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::SetMask - Device is not opened\n");
		return m_lLastError;
	}

	// Set the new mask. Note that this will generate an EEventNone
	// if there is an asynchronous WaitCommEvent pending.
	if (!::SetCommMask(m_handle,dwEventMask))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::SetMask - Unable to set event mask\n");
		return m_lLastError;
	}

	// Save event mask and return successful
	m_dwEventMask = dwEventMask;
	return m_lLastError;
}

LONG CSerial::WaitEvent (LPOVERLAPPED lpOverlapped, DWORD timeout)
{
	// Check if time-outs are supported
	CheckRequirements(lpOverlapped,timeout);

	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::WaitEvent - Device is not opened\n");
		return m_lLastError;
	}

#ifndef SERIAL_NO_OVERLAPPED

	// Check if an overlapped structure has been specified
	if (!m_hevtOverlapped && (lpOverlapped || (timeout != INFINITE)))
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_FUNCTION;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::WaitEvent - Overlapped I/O is disabled, specified parameters are illegal.\n");
		return m_lLastError;
	}

	// Wait for the event to happen
	OVERLAPPED ovInternal;
	if (!lpOverlapped && m_hevtOverlapped)
	{
		// Setup our own overlapped structure
		memset(&ovInternal,0,sizeof(ovInternal));
		ovInternal.hEvent = m_hevtOverlapped;

		// Use our internal overlapped structure
		lpOverlapped = &ovInternal;
	}

	// Make sure the overlapped structure isn't busy
	_ASSERTE(!m_hevtOverlapped || HasOverlappedIoCompleted(lpOverlapped));

	// Wait for the COM event
	if (!::WaitCommEvent(m_handle,LPDWORD(&m_eEvent),lpOverlapped))
	{
		// Set the internal error code
		long lLastError = ::GetLastError();

		// Overlapped operation in progress is not an actual error
		if (lLastError != ERROR_IO_PENDING)
		{
			// Save the error
			m_lLastError = lLastError;

			// Issue an error and quit
			_RPTF0(_CRT_WARN,"CSerial::WaitEvent - Unable to wait for COM event\n");
			return m_lLastError;
		}

		// We need to block if the client didn't specify an overlapped structure
		if (lpOverlapped == &ovInternal)
		{
			// Wait for the overlapped operation to complete
			switch (::WaitForSingleObject(lpOverlapped->hEvent,timeout))
			{
			case WAIT_OBJECT_0:
				// The overlapped operation has completed
				break;

			case WAIT_TIMEOUT:
				// Cancel the I/O operation
				CancelCommIo();

				// The operation timed out. Set the internal error code and quit
				m_lLastError = ERROR_TIMEOUT;
				return m_lLastError;

			default:
				// Set the internal error code
				m_lLastError = ::GetLastError();

				// Issue an error and quit
				_RPTF0(_CRT_WARN,"CSerial::WaitEvent - Unable to wait until COM event has arrived\n");
				return m_lLastError;
			}
		}
	}
	else
	{
		// The operation completed immediatly. Just to be sure
		// we'll set the overlapped structure's event handle.
		if (lpOverlapped)
			::SetEvent(lpOverlapped->hEvent);
	}
#else

	// Wait for the COM event
	if (!::WaitCommEvent(m_handle,LPDWORD(&m_eEvent),0))
	{
		// Set the internal error code
		m_lLastError = ::GetLastError();

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::WaitEvent - Unable to wait for COM event\n");
		return m_lLastError;
	}

#endif

	// Return successfully
	return m_lLastError;
}


LONG CSerial::SetupHandshaking (EHandshake eHandshake)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::SetupHandshaking - Device is not opened\n");
		return m_lLastError;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::SetupHandshaking - Unable to obtain DCB information\n");
		return m_lLastError;
	}

	// Set the handshaking flags
	switch (eHandshake)
	{
	case EHandshakeOff:
		dcb.fOutxCtsFlow = false;					// Disable CTS monitoring
		dcb.fOutxDsrFlow = false;					// Disable DSR monitoring
		dcb.fDtrControl = DTR_CONTROL_DISABLE;		// Disable DTR monitoring
		dcb.fOutX = false;							// Disable XON/XOFF for transmission
		dcb.fInX = false;							// Disable XON/XOFF for receiving
		dcb.fRtsControl = RTS_CONTROL_DISABLE;		// Disable RTS (Ready To Send)
		break;

	case EHandshakeHardware:
		dcb.fOutxCtsFlow = true;					// Enable CTS monitoring
		dcb.fOutxDsrFlow = true;					// Enable DSR monitoring
		dcb.fDtrControl = DTR_CONTROL_HANDSHAKE;	// Enable DTR handshaking
		dcb.fOutX = false;							// Disable XON/XOFF for transmission
		dcb.fInX = false;							// Disable XON/XOFF for receiving
		dcb.fRtsControl = RTS_CONTROL_HANDSHAKE;	// Enable RTS handshaking
		break;

	case EHandshakeSoftware:
		dcb.fOutxCtsFlow = false;					// Disable CTS (Clear To Send)
		dcb.fOutxDsrFlow = false;					// Disable DSR (Data Set Ready)
		dcb.fDtrControl = DTR_CONTROL_DISABLE;		// Disable DTR (Data Terminal Ready)
		dcb.fOutX = true;							// Enable XON/XOFF for transmission
		dcb.fInX = true;							// Enable XON/XOFF for receiving
		dcb.fRtsControl = RTS_CONTROL_DISABLE;		// Disable RTS (Ready To Send)
		break;

	default:
		// This shouldn't be possible
		_ASSERTE(false);
		m_lLastError = E_INVALIDARG;
		return m_lLastError;
	}

	// Set the new DCB structure
	if (!::SetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::SetupHandshaking - Unable to set DCB information\n");
		return m_lLastError;
	}

	// Return successful
	return m_lLastError;
}

CSerial::EBaudrate CSerial::GetBaudrate (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetBaudrate - Device is not opened\n");
		return EBaudUnknown;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetBaudrate - Unable to obtain DCB information\n");
		return EBaudUnknown;
	}

	// Return the appropriate baudrate
	return EBaudrate(dcb.BaudRate);
}

CSerial::EDataBits CSerial::GetDataBits (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetDataBits - Device is not opened\n");
		return EDataUnknown;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetDataBits - Unable to obtain DCB information\n");
		return EDataUnknown;
	}

	// Return the appropriate bytesize
	return EDataBits(dcb.ByteSize);
}

CSerial::EParity CSerial::GetParity (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetParity - Device is not opened\n");
		return EParUnknown;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetParity - Unable to obtain DCB information\n");
		return EParUnknown;
	}

	// Check if parity is used
	if (!dcb.fParity)
	{
		// No parity
		return EParNone;
	}

	// Return the appropriate parity setting
	return EParity(dcb.Parity);
}

CSerial::EStopBits CSerial::GetStopBits (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetStopBits - Device is not opened\n");
		return EStopUnknown;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetStopBits - Unable to obtain DCB information\n");
		return EStopUnknown;
	}

	// Return the appropriate stopbits
	return EStopBits(dcb.StopBits);
}

DWORD CSerial::GetEventMask (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetEventMask - Device is not opened\n");
		return 0;
	}

	// Return the event mask
	return m_dwEventMask;
}

BYTE CSerial::GetEventChar (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetEventChar - Device is not opened\n");
		return 0;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetEventChar - Unable to obtain DCB information\n");
		return 0;
	}

	// Set the new event character
	return BYTE(dcb.EvtChar);
}

CSerial::EHandshake CSerial::GetHandshaking (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetHandshaking - Device is not opened\n");
		return EHandshakeUnknown;
	}

	// Obtain the DCB structure for the device
	CDCB dcb;
	if (!::GetCommState(m_handle,&dcb))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetHandshaking - Unable to obtain DCB information\n");
		return EHandshakeUnknown;
	}

	// Check if hardware handshaking is being used
	if ((dcb.fDtrControl == DTR_CONTROL_HANDSHAKE) && (dcb.fRtsControl == RTS_CONTROL_HANDSHAKE))
		return EHandshakeHardware;

	// Check if software handshaking is being used
	if (dcb.fOutX && dcb.fInX)
		return EHandshakeSoftware;

	// No handshaking is being used
	return EHandshakeOff;
}

LONG CSerial::Write (const void* pData, size_t size, DWORD* pdwWritten, LPOVERLAPPED lpOverlapped, DWORD timeout)
{
	// Check if time-outs are supported
	CheckRequirements(lpOverlapped,timeout);

	// Overlapped operation should specify the pdwWritten variable
	_ASSERTE(!lpOverlapped || pdwWritten);

	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Use our own variable for read count
	DWORD dwWritten;
	if (pdwWritten == 0)
	{
		pdwWritten = &dwWritten;
	}

	// Reset the number of bytes written
	*pdwWritten = 0;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::Write - Device is not opened\n");
		return m_lLastError;
	}

#ifndef SERIAL_NO_OVERLAPPED

	// Check if an overlapped structure has been specified
	if (!m_hevtOverlapped && (lpOverlapped || (timeout != INFINITE)))
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_FUNCTION;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::Write - Overlapped I/O is disabled, specified parameters are illegal.\n");
		return m_lLastError;
	}

	// Wait for the event to happen
	OVERLAPPED ovInternal;
	if (!lpOverlapped && m_hevtOverlapped)
	{
		// Setup our own overlapped structure
		memset(&ovInternal,0,sizeof(ovInternal));
		ovInternal.hEvent = m_hevtOverlapped;

		// Use our internal overlapped structure
		lpOverlapped = &ovInternal;
	}

	// Make sure the overlapped structure isn't busy
	_ASSERTE(!m_hevtOverlapped || HasOverlappedIoCompleted(lpOverlapped));

	// Write the data
	if (!::WriteFile(m_handle,pData,size,pdwWritten,lpOverlapped))
	{
		// Set the internal error code
		long lLastError = ::GetLastError();

		// Overlapped operation in progress is not an actual error
		if (lLastError != ERROR_IO_PENDING)
		{
			// Save the error
			m_lLastError = lLastError;

			// Issue an error and quit
			_RPTF0(_CRT_WARN,"CSerial::Write - Unable to write the data\n");
			return m_lLastError;
		}

		// We need to block if the client didn't specify an overlapped structure
		if (lpOverlapped == &ovInternal)
		{
			// Wait for the overlapped operation to complete
			switch (::WaitForSingleObject(lpOverlapped->hEvent,timeout))
			{
			case WAIT_OBJECT_0:
				// The overlapped operation has completed
				if (!::GetOverlappedResult(m_handle,lpOverlapped,pdwWritten,FALSE))
				{
					// Set the internal error code
					m_lLastError = ::GetLastError();

					_RPTF0(_CRT_WARN,"CSerial::Write - Overlapped completed without result\n");
					return m_lLastError;
				}
				break;

			case WAIT_TIMEOUT:
				// Cancel the I/O operation
				CancelCommIo();

				// The operation timed out. Set the internal error code and quit
				m_lLastError = ERROR_TIMEOUT;
				return m_lLastError;

			default:
				// Set the internal error code
				m_lLastError = ::GetLastError();

				// Issue an error and quit
				_RPTF0(_CRT_WARN,"CSerial::Write - Unable to wait until data has been sent\n");
				return m_lLastError;
			}
		}
	}
	else
	{
		// The operation completed immediatly. Just to be sure
		// we'll set the overlapped structure's event handle.
		if (lpOverlapped)
			::SetEvent(lpOverlapped->hEvent);
	}

#else

	// Write the data
	if (!::WriteFile(m_handle,pData,size,pdwWritten,0))
	{
		// Set the internal error code
		m_lLastError = ::GetLastError();

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::Write - Unable to write the data\n");
		return m_lLastError;
	}

#endif

	// Return successfully
	return m_lLastError;
}

LONG CSerial::Write (LPCSTR pString, DWORD* pdwWritten, LPOVERLAPPED lpOverlapped, DWORD timeout)
{
	// Check if time-outs are supported
	CheckRequirements(lpOverlapped,timeout);

	// Determine the length of the string
	return Write(pString,strlen(pString),pdwWritten,lpOverlapped,timeout);
}

LONG CSerial::Purge()
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::Purge - Device is not opened\n");
		return m_lLastError;
	}

	if (!::PurgeComm(m_handle, PURGE_TXCLEAR | PURGE_RXCLEAR))
	{
		// Set the internal error code
		m_lLastError = ::GetLastError();
		_RPTF0(_CRT_WARN,"CSerial::Purge - Overlapped completed without result\n");
	}
	
	// Return successfully
	return m_lLastError;
}

LONG CSerial::Break (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::Break - Device is not opened\n");
		return m_lLastError;
	}

    // Set the RS-232 port in break mode for a little while
    ::SetCommBreak(m_handle);
    ::Sleep(100);
    ::ClearCommBreak(m_handle);

	// Return successfully
	return m_lLastError;
}

CSerial::EEvent CSerial::GetEventType (void)
{
#ifdef _DEBUG
	// Check if the event is within the mask
	if ((m_eEvent & m_dwEventMask) == 0)
		_RPTF2(_CRT_WARN,"CSerial::GetEventType - Event %08Xh not within mask %08Xh.\n", m_eEvent, m_dwEventMask);
#endif

	// Obtain the event (mask unwanted events out)
	EEvent eEvent = EEvent(m_eEvent & m_dwEventMask);

	// Reset internal event type
	m_eEvent = EEventNone;

	// Return the current cause
	return eEvent;
}

CSerial::EError CSerial::GetError (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Check if the device is open
	if (m_handle == 0)
	{
		// Set the internal error code
		m_lLastError = ERROR_INVALID_HANDLE;

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetError - Device is not opened\n");
		return EErrorUnknown;
	}

	// Obtain COM status
	DWORD dwErrors = 0;
	if (!::ClearCommError(m_handle,&dwErrors,0))
	{
		// Set the internal error code
		m_lLastError = ::GetLastError();

		// Issue an error and quit
		_RPTF0(_CRT_WARN,"CSerial::GetError - Unable to obtain COM status\n");
		return EErrorUnknown;
	}

	// Return the error
	return EError(dwErrors);
}

bool CSerial::GetCTS (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Obtain the modem status
	DWORD dwModemStat = 0;
	if (!::GetCommModemStatus(m_handle,&dwModemStat))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetCTS - Unable to obtain the modem status\n");
		return false;
	}

	// Determine if CTS is on
	return (dwModemStat & MS_CTS_ON) != 0;
}

bool CSerial::GetDSR (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Obtain the modem status
	DWORD dwModemStat = 0;
	if (!::GetCommModemStatus(m_handle,&dwModemStat))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetDSR - Unable to obtain the modem status\n");
		return false;
	}

	// Determine if DSR is on
	return (dwModemStat & MS_DSR_ON) != 0;
}

bool CSerial::GetRing (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Obtain the modem status
	DWORD dwModemStat = 0;
	if (!::GetCommModemStatus(m_handle,&dwModemStat))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetRing - Unable to obtain the modem status");
		return false;
	}

	// Determine if Ring is on
	return (dwModemStat & MS_RING_ON) != 0;
}

bool CSerial::GetRLSD (void)
{
	// Reset error state
	m_lLastError = ERROR_SUCCESS;

	// Obtain the modem status
	DWORD dwModemStat = 0;
	if (!::GetCommModemStatus(m_handle,&dwModemStat))
	{
		// Obtain the error code
		m_lLastError = ::GetLastError();

		// Display a warning
		_RPTF0(_CRT_WARN,"CSerial::GetRLSD - Unable to obtain the modem status");
		return false;
	}

	// Determine if RLSD is on
	return (dwModemStat & MS_RLSD_ON) != 0;
}
