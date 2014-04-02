//*****************************************************************************************
// Log
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
// NOTE:
// For some reason, defining PR_LOGGING=1 in the property sheets does not work
// in VS2012. You have to define it in the project settings...

#pragma once
#ifndef PR_COMMON_LOG_H
#define PR_COMMON_LOG_H

#include <string>
#include <sstream>
#include <memory>
#include <chrono>
#include <thread>
#include <cassert>
#include <pr/common/fmt.h>
#include <pr/str/prstring.h>
#include <pr/threads/concurrent_queue.h>

namespace pr
{
	namespace log
	{
		#if PR_LOGGING
		#define PR_LOG(level, message)          do { pr::log::Log::Write(pr::log::Level::level, __FILE__, __LINE__, (message), 0);       } while (0)
		#define PR_LOGE(level, except, message) do { pr::log::Log::Write(pr::log::Level::level, __FILE__, __LINE__, (message), &except); } while (0)
		#else
		#define PR_LOG(level, message)          do {} while (0)
		#define PR_LOGE(level, except, message) do { (void)(except); } while (0)
		#endif

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
			string          m_msg;
			int             m_occurrences;

			Event() :m_level(ELevel::AssertionFailure) ,m_timestamp() ,m_msg("") ,m_occurrences() {}
			Event(ELevel level, string& msg) :m_level(level) ,m_timestamp(RTC::now()) ,m_msg(msg) ,m_occurrences(1) {}
			Event(ELevel level, string&& msg) :m_level(level) ,m_timestamp(RTC::now()) ,m_msg(std::move(msg)) ,m_occurrences(1) {}
			static bool Same(Event const& lhs, Event const& rhs) { return lhs.m_level == rhs.m_level && lhs.m_msg == rhs.m_msg; }
		};

		// Producer/Consumer queue for log events
		typedef pr::threads::ConcurrentQueue<Event> LogQueue;

		// Provides logging support
		struct Log
		{
		private:
			// An id to use in log messages
			std::string m_name;

			// Queue of log events to report
			std::shared_ptr<LogQueue> m_queue;

			// The worker thread that forwards log events to the cb function
			std::shared_ptr<std::thread> m_thread;

			// Thread for consuming log events
			template <typename OutputCB>
			static void LogConsumerThread(LogQueue& log_queue, OutputCB log_cb, int const occurrences_batch_size)
			{
				using namespace std::chrono;
				try
				{
					auto start_time = RTC::now();

					Event ev, last;
					while (log_queue.Dequeue(ev))
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
							log_cb(last.m_level, millis.count(), last.m_msg.c_str(), last.m_occurrences);
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
							log_cb(ev.m_level, millis.count(), ev.m_msg.c_str(), ev.m_occurrences);

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

			//void OutputCB(ELevel level, long long timestamp_ms, char const* msg, int occurrences);
			template <typename OutputCB>
			Log(char const* name, OutputCB log_cb, int occurrences_batch_size = 0)
				:m_name(name)
				,m_queue(std::make_shared<LogQueue>())
				,m_thread(std::make_shared<std::thread>(LogConsumerThread<OutputCB>, std::ref(*m_queue.get()), log_cb, occurrences_batch_size))
			{}
			Log(char const* name, Log const& rhs)
				:m_name(name)
				,m_queue(rhs.m_queue)
				,m_thread(rhs.m_thread)
			{}
			~Log()
			{
				if (m_thread.unique())
				{
					m_queue->LastAdded();
					m_thread->join();
				}
			}

			// Log a message
			void Write(ELevel level, char const* msg)
			{
				m_queue->Enqueue(Event(level, pr::Fmt<string>("%s: %s", m_name.c_str(), msg)));
			}

			// Log an exception with message 'msg'
			void Write(ELevel level, std::exception const& ex, char const* msg)
			{
				m_queue->Enqueue(Event(level, pr::Fmt<string>("%s: %s - Exception: %s", m_name.c_str(), msg, ex.what())));
			}

			void operator ()(ELevel level, char const* msg) { Write(level, msg); }
			void operator ()(ELevel level, std::exception const& ex, char const* msg) { Write(level, ex, msg); }
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
				Log log("test", [&](ELevel level, long long, char const* msg, int occurrences)
				{
					std::stringstream s;
					s << level << "," << msg << ',' << occurrences << std::endl;
					str += s.str();
				});
				log(ELevel::Debug, "event 1");
			}
			PR_CHECK(str, "Debug,test: event 1,1\n");

			{// Copied instances
				str.resize(0);
				Log log1("log1", [&](ELevel level, long long, char const* msg, int occurrences)
				{
					std::stringstream s;
					s << level << "," << msg << ',' << occurrences << std::endl;
					str += s.str();
				});
				Log log2("log2", log1);

				log1(ELevel::Info, "event 1");
				log2(ELevel::Debug, "event 2");
				log1(ELevel::Info, "event 3");
			}
			PR_CHECK(str,
				"Info,log1: event 1,1\n"
				"Debug,log2: event 2,1\n"
				"Info,log1: event 3,1\n"
				);
		}
	}
}
#endif

#endif
