//**********************************
// Pipe
//  Copyright (C) Rylogic Ltd 2007
//**********************************
#pragma once
#include <vector>
#include <string>
#include <utility>
#include <stdexcept>
#include <cstdint>
#include <chrono>
#include <mutex>
#include <windows.h>
#include "pr/common/algorithm.h"
#include "pr/common/event_handler.h"
#include "pr/common/scope.h"
#include "pr/win32/win32.h"

namespace pr
{
	class Pipe2
	{
		// Notes:
		//  - This implementation uses an IO Completion Port which is supposed
		//    to handle IO operations more efficiently than with separate threads.

	public:

		// Pipe mode
		enum class EMode
		{
			Server,
			Client,
		};

	private:

		using Handle = pr::win32::Handle;

		// Different types of async operations that the IPC pipe queues
		enum class EAsyncOp
		{
			None = 0,
			Connect,
			Read,
			Send,
			Reconnect,
			Shutdown,
		};

		// State of the pipe being communicated over
		enum class EState
		{
			// The pipe has been created, but no connection has been made
			Disconnected,

			// A request has been made to connect to another client, but no client has yet connected
			ConnectPending,

			// A client has connected to the other side of the pipe
			Connected,

			// The pipe has been broken, likely because the client on the other end disconnected
			Broken,
		};

		// Overlapped structure passed through the IPC IOCP for use on completion of async ops
		struct Overlapped : public OVERLAPPED
		{
			// A type for a buffer of bytes
			using DataBuffer = std::vector<char>;

			// Which operation does this OVERLAPPED represent
			EAsyncOp m_op;

			// Message being built up over one or more async read operations
			DataBuffer m_data;
		
		    // The current length of valid data. (Data can be bigger during Reads)
		    int64_t m_data_len;

			explicit Overlapped(EAsyncOp op)
				: OVERLAPPED()
				, m_op(op)
				, m_data()
				, m_data_len()
			{}
		};

	private:

		// The mode this object is in (server or client)
		EMode m_mode;

		// Name of the pipe the IPC is communicating over
		std::wstring m_pipename;

		// Named pipe used for the IPC
		Handle m_pipe;

		// IO completion port monitoring the pipe
		Handle m_iocp;

		// A buffer of in-flight async IO operations
		std::vector<std::unique_ptr<Overlapped>> m_ops;

		// Mutex guarding access to Ops
		std::mutex m_mutex_ops;

		// A buffer of messages received before the pipe is connected. Send upon successful connection.
		std::vector<std::vector<char>> m_saved_messages;

		// Mutex guarding access to SavedMessages
		std::mutex m_mutex_saved_messages;

		// True when the 'Run' method should exit
		std::atomic_bool m_shutdown;

		// Read/Write buffer size
		static constexpr int PipeBufferSize = 1024;

	public:

		Pipe2(EMode mode, std::wstring_view pipe_name)
			: m_mode(mode)
			, m_pipename(pipe_name)
			, m_pipe()
			, m_iocp()
			, m_ops()
			, m_mutex_ops()
			, m_saved_messages()
			, m_mutex_saved_messages()
			, m_shutdown()
			, MessageReceived()
		{
			CreatePipe();
		}

		// Take over this thread to process incoming and outgoing IPC communication
		void Run() // Worker thread context
		{
			constexpr std::chrono::milliseconds ProcessIOWaitTime{ 10 }; // milliseconds
			constexpr std::chrono::milliseconds SleepWhileDisconectedTime{ 10 };

			// Don't initialise 'm_shutdown' to false here. 'TerminateAsync' may be called before 'Run'.
			auto state = EState::Disconnected;
			for (;!m_shutdown;)
			{
				// Run the state machine
				const auto prev_state = state;
				switch (state)
				{
					case EState::Disconnected:
					{
						// While not connected, attempt to connect.
						state = Connect();

						// If connection fails, give up the thread
						if (state == EState::Disconnected)
						{
							std::this_thread::sleep_for(SleepWhileDisconectedTime);
						}
						break;
					}
					case EState::ConnectPending:
					{
						// While connecting, just wait
						state = ProcessIO(ProcessIOWaitTime, state);
						break;
					}
					case EState::Connected:
					{
						// While connected, read from the pipe and consume queue IOCP notifications.
						QueueRead();
						state = ProcessIO(ProcessIOWaitTime, state);
						break;
					}
					case EState::Broken:
					{
						// While broken, disconnect then reconnect
						Disconnect();
						state = EState::Disconnected;
						break;
					}
					default:
					{
						throw std::runtime_error(FmtS("Unknown IpcChannel state: %d", state));
					}
				}

				// Send any buffered messages when a connection is established
				if (state == EState::Connected && state != prev_state)
				{
					SendSavedMessages();
				}
			}

			// Exit 'Run' in the disconnected state and ready to be run again
			Disconnect();
			m_shutdown = false;
		}

		// Message received event
		pr::MultiCast<std::function<void(void const* data, int size)>, true> MessageReceived;

		// Send a message from this IPC object to the remote IPC object asynchronously.
		// When this function returns, the message is queued but it may not yet be delivered.
		void SendMessageAsync(std::string_view message)
		{
			// Allocate an OVERLAPPED structure for the operation and append the message
			auto& overlapped = AllocAsyncOp(EAsyncOp::Send);
			overlapped.m_data.insert(end(overlapped.m_data), message.begin(), message.end());

			// Attempt to send the message over the pipe
			auto r = WriteFile(m_pipe, overlapped.m_data.data(), static_cast<DWORD>(overlapped.m_data.size()), nullptr, &overlapped);
			DWORD error = r ? ERROR_SUCCESS : GetLastError();
			switch (error)
			{
				case ERROR_SUCCESS: // The send was completed.
				case ERROR_IO_PENDING: // The send was started but is not complete.
				{
					// Message send is in progress, preserve 'Overlapped'
					break;
				}
				case ERROR_PIPE_LISTENING: // The pipe is not connected. 
				case ERROR_INVALID_HANDLE: // The pipe has not been created
				{
					//UE_LOG(LogStateShareIpc, Log, TEXT("Pipe is not yet connected, saving message to send on connect"));
					FreeAsyncOp(overlapped);
					SaveMessage(message);
					break;
				}
				case ERROR_NO_DATA: // The client has closed their end of the pipe.
				{
				    //UE_LOG(LogStateShareIpc, Log, TEXT("Pipe was closed at the other end. Reconnecting"));
					FreeAsyncOp(overlapped);
					SaveMessage(message);

					// Queue a message to reconnect the pipe
					QueueSignal(EAsyncOp::Reconnect);
					break;
				}
				default:
				{
					FreeAsyncOp(overlapped);
					throw std::runtime_error(FmtS("Failed to send state share message : 0x%x", error));
				}
			}
		}

		// Request that the IPC client shutdown and that the Run function return control to its calling thread.
		// When this function returns, the terminate request is queued but may not have happened yet.
		void TerminateAsync()
		{
			m_shutdown = true;

			// Post a signal to terminate the pipe connection
			// This wakes up any threads blocking in 'GetQueuedCompletionStatus'
			QueueSignal(EAsyncOp::Shutdown);
		}

	private:

		// Create/Recreate the pipe
		void CreatePipe()
		{
			m_iocp = Handle{};
			m_pipe = Handle{};

			// The server creates the pipe, the client connects to it
			if (m_mode != EMode::Server)
			{
				return;
			}

			// Create and configure the named pipe as well as configure the IOCP for this handle
			m_pipe = CreateNamedPipeW(
				m_pipename.c_str(),
				PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
				PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT | PIPE_ACCEPT_REMOTE_CLIENTS,
				PIPE_UNLIMITED_INSTANCES,
				PipeBufferSize,
				PipeBufferSize,
				0,
				nullptr);
			if (!m_pipe)
			{
				DWORD error = GetLastError();
				throw std::runtime_error(FmtS("Failed to create state share named pipe : 0x%x", error));
			}

			// Create an IO completion port so we can receive notification of completed IO operations.
			m_iocp = CreateIoCompletionPort(m_pipe, 0, 0, 0);
			if (!m_iocp)
			{
				DWORD error = GetLastError();
				throw std::runtime_error(FmtS("Failed to create state share named pipe IOCP : 0x%x", error));
			}
		}

		// Attempt to connect the pipe
		EState Connect()
		{
			switch (m_mode)
			{
				case EMode::Server:
				{
					return ConnectServerPipe();
				}
				case EMode::Client:
				{
					return ConnectClientPipe();
				}
				default:
				{
					throw std::runtime_error(FmtS("Unknown mode : %d", m_mode));
				}
			}
		}

		// Attempt to connect a client IPC object to the pipe
		EState ConnectClientPipe()
		{
			// Client connection involves opening a file to the named pipe that was created by the server,
			// configuring that file to match the settings of the named pipe, and allocating an IOCP for it.
			// If creating the pipe files, return 'disconnected' and expect to be called again.
			m_pipe = CreateFileW(m_pipename.c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);
			if (!m_pipe)
			{
				// Check for errors
				DWORD error = GetLastError();
				switch (error)
				{
					case ERROR_PIPE_BUSY:
					{
						// Wait for the server to be not-busy
						if (WaitNamedPipeW(m_pipename.c_str(), NMPWAIT_USE_DEFAULT_WAIT))
						{
							return EState::Disconnected;
						}

						// 'ERROR_BAD_PATHNAME' can indicate that the pipe is currently being setup from the server - loop until it can connect
						DWORD wait_error = GetLastError();
						if (wait_error == ERROR_BAD_PATHNAME)
						{
							break;
						}

						// Busy for some other reason
						//UE_LOG(LogStateShareIpc, Error, TEXT("State share pipe busy : 0x%x"), wait_error);
						return EState::Disconnected;
					}
					case ERROR_FILE_NOT_FOUND:
					{
						// Pipe name doesn't exist
						//UE_LOG(LogStateShareIpc, Warning, TEXT("State share pipe name (%s) not found : 0x%x"), *StateSharePipeName, error);
						return EState::Disconnected;
					}
					default:
					{
						// Some other error
						//UE_LOG(LogStateShareIpc, Error, TEXT("Failed to initialize state share pipe (%s) : 0x%x"), *StateSharePipeName, error);
						return EState::Disconnected;
					}
				}
			}

			// Set the mode of the pipe to read-mode+message.
			DWORD PipeMode = PIPE_READMODE_MESSAGE | PIPE_WAIT;
			if (!SetNamedPipeHandleState(m_pipe, &PipeMode, nullptr, nullptr))
			{
				DWORD error = GetLastError();
				(void)error;//LOG(Error, TEXT("Failed to set pipe handle state : 0x%x"), error);
				return EState::Disconnected;
			}

			// Create an IO completion port to queue async IO operations
			m_iocp = CreateIoCompletionPort(m_pipe, 0, 0, 0);
			if (!m_iocp)
			{
				DWORD error = GetLastError();
				(void)error;//LOG(Error, TEXT("Failed to initialize state share IOCP : 0x%x"), error);
				return EState::Disconnected;
			}

			return EState::Connected;
		}

		// Attempt to connect a server IPC object to the pipe
		EState ConnectServerPipe()
		{
			//UE_LOG(LogStateShareIpc, Log, TEXT("Waiting for a connection on the state share pipe"));
			if (!m_pipe)
				throw std::runtime_error("Pipe handle doesn't exist");

			// Connect the pipe
			auto& overlapped = AllocAsyncOp(EAsyncOp::Connect);
			if (ConnectNamedPipe(m_pipe, &overlapped))
			{
				// Async connect is always expected to return FALSE even if the pipe is already connected
				//UE_LOG(LogStateShareIpc, Log, TEXT("Unexpected TRUE result from async ConnectNamedPipe"));
				FreeAsyncOp(overlapped);
				return EState::Broken;
			}

			// Check for errors
			DWORD error = GetLastError();
			switch (error)
			{
				case ERROR_PIPE_CONNECTED:
				{
					// Pipe has connected immediately.
					//LOG(Log, TEXT("State share pipe connected (synchronously)"));
					FreeAsyncOp(overlapped);
					return EState::Connected;
				}
				case ERROR_PIPE_LISTENING:
				case ERROR_IO_PENDING:
				{
					// Pipe is ready, waiting for the client to connect or a connection is in progress.
					return EState::ConnectPending;
				}
				case ERROR_NO_DATA:
				{
					// The previous client has closed the pipe handle, but the server has not yet disconnected.
					//UE_LOG(LogStateShareIpc, Error, TEXT("State share pipe closed by client"));
					FreeAsyncOp(overlapped);
					return EState::Broken;
				}
				case ERROR_INVALID_HANDLE:
				{
					//UE_LOG(LogStateShareIpc, Error, TEXT("State share pipe handle is invalid"));
					FreeAsyncOp(overlapped);

					// There's something wrong with the pipe handle, create a new one.
					CreatePipe();
					return EState::Disconnected;
				}
				default:
				{
					//UE_LOG(LogStateShareIpc, Error, TEXT("Error connecting state share pipe : 0x%x"), error);
					FreeAsyncOp(overlapped); 
					return EState::Broken;
				}
			}
		}

		// Disconnect from the IPC pipe
		void Disconnect()
		{
			if (!m_pipe)
				return;

			// Mark existing IO operations as cancel-pending.
			if (!CancelIo(m_pipe))
			{
				// If this fails, the documentation doesn't say what to do with in-flight IO operations.
				DWORD error = GetLastError();
				(void)error;//LOG(LogStateShareIpc, Error, TEXT("Error cancelling IO on state share pipe : 0x%x"), error);
			}

			// Process any remaining queued IO operations
			ProcessIO(std::chrono::milliseconds{ 0 }, EState::Disconnected);

			// Disconnect the pipe
			if (!DisconnectNamedPipe(m_pipe))
			{
				DWORD error = GetLastError();
				if (error != ERROR_PIPE_NOT_CONNECTED)
				{
					//UE_LOG(LogStateShareIpc, Error, TEXT("Error disconnecting state share pipe : 0x%x"), error);
				}
			}

			// Release any leftover overlapped objects. Ideally there shouldn't be any
			// but that can't be guaranteed depending on how the pipe connection is lost.
			m_ops.clear();
		}

		// Pump the queue of completed async IO operations
		EState ProcessIO(std::chrono::milliseconds wait_time, EState current_state)
		{
			// Process completed IO operations
			for (;;)
			{
				ULONG_PTR key = 0;
				DWORD bytes_transferred = 0;
				OVERLAPPED* completion_overlapped = nullptr;
				bool MoreData = false;

				// Wait up to 'wait_time_ms' for an IO completion event
				if (!GetQueuedCompletionStatus(m_iocp, &bytes_transferred, &key, &completion_overlapped, static_cast<DWORD>(wait_time.count())))
				{
					DWORD error = GetLastError();
					switch (error)
					{
						case WAIT_TIMEOUT: // No more queued IO operations
						{
							return current_state;
						}
						case ERROR_MORE_DATA: // Incomplete read
						{
							MoreData = true;
							break;
						}
						case ERROR_BROKEN_PIPE:
						case ERROR_OPERATION_ABORTED:
						case ERROR_PIPE_NOT_CONNECTED:
						case ERROR_ABANDONED_WAIT_0:
						case ERROR_INVALID_HANDLE:
						{
							// The pipe has been disconnected
							//UE_LOG(LogStateShareIpc, Log, TEXT("State share pipe connection lost"));
							return EState::Broken;
						}
						default:
						{
							//UE_LOG(LogStateShareIpc, Error, TEXT("Error getting state share completion status : 0x%x"), error);
							return EState::Broken;
						}
					}
				}

				// The completed IO operation does not have an associated overlapped object, on to the next...
				if (completion_overlapped == nullptr)
				{
					continue;
				}

				// The completed overlapped IO operation data
				auto& overlapped = *static_cast<Overlapped*>(completion_overlapped);
				auto cleanup = pr::Scope<void>([] {}, [this,&overlapped] { FreeAsyncOp(overlapped); });

			    // Process the completed IO operation. Remove the overlapped object from the collection of pending IO operations.
			    // When this goes out of scope the allocation will be released (unless added back to 'OutstandingOps').
			    switch (overlapped.m_op)
				{
					case EAsyncOp::Connect:
					{
						// Connection completed
						current_state = EState::Connected;
						//UE_LOG(LogStateShareIpc, Log, TEXT("State share pipe connected (asynchronously)"));
						break;
					}
					case EAsyncOp::Send:
					{
						// A Send completed, nothing else needed
						break;
					}
					case EAsyncOp::Read:
					{
					    // A Read has completed, but maybe only partially.
					    overlapped.m_data_len += bytes_transferred;

						if (MoreData)
						{
						    // If the message isn't complete, read again.
						    // Allocate a new overlapped object since 'overlapped' will be cleaned on scope exit.
						    auto& overlapped_more = AllocAsyncOp(overlapped.m_op);
						    swap(overlapped_more.m_data, overlapped.m_data);
						    QueueRead(overlapped_more);
						}
						else
						{
							// Otherwise, the full message has been received.
							overlapped.m_data.resize(overlapped.m_data_len);
							ProcessMessage(overlapped.m_data);
						}
						break;
					}
					case EAsyncOp::Reconnect:
					{
						// This message signals that the connection should be dropped and re-connected
						return EState::Broken;
					}
					case EAsyncOp::Shutdown:
					{
						// A terminate request was sent
						m_shutdown = true;
						break;
					}
					default:
					{
						throw std::runtime_error(FmtS("Unknown state share async operation : %d", static_cast<int>(overlapped.m_op)));
					}
				}
			}
		}

		// Forward a complete message to observers
		void ProcessMessage(Overlapped::DataBuffer const& message)
		{
			// Pass the message to observers
			MessageReceived(message.data(), static_cast<int>(message.size()));
		}

		// Begin an async read on the pipe
		void QueueRead()
		{
			QueueRead(AllocAsyncOp(EAsyncOp::Read));
		}

		// Continue an async read on the pipe
		void QueueRead(Overlapped& overlapped)
		{
		    // Add space to the data buffer to read into
		    assert(overlapped.m_data_len >= 0 && overlapped.m_data_len <= static_cast<int>(overlapped.m_data.size()));
		    overlapped.m_data.resize(overlapped.m_data_len + PipeBufferSize);
		    auto* buffer = static_cast<char*>(overlapped.m_data.data()) + overlapped.m_data_len;
		    auto count = static_cast<DWORD>(overlapped.m_data.size() - overlapped.m_data_len);
    
			// Read into 'Overlapped.Data'
			auto r = ReadFile(m_pipe, buffer, count, nullptr, &overlapped);
			DWORD error = r ? ERROR_SUCCESS : GetLastError();
			switch (error)
			{
				case ERROR_SUCCESS: // Read was completed
				case ERROR_IO_PENDING: // Read was started
				{
					break;
				}
				case ERROR_BROKEN_PIPE:
				case ERROR_PIPE_NOT_CONNECTED:
				{
					//UE_LOG(LogStateShareIpc, Log, TEXT("State share pipe broken"));
					FreeAsyncOp(overlapped);

					// Queue a message to reconnect
					QueueSignal(EAsyncOp::Reconnect);
					break;
				}
				default:
				{
					//UE_LOG(LogStateShareIpc, Error, TEXT("Error reading state share pipe : 0x%x"), error);
					FreeAsyncOp(overlapped); 
					break;
				}
			}
		}

		// Queue a signal to the worker thread
		void QueueSignal(EAsyncOp op)
		{
			auto& overlapped = AllocAsyncOp(op);
			if (!PostQueuedCompletionStatus(m_iocp, 0, 0, &overlapped))
			{
				FreeAsyncOp(overlapped);
				DWORD error = GetLastError();
				(void)error; //UE_LOG(LogStateShareIpc, Error, TEXT("Failed to post signal message to state share IOCP : 0x%x"), error);
			}
		}

	    // Save a message to be sent once a connection is established
	    void SaveMessage(std::string_view message)
	    {
			std::scoped_lock<std::mutex> lock(m_mutex_saved_messages);
		    m_saved_messages.push_back(std::vector<char>(begin(message), end(message)));
	    }

		// Send any messages that were saved to be sent once we connect
		void SendSavedMessages()
		{
			std::vector<std::vector<char>> messages;
			{
				std::scoped_lock<std::mutex> lock(m_mutex_saved_messages);
				swap(messages, m_saved_messages);
			}

			for (auto const& message : messages)
			{
				SendMessageAsync(message);
			}
		}
    
	    // Allocate an overlapped object for an async IO operation
	    Overlapped& AllocAsyncOp(EAsyncOp op)
	    {
		    // There is a design issue here. FOverlapped objects need to be allocated for IO operations that
		    // are in progress, but not all calls to functions that take an OVERLAPPED* result in a queued message
		    // on the IOCP. That means we need to guess whether the overlapped object should be stored or not.
		    // If we guess wrong, overlapped objects are leaked, or we get a crash.
			//
			// This leads to the ugly design where you 'allocate' then *remember* to 'free' if an error is reported.
			// Could do the inverse (allocate an RAII object, and prevent its destruction if needed), but this would
			// lead to a crash if we guess wrong. Playing it safe, it's better to leak a few objects than crash.
			// These will get cleaned up when the connection is closed.
    
		    std::scoped_lock<std::mutex> lock(m_mutex_ops);
		    m_ops.push_back(std::unique_ptr<Overlapped>(new Overlapped(op)));
		    return *m_ops.back().get();
	    }
    
	    // Remove overlapped object 'op' from the container of in-progress operations.
		void FreeAsyncOp(Overlapped& op)
		{
		    std::scoped_lock<std::mutex> lock(m_mutex_ops);
		    pr::erase_if_unstable(m_ops, [&op](const auto& x) { return x.get() == &op; });
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/threads/name_thread.h"
#include <condition_variable>
namespace pr::network
{
	PRUnitTest(Pipe2Test)
	{
		const wchar_t UnitTestPipeName[] = L"\\\\.\\pipe\\Pipe_UnitTest";
		int ServerMsgsSent = 0;
		int ServerMsgsRecv = 0;
		int ClientMsgsSent = 0;
		int ClientMsgsRecv = 0;

		// Event for signalling message received
		std::condition_variable cv_signal;
		std::mutex mutex;

		// Create the pipe server and client
		Pipe2 ipc_server(Pipe2::EMode::Server, UnitTestPipeName);
		Pipe2 ipc_client(Pipe2::EMode::Client, UnitTestPipeName);

		// Attach message handlers
		ipc_server.MessageReceived += [&ServerMsgsRecv, &cv_signal](void const* data, int size)
		{
			std::string_view msg(static_cast<char const*>(data), size);
			PR_CHECK(msg == "Message To Server", true);

			++ServerMsgsRecv;
			cv_signal.notify_one();
		};
		ipc_client.MessageReceived += [&ClientMsgsRecv, &cv_signal](void const* data, int size)
		{
			std::string_view msg(static_cast<char const*>(data), size);
			PR_CHECK(msg == "Message To Client", true);

			++ClientMsgsRecv;
			cv_signal.notify_one();
		};

		// Send messages before starting the client or server (these should get buffered and sent on connection)
		ipc_server.SendMessageAsync("Message To Client");
		ipc_client.SendMessageAsync("Message To Server");
		++ServerMsgsSent;
		++ClientMsgsSent;

		// Start the server and client
		std::thread ipc_server_thread([&ipc_server]
			{
				pr::threads::SetCurrentThreadName("IPC Server");
				ipc_server.Run();
			});
		std::thread ipc_client_thread([&ipc_client]
			{
				pr::threads::SetCurrentThreadName("IPC Client");
				ipc_client.Run();
			});

		// Send more messages, potentially while the connection is still being established
		for (int i = 0; i != 10; ++i)
		{
			ipc_server.SendMessageAsync("Message To Client");
			std::this_thread::yield();
			++ClientMsgsSent;
			ipc_client.SendMessageAsync("Message To Server");
			std::this_thread::yield();
			++ServerMsgsSent;
		}

		// Send a message from server to client and confirm all messages received
		{
			std::unique_lock<std::mutex> lock(mutex);
			ipc_server.SendMessageAsync("Message To Client");
			++ClientMsgsSent;

			cv_signal.wait_for(lock, std::chrono::milliseconds(500), [&ClientMsgsRecv, ClientMsgsSent] { return ClientMsgsRecv == ClientMsgsSent; });
		}

		// Send a message from client to server and confirm all messages received
		{
			std::unique_lock<std::mutex> lock(mutex);
			ipc_client.SendMessageAsync("Message To Server");
			++ServerMsgsSent;

			cv_signal.wait_for(lock, std::chrono::milliseconds(500), [&ServerMsgsRecv, ServerMsgsSent] { return ServerMsgsRecv == ServerMsgsSent; });
		}
		PR_CHECK(ClientMsgsRecv == ClientMsgsSent, true);
		PR_CHECK(ServerMsgsRecv == ServerMsgsSent, true);

		// Shutdown
		ipc_server.TerminateAsync();
		//std::this_thread::sleep_for(std::chrono::milliseconds(10));
		ipc_client.TerminateAsync();
		
		// Join threads
		ipc_server_thread.join();
		ipc_client_thread.join();
	}
	PRUnitTest(Pipe2Test_ClientOnly)
	{
		// Create a client with no server
		const wchar_t UnitTestPipeName[] = L"\\\\.\\pipe\\Pipe_UnitTest";
		int ClientMsgsRecv = 0;

		// Event for signalling message received
		std::condition_variable cv_signal;
		std::mutex mutex;

		// Create the pipe client
		Pipe2 ipc_client(Pipe2::EMode::Client, UnitTestPipeName);

		// Attach message handlers
		ipc_client.MessageReceived += [&ClientMsgsRecv, &cv_signal](void const* data, int size)
		{
			std::string_view msg(static_cast<char const*>(data), size);
			PR_CHECK(msg == "Message To Client", true);

			++ClientMsgsRecv;
			cv_signal.notify_one();
		};

		// Send messages before starting (these will be buffered and never sent because we're not starting a server)
		ipc_client.SendMessageAsync("Message To Server");

		// Start client
		std::thread ipc_client_thread([&ipc_client]
		{
			pr::threads::SetCurrentThreadName("IPC Client");
			ipc_client.Run();
		});

		// Send more messages (again, no server, they'll be buffered)
		for (int i = 0; i != 10; ++i)
		{
			ipc_client.SendMessageAsync("Message To Server");
			std::this_thread::yield();
		}
		PR_CHECK(ClientMsgsRecv == 0, true);

		// Shutdown
		ipc_client.TerminateAsync();

		// Join threads
		ipc_client_thread.join();
	}
	PRUnitTest(Pipe2Test_ServerOnly)
	{
		const wchar_t UnitTestPipeName[] = L"\\\\.\\pipe\\Pipe_UnitTest";
		int ServerMsgsRecv = 0;

		// Event for signalling message received
		std::condition_variable cv_signal;
		std::mutex mutex;

		// Create the pipe server and client
		Pipe2 ipc_server(Pipe2::EMode::Server, UnitTestPipeName);

		// Attach message handlers
		ipc_server.MessageReceived += [&ServerMsgsRecv, &cv_signal](void const* data, int size)
		{
			std::string_view msg(static_cast<char const*>(data), size);
			PR_CHECK(msg == "Message To Server", true);

			++ServerMsgsRecv;
			cv_signal.notify_one();
		};

		// Send messages before starting (these will be buffered and never sent because we're not starting a client)
		ipc_server.SendMessageAsync("Message To Client");

		// Start the server
		std::thread ipc_server_thread([&ipc_server]
		{
			pr::threads::SetCurrentThreadName("IPC Server");
			ipc_server.Run();
		});
	
		// Send more messages (again, no client, they'll be buffered)
		for (int i = 0; i != 10; ++i)
		{
			ipc_server.SendMessageAsync("Message To Client");
			std::this_thread::yield();
		}

		PR_CHECK(ServerMsgsRecv == 0, true);

		// Shutdown
		ipc_server.TerminateAsync();

		// Join threads
		ipc_server_thread.join();
	}
}
#endif
