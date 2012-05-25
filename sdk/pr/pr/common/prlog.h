//******************************************************************************************
//
//	List of all the HRESULTs used by me
//
//******************************************************************************************

#ifndef PR_LOG_H
#define PR_LOG_H

#include <stdio.h>
#include <windows.h>
#include "pr/common/assert.h"

// Log file enabler
#ifndef PR_ENABLE_LOGGING
	#ifndef NDEBUG	
		#define PR_ENABLE_LOGGING 1
	#endif//NDEBUG
#endif//PR_ENABLE_LOGGING


namespace pr
{
	namespace log
	{
		#ifdef PR_ENABLE_LOGGING

			inline FILE*& Output() { static FILE* output = stdout; return output; }
			inline void StartFile(const char* filename, const char* mode)
			{
				PR_ASSERT(PR_DBG, Output() == stdout, "Missing EndFile somewhere");
				if( Output() != stdout ) return;
				Output() = fopen(filename, mode);
				if( !Output() ) Output() = stdout;
			}
			inline void EndFile()
			{
				PR_ASSERT(PR_DBG, Output() != stdout, "Missing StartFile somewhere");
				if( Output() != stdout ) fclose(Output());
				Output() = stdout;
			}
			struct AutoFile
			{
				AutoFile(const char* filename, const char* mode)	{ StartFile(filename, mode); }
				~AutoFile()											{ EndFile();				 }
			};
			inline void Error(const char* msg)				{ fprintf(Output(), "ERROR| %s\n", msg); }
			inline void Warn (const char* msg)				{ fprintf(Output(), " WARN| %s\n", msg); }
			inline void Info (const char* msg)				{ fprintf(Output(), " INFO| %s\n", msg); }
			inline void Msg  (const char* msg)				{ fprintf(Output(), "     | %s\n", msg); }

		#else//PR_ENABLE_LOGGING
		
			inline FILE*& Output() { return stdout; }
			inline void StartFile(const char*, const char*) {}
			inline void EndFile()							{}
			struct AutoFile
			{
				AutoFile(const char*, const char*)	{}
				~AutoFile()							{}
			};
			inline void LogError(const char*)		{}
			inline void LogWarn	(const char*)		{}
			inline void LogInfo	(const char*)		{}
			inline void Log		(const char*)		{}
		
		#endif//PR_ENABLE_LOGGING

	}//namespace log
}//namespace pr

#endif//PR_LOG_H
