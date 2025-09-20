//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include <type_traits>

#ifndef PR_PIX_ENABLED
#define PR_PIX_ENABLED 0
#endif

#if PR_PIX_ENABLED
#include <windows.h>
#include <pix3.h>
#endif

namespace pr::rdr12::pix
{
	// Return the PIX dll module handle if it can be loaded
	HMODULE LoadDll();

	inline bool IsAttachedForGpuCapture()
	{
		#if PR_PIX_ENABLED
		return PIXIsAttachedForGpuCapture();
		#else
		return false;
		#endif
	}

	inline void BeginCapture(wchar_t const* wpix_filepath)
	{
		#if PR_PIX_ENABLED
		PIXCaptureParameters parameters = { .GpuCaptureParameters = {.FileName = wpix_filepath} };
		PIXBeginCapture2(PIX_CAPTURE_GPU, &parameters);
		#else
		(void)wpix_filepath;
		#endif
	}

	inline void EndCapture()
	{
		#if PR_PIX_ENABLED
		PIXEndCapture(FALSE);
		#endif
	}

	template<typename CONTEXT, typename... ARGS>
	inline void BeginEvent(CONTEXT* context, unsigned long colour, char const* formatString, ARGS... args)
	{
		#if PR_PIX_ENABLED
		PIXBeginEvent(context, colour, formatString, args...);
		#else
		(void)context, colour, formatString;
		#endif
	}

	template<typename CONTEXT>
	inline void EndEvent(CONTEXT* context)
	{
		#if PR_PIX_ENABLED
		PIXEndEvent(context);
		#else
		(void)context;
		#endif
	}

	struct CaptureScope
	{
		bool m_active;

		CaptureScope(wchar_t const* wpix_filepath, bool active)
			: m_active(active)
		{
			if (m_active) BeginCapture(wpix_filepath);
		}
		CaptureScope(CaptureScope&&) = delete;
		CaptureScope(CaptureScope const&) = delete;
		CaptureScope& operator=(CaptureScope&&) = delete;
		CaptureScope& operator=(CaptureScope const&) = delete;
		~CaptureScope()
		{
			if (m_active) EndCapture();
		}
	};

	template<typename CONTEXT>
	struct EventScope
	{
		CONTEXT* m_context;

		template<typename... ARGS>
		EventScope(CONTEXT* context, unsigned long colour, char const* format_string, ARGS... args)
			: m_context(context)
		{
			BeginEvent(context, colour, format_string, std::forward<ARGS>(args)...);
		}
		EventScope(EventScope&&) = delete;
		EventScope(EventScope const&) = delete;
		EventScope& operator=(EventScope&&) = delete;
		EventScope& operator=(EventScope const&) = delete;
		~EventScope() { EndEvent(m_context); }
	};
}
