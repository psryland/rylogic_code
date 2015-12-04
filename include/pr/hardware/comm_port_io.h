//*********************************************************************
// Serial IO port comms
//*********************************************************************
#pragma once

#include <cassert>
#include <exception>
#include <windows.h>
#include "pr/common/fmt.h"
#include "pr/common/hresult.h"

namespace pr
{
	// Settings for configuring a comm port
	struct CommPortSettings
	{
		DWORD m_baud;       // Baud rate
		BYTE  m_data_bits;  // Valid values are 5,6,7,8 (8 is default)
		BYTE  m_parity;     // Parity
		BYTE  m_stop_bits;  // Stop bits

		CommPortSettings(DWORD baud = CBR_9600, BYTE data_bits = 8, BYTE parity = NOPARITY, BYTE stop_bits = ONESTOPBIT)
			:m_baud     (baud)
			,m_data_bits(data_bits)
			,m_parity   (parity)
			,m_stop_bits(stop_bits)
		{}
	};

	// RS232 communication interface
	class CommPortIO
	{
		CommPortSettings m_settings;    // Port settings
		HANDLE           m_handle;      // File handle
		HANDLE           m_io_complete; // Manual reset event for overlapped i/o operations
		mutable DWORD    m_last_error;  // The last error returned from a call to ::GetLastError()

		void ApplyConfig()
		{
			// Read the comm state, update the data, then set it again
			DCB comm_state = {sizeof(DCB)};
			Throw(::GetCommState(m_handle, &comm_state), "Failed to read comm state");
			comm_state.BaudRate = m_settings.m_baud;
			comm_state.ByteSize = m_settings.m_data_bits;
			comm_state.Parity   = m_settings.m_parity;
			comm_state.StopBits = m_settings.m_stop_bits;
			comm_state.fParity  = m_settings.m_parity != NOPARITY;
			Throw(::SetCommState(m_handle, &comm_state), "Failed to set comm state");
		}

		// Throws if 'res' is not TRUE
		void Throw(BOOL res, char const* msg)
		{
			if (res) return;
			std::string error_desc;
			auto last_error = LastError(&error_desc);
			throw std::exception(pr::FmtS("%s. 0x%08X - %s", msg, last_error, error_desc.c_str()));
		}

		// Helper for creating a port name from a port number
		struct port_name
		{
			wchar_t m_name[32];
			port_name(int port_number)      { _snwprintf_s(m_name,_countof(m_name), L"\\\\.\\COM%d", port_number); }
			operator wchar_t const*() const { return m_name; }
			wchar_t const* com() const      { return &m_name[4]; }
		};

	public:

		CommPortIO()
			:m_settings()
			,m_handle(INVALID_HANDLE_VALUE)
			,m_io_complete(0)
			,m_last_error(0)
		{}
		~CommPortIO()
		{
			Close();
		}

		// Return the last error received
		DWORD LastError(std::string* error_desc = nullptr) const
		{
			m_last_error = ::GetLastError();
			if (error_desc) *error_desc = pr::HrMsg(m_last_error);
			return m_last_error;
		}

		// Configure the port
		void Config(CommPortSettings const& settings)
		{
			m_settings = settings;
			if (IsOpen()) ApplyConfig(); // If the port is open, update the settings
		}
		void Config(int baud, int data_bits, int parity, int stop_bits)
		{
			CommPortSettings s;
			s.m_baud      = DWORD(baud);
			s.m_data_bits = BYTE(data_bits);
			s.m_parity    = BYTE(parity);
			s.m_stop_bits = BYTE(stop_bits);
			Config(s);
		}

		// Return true if the io connection is currently open
		bool IsOpen() const { return m_handle != INVALID_HANDLE_VALUE; }

		// Returns true if serial port number 'port_number' is available for use
		static bool PortAvailable(int port_number, int access = GENERIC_READ|GENERIC_WRITE)
		{
			auto handle = ::CreateFileW(port_name(port_number), access, 0, 0, OPEN_EXISTING, 0, 0);
			auto available = handle != INVALID_HANDLE_VALUE;
			::CloseHandle(handle);
			return available;
		}

		// Open the serial io connection
		void Open(int port_number) { Open(port_number, 0, 0); }
		void Open(int port_number, size_t ibuf_size, size_t obuf_size)
		{
			if (IsOpen())
			{
				assert(false && "Serial port already open");
				Close();
			}

			try
			{
				// Open for overlapped I/O
				port_name name(port_number);
				m_handle = ::CreateFileW(name, GENERIC_READ|GENERIC_WRITE, 0, 0, OPEN_EXISTING, FILE_FLAG_OVERLAPPED|FILE_FLAG_NO_BUFFERING|FILE_FLAG_WRITE_THROUGH, 0);
				Throw(m_handle != INVALID_HANDLE_VALUE, pr::FmtS("Could not open 'COM%d'", port_number));

				// Create a manual reset event for the overlapped i/o calls
				m_io_complete = ::CreateEventW(0, TRUE, FALSE, 0);
				Throw(m_io_complete != 0, "Failed to create async i/o event");

				// Setup buffering
				if (ibuf_size != 0 || obuf_size != 0)
				{
					if (ibuf_size < 16) ibuf_size = 16;
					if (obuf_size < 16) obuf_size = 16;
					Throw(::SetupComm(m_handle, DWORD(ibuf_size), DWORD(obuf_size)), "Failed to set comm port i/o buffering");
				}

				// Set non-blocking reads/writes as default
				SetBlockingReads(false);

				// Try to setup the device with default settings
				COMMCONFIG config = {sizeof(COMMCONFIG)};
				if (::GetDefaultCommConfigW(name.com(), &config, &config.dwSize) && config.dwSize == sizeof(COMMCONFIG))
				{
					// 'GetDefaultCommConfigW' can fail for bluetooth ports and some virtual ports
					// ('http://stackoverflow.com/questions/6850965/how-come-getdefaultcommconfig-doesnt-work-with-bluetooth-spp-devices')
					// Try to set defaults but if we can't, just hope for the best.
					Throw(::SetCommConfig(m_handle, &config, config.dwSize), "Failed to set default comm port configuration");
				}

				// Apply the port settings
				ApplyConfig();

				// Check the properties of the comm port and warn if we're trying to exceed them
				COMMPROP prop;
				Throw(::GetCommProperties(m_handle, &prop), "GetCommProperties failed");
				if (m_settings.m_baud > prop.dwMaxBaud)
					OutputDebugStringA(FmtS("Requested baud rate %d exceeds the maximum of %d for this comm port", m_settings.m_baud, prop.dwMaxBaud));

				// Check that the configured state of the port matches what we asked for
				DCB dcb;
				Throw(::GetCommState(m_handle, &dcb), "GetCommState failed");
				if (dcb.BaudRate != m_settings.m_baud)
					OutputDebugStringA(FmtS("Baud rate of %d being used instead of the requested baud rate: %d", dcb.BaudRate, m_settings.m_baud));
				if (dcb.ByteSize != m_settings.m_data_bits)
					OutputDebugStringA(FmtS("Byte size of %d being used instead of the requested byte size: %d", dcb.ByteSize, m_settings.m_data_bits));
				if (dcb.Parity != m_settings.m_parity)
					OutputDebugStringA(FmtS("Parity of %d being used instead of the requested parity: %d", dcb.Parity, m_settings.m_parity));
				if (dcb.StopBits != m_settings.m_stop_bits)
					OutputDebugStringA(FmtS("Stop bits of %d being used instead of the requested stop bits: %d", dcb.StopBits, m_settings.m_stop_bits));
			}
			catch (...)
			{
				Close();
				throw;
			}
		}

		// Close the serial io connection
		void Close()
		{
			if (m_io_complete != 0)
			{
				::CloseHandle(m_io_complete);
				m_io_complete = 0;
			}
			if (m_handle != INVALID_HANDLE_VALUE)
			{
				::CloseHandle(m_handle);
				m_handle = INVALID_HANDLE_VALUE;
			}
		}

		// Set the mask for comm events to watch for
		void SetCommMask(DWORD mask) // EV_TXEMPTY etc
		{
			::SetCommMask(m_handle, mask);
		}

		// Waits for a comm event and returns the mask of the comms events that have occurred
		bool WaitCommEvent(DWORD timeout, DWORD& mask)
		{
			// Write the data and wait for the overlapped operation to complete
			OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_io_complete;
			Throw(::WaitCommEvent(m_handle, &mask, &ovrlap) || GetLastError() == ERROR_IO_PENDING, "");

			DWORD bytes_sent;
			switch (::WaitForSingleObject(ovrlap.hEvent, timeout))
			{
			default:
			case WAIT_TIMEOUT:
				return false;
			case WAIT_OBJECT_0:
				return ::GetOverlappedResult(m_handle, &ovrlap, &bytes_sent, FALSE) != 0;
			}
		}

		// Send data over the i/o connection using overlapped io
		bool Write(void const* data, size_t size, size_t& bytes_sent, DWORD timeout)
		{
			bytes_sent = 0;
			if (!IsOpen())
			{
				assert(false && "Port not open for writing");
				return false;
			}

			// Write the data and wait for the overlapped operation to complete
			OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_io_complete;
			if (!::WriteFile(m_handle, data, DWORD(size), 0, &ovrlap) && GetLastError() != ERROR_IO_PENDING)
				return false;

			Flush();

			// Wait for the write to complete
			auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout);
			switch (r)
			{
			default:
				throw std::exception(FmtS("Unknown return code (%d) during Serial port Write command", r));
			case WAIT_OBJECT_0:
				return ::GetOverlappedResult(m_handle, &ovrlap, (DWORD*)&bytes_sent, FALSE) != 0;
			case WAIT_TIMEOUT:
				::CancelIo(m_handle); // Cancel the I/O operation
				return false;
			case WAIT_ABANDONED:
				return false; // Connection dropped
			case WAIT_FAILED:
				auto err = ::GetLastError();
				throw std::exception(FmtS("Serial port Write command failed with error code %X", err));
			}
		}

		// Write all of 'size' to the i/o connection
		bool Write(void const* data, size_t size, DWORD timeout)
		{
			BYTE const* inb = (BYTE const*)data;
			for (size_t writ, bytes_writ = 0; bytes_writ != size; bytes_writ += writ)
			{
				if (!Write(inb + bytes_writ, size - bytes_writ, writ, timeout))
					return false;
			}
			return true;
		}

		// Write an object from the i/o connection
		template <typename Type> bool Write(Type const& type, DWORD timeout)
		{
			return Write(&type, sizeof(type), timeout);
		}

		// Flush any buffered data
		void Flush()
		{
			auto res = ::FlushFileBuffers(m_handle);
			if (res == 0)
			{
				auto last_error = GetLastError();
				if (last_error == ERROR_NOT_SUPPORTED) return;
				if (last_error == ERROR_INVALID_FUNCTION) return;
				Throw(res, "Failed to flush write buffer");
			}
		}

		// Set/Clear the break state
		void BreakChar(bool set)
		{
			set ? SetCommBreak(m_handle) : ClearCommBreak(m_handle);
		}

		// Set the read timeout behaviour
		// If 'blocking' is true, the read functions will block until the requested data is available or a timeout occurs
		void SetBlockingReads(bool blocking)
		{
			COMMTIMEOUTS cto;
			Throw(::GetCommTimeouts(m_handle, &cto), "Failed to read comm port timeouts");
			cto.ReadIntervalTimeout = blocking ? 0 : MAXDWORD;
			cto.ReadTotalTimeoutConstant = 0;
			cto.ReadTotalTimeoutMultiplier = 0;
			Throw(::SetCommTimeouts(m_handle, &cto), "Failed to set comm port timeouts");
		}

		// Set the read/write timeout values
		void SetCommTimeouts(DWORD read_timeout, DWORD read_multiplier, DWORD read_constant, DWORD write_multiplier, DWORD write_constant)
		{
			COMMTIMEOUTS cto = {};
			cto.ReadIntervalTimeout         = read_timeout;     // Maximum time between read chars.
			cto.ReadTotalTimeoutMultiplier  = read_multiplier;  // Multiplier of characters.
			cto.ReadTotalTimeoutConstant    = read_constant;    // Constant in milliseconds.
			cto.WriteTotalTimeoutMultiplier = write_multiplier; // Multiplier of characters.
			cto.WriteTotalTimeoutConstant   = write_constant;   // Constant in milliseconds.
			Throw(::SetCommTimeouts(m_handle, &cto), "Failed to set comm port timeouts");
		}

		// Read buffered data from the io connection.
		// 'buffer' is the buffer to copy data into
		// 'size' is the length of 'buffer'
		// 'bytes_read' is the number of bytes read
		// 'timeout' is the maximum time to block waiting for input
		// Returns true if data was read, false if the timeout was reached
		bool Read(void* buffer, size_t size, size_t& bytes_read, DWORD timeout)
		{
			bytes_read = 0;
			if (!IsOpen())
			{
				assert(false && "Port not open for reading");
				return false;
			}

			// Block for up to 'timeout' while reading data
			OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_io_complete;
			if (!::ReadFile(m_handle, buffer, DWORD(size), 0, &ovrlap) && GetLastError() != ERROR_IO_PENDING)
				return false;

			switch (::WaitForSingleObject(ovrlap.hEvent, timeout))
			{
			default:
			case WAIT_TIMEOUT:
				::CancelIo(m_handle); // Cancel the I/O operation
				return false;
			case WAIT_OBJECT_0:
				return ::GetOverlappedResult(m_handle, &ovrlap, (DWORD*)&bytes_read, FALSE) != 0;
			}
		}

		// Read all of 'size' into buffer or timeout
		// Remember to 'SetBlockingReads' to blocking
		bool Read(void* buffer, size_t size, DWORD timeout)
		{
			BYTE* inb = (BYTE*)buffer;
			for (size_t read, bytes_read = 0; bytes_read != size; bytes_read += read)
			{
				if (!Read(inb + bytes_read, size - bytes_read, read, timeout))
					return false;
			}
			return true;
		}

		// Read an object from the i/o connection
		// Remember to 'SetBlockingReads' to blocking
		template <typename Type> bool Read(Type& type, DWORD timeout)
		{
			return Read(&type, sizeof(type), timeout);
		}

		// Purge the I/O buffers
		void Purge()
		{
			if (!IsOpen()) return;

			DWORD errors; COMSTAT stat;
			Throw(::PurgeComm(m_handle, PURGE_RXABORT|PURGE_TXABORT|PURGE_RXCLEAR|PURGE_TXCLEAR), "Purge comm port failed");
			Throw(::ClearCommError(m_handle, &errors, &stat), "Failed to clear comm errors");
		}

		//// Set the timeouts
		//bool SetReadTimeout(DWORD timeout)
		//{
		//	if (!IsOpen()) return false;
		//
		//	COMMTIMEOUTS cto;
		//	if (!::GetCommTimeouts(m_handle, &cto)) return false;
		//	cto.ReadTotalTimeoutConstant = timeout;
		//	return ::SetCommTimeouts(m_handle, &cto) != 0;
		//}

		// Return the number of bytes available for reading by the serial port
		size_t BytesAvailable() const
		{
			if (!IsOpen()) return 0;

			DWORD dwErrorFlags;
			COMSTAT ComStat;
			::ClearCommError(m_handle, &dwErrorFlags, &ComStat);
			return ComStat.cbInQue;
		}

		// Enumerate the comm port names on the current machine
		template <typename Func> static void EnumPortNames(Func func)
		{
			struct HKey
			{
				HKEY m_key;
				HKey(HKEY key, char const* subkey)
				{
					auto r = RegOpenKeyExA(key, subkey, 0, KEY_READ, &m_key);
					if (r != ERROR_SUCCESS)
						throw std::exception(std::string("Failed to open registry key ").append(subkey).append(" to enumerate comm ports. RegOpenKeyA returned ").append(std::to_string(r)).c_str());
				}
				~HKey() { RegCloseKey(m_key); }
				operator HKEY() const { return m_key; }
			} hk(HKEY_LOCAL_MACHINE, "HARDWARE\\DEVICEMAP\\SERIALCOMM");

			LSTATUS r;
			enum { MAX_VALUE_NAME_SIZE = 256, MAX_DATA_LENGTH = 16384 };
			char data[MAX_DATA_LENGTH]; DWORD datalen = _countof(data);
			char value[MAX_VALUE_NAME_SIZE]; DWORD vallen = _countof(value); DWORD type;
			for (DWORD i = 0; (r = RegEnumValueA(hk, i, &value[0], &vallen, NULL, &type, (BYTE*)&data[0], &datalen)) != ERROR_NO_MORE_ITEMS; ++i, vallen = _countof(value), datalen = _countof(data))
			{
				if (r != ERROR_SUCCESS)
					throw std::exception(std::string("Enumerating comm ports failed. RegEnumKey returned ").append(std::to_string(r)).c_str());
				if (type != REG_SZ)
					continue;

				data[datalen] = 0;
				func(data);
			}
		}
	};
}
