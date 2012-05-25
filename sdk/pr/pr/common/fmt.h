//****************************************************
// Fmt - An inline function for inserting formated text
//****************************************************
// Note about __declspec(thread):
//  Doesn't work in dll's on windows <= XP
//  In multithreaded situations use Fmt() or a FmtX<> per thread

#pragma once
#ifndef PR_FMT_H
#define PR_FMT_H

#include <stdio.h>
#include <stdarg.h>
#include <string>

#ifndef PR_ASSERT
#	define PR_ASSERT_DEFINED
#	define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	// Version independent vsnprintf function
	inline int vararg_printf(char* dst, size_t dst_size, size_t max_count, char const* format_string, va_list arg_list)
	{
		(void)dst_size;
		return _vsprintf_p(dst, max_count, format_string, arg_list);
		//#if _MSC_VER >= 1400
		//return vsnprintf_s(dst, dst_size, max_count, format_string, arg_list);
		//#elif _MSC_VER < 1400
		//(void)dst_size;
		//return _vsnprintf(dst, max_count, format_string, arg_list);
		//#endif
	}
	inline int vararg_printf(wchar_t* dst, size_t dst_size, size_t max_count, wchar_t const* format_string, va_list arg_list)
	{
		(void)dst_size;
		return _vswprintf_p(dst, max_count, format_string, arg_list);
		//#if _MSC_VER >= 1400
		//(void)dst_size;
		//return _vsnwprintf_s(dst, dst_size, max_count, format_string, arg_list);
		//#elif MSC_VER < 1400
		//(void)dst_size;
		//return _vsnwprintf(dst, max_count, format_string, arg_list);
		//#endif
	}

	// Stick a formatted string in place of a normal string
	inline std::string Fmt(const char* format, ...)
	{
		std::string str(1024, 0);
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(&str[0], 1024, 1023, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		va_end(arglist);
		return str.c_str();
	}
	inline std::string Fmt(size_t msg_buffer_size, const char* format, ...)
	{
		std::string str(msg_buffer_size, 0);
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(&str[0], msg_buffer_size, msg_buffer_size - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		va_end(arglist);
		return str.c_str();
	}
	template <typename Ctx> inline const char* FmtX(const char* format, ...)
	{
		const size_t MaxMsgLength = 1024;
		static char buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}
	inline const char* FmtS(const char* format, ...)
	{
		const size_t MaxMsgLength = 1024;
		static char buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}
	template <int MaxMsgLength> inline const char* FmtS(const char* format, ...)
	{
		static char buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}

	// Wide char versions
	inline std::wstring Fmt(const wchar_t* format, ...)
	{
		std::wstring str(1024, 0);
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(&str[0], 1024, 1023, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		va_end(arglist);
		return str.c_str();
	}
	inline std::wstring Fmt(size_t msg_buffer_size, const wchar_t* format, ...)
	{
		std::wstring str(msg_buffer_size, 0);
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(&str[0], msg_buffer_size, msg_buffer_size - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		va_end(arglist);
		return str.c_str();
	}
	template <typename Ctx> inline const wchar_t* FmtX(const wchar_t* format, ...)
	{
		const size_t MaxMsgLength = 1024;
		static wchar_t buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}
	inline const wchar_t* FmtS(const wchar_t* format, ...)
	{
		const size_t MaxMsgLength = 1024;
		static wchar_t buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}
	template <int MaxMsgLength> inline const wchar_t* FmtS(const wchar_t* format, ...)
	{
		static wchar_t buffer[MaxMsgLength];
		va_list arglist;
		va_start(arglist, format);
		if (vararg_printf(buffer, MaxMsgLength, MaxMsgLength - 1, format, arglist) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
		buffer[MaxMsgLength - 1] = 0;
		va_end(arglist);
		return buffer;
	}
}

#ifdef PR_ASSERT_DEFINED
#	undef PR_ASSERT_DEFINED
#	undef PR_ASSERT
#endif

#endif//PR_FMT_H
