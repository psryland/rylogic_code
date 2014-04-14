//***********************************************************************
// ToString functions
//  Copyright © Rylogic Ltd 2008
//***********************************************************************

#pragma once
#ifndef PR_STR_TOSTRING_H
#define PR_STR_TOSTRING_H

#include <string>
#include <sstream>
#include <clocale>
#include <locale>
#include <vector>
#include "pr/common/to.h"
#include "pr/str/prstring.h"

namespace pr
{
	// A static instance of the locale, because this thing takes ages to construct
	inline std::locale const& locale()
	{
		static std::locale s_locale("");
		return s_locale;
	}

	// Narrow
	inline std::string Narrow(char const* from, std::size_t len = 0)
	{
		if (len == 0) len = strlen(from);
		return std::string(from, from+len);
	}
	inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
	{
		if (len == 0) len = wcslen(from);
		std::vector<char> buffer(len + 1);
		std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from, from + len, '_', &buffer[0]);
		return std::string(&buffer[0], &buffer[len]);
	}
	template <std::size_t Len> inline std::string Narrow(char const (&from)[Len])    { return Narrow(from, Len); }
	template <std::size_t Len> inline std::string Narrow(wchar_t const (&from)[Len]) { return Narrow(from, Len); }

	// Widen
	inline std::wstring Widen(wchar_t const* from, std::size_t len = 0)
	{
		if (len == 0) len = wcslen(from);
		return std::wstring(from, from+len);
	}
	inline std::wstring Widen(char const* from, std::size_t len = 0)
	{
		if (len == 0) len = strlen(from);
		std::vector<wchar_t> buffer(len + 1);
		std::use_facet<std::ctype<wchar_t>>(locale()).widen(from, from + len, &buffer[0]);
		return std::wstring(&buffer[0], &buffer[len]);
	}
	template <std::size_t Len> inline std::wstring Widen (wchar_t const (&from)[Len]) { return Widen(from, Len); }
	template <std::size_t Len> inline std::wstring Widen (char    const (&from)[Len]) { return Widen(from, Len); }

	namespace impl
	{
		// Convert a signed/unsigned int to a string. buf should be at least 65 characters long
		template <size_t Sz> static char*    itostr (long long from         , char    (&buf)[Sz], int radix) { ::_i64toa_s (from, buf, Sz, radix); return buf; }
		template <size_t Sz> static wchar_t* itostr (long long from         , wchar_t (&buf)[Sz], int radix) { ::_i64tow_s (from, buf, Sz, radix); return buf; }
		template <size_t Sz> static char*    uitostr(unsigned long long from, char    (&buf)[Sz], int radix) { ::_ui64toa_s(from, buf, Sz, radix); return buf; }
		template <size_t Sz> static wchar_t* uitostr(unsigned long long from, wchar_t (&buf)[Sz], int radix) { ::_ui64tow_s(from, buf, Sz, radix); return buf; }
	}

	// To<std::string>
	template <typename TFrom> struct Convert<std::string,TFrom>
	{
		static std::string To(bool from)                           { return from ? "true" : "false"; }
		static std::string To(char from)                           { return std::string(1, from); }
		static std::string To(long long from, int radix)           { char buf[128]; return impl::itostr(from, buf, radix); }
		static std::string To(long long from)                      { return To(from, 10); }
		static std::string To(long from, int radix)                { return To(static_cast<long long>(from), radix); }
		static std::string To(long from)                           { return To(from, 10); }
		static std::string To(int from, int radix)                 { return To(static_cast<long long>(from), radix); }
		static std::string To(int from)                            { return To(from, 10); }
		static std::string To(short from, int radix)               { return To(static_cast<long long>(from), radix); }
		static std::string To(short from)                          { return To(from, 10); }
		static std::string To(unsigned long long from, int radix)  { char buf[128]; return impl::uitostr(from, buf, radix); }
		static std::string To(unsigned long long from)             { return To(from, 10); }
		static std::string To(unsigned long from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static std::string To(unsigned long from)                  { return To(from, 10); }
		static std::string To(unsigned int from, int radix)        { return To(static_cast<unsigned long long>(from), radix); }
		static std::string To(unsigned int from)                   { return To(from, 10); }
		static std::string To(unsigned short from, int radix)      { return To(static_cast<unsigned long long>(from), radix); }
		static std::string To(unsigned short from)                 { return To(from, 10); }
		static std::string To(unsigned char from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static std::string To(unsigned char from)                  { return To(from, 10); }
		static std::string To(long double from)                    { return std::to_string(from); }
		static std::string To(double from)                         { return To(static_cast<long double>(from)); }
		static std::string To(float from)                          { return To(static_cast<long double>(from)); }
		static std::string To(wchar_t const* from)                 { return Narrow(from, wcslen(from)); }
		static std::string To(char const* from)                    { return from; }
		static std::string To(std::string const& from)             { return from; }
		static std::string To(std::wstring const& from)            { return Narrow(from.c_str(), from.size()); }
	};

	// To<std::wstring>
	template <typename TFrom> struct Convert<std::wstring,TFrom>
	{
		static std::wstring To(bool from)                           { return from ? L"true" : L"false"; }
		static std::wstring To(char from)                           { return std::wstring(1, from); }
		static std::wstring To(long long from, int radix)           { wchar_t buf[128]; return impl::itostr(from, buf, radix); }
		static std::wstring To(long long from)                      { return To(from, 10); }
		static std::wstring To(long from, int radix)                { return To(static_cast<long long>(from), radix); }
		static std::wstring To(long from)                           { return To(from, 10); }
		static std::wstring To(int from, int radix)                 { return To(static_cast<long long>(from), radix); }
		static std::wstring To(int from)                            { return To(from, 10); }
		static std::wstring To(short from, int radix)               { return To(static_cast<long long>(from), radix); }
		static std::wstring To(short from)                          { return To(from, 10); }
		static std::wstring To(unsigned long long from, int radix)  { wchar_t buf[128]; return impl::uitostr(from, buf, radix); }
		static std::wstring To(unsigned long long from)             { return To(from, 10); }
		static std::wstring To(unsigned long from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static std::wstring To(unsigned long from)                  { return To(from, 10); }
		static std::wstring To(unsigned int from, int radix)        { return To(static_cast<unsigned long long>(from), radix); }
		static std::wstring To(unsigned int from)                   { return To(from, 10); }
		static std::wstring To(unsigned short from, int radix)      { return To(static_cast<unsigned long long>(from), radix); }
		static std::wstring To(unsigned short from)                 { return To(from, 10); }
		static std::wstring To(unsigned char from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static std::wstring To(unsigned char from)                  { return To(from, 10); }
		static std::wstring To(long double from)                    { return std::to_wstring(from); }
		static std::wstring To(double from)                         { return To(static_cast<long double>(from)); }
		static std::wstring To(float from)                          { return To(static_cast<long double>(from)); }
		static std::wstring To(wchar_t const* from)                 { return from; }
		static std::wstring To(char const* from)                    { return Widen(from, strlen(from)); }
		static std::wstring To(std::string const& from)             { return Widen(from.c_str(), from.size()); }
		static std::wstring To(std::wstring const& from)            { return from; }
	};

	// To<pr::string<char>>
	template <typename TFrom, int LocalCount, bool Fixed, typename Allocator>
	struct Convert<pr::string<char,LocalCount,Fixed,Allocator>, TFrom>
	{
	private:
		typedef pr::string<char,LocalCount,Fixed,Allocator> pr_string;

	public:
		static pr_string To(bool from)                           { return from ? "true" : "false"; }
		static pr_string To(char from)                           { return pr_string(1, from); }
		static pr_string To(long long from, int radix)           { char buf[128]; return impl::itostr(from, buf, radix); }
		static pr_string To(long long from)                      { return To(from, 10); }
		static pr_string To(long from, int radix)                { return To(static_cast<long long>(from), radix); }
		static pr_string To(long from)                           { return To(from, 10); }
		static pr_string To(int from, int radix)                 { return To(static_cast<long long>(from), radix); }
		static pr_string To(int from)                            { return To(from, 10); }
		static pr_string To(short from, int radix)               { return To(static_cast<long long>(from), radix); }
		static pr_string To(short from)                          { return To(from, 10); }
		static pr_string To(unsigned long long from, int radix)  { char buf[128]; return impl::uitostr(from, buf, radix); }
		static pr_string To(unsigned long long from)             { return To(from, 10); }
		static pr_string To(unsigned long from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned long from)                  { return To(from, 10); }
		static pr_string To(unsigned int from, int radix)        { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned int from)                   { return To(from, 10); }
		static pr_string To(unsigned short from, int radix)      { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned short from)                 { return To(from, 10); }
		static pr_string To(unsigned char from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned char from)                  { return To(from, 10); }
		static pr_string To(long double from)                    { return std::to_string(from); }
		static pr_string To(double from)                         { return To(static_cast<long double>(from)); }
		static pr_string To(float from)                          { return To(static_cast<long double>(from)); }
		static pr_string To(wchar_t const* from)                 { return Narrow(from, wcslen(from)); }
		static pr_string To(char const* from)                    { return from; }
		static pr_string To(std::string const& from)             { return from; }
		static pr_string To(std::wstring const& from)            { return Narrow(from.c_str(), from.size()); }
	};

	// To<pr::string<wchar_t>>
	template <typename TFrom, int LocalCount, bool Fixed, typename Allocator>
	struct Convert<pr::string<wchar_t,LocalCount,Fixed,Allocator>, TFrom>
	{
	private:
		typedef pr::string<wchar_t,LocalCount,Fixed,Allocator> pr_string;

	public:
		static pr_string To(bool from)                           { return from ? L"true" : L"false"; }
		static pr_string To(wchar_t from)                        { return pr_string(1, from); }
		static pr_string To(long long from, int radix)           { wchar_t buf[128]; return impl::itostr(from, buf, radix); }
		static pr_string To(long long from)                      { return To(from, 10); }
		static pr_string To(long from, int radix)                { return To(static_cast<long long>(from), radix); }
		static pr_string To(long from)                           { return To(from, 10); }
		static pr_string To(int from, int radix)                 { return To(static_cast<long long>(from), radix); }
		static pr_string To(int from)                            { return To(from, 10); }
		static pr_string To(short from, int radix)               { return To(static_cast<long long>(from), radix); }
		static pr_string To(short from)                          { return To(from, 10); }
		static pr_string To(unsigned long long from, int radix)  { wchar_t buf[128]; return impl::uitostr(from, buf, radix); }
		static pr_string To(unsigned long long from)             { return To(from, 10); }
		static pr_string To(unsigned long from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned long from)                  { return To(from, 10); }
		static pr_string To(unsigned int from, int radix)        { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned int from)                   { return To(from, 10); }
		static pr_string To(unsigned short from, int radix)      { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned short from)                 { return To(from, 10); }
		static pr_string To(unsigned char from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
		static pr_string To(unsigned char from)                  { return To(from, 10); }
		static pr_string To(long double from)                    { return std::to_wstring(from); }
		static pr_string To(double from)                         { return To(static_cast<long double>(from)); }
		static pr_string To(float from)                          { return To(static_cast<long double>(from)); }
		static pr_string To(wchar_t const* from)                 { return from; }
		static pr_string To(char const* from)                    { return Widen(from, strlen(from)); }
		static pr_string To(std::string const& from)             { return Widen(from.c_str(), from.size()); }
		static pr_string To(std::wstring const& from)            { return from; }
	};

	// To<int>
	template <typename TFrom> struct Convert<int,TFrom>
	{
		static int To(char const* from, int radix)               { return static_cast<int>(::strtol(from, 0, radix)); }
		static int To(wchar_t const* from, int radix)            { return static_cast<int>(::wcstol(from, 0, radix)); }
		static int To(std::string const& from, int radix)        { return static_cast<int>(::strtol(from.c_str(), 0, radix)); }
		static int To(std::wstring const& from, int radix)       { return static_cast<int>(::wcstol(from.c_str(), 0, radix)); }
	};

	// To<size_t>
	template <typename TFrom> struct Convert<size_t,TFrom>
	{
		static int To(char const* from, int radix)               { return static_cast<size_t>(::strtoul(from, 0, radix)); }
		static int To(wchar_t const* from, int radix)            { return static_cast<size_t>(::wcstoul(from, 0, radix)); }
		static int To(std::string const& from, int radix)        { return static_cast<size_t>(::strtoul(from.c_str(), 0, radix)); }
		static int To(std::wstring const& from, int radix)       { return static_cast<size_t>(::wcstoul(from.c_str(), 0, radix)); }
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_str_tostring)
		{
			char         narr[] =  "junk_str_junk";
			wchar_t      wide[] = L"junk_str_junk";
			std::string  cstr   =  "junk_str_junk";
			std::wstring wstr   = L"junk_str_junk";
			pr::string<> pstr   =  "junk_str_junk";

			PR_CHECK(pr::To<std::wstring>(narr), wstr);
			PR_CHECK(pr::To<std::wstring>(wide), wstr);
			PR_CHECK(pr::To<std::wstring>(cstr), wstr);
			PR_CHECK(pr::To<std::wstring>(wstr), wstr);
			PR_CHECK(pr::To<std::wstring>(pstr), wstr);

			PR_CHECK(pr::To<std::string>(narr), cstr);
			PR_CHECK(pr::To<std::string>(wide), cstr);
			PR_CHECK(pr::To<std::string>(cstr), cstr);
			PR_CHECK(pr::To<std::string>(wstr), cstr);
			PR_CHECK(pr::To<std::string>(pstr), cstr);

			PR_CHECK(pr::To<int>("1234",10), 1234);
			PR_CHECK(pr::To<int>(L"1234",10), 1234);
		}
	}
}
#endif
#endif
