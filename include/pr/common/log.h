//*****************************************************************************************
// Log
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// Notes:
//  If you create a log function like this:
//     inline Logger& Log() { static Logger s_log; return s_log; }
//  Be careful about async access. Multiple threads calling the Log() function
//  is a race condition, you need to instantiate the static s_log object first.
//
//  For some reason, defining PR_LOGGING=1 in the property sheets does not work
//  in VS2012. You have to define it in the project settings...
//
#pragma once

#include <string>
#include <sstream>
#include <fstream>
#include <iostream>
#include <filesystem>
#include <memory>
#include <chrono>
#include <atomic>
#include <mutex>
#include <thread>
#include <cassert>
#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/common/datetime.h"
#include "pr/macros/enum.h"
#include "pr/str/string_core.h"
#include "pr/threads/concurrent_queue.h"
#include "pr/threads/name_thread.h"

namespace pr
{
	namespace log
	{
		#if PR_LOGGING
		#define PR_LOG(logger, level, message)          do { logger.Write(pr::log::ELevel::level,         (message), __FILE__, __LINE__); } while (0)
		#define PR_LOGE(logger, level, except, message) do { logger.Write(pr::log::ELevel::level, except, (message), __FILE__, __LINE__); } while (0)
		#else
		#define PR_LOG(logger, level, message)          do {} while (0)
		#define PR_LOGE(logger, level, except, message) do { (void)(except); } while (0)
		#endif

		// Log levels
		#define PR_ENUM(x)\
			x(Debug)\
			x(Info)\
			x(Warn)\
			x(Error)
		PR_DEFINE_ENUM1(ELevel, PR_ENUM);
		#undef PR_ENUM

		// Timer
		using RTC = std::chrono::high_resolution_clock;

		// An individual log event
		struct Event
		{
			using string = std::wstring;
			using path = std::filesystem::path;

			ELevel        m_level;
			RTC::duration m_timestamp;
			string        m_context;
			string        m_msg;
			path          m_file;
			size_t        m_line;
			int           m_occurrences;

			Event()
				:m_level(ELevel::Error)
				,m_timestamp()
				,m_context()
				,m_msg()
				,m_file()
				,m_line()
				,m_occurrences()
			{}
			Event(ELevel level, RTC::time_point tzero, std::wstring_view ctx, std::wstring_view msg, path const& file, size_t line)
				:m_level(level)
				,m_timestamp(RTC::now() - tzero)
				,m_context(ctx)
				,m_msg(msg)
				,m_file(file)
				,m_line(line)
				,m_occurrences(1)
			{}
			Event(ELevel level, RTC::time_point tzero, std::wstring_view ctx, string&& msg, path const& file, size_t line)
				:m_level(level)
				,m_timestamp(RTC::now() - tzero)
				,m_context(ctx)
				,m_msg(std::move(msg))
				,m_file(file)
				,m_line(line)
				,m_occurrences(1)
			{}
			static bool Same(Event const& lhs, Event const& rhs)
			{
				return
					lhs.m_level == rhs.m_level &&
					lhs.m_context == rhs.m_context &&
					lhs.m_file == rhs.m_file &&
					lhs.m_line == rhs.m_line &&
					lhs.m_msg == rhs.m_msg;
			}
		};

		// Producer/Consumer queue for log events
		using LogQueue = pr::threads::ConcurrentQueue<log::Event>;

		// Helper object for writing log output to a 'stdout'
		struct ToStdout
		{
			void operator ()(Event const& ev)
			{
				wchar_t const* delim = L"";
				auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(ev.m_timestamp);
				if (!ev.m_file.empty()) { std::wcout << ev.m_file;                delim = L" "; }
				if (ev.m_line != -1)    { std::wcout << "(" << ev.m_line << "):"; delim = L" "; }
				auto lvl = Enum<ELevel>::ToStringW(ev.m_level);
				auto ts = To<std::wstring>(ev.m_timestamp, L"%h:%mm:%ss:%fff");
				auto s = FmtS(L"%s%8s|%s|%s|%s\n", delim, ev.m_context.c_str(), lvl, ts.c_str(), ev.m_msg.c_str());
				std::wcout << s;
			}
		};

		// Helper object for writing log output to a file
		struct ToFile
		{
			std::filesystem::path m_filepath;
			std::shared_ptr<std::wofstream> m_outf;

			ToFile(std::filesystem::path const& filepath, std::ios_base::openmode mode = std::ios_base::out)
				:m_filepath(filepath)
				,m_outf(std::make_shared<std::wofstream>(filepath, mode))
			{}
			void operator ()(Event const& ev)
			{
				auto& fp = *m_outf;
				fp.seekp(0, std::ios_base::end);

				wchar_t const* delim = L"";
				if (!ev.m_file.empty()) { fp << ev.m_file.c_str(); delim = L" "; }
				if (ev.m_line != -1)    { fp << FmtS(L"(%d):", ev.m_line); delim = L" "; }
				auto lvl = Enum<ELevel>::ToStringW(ev.m_level);
				auto ts = To<std::wstring>(ev.m_timestamp, L"%h:%mm:%ss:%fff");
				fp << FmtS(L"%s%8s|%s|%s|%s\n", delim, ev.m_context.c_str(), lvl, ts.c_str(), ev.m_msg.c_str());
				fp.flush();
			}
		};

		// Helper object for writing log output to a file, synchronised by named mutex
		struct ToFileIPC :ToFile
		{
			HANDLE m_ipc_mutex;

			explicit ToFileIPC(std::filesystem::path const& filepath, wchar_t const* mutex_name, std::ios_base::openmode mode = std::ios_base::out)
				:ToFile(filepath, mode)
				,m_ipc_mutex(CreateMutexW(nullptr, FALSE, mutex_name))
			{}
			void operator ()(Event const& ev)
			{
				// Lock the mutex before writing to the file
				if (::WaitForSingleObject(m_ipc_mutex, INFINITE) == WAIT_OBJECT_0) try
				{
					ToFile::operator()(ev);
					::ReleaseMutex(m_ipc_mutex);
				}
				catch (...)
				{
					::ReleaseMutex(m_ipc_mutex);
					throw;
				}
			}
		};
	}

	// Provides logging support
	struct Logger
	{
		// Logger context. A single Context is shared by many instances of Logger
		struct Context
		{
		private:

			friend struct Logger;

			// The time point when logging started
			log::RTC::time_point const m_time_zero;

			// Queue of log events to report
			log::LogQueue m_queue;

			// A flag to indicate when the logger is idle
			std::condition_variable m_cv_idle;
			bool m_idle;

			// A callback function used in immediate mode
			std::function<void(log::Event const&)> m_log_cb;

			// The worker thread that forwards log events to the callback function
			std::thread m_thread;

			// Thread entry point for consuming log events
			template <typename OutputCB>
			static void LogConsumerThread(Context& ctx, OutputCB log_cb, int const occurrences_batch_size = 0)
			{
				using namespace std::chrono;
				try
				{
					pr::threads::SetCurrentThreadName("pr::Logger");

					log::Event ev, prev;
					for (;ctx.Dequeue(ev);)
					{
						auto is_same = log::Event::Same(ev, prev);

						// Same event as last time? add it to the batch
						if (is_same && prev.m_occurrences < occurrences_batch_size)
						{
							++prev.m_occurrences;
							prev.m_timestamp = ev.m_timestamp;
							continue;
						}

						// Have events been batched? Report them now
						if (prev.m_occurrences != 0)
						{
							log_cb(prev);
							prev.m_occurrences = 0;
						}

						// Start of the next batch (and batching is enabled)? Add it to the batch
						if (is_same && occurrences_batch_size != 0)
						{
							prev.m_occurrences = 1;
							prev.m_timestamp = ev.m_timestamp;
						}
						else
						{
							log_cb(ev);
							prev = ev;
							prev.m_occurrences = 0;
						}
					}
				}
				catch (...)
				{
					assert(!"Unknown exception in log thread");
				}
			}

			Context& This() { return *this; }

		public:

			template <typename OutputCB>
			Context(OutputCB log_cb, int occurrences_batch_size)
				:m_time_zero(log::RTC::now())
				,m_queue()
				,m_cv_idle()
				,m_idle()
				,m_log_cb()
				,m_thread(LogConsumerThread<OutputCB>, std::ref(This()), log_cb, occurrences_batch_size)
			{}
			~Context()
			{
				m_queue.LastAdded();
				m_thread.join();
			}

			// Enable/Disable immediate mode.
			// In immediate mode, log events are written to 'log_cb' instead of being queued for processing
			// by the background thread. Useful when you want the log to be written in sync with debugging.
			template <typename OutputCB> void ImmediateWrite(OutputCB log_cb)
			{
				m_log_cb = log_cb;
			}

			// Queue a log event for writing to the log callback function
			void Enqueue(log::Event&& ev)
			{
				log::LogQueue::MLock lock(m_queue.m_mutex);
				if (m_log_cb == nullptr)
				{
					m_cv_idle.notify_all();
					m_idle = false;
					m_queue.Enqueue(std::forward<log::Event>(ev), lock);
				}
				else
				{
					m_log_cb(std::forward<log::Event>(ev));
				}
			}

			// Pull an event from the queue of log events
			bool Dequeue(log::Event& ev)
			{
				log::LogQueue::MLock lock(m_queue.m_mutex);
				m_cv_idle.notify_all();
				m_idle = true;
				auto r = m_queue.Dequeue(ev, lock);
				m_idle = false;
				return r;
			}

			// Wait for the log event queue to become empty
			void WaitTillIdle(bool idle)
			{
				log::LogQueue::MLock lock(m_queue.m_mutex);
				m_cv_idle.wait(lock, [&]{ return m_idle == idle; });
			}
		};

		// The shared Context that this instance references
		std::shared_ptr<Context> m_context;

		// An id to use in log messages
		std::wstring Tag;

		// On/Off switch for logging
		std::atomic_bool Enabled;

		//void OutputCB(pr::log::Event const& ev);
		template <typename OutputCB>
		Logger(std::wstring_view tag, OutputCB log_cb, int occurrences_batch_size)
			:m_context(new Context(log_cb, occurrences_batch_size))
			,Tag(tag)
			,Enabled()
		{
			Enabled = true;
		}
		Logger(Logger const& rhs, std::wstring_view tag)
			:m_context(rhs.m_context)
			,Tag(tag)
			,Enabled()
		{
			Enabled = rhs.Enabled.load();
		}

		// Access to the shared logger context
		Context& SharedContext() const
		{
			return *m_context;
		}

		// Log a message
		void Write(log::ELevel level, std::string_view msg, std::filesystem::path const& file = L"", int line = -1) const
		{
			Write(level, Widen(msg), file, line);
		}
		void Write(log::ELevel level, std::wstring_view msg, std::filesystem::path const& file = L"", int line = -1) const
		{
			if (!Enabled) return;
			log::Event evt(level, m_context->m_time_zero, Tag, msg, file, line);
			m_context->Enqueue(std::move(evt));
		}

		// Log an exception with message 'msg'
		void Write(log::ELevel level, std::exception const& ex, std::string_view msg, std::filesystem::path const& file = L"", int line = -1) const
		{
			Write(level, ex, Widen(msg), file, line);
		}
		void Write(log::ELevel level, std::exception const& ex, std::wstring_view msg, std::filesystem::path const& file = L"", int line = -1) const
		{
			if (!Enabled) return;
			auto message = std::wstring(msg).append(L" - Exception: ").append(Widen(ex.what()));
			log::Event evt(level, m_context->m_time_zero, Tag, message, file, line);
			m_context->Enqueue(std::move(evt));
		}

		// Block the caller until the logger is idle
		void Flush() const
		{
			if (!Enabled) return;
			m_context->WaitTillIdle(true);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::log
{
	PRUnitTest(LogTests)
	{
		std::wstring str;

		{// Single instance
			str.resize(0);
			Logger log(L"test", [&](Event const& ev)
			{
				std::wstringstream s;
				s << Enum<ELevel>::ToStringW(ev.m_level) << L"," << ev.m_context << L": " << ev.m_msg << L"," << ev.m_occurrences << std::endl;
				str += s.str();
			}, 0);
			log.Write(ELevel::Debug, "event 1");
			log.Flush();
			PR_CHECK(str, L"Debug,test: event 1,1\n");
		}
		{// Copied instances
			str.resize(0);
			Logger log1(L"log1", [&](pr::log::Event const& ev)
			{
				std::wstringstream s;
				s << Enum<ELevel>::ToStringW(ev.m_level) << L"," << ev.m_context << L": " << ev.m_msg << L"," << ev.m_occurrences << std::endl;
				str += s.str();
			}, 0);
			Logger log2(log1, L"log2");

			log1.Write(ELevel::Info, "event 1");
			log2.Write(ELevel::Debug, "event 2");
			log1.Write(ELevel::Info, "event 3");
			log1.Flush();
			PR_CHECK(str,
				L"Info,log1: event 1,1\n"
				L"Debug,log2: event 2,1\n"
				L"Info,log1: event 3,1\n"
				);
		}
	}
}
#endif
