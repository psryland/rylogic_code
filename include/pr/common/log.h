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
#ifndef PR_COMMON_LOG_H
#define PR_COMMON_LOG_H

#include <string>
#include <sstream>
#include <fstream>
#include <iostream>
#include <memory>
#include <chrono>
#include <atomic>
#include <mutex>
#include <thread>
#include <cassert>
#include "pr/common/fmt.h"
#include "pr/common/datetime.h"
//#include "pr/str/prstring.h"
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

		enum class ELevel
		{
			Debug,
			Info,
			Warn,
			Error,
		};
		template <typename Strm> inline Strm& operator << (Strm& s, ELevel level)
		{
			switch (level)
			{
			default: return s;
			case ELevel::Debug           : s << "Debug" ; return s;
			case ELevel::Info            : s << "Info"  ; return s;
			case ELevel::Warn            : s << "Warn"  ; return s;
			case ELevel::Error           : s << "Error" ; return s;
			}
		}

		// String
		typedef pr::string<char> string;

		// Timer
		typedef std::chrono::high_resolution_clock RTC;

		// An individual log event
		struct Event
		{
			ELevel        m_level;
			RTC::duration m_timestamp;
			string        m_context;
			string        m_msg;
			string        m_file;
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
			Event(ELevel level, RTC::time_point tzero, string& ctx, string& msg, string file, size_t line)
				:m_level(level)
				,m_timestamp(RTC::now() - tzero)
				,m_context(ctx)
				,m_msg(msg)
				,m_file(file)
				,m_line(line)
				,m_occurrences(1)
			{}
			Event(ELevel level, RTC::time_point tzero, string& ctx, string&& msg, string file, size_t line)
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
		typedef pr::threads::ConcurrentQueue<log::Event> LogQueue;

		// Helper object for writing log output to a stdout
		struct ToStdout
		{
			void operator ()(Event const& ev)
			{
				char const* delim = "";
				auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(ev.m_timestamp);
				if (!ev.m_file.empty()) { std::cout << ev.m_file;                delim = " "; }
				if (ev.m_line != -1)    { std::cout << "(" << ev.m_line << "):"; delim = " "; }
				std::cout << delim << FmtS("%8s",ev.m_context.c_str()) << "|" << ev.m_level << "|" << pr::To<std::string>(ev.m_timestamp, "%h:%mm:%ss:%fff") << "|" << ev.m_msg << std::endl;
			}
		};

		// Helper object for writing log output to a file
		struct ToFile
		{
			std::shared_ptr<std::ofstream> m_outf;
			ToFile(string filepath, std::ios_base::openmode mode = std::ios_base::out)
				:m_outf(std::make_shared<std::ofstream>(filepath, mode))
			{}
			void operator ()(Event const& ev)
			{
				char const* delim = "";
				auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(ev.m_timestamp);
				if (!ev.m_file.empty()) { *m_outf << ev.m_file;                delim = " "; }
				if (ev.m_line != -1)    { *m_outf << "(" << ev.m_line << "):"; delim = " "; }
				*m_outf << delim << FmtS("%8s",ev.m_context.c_str()) << "|" << ev.m_level << "|" << pr::To<std::string>(ev.m_timestamp, "%h:%mm:%ss:%fff") << "|" << ev.m_msg << std::endl;
			}
		};
	}

	// Provides logging support
	struct Logger
	{
		private:

			// Logger context. A single Context is shared by many instances of Logger
			struct Context
			{
				// The time point when logging started
				log::RTC::time_point const m_time_zero;

				// Queue of log events to report
				log::LogQueue m_queue;

				// A flag to indicate when the logger is idle
				std::condition_variable m_cv_idle;
				bool m_idle;

				// The worker thread that forwards log events to the callback function
				std::thread m_thread;

				template <typename OutputCB>
				Context(OutputCB log_cb, int occurrences_batch_size)
					:m_time_zero(log::RTC::now())
					,m_queue()
					,m_cv_idle()
					,m_idle()
					,m_thread(LogConsumerThread<OutputCB>, std::ref(*this), log_cb, occurrences_batch_size)
				{}
				~Context()
				{
					m_queue.LastAdded();
					m_thread.join();
				}
				void WaitTillIdle(bool idle)
				{
					log::LogQueue::MLock lock(m_queue.m_mutex);
					m_cv_idle.wait(lock, [&]{ return m_idle == idle; });
				}
				bool Dequeue(log::Event& ev)
				{
					log::LogQueue::MLock lock(m_queue.m_mutex);
					m_cv_idle.notify_all();
					m_idle = true;
					auto r = m_queue.Dequeue(ev, lock);
					m_idle = false;
					return r;
				}
				void Enqueue(log::Event&& ev)
				{
					log::LogQueue::MLock lock(m_queue.m_mutex);
					m_cv_idle.notify_all();
					m_idle = false;
					m_queue.Enqueue(std::forward<log::Event>(ev), lock);
				}
			};

			// An id to use in log messages
			log::string m_context_name;

			// The logger that this instance references
			std::shared_ptr<Context> m_context;

			// Thread for consuming log events
			template <typename OutputCB>
			static void LogConsumerThread(Context& ctx, OutputCB log_cb, int const occurrences_batch_size)
			{
				using namespace std::chrono;
				try
				{
					pr::threads::SetCurrentThreadName("pr::Logger");

					log::Event ev, last;
					for (;ctx.Dequeue(ev);)
					{
						auto is_same = log::Event::Same(ev, last);

						// Same event as last time? add it to the batch
						if (is_same && last.m_occurrences < occurrences_batch_size)
						{
							++last.m_occurrences;
							last.m_timestamp = ev.m_timestamp;
							continue;
						}

						// Have events been batched? Report them now
						if (last.m_occurrences != 0)
						{
							log_cb(last);
							last.m_occurrences = 0;
						}

						// Start of the next batch (and batching is enabled)? Add it to the batch
						if (is_same && occurrences_batch_size != 0)
						{
							last.m_occurrences = 1;
							last.m_timestamp = ev.m_timestamp;
						}
						else
						{
							log_cb(ev);
							last = ev;
							last.m_occurrences = 0;
						}
					}
				}
				catch (...)
				{
					assert(!"Unknown exception in log thread");
				}
			}

		public:

			//void OutputCB(pr::log::Event const& ev);
			template <typename OutputCB>
			Logger(log::string context_name, OutputCB log_cb, int occurrences_batch_size = 0)
				:m_context_name(context_name)
				,m_context(new Context(log_cb, occurrences_batch_size))
				,Enabled()
			{
				Enabled = true;
			}
			Logger(Logger const& rhs, log::string context_name)
				:m_context_name(context_name)
				,m_context(rhs.m_context)
				,Enabled(rhs.Enabled)
			{}

			// On/Off switch for logging
			std::atomic_bool Enabled;

			// Log a message
			void Write(log::ELevel level, log::string msg, char const* file = "", int line = -1)
			{
				if (!Enabled) return;
				m_context->Enqueue(log::Event(level, m_context->m_time_zero, m_context_name, msg, file, line));
			}

			// Log an exception with message 'msg'
			void Write(log::ELevel level, std::exception const& ex, log::string msg, char const* file = "", int line = -1)
			{
				if (!Enabled) return;
				m_context->Enqueue(log::Event(level, m_context->m_time_zero, m_context_name, msg + " - Exception: " + ex.what(), file, line));
			}

			// Block the caller until the logger is idle
			void Flush()
			{
				if (!Enabled) return;
				m_context->WaitTillIdle(true);
			}
		};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_log)
		{
			using namespace pr::log;

			std::string str;

			{// Single instance
				str.resize(0);
				Logger log("test", [&](pr::log::Event const& ev)
				{
					std::stringstream s;
					s << ev.m_level << "," << ev.m_context << ": " << ev.m_msg << ',' << ev.m_occurrences << std::endl;
					str += s.str();
				});
				log.Write(ELevel::Debug, "event 1");
				log.Flush();
				PR_CHECK(str, "Debug,test: event 1,1\n");
			}
			{// Copied instances
				str.resize(0);
				Logger log1("log1", [&](pr::log::Event const& ev)
				{
					std::stringstream s;
					s << ev.m_level << "," << ev.m_context << ": " << ev.m_msg << ',' << ev.m_occurrences << std::endl;
					str += s.str();
				});
				Logger log2(log1, "log2");

				log1.Write(ELevel::Info, "event 1");
				log2.Write(ELevel::Debug, "event 2");
				log1.Write(ELevel::Info, "event 3");
				log1.Flush();
				PR_CHECK(str,
					"Info,log1: event 1,1\n"
					"Debug,log2: event 2,1\n"
					"Info,log1: event 3,1\n"
					);
			}
		}
	}
}
#endif

#endif
