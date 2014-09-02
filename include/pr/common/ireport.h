//*******************************************************************
//
//	IReport
//
//*******************************************************************
// An interface for reporting errors, warnings, messages, asserts
#ifndef PR_COMMON_IREPORT_H
#define PR_COMMON_IREPORT_H

#include "pr/common/assert.h"

namespace pr
{
	struct IReport
	{
		virtual ~IReport() {}
		virtual void Error  (const char* msg) const = 0;
		virtual void Warn   (const char* msg) const = 0;
		virtual void Message(const char* msg) const = 0;
		virtual void Assert (const char* msg) const = 0;
	};

	// Helpers *********************************************************

	struct DefaultReport : IReport
	{
		void Error  (const char* msg) const { msg; PR_ASSERT(PR_DBG, false, msg); }
		void Warn   (const char* msg) const { msg; PR_INFO     (PR_DBG, msg); }
		void Message(const char* msg) const	{ msg; PR_INFO     (PR_DBG, msg); }
		void Assert (const char* msg) const { msg; PR_ASSERT(PR_DBG, false, msg); }
	};
}//namespace pr

#endif//PR_COMMON_IREPORT_H