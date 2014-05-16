//**********************************
// Pipe
//  Copyright (c) Rylogic Ltd 2007
//**********************************
// Usage:
//	pr::Pipe<> pipe;
//	pipe.Send("Paul was here", sizeof(..));
//
//	void OnRecv(void const* data, std::size_t data_size, bool partial, void* user_data)
//	{
//		// Warning, this function is called asynchronously
//		if( mutex.Acquire() )
//		{
//			printf("%s\n", static_cast<char const*>(data));
//			mutex.UnAcquire();
//		}
//	}

#pragma once
#ifndef PR_PIPE_H
#define PR_PIPE_H

#include <vector>
#include <tchar.h>
#include "pr/common/tstring.h"
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/exception.h"

namespace pr
{
	namespace pipe
	{
		enum EResult
		{
			EResult_Success = 0,
			EResult_Failed = 0x80000000,
			EResult_ListenThreadCreationFailed,
		};
		typedef pr::Exception<EResult> Exception;

		// Receive function for incoming data,
		// 'data' is a pointer to a temporary buffer
		// 'data_size' is the amount of data available
		// 'partial' indicates this is not the whole message
		//	e.g.
		//		void OnRecv(void const* data, std::size_t data_size, bool partial, void* user_data)
		//		{
		//			printf("%s\n", static_cast<char const*>(data));
		//		}
		typedef void (*OnRecv)(void const* data, std::size_t data_size, bool partial, void* user_data);
	}//namespace pipe

	template <std::size_t BufferSize = 1024>
	class Pipe
	{
		pr::tstring         m_pipe_name;     // The unique name for the pipe
		pipe::OnRecv        m_recv;          // The function to call when data is received
		mutable std::mutex  m_cs;            // A critical section for adding/removing listen threads
		std::vector<HANDLE> m_listen_thread; // The threads that are listening for data
		HANDLE              m_pipe;          // The handle to the pipe for outgoing data
		void*               m_user_data;     // User data for the pipe
		volatile bool       m_terminate;

	public:
		Pipe(_TCHAR const* pipe_name, pipe::OnRecv recv_func, void* user_data)
		:m_pipe_name(pipe_name)
		,m_recv(recv_func)
		,m_pipe(0)
		,m_user_data(user_data)
		,m_terminate(false)
		{}
		~Pipe()
		{
			if( m_pipe ) CloseHandle(m_pipe);
			TerminateListenThreads();
		}

		pr::tstring GetPipeName() const
		{
			return pr::tstring(_T("\\\\.\\pipe\\") + m_pipe_name);
		}

		// Send data on the pipe
		bool Send(void const* data, std::size_t data_size)
		{
			if( !m_pipe && !OpenPipe() ) return false;

			DWORD written = 0;
			if( WriteFile(m_pipe, data, (DWORD)data_size, &written, NULL) && written == data_size )
				return true;

			if( !OpenPipe() ) return false;
			return WriteFile(m_pipe, data, (DWORD)data_size, &written, NULL) && written == data_size;
		}

		// Begin a thread to listen for data on the pipe
		void SpawnListenThread()
		{
			if (m_terminate)
				return;

			DWORD listen_thread_id;
			HANDLE listen_thread = CreateThread(NULL, 0, ListenThreadMain, this, 0, &listen_thread_id);
			if (!listen_thread) throw pipe::Exception(pipe::EResult_ListenThreadCreationFailed);

			std::lock_guard<std::mutex> lock(m_cs);
			m_listen_thread.push_back(listen_thread);
		}

		// End all threads that are listening for data
		void TerminateListenThreads()
		{
			m_terminate = true;

			std::lock_guard<std::mutex> lock(m_cs);
			for( std::vector<HANDLE>::const_iterator h = m_listen_thread.begin(), h_end = m_listen_thread.end(); h != h_end; ++h )
				CloseHandle(*h);
		}

	private:
		static DWORD WINAPI ListenThreadMain(void* user)
		{
			Pipe* this_ = static_cast<Pipe*>(user);
			this_->Listen();
			std::lock_guard<std::mutex> lock(this_->m_cs);
			std::vector<HANDLE>::iterator t = std::find(this_->m_listen_thread.begin(), this_->m_listen_thread.end(), GetCurrentThread());
			if( t != this_->m_listen_thread.end() )
			{
				if( *t ) CloseHandle(*t);
				this_->m_listen_thread.erase(t);
			}
			return 0;
		}
		void Listen()
		{
			HANDLE pipe = CreateNamedPipe(	GetPipeName().c_str(),		// pipe name
											PIPE_ACCESS_DUPLEX,			// read/write access
											PIPE_TYPE_MESSAGE |			// message type pipe
											PIPE_READMODE_MESSAGE |		// message-read mode
											PIPE_WAIT,					// blocking mode
											PIPE_UNLIMITED_INSTANCES,	// max. instances
											BufferSize,					// output buffer size
											BufferSize,					// input buffer size
											NMPWAIT_USE_DEFAULT_WAIT,	// client time-out
											NULL);
			// If the pipe couldn't be opened, then wait a bit, and try again
			if( !pipe )		{ Sleep(500); SpawnListenThread(); return; }

			// Wait for a connection to the pipe
			if( !ConnectNamedPipe(pipe, NULL) && GetLastError() != ERROR_PIPE_CONNECTED )
			{ return; }

			// We got a connection, spawn another listen thread, receive the data, then end this thread
			SpawnListenThread();

			DWORD readed = 0;
			char buffer[BufferSize + 1];
			while (ReadFile(pipe, buffer, BufferSize, &readed, NULL) && !m_terminate)
			{
				buffer[readed] = 0;
				std::lock_guard<std::mutex> lock(m_cs);
				m_recv(buffer, readed, readed == BufferSize, m_user_data);
			}
		}

		// Open the pipe for sending
		bool OpenPipe()
		{
			if( m_pipe ) CloseHandle(m_pipe);
			m_pipe = CreateFile(	GetPipeName().c_str(),
									GENERIC_WRITE|GENERIC_READ,
									0,
									NULL,
									OPEN_EXISTING,
									FILE_ATTRIBUTE_NORMAL,
									NULL);
			if( m_pipe == INVALID_HANDLE_VALUE ) m_pipe = 0;
			return m_pipe != 0; // If the pipe doesn't exist, then there's no one to send the data to anyway
		}
	};
}//namespace pr

#endif//PR_PIPE_H