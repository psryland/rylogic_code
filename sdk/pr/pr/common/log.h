//*****************************************************************************************
// Log
//  Copyright © Rylogic Ltd 2012
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
#include <memory>
#include <chrono>
#include <mutex>
#include <thread>
#include <cassert>
#include <pr/common/fmt.h>
#include <pr/str/prstring.h>
#include <pr/threads/concurrent_queue.h>
#include <pr/threads/name_thread.h>

namespace pr
{
	namespace log
	{
		//#if PR_LOGGING
		//#define PR_LOG(level, message)          do { pr::log::Log::Write(pr::log::Level::level, __FILE__, __LINE__, (message), 0);       } while (0)
		//#define PR_LOGE(level, except, message) do { pr::log::Log::Write(pr::log::Level::level, __FILE__, __LINE__, (message), &except); } while (0)
		//#else
		//#define PR_LOG(level, message)          do {} while (0)
		//#define PR_LOGE(level, except, message) do { (void)(except); } while (0)
		//#endif

		enum class ELevel
		{
			Debug,
			Info,
			Warn,
			Error,
			AssertionFailure,
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
			case ELevel::AssertionFailure: s << "Assert"; return s;
			}
		}

		// String
		typedef pr::string<char> string;

		// Timer
		typedef std::chrono::high_resolution_clock RTC;

		// An individual log event
		struct Event
		{
			ELevel          m_level;
			RTC::time_point m_timestamp;
			string          m_context;
			string          m_msg;
			int             m_occurrences;

			Event() :m_level(ELevel::AssertionFailure) ,m_timestamp() ,m_context("") ,m_msg("") ,m_occurrences() {}
			Event(ELevel level, string& ctx, string& msg)  :m_level(level) ,m_timestamp(RTC::now()) ,m_context(ctx) ,m_msg(msg)            ,m_occurrences(1) {}
			Event(ELevel level, string& ctx, string&& msg) :m_level(level) ,m_timestamp(RTC::now()) ,m_context(ctx) ,m_msg(std::move(msg)) ,m_occurrences(1) {}
			static bool Same(Event const& lhs, Event const& rhs) { return lhs.m_level == rhs.m_level &&lhs.m_context == rhs.m_context && lhs.m_msg == rhs.m_msg; }
		};

		// Producer/Consumer queue for log events
		typedef pr::threads::ConcurrentQueue<Event> LogQueue;

		// Provides logging support
		struct Logger
		{
		private:

			// Logger context. A single Context is shared by many instances of Logger
			struct Context
			{
				std::mutex m_mutex;

				// Queue of log events to report
				LogQueue m_queue;

				// A flag to indicate when the logger is idle
				std::condition_variable m_cv_idle;
				bool m_idle;

				// The worker thread that forwards log events to the callback function
				std::thread m_thread;

				template <typename OutputCB>
				Context(OutputCB log_cb, int occurrences_batch_size)
					:m_mutex()
					,m_queue(m_mutex)
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
					LogQueue::MLock lock(m_mutex);
					m_cv_idle.wait(lock, [&]{ return m_idle == idle; });
				}
				bool Dequeue(Event& ev)
				{
					LogQueue::MLock lock(m_mutex);
					m_cv_idle.notify_all();
					m_idle = true;
					auto r = m_queue.Dequeue(ev, lock);
					m_idle = false;
					return r;
				}
				void Enqueue(Event&& ev)
				{
					LogQueue::MLock lock(m_mutex);
					m_cv_idle.notify_all();
					m_idle = false;
					m_queue.Enqueue(std::forward<Event>(ev), lock);
				}
			};

			// An id to use in log messages
			string m_context_name;

			// The logger that this instance references
			std::shared_ptr<Context> m_context;

			// Thread for consuming log events
			template <typename OutputCB>
			static void LogConsumerThread(Context& ctx, OutputCB log_cb, int const occurrences_batch_size)
			{
				using namespace std::chrono;
				try
				{
					pr::threads::SetCurrentThreadName("pr::log::Logger");
					auto start_time = RTC::now();

					Event ev, last;
					for (;ctx.Dequeue(ev);)
					{
						auto is_same = Event::Same(ev, last);

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
							milliseconds millis = duration_cast<milliseconds>(last.m_timestamp - start_time);
							log_cb(last.m_level, millis.count(), last.m_context.c_str(), last.m_msg.c_str(), last.m_occurrences);
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
							milliseconds millis = duration_cast<milliseconds>(ev.m_timestamp - start_time);
							log_cb(ev.m_level, millis.count(), ev.m_context.c_str(), ev.m_msg.c_str(), ev.m_occurrences);

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

			//void OutputCB(ELevel level, long long timestamp_ms, char const* ctx, char const* msg, int occurrences);
			template <typename OutputCB>
			Logger(string context_name, OutputCB log_cb, int occurrences_batch_size = 0)
				:m_context_name(context_name)
				,m_context(std::make_shared<Context>(log_cb, occurrences_batch_size))
			{}
			Logger(string context_name, Logger const& rhs)
				:m_context_name(context_name)
				,m_context(rhs.m_context)
			{}

			// Log a message
			void Write(ELevel level, string msg)
			{
				m_context->Enqueue(Event(level, m_context_name, msg));
			}

			// Log an exception with message 'msg'
			void Write(ELevel level, std::exception const& ex, string msg)
			{
				m_context->Enqueue(Event(level, m_context_name, msg + " - Exception: " + ex.what()));
			}

			// Block the caller until the logger is idle
			void Flush()
			{
				m_context->WaitTillIdle(true);
			}

			void operator ()(ELevel level, string msg) { Write(level, msg); }
			void operator ()(ELevel level, std::exception const& ex, string msg) { Write(level, ex, msg); }
		};
	}
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
				Logger log("test", [&](ELevel level, long long, char const* ctx, char const* msg, int occurrences)
				{
					std::stringstream s;
					s << level << "," << ctx << ": " << msg << ',' << occurrences << std::endl;
					str += s.str();
				});
				log(ELevel::Debug, "event 1");
				log.Flush();
				PR_CHECK(str, "Debug,test: event 1,1\n");
			}
			{// Copied instances
				str.resize(0);
				Logger log1("log1", [&](ELevel level, long long, char const* ctx, char const* msg, int occurrences)
				{
					std::stringstream s;
					s << level << "," << ctx << ": " << msg << ',' << occurrences << std::endl;
					str += s.str();
				});
				Logger log2("log2", log1);

				log1(ELevel::Info, "event 1");
				log2(ELevel::Debug, "event 2");
				log1(ELevel::Info, "event 3");
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
