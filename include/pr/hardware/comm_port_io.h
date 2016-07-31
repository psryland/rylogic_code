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
		CommPortSettings m_settings;      // Port settings
		HANDLE           m_handle;        // File handle
		HANDLE           m_evt_read;      // Manual reset event for overlapped read operations
		HANDLE           m_evt_write;     // Manual reset event for overlapped write operations
		DWORD            m_read_interval; // The maximum time between data characters arriving
		mutable DWORD    m_last_error;    // The last error returned from a call to ::GetLastError()
		bool             m_overlapped;    // True if overlapped IO is being used

	public:

		// Construct
		CommPortIO()
			:m_settings()
			,m_handle(INVALID_HANDLE_VALUE)
			,m_evt_read(INVALID_HANDLE_VALUE)
			,m_evt_write(INVALID_HANDLE_VALUE)
			,m_read_interval(1)
			,m_last_error(0)
			,m_overlapped(false)
		{}
		explicit CommPortIO(CommPortSettings const& settings)
			:CommPortIO()
		{
			Config(settings);
		}
		CommPortIO(int baud, int data_bits, int parity, int stop_bits)
			:CommPortIO()
		{
			Config(baud, data_bits, parity, stop_bits);
		}
		~CommPortIO()
		{
			Close();
		}

		// Moveable, but not copyable
		CommPortIO(CommPortIO&& rhs)
			:m_settings(rhs.m_settings)
			,m_handle(rhs.m_handle)
			,m_evt_read(rhs.m_evt_read)
			,m_evt_write(rhs.m_evt_write)
			,m_read_interval(rhs.m_read_interval)
			,m_last_error(rhs.m_last_error)
			,m_overlapped(rhs.m_overlapped)
		{
			rhs.m_handle = INVALID_HANDLE_VALUE;
			rhs.m_evt_read = INVALID_HANDLE_VALUE;
			rhs.m_evt_write = INVALID_HANDLE_VALUE;
			rhs.m_read_interval = 1;
			rhs.m_last_error = 0;
			rhs.m_overlapped = false;
		}
		CommPortIO& operator = (CommPortIO&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_settings, rhs.m_settings);
				std::swap(m_handle, rhs.m_handle);
				std::swap(m_evt_read, rhs.m_evt_read);
				std::swap(m_evt_write, rhs.m_evt_write);
				std::swap(m_read_interval, rhs.m_read_interval);
				std::swap(m_last_error, rhs.m_last_error);
				std::swap(m_overlapped, rhs.m_overlapped);
			}
			return *this;
		}
		CommPortIO(CommPortIO const&) = delete;
		CommPortIO& operator = (CommPortIO const&) = delete;

		// Return the handle associated with the comm port
		HANDLE Handle() const
		{
			return m_handle;
		}

		// Return the last error received
		DWORD LastError(std::string* error_desc = nullptr) const
		{
			m_last_error = ::GetLastError();
			if (error_desc) *error_desc = pr::HrMsg(m_last_error);
			return m_last_error;
		}

		// Return true if the IO connection is currently open
		bool IsOpen() const
		{
			return m_handle != INVALID_HANDLE_VALUE;
		}

		// Configure the port
		void Config(CommPortSettings const& settings)
		{
			m_settings = settings;

			// If the port is open, update the settings
			if (IsOpen())
				ApplyConfig();
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

		// Open the serial IO connection
		// Note: overlapped IO is supported, but I've found it to be buggy when run in background threads. FILE_FLAG_OVERLAPPED
		void Open(int port_number, size_t file_flags = FILE_FLAG_NO_BUFFERING|FILE_FLAG_WRITE_THROUGH, size_t ibuf_size = 0, size_t obuf_size = 0)
		{
			if (IsOpen())
			{
				assert(!"Serial port already open");
				Close();
			}

			try
			{
				port_name name(port_number);
				m_handle = ::CreateFileW(name, GENERIC_READ|GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, DWORD(file_flags), nullptr);
				Throw(m_handle != INVALID_HANDLE_VALUE, pr::FmtS("Could not open 'COM%d'", port_number));
				m_overlapped = (file_flags & FILE_FLAG_OVERLAPPED) != 0;

				// Create manual reset events for the overlapped i/o calls
				if (m_overlapped)
				{
					m_evt_read = ::CreateEventW(0, TRUE, FALSE, 0);
					Throw(m_evt_read != nullptr, "Failed to create async read event");
					m_evt_write = ::CreateEventW(0, TRUE, FALSE, 0);
					Throw(m_evt_write != nullptr, "Failed to create async write event");
				}

				// Set up buffering
				if (ibuf_size != 0 || obuf_size != 0)
				{
					if (ibuf_size < 16) ibuf_size = 16;
					if (obuf_size < 16) obuf_size = 16;
					Throw(::SetupComm(m_handle, DWORD(ibuf_size), DWORD(obuf_size)), "Failed to set comm port i/o buffering");
				}

				// Set the data read interval to the minimum. This means calls to Read() will block for up to the 
				// given timeout period but will return as soon as the interval between data arriving exceeds 1ms.
				// Note: not using 0, because 0 causes the ReadFile() function to return immediately, regardless of
				// the timeout value used in our Read() call.
				SetReadIntervalTimeout(1);

				// Try to set up the device with default settings
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

		// Close the serial IO connection
		void Close()
		{
			if (m_evt_read != INVALID_HANDLE_VALUE)
			{
				::CloseHandle(m_evt_read);
				m_evt_read = INVALID_HANDLE_VALUE;
			}
			if (m_evt_write != INVALID_HANDLE_VALUE)
			{
				::CloseHandle(m_evt_write);
				m_evt_write = INVALID_HANDLE_VALUE;
			}
			if (m_handle != INVALID_HANDLE_VALUE)
			{
				::CloseHandle(m_handle);
				m_handle = INVALID_HANDLE_VALUE;
			}
		}

		// Send data over the i/o connection using overlapped IO.
		// Returns true if some data was sent.
		// Note: timeouts are only supported for overlapped IO
		bool Write(void const* data, size_t size, size_t& bytes_sent, DWORD timeout_ms = 0)
		{
			bytes_sent = 0;
			if (!IsOpen())
			{
				assert(!"Port not open for writing");
				return false;
			}

			DWORD sent;
			bool res;

			// Sync write
			if (!m_overlapped)
			{
				res = ::WriteFile(m_handle, data, DWORD(size), &sent, nullptr) != 0;
			}
			else
			{
				// Write the data and wait for the overlapped operation to complete
				OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_evt_write;
				if (!::WriteFile(m_handle, data, DWORD(size), nullptr, &ovrlap) && GetLastError() != ERROR_IO_PENDING)
					return false;

				// Wait for the write to complete
				auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
				switch (r)
				{
				case WAIT_OBJECT_0:
					res = ::GetOverlappedResult(m_handle, &ovrlap, &sent, FALSE) != 0;
					break;
				case WAIT_TIMEOUT:
					::CancelIo(m_handle);
					return false;
				case WAIT_ABANDONED:
					return false;
				case WAIT_FAILED:
					throw std::exception(FmtS("Serial port Write command failed with error code %X", LastError()));
				default:
					throw std::exception(FmtS("Unknown return code (%d) during Serial port Write command", r));
				}
			}

			// Condition here to allow break points when 'sent' != 0
			if (sent != 0)
				bytes_sent = sent;

			return res;
		}

		// Write all of 'size' to the i/o connection
		bool Write(void const* data, size_t size, DWORD timeout = 0)
		{
			auto inb = (BYTE const*)data;
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

		// Read data from the IO connection.
		// Returns true if data was read, false if the timeout was reached
		// Note: timeouts are only supported for overlapped IO
		bool Read(void* buffer, size_t size, size_t& bytes_read, DWORD timeout_ms)
		{
			bytes_read = 0;
			if (!IsOpen())
			{
				assert(false && "Port not open for reading");
				return false;
			}

			// Set the comms timeout
			auto cto = CommTimeouts();
			cto.ReadIntervalTimeout = timeout_ms != 0 ? m_read_interval : MAXDWORD;
			CommTimeouts(cto);

			DWORD read;
			bool res;

			if (!m_overlapped)
			{
				// Sync read
				res = ::ReadFile(m_handle, buffer, DWORD(size), &read, nullptr) != 0;
			}
			else
			{
				// Read data and wait for the overlapped operation to complete
				OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_evt_read;
				if (!::ReadFile(m_handle, buffer, DWORD(size), 0, &ovrlap) && GetLastError() != ERROR_IO_PENDING)
					return false;

				// Wait for the read to complete
				auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
				switch (r)
				{
				case WAIT_OBJECT_0:
					res = ::GetOverlappedResult(m_handle, &ovrlap, &read, FALSE) != 0;
					break;
				case WAIT_TIMEOUT:
					::CancelIo(m_handle);
					return false;
				case WAIT_ABANDONED:
					return false;
				case WAIT_FAILED:
					throw std::exception(FmtS("Serial port Read command failed with error code %X", LastError()));
				default:
					throw std::exception(FmtS("Unknown return code (%d) during Serial port Read command", r));
				}
			}

			// Condition here to allow break points when 'read' != 0
			if (read != 0)
				bytes_read = read;

			return res;
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

		// Purge the I/O buffers
		// Discards all characters from the output or input buffer of a specified communications
		// resource. It can also terminate pending read or write operations on the resource.
		// To empty the output buffer while ensuring that the contents are transmitted,
		// call the FlushFileBuffers function (a synchronous operation).
		void Purge()
		{
			if (!IsOpen()) return;

			DWORD errors; COMSTAT stat;
			Throw(::PurgeComm(m_handle, PURGE_RXABORT|PURGE_TXABORT|PURGE_RXCLEAR|PURGE_TXCLEAR), "Purge comm port failed");
			Throw(::ClearCommError(m_handle, &errors, &stat), "Failed to clear comm errors");
		}

		// Set/Clear the break state
		void BreakChar(bool set)
		{
			set ? SetCommBreak(m_handle) : ClearCommBreak(m_handle);
		}

		// Return the number of bytes available for reading by the serial port
		size_t BytesAvailable() const
		{
			if (!IsOpen()) return 0;

			DWORD dwErrorFlags;
			COMSTAT ComStat;
			::ClearCommError(m_handle, &dwErrorFlags, &ComStat);
			return ComStat.cbInQue;
		}

		// Get/Set the current values for the comm read/write timeouts
		COMMTIMEOUTS CommTimeouts() const
		{
			COMMTIMEOUTS cto = {};
			Throw(::GetCommTimeouts(m_handle, &cto), "Failed to read comm port timeouts");
			return cto;
		}
		void CommTimeouts(COMMTIMEOUTS cto)
		{
			Throw(::SetCommTimeouts(m_handle, &cto), "Failed to set comm port timeouts");
		}

		// Set the maximum time allowed to elapse before the arrival of the next byte on the communications line, in milliseconds.
		// If the interval between the arrival of any two bytes exceeds this amount, the ReadFile operation is completed and any
		// buffered data is returned.
		// 0        = return immediately with the bytes that have already been received, even if no bytes have been received.
		// INFINITE = block until the requested number of bytes have been received, or a 'total' timeout occurs.
		void SetReadIntervalTimeout(unsigned int timeout_ms)
		{
			auto cto = CommTimeouts();
			if (timeout_ms == 0)
			{
				// A value of MAXDWORD, combined with zero values for both the ReadTotalTimeoutConstant and ReadTotalTimeoutMultiplier members,
				// specifies that the read operation is to return immediately with the bytes that have already been received, even if no bytes
				// have been received.
				cto.ReadIntervalTimeout        = m_read_interval = MAXDWORD;
				cto.ReadTotalTimeoutMultiplier = 0;
				cto.ReadTotalTimeoutConstant   = 0;
			}
			else if (timeout_ms == INFINITE)
			{
				cto.ReadIntervalTimeout        = m_read_interval = 0;
				cto.ReadTotalTimeoutMultiplier = 0;
				cto.ReadTotalTimeoutConstant   = 0;
			}
			else
			{
				cto.ReadIntervalTimeout = m_read_interval = timeout_ms;
			}
			CommTimeouts(cto);
		}

		// Set the mask for comm events to watch for
		void SetCommMask(DWORD mask) // EV_TXEMPTY etc
		{
			::SetCommMask(m_handle, mask);
		}

		// Waits for a comm event and returns the mask of the comms events that have occurred
		// Note: timeouts are only supported for overlapped IO
		bool WaitCommEvent(DWORD timeout_ms, DWORD& mask)
		{
			DWORD sent;
			bool res;

			if (m_overlapped)
			{
				res = ::WaitCommEvent(m_handle, &mask, nullptr) != 0;
			}
			else
			{
				// Write the data and wait for the overlapped operation to complete
				OVERLAPPED ovrlap = {}; ovrlap.hEvent = m_evt_read; // Uses the same event as Read
				Throw(::WaitCommEvent(m_handle, &mask, &ovrlap) || GetLastError() == ERROR_IO_PENDING, "WaitCommEvent failed");

				auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
				switch (r)
				{
				case WAIT_OBJECT_0:
					res = ::GetOverlappedResult(m_handle, &ovrlap, &sent, FALSE) != 0;
					break;
				case WAIT_TIMEOUT:
					::CancelIo(m_handle);
					return false;
				case WAIT_ABANDONED:
					return false;
				case WAIT_FAILED:
					throw std::exception(FmtS("Serial port Wait command failed with error code %X", LastError()));
				default:
					throw std::exception(FmtS("Unknown return code (%d) during Serial port Wait command", r));
				}
			}
			return res;
		}

		// Returns true if serial port number 'port_number' is available for use
		static bool PortAvailable(int port_number, int access = GENERIC_READ|GENERIC_WRITE)
		{
			auto handle = ::CreateFileW(port_name(port_number), access, 0, 0, OPEN_EXISTING, 0, 0);
			auto available = handle != INVALID_HANDLE_VALUE;
			::CloseHandle(handle);
			return available;
		}

		// Enumerate the comm port names on the current machine. 'Func(char const* port_name)'
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
				~HKey()
				{
					RegCloseKey(m_key);
				}
				operator HKEY() const
				{
					return m_key;
				}
			};
			HKey hk(HKEY_LOCAL_MACHINE, "HARDWARE\\DEVICEMAP\\SERIALCOMM");

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

		// Enumerate the standard baud rates. 'Func(int baud_rate)'
		template <typename Func> static void EnumBaudRates(Func func)
		{
			for (auto br : {921600, 460800, 230400, 115200, 57600, 38400, 19200, 9600})
				func(br);
		}

	private:

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
		void Throw(BOOL res, char const* msg) const
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
	};
}
