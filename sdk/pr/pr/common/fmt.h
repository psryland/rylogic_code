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
#include <thread>

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#endif

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

		template <typename TString, typename TChar> inline TString& Format(TString& dst, size_t hint_size, TChar const* fmt, va_list arg_list)
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

				// Otherwise, try a larger buffer
				hint_size *= 2;
			}

			return dst;
		}
	}

	// Returns a formatted TString
	template <typename TString, typename TChar> inline TString Fmt(TChar const* format, va_list arg_list)
	{
		TString str;
		return impl::Format(str, 1024, format, arg_list);
	}
	template <typename TString, typename TChar> inline TString Fmt(TChar const* format, ...)
	{
		TString str;
		va_list arg_list;
		va_start(arg_list, format);
		impl::Format(str, 1024, format, arg_list);
		va_end(arg_list);
		return str;
	}

	// Returns a formatted std::string/std::wstring
	template <typename TChar> inline std::basic_string<TChar> Fmt(TChar const* format, ...)
	{
		std::basic_string<TChar> str;
		va_list arg_list;
		va_start(arg_list, format);
		impl::Format(str, 1024, format, arg_list);
		va_end(arg_list);
		return str;
	}
	template <typename TChar> inline std::basic_string<TChar> Fmt(size_t hint_size, TChar const* format, ...)
	{
		std::basic_string<TChar> str;
		va_list arg_list;
		va_start(arg_list, format);
		impl::Format(str, hint_size, format, arg_list);
		va_end(arg_list);
		return str;
	}

	// Returns a formatted std::string/std::wstring where 'func' translates the format codes.
	// Format codes should be '%?' where '?' is up to func to interpret.
	// 'func' should take a pointer into the format string and return something
	// that can be appended to a std::string. 'func' is allowed to advance 's'
	// to the last character of the code e.g. "%3.3d foos", 's' = "3.3d...", func can advance 's' to the 'd'
	template <typename TFunc, typename TChar> inline std::basic_string<TChar> FmtF(TChar const* format, TFunc func)
	{
		std::basic_string<TChar> str;
		TChar const pc('%');
		for (TChar const* s = format; *s; ++s)
		{
			if (*s != pc)     { str.append(1,*s); continue; }
			if (*(++s) == pc) { str.append(1,*s); continue; }
			str.append(func(s));
		}
		return str;
	}

	// Static, non-thread safe, use with caution, but fast string format
	template <typename Ctx, size_t Sz, typename TChar> inline TChar const* FmtX(TChar const* format, va_list arg_list)
	{
		thread_local static TChar buf[Sz];
		int result = impl::Format(buf, Sz-1, format, arg_list);
		assert(result >= 0 && result < Sz-1 && "formatted string truncated");
		buf[result] = 0;
		return buf;
	}
	template <typename Ctx, size_t Sz, typename TChar> inline TChar const* FmtX(TChar const* format, ...)
	{
		va_list arg_list;
		va_start(arg_list, format);
		auto s = FmtX<Ctx, Sz, TChar>(format, arg_list);
		va_end(arg_list);
		return s;
	}
	template <typename TChar> inline TChar const* FmtS(TChar const* format, ...)
	{
		struct S;
		va_list arg_list;
		va_start(arg_list, format);
		auto s = FmtX<S, 1024, TChar>(format, arg_list);
		va_end(arg_list);
		return s;
	}
}
#else
	static __inline char const* Fmt(char* buffer, int length, char const* format, ...)
	{
		va_list arg_list;
		va_start(arg_list, format);
		vsnprintf(buffer, length, format, arg_list);
		buffer[length - 1] = 0;
		va_end(arg_list);
		return buffer;
	}

	static __inline char const* FmtS(char const* format, ...)
	{
		static char buffer[256];
		va_list arg_list;
		va_start(arg_list, format);
		vsnprintf(buffer, sizeof(buffer), format, arg_list);
		buffer[sizeof(buffer) - 1] = 0;
		va_end(arg_list);
		return buffer;
	}
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/prstring.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_fmt)
		{
			char const* s1 = pr::FmtS("String %d",1);
			PR_CHECK(s1, "String 1");

			auto s2 = pr::FmtS(L"wide string %d", 2);
			PR_CHECK(s2, L"wide string 2");

			auto s3 = pr::Fmt("std::string %d", 3);
			PR_CHECK(s3.size(), 13U);

			auto s4 = pr::Fmt<pr::string<>>("pr::string %d",4);
			PR_CHECK(s4, "pr::string 4");
			PR_CHECK(s4.size(), 12U);

			auto s5 = pr::FmtX<struct P, 128>("c-string %d", 5);
			PR_CHECK(s5, "c-string 5");
		}
	}
}
#endif

#endif
