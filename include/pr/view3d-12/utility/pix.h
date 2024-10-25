//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once

#ifndef PR_PIX_ENABLED
#define PR_PIX_ENABLED 0
#endif

#if PR_PIX_ENABLED
#include <windows.h>
#include <pix3.h>
#endif

namespace pr::rdr12::pix
{
	inline bool IsAttachedForGpuCapture()
	{
		#if PR_PIX_ENABLED
		return PIXIsAttachedForGpuCapture();
		#else
		return false;
		#endif
	}

	template<typename CONTEXT, typename... ARGS>
	void BeginEvent(CONTEXT* context, unsigned long colour, char const* formatString, ARGS... args)
	{
		#if PR_PIX_ENABLED
		PIXBeginEvent(context, colour, formatString, args...);
		#else
		(void)context, colour, formatString;
		#endif
	}

	template<typename CONTEXT>
	void EndEvent(CONTEXT* context)
	{
		#if PR_PIX_ENABLED
		PIXEndEvent(context);
		#else
		(void)context;
		#endif
	}
}
