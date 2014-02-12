//****************************************************
// Fmt - An inline function for inserting formated text
//  Copyright © Rylogic Ltd 2008
//****************************************************
// Note about __declspec(thread):
//  Doesn't work in dll's on windows <= XP
//  In multithreaded situations use Fmt() or a FmtX<> per thread

#pragma once
#ifndef PR_COMMON_FMT_H
#define PR_COMMON_FMT_H

#include <cstdio>
#include <cstdarg>
#include <cassert>
#include <string>
#include <malloc.h>

#ifdef __cplusplus
namespace pr
{
	namespace impl
	{
		// Version independent vsnprintf function
		inline int Format(char* dst, size_t max_count, char const* fmt, va_list arg_list)
		{
			return _vsprintf_p(dst, max_count, fmt, arg_list);
		}
		inline int Format(wchar_t* dst, size_t max_count, wchar_t const* fmt, va_list arg_list)
		{
			return _vswprintf_p(dst, max_count, fmt, arg_list);
		}

		template <typename TString, typename TChar> inline void Format(TString& dst, size_t hint_size, TChar const* fmt, va_list arg_list)
		{
			struct MallocaScope
			{
				TChar* m_ptr;
				size_t m_count;
				MallocaScope(void* mem, size_t count) :m_ptr(static_cast<TChar*>(mem)) ,m_count(count) {}
				~MallocaScope() { _freea(m_ptr); }
			};

			for (;;)
			{
				enum { sane_hint_size_limit = 32 * 1024 * 1024 };
				if (hint_size > sane_hint_size_limit)
					throw std::exception("failed to format string, string too large");

				// Allocate a buffer, preferably on the stack
				MallocaScope buf(_malloca(hint_size * sizeof(TChar)), hint_size);

				// NOTE: In C99 you can only use a va_list once so you need to make a new
				// copy each time. For now, we don't need to
				int result = Format(buf.m_ptr, buf.m_count, fmt, arg_list);

				// It fits, w00t!
				if (result >= 0 && result < int(buf.m_count))
				{
					dst.append(buf.m_ptr, result);
					break;
				}

				// Otherwise, true a larger buffer
				hint_size *= 2;
			}
		}
	}

	// Returns a formated std::string/std::wstring
	inline std::string Fmt(char const* format, ...)
	{
		std::string str;
		va_list arglist;
		va_start(arglist, format);
		impl::Format(str, 1024, format, arglist);
		va_end(arglist);
		return str.c_str();
	}
	inline std::wstring Fmt(wchar_t const* format, ...)
	{
		std::wstring str;
		va_list arglist;
		va_start(arglist, format);
		impl::Format(str, 1024, format, arglist);
		va_end(arglist);
		return str.c_str();
	}
	inline std::string Fmt(size_t hint_size, const char* format, ...)
	{
		std::string str;
		va_list arglist;
		va_start(arglist, format);
		impl::Format(str, hint_size, format, arglist);
		va_end(arglist);
		return str.c_str();
	}
	inline std::wstring Fmt(size_t hint_size, const wchar_t* format, ...)
	{
		std::wstring str;
		va_list arglist;
		va_start(arglist, format);
		impl::Format(str, hint_size, format, arglist);
		va_end(arglist);
		return str.c_str();
	}

	// Returns a formatted std::string/std::wstring where 'func' translates the format codes.
	// Format codes should be '%?' where '?' is up to func to interpret.
	// 'func' should take a pointer into the format string and return something
	// that can be appended to a std::string. 'func' is allowed to advance 's'
	// to the last charactor of the code e.g. "%3.3d foos", 's' = "3.3d...", func can advance 's' to the 'd'
	template <typename TFunc> inline std::string FmtF(char const* format, TFunc func)
	{
		std::string str;
		for (char const* s = format; *s; ++s)
		{
			if (*s != '%')     { str.append(1,*s); continue; }
			if (*(++s) == '%') { str.append(1,'%'); continue; }
			str.append(func(s));
		}
		return str;
	}
	template <typename TFunc> inline std::wstring FmtF(wchar_t const * format, TFunc func)
	{
		std::wstring str;
		for (wchar_t const* s = format; *s; ++s)
		{
			if (*s != L'%')     { str.append(1,*s); continue; }
			if (*(++s) == L'%') { str.append(1,L'%'); continue; }
			str.append(func(s));
		}
		return str;
	}

	// Static, non-thread safe, use with caution, but fast string format
	template <typename Ctx, size_t Sz> inline const char* FmtX(char const* format, va_list arglist)
	{
		static char buf[Sz];
		int result = impl::Format(buf, Sz-1, format, arglist);
		assert(result >= 0 && result < Sz-1 && "formatted string truncated");
		buf[result] = 0;
		return buf;
	}
	template <typename Ctx, size_t Sz> inline const wchar_t* FmtX(wchar_t const* format, va_list arglist)
	{
		static wchar_t buf[Sz];
		int result = impl::Format(buf, Sz-1, format, arglist);
		assert(result >= 0 && result < Sz-1 && "formatted string truncated");
		buf[result] = 0;
		return buf;
	}
	template <typename Ctx, size_t Sz> inline char const* FmtX(char const* format, ...)
	{
		va_list arglist;
		va_start(arglist, format);
		char const* s = FmtX<Ctx, Sz>(format, arglist);
		va_end(arglist);
		return s;
	}
	template <typename Ctx, size_t Sz> inline wchar_t const* FmtX(wchar_t const* format, ...)
	{
		va_list arglist;
		va_start(arglist, format);
		wchar_t const* s = FmtX<Ctx,Sz>(format, arglist);
		va_end(arglist);
		return s;
	}
	inline char const* FmtS(char const* format, ...)
	{
		va_list arglist;
		va_start(arglist, format);
		char const* s = FmtX<void, 1024>(format, arglist);
		va_end(arglist);
		return s;
	}
	inline wchar_t const* FmtS(wchar_t const* format, ...)
	{
		va_list arglist;
		va_start(arglist, format);
		wchar_t const* s = FmtX<void, 1024>(format, arglist);
		va_end(arglist);
		return s;
	}
}
#else
	static __inline char const* Fmt(char* buffer, int length, char const* format, ...)
	{
		va_list arglist;
		va_start(arglist, format);
		vsnprintf(buffer, length, format, arglist);
		buffer[length - 1] = 0;
		va_end(arglist);
		return buffer;
	}

	static __inline char const* FmtS(char const* format, ...)
	{
		static char buffer[256];
		va_list arglist;
		va_start(arglist, format);
		vsnprintf(buffer, sizeof(buffer), format, arglist);
		buffer[sizeof(buffer) - 1] = 0;
		va_end(arglist);
		return buffer;
	}
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_fmt)
		{
			char const* s1 = pr::FmtS("String %d",1);
			PR_CHECK(s1, "String 1");
		}
	}
}
#endif

#endif
