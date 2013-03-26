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
#ifndef PR_COMMON_LOG_H
#define PR_COMMON_LOG_H

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
#include <windows.h>
#else
#include <sys/time.h>
#endif
#include <iostream>
#include <sstream>
#include <string>
#include <stdio.h>

namespace pr
{
	namespace log
	{
		#ifdef PR_LOGGING
		#define PR_LOG(level, message)          pr::log::Log.Write(pr::log::Level::level, __FILE__, __LINE__, message, 0)
		#define PR_LOGE(level, except, message) pr::log::Log.Write(pr::log::Level::level, __FILE__, __LINE__, message, &except)
		#else
		#define PR_LOG(level, message)
		#define PR_LOGE(level, except, message)
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
			// The file descriptor to send log statements to
			static FILE*& Out()
			{
				static FILE* out = stderr;
				return out;
			}

			static Level::Type& Level()
			{
				static Level::Type s_level = Level::Info;
				return s_level;
			}

			// Write to the log
			static void Write(Level::Type level, char const* file, size_t line, char const* message, std::exception const* e)
			{
				auto out = Out();
				if (!out || level < Level()) return;
				
				(void)file;
				(void)line;
				fprintf(out, "(%s) [%s] %s\n", Timestamp().c_str(), Level::ToString(level), message);
				if (e) fprintf(out, "  Exception Message: %s\n", e->what());
				fflush(out);
			}

			// Returns the current timestamp
			static std::string Timestamp()
			{
				#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
				char buffer[200] = {};
				if (GetTimeFormatA(LOCALE_USER_DEFAULT, 0, 0, "HH':'mm':'ss", buffer, sizeof(buffer)) == 0) return "";
				
				static DWORD first = GetTickCount();
				char result[100] = {};
				std::sprintf(result, "%s.%03ld", buffer, (long)(GetTickCount() - first) % 1000); 
				return result;
				#else
				char buffer[11];
				time_t t;
				time(&t);
				tm r = {0};
				strftime(buffer, sizeof(buffer), "%X", localtime_r(&t, &r));
				struct timeval tv;
				gettimeofday(&tv, 0);
				char result[100] = {0};
				std::sprintf(result, "%s.%03ld", buffer, (long)tv.tv_usec / 1000); 
				return result;
				#endif
			}
		};
	}
}

#endif
