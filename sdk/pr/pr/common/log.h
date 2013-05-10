//*****************************************************************************************
// Log
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
// Simple logger
// Use:
// try
// {
//    PR_LOG(Info, "Trying summik");
//    PR_LOG(Debug, "Summik started");
//    PR_LOG(Warn, "Summik is going to fail!");
// }
// catch (std::exception const& e)
// {
//    PR_LOG(Exception, e, "Tried and failed");
// }

// NOTE:
// For some reason, defining PR_LOGGING=1 in the property sheets does not work
// in VS2012. You have to define it in the project settings...

#ifndef PR_COMMON_LOG_H
#define PR_COMMON_LOG_H

#include <windows.h>
#include <iostream>
#include <sstream>
#include <string>
#include <stdio.h>

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

		// Logging output levels
		namespace Level
		{
			enum Type
			{
				Debug,
				Info,
				Warn,
				Error,
				Exception,
				NumberOf,
			};
			inline char const* ToString(size_t level)
			{
				switch (static_cast<Type>(level)) {
				default:        return "";
				case Debug:     return "Debug";
				case Info:      return "Info";
				case Warn:      return "Warn";
				case Error:     return "Error";
				case Exception: return "Exception";
				}
			}
			inline Type Parse(char const* level)
			{
				int i; for (i = 0; i != NumberOf && ::_stricmp(level, ToString(static_cast<Type>(i))) != 0; ++i) {}
				return static_cast<Type>(i);
			}
		}

		struct Log
		{
			// RAII Lock class
			struct Lock
			{
				volatile long* m_lock;
				Lock(volatile long* lock) :m_lock(lock) { for (; ::InterlockedCompareExchange(m_lock, 1, 0) != 0; Sleep(0)) {} }
				~Lock() { ::InterlockedDecrement(m_lock); }
			};
			static volatile long* boundary()
			{
				static volatile long s_boundary = 0;
				return &s_boundary;
			}

		public:
			// The stream to send log data to
			static std::ostream*& Out()
			{
				static std::ostream* out = &std::cerr;
				return out;
			}

			static Level::Type& Level()
			{
				static Level::Type s_level = Level::Info;
				return s_level;
			}

			// Write to the log
			#if PR_LOGGING
			static void Write(Level::Type level, char const* file, size_t line, char const* message, std::exception const* e)
			{
				Lock lock(boundary());
				if (level < Level()) return;
				
				auto& out = *Out();
				out << "(" << Timestamp() << ") [" << Level::ToString(level) << "] " <<  message << "\n";
				if (e)
				{
					out << "  Exception Message: " << e->what() << "\n";
					out << "  Source: " << file << "(" << line << ")\n";
				}
				out.flush();
			}
			#else
			static void Write(Level::Type, char const*, size_t, char const*, std::exception const*) {}
			#endif

			// Returns the current timestamp
			#if PR_LOGGING
			static std::string Timestamp()
			{
				char buffer[200] = {};
				if (GetTimeFormatA(LOCALE_USER_DEFAULT, 0, 0, "HH':'mm':'ss", buffer, sizeof(buffer)) == 0) return "";
				
				static DWORD first = GetTickCount();
				char result[100] = {};
				std::sprintf(result, "%s.%03ld", buffer, (long)(GetTickCount() - first) % 1000); 
				return result;
			}
			#else
			static std::string Timestamp() { return std::string(); }
			#endif
		};
	}
}

#endif
