//******************************************************************
// Inter-Process Communication
// (c) copyright Rylogic Limited 2006
//******************************************************************
//
//	Usage:
//		pr::IPC ipc;
//		ipc.Initialise("test_ipc", 100);
//		ipc.Connect();
//		ipc.Send("data", sizeof("data"));
//
// In MFC, to convert the event into a message use:
//	RegisterWaitForSingleObject(&m_wait_handle, m_ipc.GetClientEventHandle(), WaitOrTimerCallback, 0, INFINITE, WT_EXECUTEDEFAULT);
//	void CALLBACK WaitOrTimerCallback(PVOID parameter, BOOLEAN timed_out)
//	{
//		if( timed_out == FALSE )
//			CMyApp::Get().m_pMainWnd->PostMessage(WM_COMMAND, ID_IPC_DATA_READY);
//	}
//
// To maintain a connect across server or client restarts use this approach:
//	if( ipc.IsConnected() )
//	{
//		// Check for Data ready
//		// Send data...
//	}
//	else
//	{
//		ipc.Connect(100);
//	}
//
// Implementation details:
//	The shared memory contains a header object followed by a buffer.
//	The buffer is natively half duplex, i.e. either process can write
//	into the buffer, the header shows who wrote last, flags control
//	whether data is overwritten or the send call is rejected.
//	Successive send calls append or overwrite data in the buffer (depending on flags)
//	Sends are atomic, i.e. only sends if all data can be sent
//	Reads copy from the start of the buffer up to some number of bytes
//	if not all data is read, the remainder is moved to the start of the buffer
//	Reads are atomic, i.e. only receive data if the amount of data requested is available
#ifndef PR_THREADS_IPC_H
#define PR_THREADS_IPC_H

#include <new>
#include <windows.h>
#include "pr/common/assert.h"
#include "pr/common/prtypes.h"
#include "pr/common/stdstring.h"
#include "pr/threads/mutex.h"

namespace pr
{
	namespace ipc
	{
		enum EAccess { ReadOnly, WriteOnly, ReadWrite, WriteCopy };
		enum EFlags 
		{
			DontSignal				= 0,
			Signal					= 1,
			
			// Send flags
			NoOverwriteMyData		= 0x01,	// If there's some of my data in there then add to the end of it
			OverwriteMyData			= 0x02,	// If there's some of my data in there then overwrite it
			NoOverwriteTheirData	= 0x04,	// If there's someone elses data in there then don't write anything
			OverwriteTheirData		= 0x08,	// If theer's someone elses data in there overwrite it anyway
			
			// Receive flags
			Peek					= 0x01,	// Read data but don't remove it from the buffer
			LeaveUnreadData			= 0x02,	// If the receive call does not read all of the data, don't change m_bytes_available
			DumpUnreadData			= 0x04,	// If the receive call does not read all of the data, dump the excess

			// Role
			Server					= 0,
			Client					= 1,
			Unknown					= 2,
		};
	}//namespace ipc

	namespace impl
	{
		template <typename T>
		class IPC
		{
			struct IPCHeader
			{
				uint m_server_pid;		
				uint m_client_pid;
				uint m_bytes_available;	// The size of the data to read from shared_memory
				uint m_writers_pid;		// The PID of the process that last wrote
			};

			// Initialisation data
			std::string		m_channel;			// A unique name for the connection
			uint			m_size_in_bytes;	// The available space in bytes
			ipc::EAccess	m_access;

			Mutex			m_mutex;			// Used to synchronise access to the shared memory
			bool			m_locked;			// True when the mutex has been acquired. Used for debugging
			IPCHeader*		m_header;			// Some info used to communicate info between two of these objects
			unsigned char*	m_shared_memory;	// The memory that is shared between processes
			bool			m_server;			// True if this is the server
			uint			m_my_pid;			// Our pid
			HANDLE			m_mapped_file;		// The file handle for the mapped file
			HANDLE			m_server_event;		// Event for server behaviour
			HANDLE			m_client_event;		// Event for client behaviour

			bool Initialise(uint block_time_ms)
			{
				// Synchronise the setup process
				Mutex setup_mutex(FALSE, (m_channel + "_SETUP_MUTEX").c_str());
				if( !setup_mutex.Acquire(block_time_ms) ) return false;
				
				uint create_access;
				uint open_access;
				switch( m_access )
				{
				default:				create_access = PAGE_READONLY;  open_access = FILE_MAP_READ; PR_ASSERT(PR_DBG, false); break;
				case ipc::ReadOnly:		create_access = PAGE_READONLY;  open_access = FILE_MAP_READ;       break;
				case ipc::WriteOnly:	create_access = PAGE_READWRITE; open_access = FILE_MAP_WRITE;      break;
				case ipc::ReadWrite:	create_access = PAGE_READWRITE; open_access = FILE_MAP_ALL_ACCESS; break;
				case ipc::WriteCopy:	create_access = PAGE_WRITECOPY; open_access = FILE_MAP_COPY;       break;
				};

				// Create or open a mapped file
				m_mapped_file = CreateFileMapping(INVALID_HANDLE_VALUE, 0, create_access, 0, m_size_in_bytes + sizeof(IPCHeader), (m_channel + "_FILE_MAPPING").c_str());
				if( m_mapped_file == 0 || m_mapped_file == INVALID_HANDLE_VALUE )
				{	m_mapped_file = INVALID_HANDLE_VALUE; return false; }
				bool already_exists = ::GetLastError() == ERROR_ALREADY_EXISTS;
				
				// Now map the file into this process' address space
				void* memory = MapViewOfFile(m_mapped_file, open_access, 0, 0, m_size_in_bytes + sizeof(IPCHeader));
				if( !memory ) { Release(); return false; }

				m_header		= static_cast<IPCHeader*>(memory);
				m_shared_memory = reinterpret_cast<unsigned char*>(m_header + 1);
				m_my_pid		= GetCurrentProcessId();
				m_server		= !already_exists; // We are the server if we are the first to create the mapped file
				if( m_server )
				{
					m_header->m_server_pid = m_my_pid;
				}
				else if( m_header->m_server_pid == 0 )
				{
					m_server = true;
					m_header->m_server_pid = m_my_pid;
				}
				else
				{
					m_header->m_client_pid = m_my_pid;
					m_header->m_server_pid = 0;	// Force the server to re-connect
				}
				m_header->m_writers_pid = 0;

				return m_mutex.Initialise(FALSE, (m_channel + "_DATA_MUTEX").c_str());
			}

		public:
			IPC()
			:m_channel()
			,m_size_in_bytes(0)
			,m_access(ipc::ReadWrite)
			,m_mutex()
			,m_locked(false)
			,m_header(0)
			,m_shared_memory(0)
			,m_server(false)
			,m_my_pid(0)
			,m_mapped_file(INVALID_HANDLE_VALUE)
			,m_server_event(0)
			,m_client_event(0)
			{}
			IPC(char const* channel, uint shared_memory_size_in_bytes, uint block_time_ms = INFINITE, ipc::EAccess access = ipc::ReadWrite)
			{
				new (this) IPC();
				Initialise(channel, shared_memory_size_in_bytes, block_time_ms, access);
			}
			~IPC()
			{
				Release();
			}

			// Initialise the IPC
			// 'channel' - is a unique system-wide string that identifies this shared object
			// 'shared_memory_size_in_bytes' - size in bytes of shared memory size
			// 'block_time_ms' - The maximum time to block when trying to initialise the IPC
			// 'access' - Read, Write or both
			bool Initialise(char const* channel, uint shared_memory_size_in_bytes, uint block_time_ms = INFINITE, ipc::EAccess access = ipc::ReadWrite)
			{
				m_channel		= channel; PR_ASSERT(PR_DBG, !m_channel.empty());
				m_size_in_bytes	= shared_memory_size_in_bytes;
				m_access		= access;
				return Initialise(block_time_ms);
			}

			// Release the ipc and all of its resources
			void Release()
			{
				PR_ASSERT(PR_DBG, !m_locked);
				if( m_locked ) Unlock();

				// Notify the other process
				SignalDataReady();

				if( m_server_event )	{ CloseHandle(m_server_event); m_server_event = 0; }
				if( m_client_event )	{ CloseHandle(m_client_event); m_client_event = 0; }
				if( m_header )
				{
					if( m_header->m_server_pid == m_my_pid ) m_header->m_server_pid = 0;
					if( m_header->m_client_pid == m_my_pid ) m_header->m_client_pid = 0;
					m_header->m_writers_pid = 0;

					UnmapViewOfFile(m_header);
					m_header = 0;
					m_shared_memory = 0;
				}
				if( m_mapped_file != INVALID_HANDLE_VALUE )
				{
					CloseHandle(m_mapped_file);
					m_mapped_file = INVALID_HANDLE_VALUE;
				}
			}
			
			// Return true if both processes have the mapped file open
			bool IsConnected() const
			{
				if( !m_header ) return false;

				// A process is missing
				if( m_header->m_server_pid == 0 || m_header->m_client_pid == 0 ) return false;

				// If our pid doesn't match who we think we are then we're not connected
				if( ( m_server && m_header->m_server_pid != m_my_pid) ||
					(!m_server && m_header->m_client_pid != m_my_pid) ) return false;

				return m_server_event && m_client_event;
			}
			
			// Connect to another process using this channel
			bool Connect(uint block_time_ms = INFINITE)
			{
				PR_ASSERT(PR_DBG, m_header, "Must call Initialise first");
				if( !m_header ) return false;

				// Synchronise connection
				Mutex connect_mutex(FALSE, (m_channel + "_CONNECT_MUTEX").c_str());
				if( !connect_mutex.Acquire(block_time_ms) ) return false;

				// If our pid doesn't match who we think we are then re-initialise
				if( ( m_server && m_header->m_server_pid != m_my_pid) ||
					(!m_server && m_header->m_client_pid != m_my_pid) )
				{
					Release();
					if( !Initialise(block_time_ms) ) return false;
				}

				// Check if both processes are present
				if( m_header->m_server_pid == 0 || m_header->m_client_pid == 0 ) return false;

				// Create the server event
				std::string server_event_name = m_channel + "_ServerEvent";
				m_server_event = CreateEvent(0, FALSE, FALSE, server_event_name.c_str());
				if( !m_server_event )	{ return false; }

				// Create the client event
				std::string client_event_name = m_channel + "_ClientEvent";
				m_client_event = CreateEvent(0, FALSE, FALSE, client_event_name.c_str());
				if( !m_client_event )	{ return false; }
				return true;
			}
		
			// Returns true if an event has been signed to indicate that data is ready for reading
			bool IsDataAvailable(uint block_time_ms = INFINITE) const
			{
				PR_ASSERT(PR_DBG, m_header && IsConnected());
				return WaitForSingleObject(GetClientEventHandle(), block_time_ms) == WAIT_OBJECT_0;
			}

			// Returns whether we are the server, client, or don't know
			uint GetRole() const
			{
				if( m_header )
				{
					if( m_header->m_server_pid == m_my_pid ) return ipc::Server;
					if( m_header->m_client_pid == m_my_pid ) return ipc::Client;
				}
				return ipc::Unknown;
			}

			// Return the number of bytes available. If the client code plans to read number of
			// bytes this method returns then it should lock the shared memory before calling
			// this. If it's just checking that the bytes have been read then no locking is needed
			uint GetNumBytesAvailable() const
			{
				PR_ASSERT(PR_DBG, m_header && IsConnected());
				return m_header ? m_header->m_bytes_available : 0;
			}

			// Return the event handle we use to tell if data is ready
			HANDLE GetClientEventHandle() const
			{
				if( m_server )	return m_client_event;
				else			return m_server_event;
			}

			// Lock the shared memory to allow batch reads/writes
			bool Lock(uint block_time_ms = INFINITE)
			{
				m_locked = m_mutex.Acquire(block_time_ms);
				return m_locked;
			}
			void Unlock()
			{
				m_locked = false;
				m_mutex.UnAcquire();
			}

			// Add data to the shared memory buffer.
			// This method can be used for batching or one time writes:
			//	If the shared memory has not been locked by the client code
			//		this call is assumed to be a one shot write
			//		memory will be locked (block time used), the data copied, memory unlocked, and then signaled
			//	If the shared memory has been locked by the client code
			//		this call is assumed to be a batch send
			//		data will be added, and signaling occurs based on 'signal'
			//	'data'			- a pointer to a buffer to send data from
			//	'size_in_bytes'	- the number of bytes to send. The buffer pointed to by 'data' is assumed to be at least this big
			//	'signal'		- indicates whether or not to signal the other thread to say that data is ready
			//	'block_time_ms'	- if not already locked, the maximum time to wait to obtain synchronous access to the shared memory
			//	'flags'			- what to do if there is already data in the shared memory
			//	Successive send calls append or overwrite data in the buffer (depending on flags)
			//	Sends are atomic, i.e. only sends if all data can be sent
			bool Send(const void* data, uint size_in_bytes, uint signal = ipc::Signal, uint block_time_ms = INFINITE, uint flags = ipc::NoOverwriteMyData | ipc::NoOverwriteTheirData)
			{
				PR_ASSERT(PR_DBG, m_header && IsConnected());
				if( !m_header ) { PR_INFO(PR_DBG, "IPC: Send attempted while not connected"); return false; }

				bool locked_by_me = false;
				if( !m_locked )
				{
					if( !Lock(block_time_ms) ) return false;
					locked_by_me = true;
					signal		 = true;
				}

				if( m_header->m_writers_pid != 0 )
				{
					// If we didn't write what's in there...
					if( m_header->m_writers_pid != m_my_pid )
					{
						// and we're not allowed to overwrite then don't
						if( flags & ipc::NoOverwriteTheirData )
						{
							if( locked_by_me ) Unlock();
							return false;
						}
						else
						{
							m_header->m_bytes_available = 0;
						}
					}
					// If we wrote what's in there...
					else
					{
						// and we're supposed to overwrite it then do
						if( flags & ipc::OverwriteMyData )
						{
							m_header->m_bytes_available = 0;
						}
					}
				}

				// Sends are atomic, if we can't write the entire block, then write nothing
				if( size_in_bytes > m_size_in_bytes - m_header->m_bytes_available )
				{
					if( locked_by_me ) Unlock();
					return false;
				}
				
				// Write to shared memory
				memcpy(&m_shared_memory[m_header->m_bytes_available], data, size_in_bytes);
				m_header->m_bytes_available += size_in_bytes;
				m_header->m_writers_pid = m_my_pid;

				// Signal that there is something to read
				if( signal ) SignalDataReady();

				if( locked_by_me ) Unlock();
				return true;
			}

			// Read data from the shared memory buffer.
			// This method can be used for batching or one time writes:
			//	If the shared memory has not been locked by the client code then 'block_time_ms' is used to
			//		synchronise access to the shared memory
			//  'data'			- a pointer to a buffer in which to read data into
			//	'size_in_bytes'	- the number of bytes to read. The buffer pointed to by 'data' is assumed to be at least this big
			//	'block_time_ms'	- if not already locked, the maximum time to wait to obtain synchronous access to the shared memory
			//	'byte_offset'	- offset into shared memory to begin reading from
			//	'flags'			- what to do if not all data was read
			//	Reads copy from the start of the buffer up to some number of bytes
			//	if not all data is read, the remainder is moved to the start of the buffer
			//	Reads are atomic, i.e. only receive data if the amount of data requested is available
			bool Receive(void* data, uint size_in_bytes, uint block_time_ms = INFINITE, uint flags = ipc::LeaveUnreadData)
			{
				PR_ASSERT(PR_DBG, m_header && IsConnected());
				if( !m_header ) { PR_INFO(PR_DBG, "IPC: Receive attempted while not connected"); return false; }

				// Synchronise data access
				bool locked_by_me = false;
				if( !m_locked )
				{
					if( !Lock(block_time_ms) ) return false;
					locked_by_me = true;
				}

				// Reads are atomic, only allow read if data available is >= requested.
				// Also, don't receive our own data
				if( size_in_bytes > m_header->m_bytes_available ||
					m_my_pid == m_header->m_writers_pid )
				{
					if( locked_by_me ) Unlock();
					return false;
				}

				// Read from mapped memory
				memcpy(data, m_shared_memory, size_in_bytes);
				
				// Only remove data from the buffer if we're not peeking at it
				if( !(flags & ipc::Peek) )
				{
					m_header->m_bytes_available -= size_in_bytes;
					if( flags & ipc::DumpUnreadData ) m_header->m_bytes_available = 0;

					// Move remaining data to the start of the buffer
					memmove(m_shared_memory, &m_shared_memory[size_in_bytes], m_header->m_bytes_available);

					// No data in the buffer? no writer id
					if( m_header->m_bytes_available == 0 )
						m_header->m_writers_pid = 0;
				}

				if( locked_by_me ) Unlock();
				return true;
			}

			// Tells the other process that data is waiting for it in shared memory
			void SignalDataReady()
			{
				if( m_server )	SetEvent(m_server_event);
				else			SetEvent(m_client_event);
			}
		};
	}//namespace impl

	typedef impl::IPC<void> InterProcessCommunicator;
	typedef impl::IPC<void> IPC;

	// Message passing helper object
	struct IPCMessage
	{
		IPC m_ipc;

		enum { NoMessage = 0xFFFFFFFF };
		IPCMessage() {}
		IPCMessage(char const* channel, uint max_message_size_in_bytes, uint block_time_ms = INFINITE)
		:m_ipc(channel, max_message_size_in_bytes + sizeof(uint), block_time_ms)
		{}
		
		bool Connect(uint block_time_ms = INFINITE)
		{
			if( m_ipc.IsConnected() ) return true;
			else { m_ipc.Connect(block_time_ms); return m_ipc.IsConnected(); }
		}
		uint GetMessageId(uint block_time_ms = INFINITE)
		{
			if( !Connect() ) return (uint)NoMessage;
			uint id;
			if( !m_ipc.Receive(&id, sizeof(id), block_time_ms, ipc::Peek) ) return (uint)NoMessage;
			return id;
		}
		template <typename Type> bool Recv(Type& msg, uint block_time_ms = INFINITE)
		{
			if( !Connect(block_time_ms) ) return false;

			struct Packet { uint id; Type msg; } packet;
			uint start = GetTickCount();
			do {
				if( m_ipc.Receive(&packet, sizeof(packet), block_time_ms) ) { msg = packet.msg; return true; }
				Sleep(10);
			} while( GetTickCount() - start < block_time_ms );
			return false;
		}
		template <typename Type> bool Send(Type const& msg, uint message_id = 0, uint block_time_ms = INFINITE)
		{
			if( !Connect(block_time_ms) ) return false;

			struct Packet { uint id; Type msg; } packet = {message_id, msg};
			uint start = GetTickCount();
			do {
				if( m_ipc.Send(&packet, sizeof(packet), ipc::Signal, block_time_ms) ) return true;
				Sleep(10);
			} while( GetTickCount() - start < block_time_ms );
			return false;
		}
	};
}

#endif
