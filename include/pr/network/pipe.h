//**********************************
// Pipe
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once

#include <vector>
#include <string>
#include <exception>
#include <cassert>
#include <thread>
#include <algorithm>
#include <type_traits>
#include <windows.h>

#include "pr/common/fmt.h"
#include "pr/common/hresult.h"

namespace pr
{
	// Named Pipe IO
	class Pipe
	{
		// Open the file
		struct handle_closer { void operator()(HANDLE h) { if (h) CloseHandle(h); } };
		using ScopedHandle = std::unique_ptr<void, handle_closer>;
		ScopedHandle SafeHandle(HANDLE h) { return ScopedHandle((h != INVALID_HANDLE_VALUE) ? h : nullptr); }

		std::wstring  m_pipe_name;  // The unique name for the pipe
		ScopedHandle  m_handle;     // The handle to the pipe for outgoing data
		ScopedHandle  m_evt_read;   // Manual reset event for overlapped read operations
		ScopedHandle  m_evt_write;  // Manual reset event for overlapped write operations
		mutable DWORD m_last_error; // The last error returned from a call to ::GetLastError()
		bool          m_overlapped; // Overlapped IO

	public:

		explicit Pipe(wchar_t const* pipe_name)
			:m_pipe_name(pipe_name)
			,m_handle()
			,m_evt_read()
			,m_evt_write()
			,m_last_error()
			,m_overlapped(false)
		{
			// Attempt to connect on construction
			Connect();
		}
		Pipe(Pipe&& rhs) noexcept
			:Pipe(L"")
		{
			std::swap(m_pipe_name , rhs.m_pipe_name );
			std::swap(m_handle    , rhs.m_handle    );
			std::swap(m_evt_read  , rhs.m_evt_read  );
			std::swap(m_evt_write , rhs.m_evt_write );
			std::swap(m_last_error, rhs.m_last_error);
			std::swap(m_overlapped, rhs.m_overlapped);
		}
		Pipe(Pipe const& rhs) = delete;
		~Pipe()
		{
			Disconnect();
		}

		// Assignment
		Pipe& operator = (Pipe const& rhs) = delete;
		Pipe& operator = (Pipe&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_pipe_name , rhs.m_pipe_name );
			std::swap(m_handle    , rhs.m_handle    );
			std::swap(m_evt_read  , rhs.m_evt_read  );
			std::swap(m_evt_write , rhs.m_evt_write );
			std::swap(m_last_error, rhs.m_last_error);
			std::swap(m_overlapped, rhs.m_overlapped);
			return *this;
		}

		// The global name of the pipe
		std::wstring GetPipeName() const
		{
			return std::wstring(L"\\\\.\\pipe\\") + m_pipe_name;
		}

		// True if the pipe is connected and someone is listening
		bool IsConnected() const
		{
			return m_handle != nullptr;
		}

		// Attempt to connect to someone listening
		bool Connect(DWORD timeout_ms = 0, DWORD buffer_size = 4096)
		{
			// Try to open the named pipe.
			m_handle = SafeHandle(CreateFileW(
				GetPipeName().c_str(),
				GENERIC_WRITE | GENERIC_READ,
				0,
				nullptr,
				OPEN_EXISTING,
				FILE_ATTRIBUTE_NORMAL,
				nullptr));

			// The pipe will exist if someone is listening
			if (m_handle != nullptr)
				return true;

			// Create the named pipe
			m_handle = SafeHandle(CreateNamedPipeW(
				GetPipeName().c_str(),                     // pipe name
				PIPE_ACCESS_DUPLEX,                        // read/write access
				PIPE_TYPE_MESSAGE |                        // message type pipe
				PIPE_READMODE_MESSAGE |                    // message-read mode
				PIPE_NOWAIT |                              // non-blocking mode
				(m_overlapped ? FILE_FLAG_OVERLAPPED : 0), // overlapped io
				PIPE_UNLIMITED_INSTANCES,                  // max. instances
				DWORD(buffer_size),                        // output buffer size
				DWORD(buffer_size),                        // input buffer size
				NMPWAIT_USE_DEFAULT_WAIT,                  // client time-out
				nullptr));

			// The pipe couldn't be opened
			if (m_handle == nullptr)
				return false;

			bool res;

			// Wait for a connection to the pipe
			auto ovrlap = OVERLAPPED{};
			ovrlap.hEvent = m_evt_read.get();
			if (!ConnectNamedPipe(m_handle.get(), &ovrlap) && (m_last_error = GetLastError()) != ERROR_PIPE_CONNECTED)
				return false;

			// Wait for the write to complete
			auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
			switch (r)
			{
			case WAIT_OBJECT_0:
				res = ::GetOverlappedResult(m_handle.get(), &ovrlap, nullptr, FALSE) != 0;
				break;
			case WAIT_TIMEOUT:
				::CancelIo(m_handle.get());
				return false;
			case WAIT_ABANDONED:
				return false;
			case WAIT_FAILED:
				pr::Throw(HRESULT_FROM_WIN32(m_last_error), "Named pipe connect failed");
			default:
				throw std::exception(FmtS("Unknown return code (%d) during Named pipe connect", r));
			}

			return res;
		}

		// Close the connection
		void Disconnect()
		{
			m_handle = nullptr;
		}

		// Send data on the pipe
		// Returns true if some data was sent.
		// Note: timeouts are only supported for overlapped IO
		bool Write(void const* data, size_t size, size_t& bytes_sent, DWORD timeout_ms = 0)
		{
			bytes_sent = 0;
			if (!IsConnected())
			{
				assert(!"Named pipe not connected");
				return false;
			}

			// Sanity check
			if (size > DWORD(~0))
				throw std::exception("Too much data to send on named pipe");

			DWORD sent;
			bool res;

			// Write the data to the pipe
			if (!m_overlapped)
			{
				res = ::WriteFile(m_handle.get(), data, DWORD(size), &sent, nullptr) != 0;
			}
			else
			{
				// Write the data and wait for the overlapped operation to complete
				auto ovrlap = OVERLAPPED{};
				ovrlap.hEvent = m_evt_write.get();
				if (!::WriteFile(m_handle.get(), data, DWORD(size), nullptr, &ovrlap) && (m_last_error = ::GetLastError()) != ERROR_IO_PENDING)
					return false;

				// Wait for the write to complete
				auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
				switch (r)
				{
				case WAIT_OBJECT_0:
					res = ::GetOverlappedResult(m_handle.get(), &ovrlap, &sent, FALSE) != 0;
					break;
				case WAIT_TIMEOUT:
					::CancelIo(m_handle.get());
					return false;
				case WAIT_ABANDONED:
					return false;
				case WAIT_FAILED:
					pr::Throw(HRESULT_FROM_WIN32(m_last_error), "Named pipe Write command failed");
				default:
					throw std::exception(FmtS("Unknown return code (%d) during Named pipe Write", r));
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
		template <typename Type, typename = std::enable_if_t<std::is_trivially_copyable_v<Type>>>
		bool Write(Type const& type, DWORD timeout)
		{
			return Write(&type, sizeof(type), timeout);
		}

		// Read data from the IO connection.
		// Returns true if data was read, false if the timeout was reached
		// Note: timeouts are only supported for overlapped IO
		bool Read(void* buffer, size_t size, size_t& bytes_read, DWORD timeout_ms)
		{
			bytes_read = 0;
			if (!IsConnected())
			{
				assert(!"Pipe not connected");
				return false;
			}

			// Sanity check
			if (size > DWORD(~0))
				throw std::exception("Too much data to send on named pipe");

			DWORD read;
			bool res;

			if (!m_overlapped)
			{
				// Sync read
				res = ::ReadFile(m_handle.get(), buffer, DWORD(size), &read, nullptr) != 0;
			}
			else
			{
				// Read data and wait for the overlapped operation to complete
				auto ovrlap = OVERLAPPED{};
				ovrlap.hEvent = m_evt_read.get();
				if (!::ReadFile(m_handle.get(), buffer, DWORD(size), 0, &ovrlap) && (m_last_error = GetLastError()) != ERROR_IO_PENDING)
					return false;

				// Wait for the read to complete
				auto r = ::WaitForSingleObject(ovrlap.hEvent, timeout_ms);
				switch (r)
				{
				case WAIT_OBJECT_0:
					res = ::GetOverlappedResult(m_handle.get(), &ovrlap, &read, FALSE) != 0;
					break;
				case WAIT_TIMEOUT:
					::CancelIo(m_handle.get());
					return false;
				case WAIT_ABANDONED:
					return false;
				case WAIT_FAILED:
					pr::Throw(HRESULT_FROM_WIN32(LastError()), "Named pipe Read failed");
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
		bool Read(void* buffer, size_t size, DWORD timeout)
		{
			auto inb = (BYTE*)buffer;
			for (size_t read, bytes_read = 0; bytes_read != size; bytes_read += read)
			{
				if (!Read(inb + bytes_read, size - bytes_read, read, timeout))
					return false;
			}
			return true;
		}

		// Read a POD object from the i/o connection. (Careful with padding!)
		template <typename Type, typename = std::enable_if_t<std::is_trivially_copyable_v<Type>>>
		bool Read(Type& type, DWORD timeout)
		{
			return Read(&type, sizeof(type), timeout);
		}

		// Flush any buffered data
		void Flush()
		{
			auto res = ::FlushFileBuffers(m_handle.get());
			if (res == 0)
			{
				m_last_error = GetLastError();
				if (m_last_error == ERROR_NOT_SUPPORTED) return;
				if (m_last_error == ERROR_INVALID_FUNCTION) return;
				Throw(res, "Failed to flush write buffer");
			}
		}

		// Look at, but don't remove, data from the pipe
		bool Peek(void* data, size_t size, size_t& bytes_read, size_t& available, size_t& message_bytes_left)
		{
			bytes_read = 0;
			available = 0;
			message_bytes_left = 0;
			if (!IsConnected())
			{
				assert(!"Pipe not connected");
				return false;
			}

			// Sanity check
			if (size > DWORD(~0))
				throw std::exception("Too much data to send on named pipe");

			DWORD read, avail, msg_left;
			if (!::PeekNamedPipe(m_handle.get(), data, DWORD(size), &read, &avail, &msg_left))
			{
				m_last_error = GetLastError();
				return false;
			}

			// Condition here to allow break points when 'read' != 0
			if (read != 0)
			{
				bytes_read = read;
				available = avail;
				message_bytes_left = msg_left;
			}
			return true;
		}

		// GetLastError
		DWORD LastError() const
		{
			return m_last_error;
		}
	};
}
