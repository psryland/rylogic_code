//***********************************************************************
// ToString functions
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************

#pragma once

#include <string>
#include <sstream>
#include <clocale>
#include <cstdlib>
#include <locale>
#include <vector>
#include "pr/common/to.h"
#include "pr/str/string_core.h"
#include "pr/str/string.h"

namespace pr
{
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
		typedef pr::string<wchar_t,LocalCount,Fixed,Allocator> pr_string;
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

	// To<Intg>
	template <typename TTo, typename TFrom> struct ConvertIntg
	{
		static TTo To(char const* from, int radix)               { return static_cast<TTo>(::strtol(from, nullptr, radix) & ~TTo()); }
		static TTo To(wchar_t const* from, int radix)            { return static_cast<TTo>(::wcstol(from, nullptr, radix) & ~TTo()); }
		static TTo To(std::string const& from, int radix)        { return static_cast<TTo>(::strtol(from.c_str(), nullptr, radix) & ~TTo()); }
		static TTo To(std::wstring const& from, int radix)       { return static_cast<TTo>(::wcstol(from.c_str(), nullptr, radix) & ~TTo()); }

		static TTo To(char const* from, char** end, int radix)            { return static_cast<unsigned short>(::strtol(from, end, radix) & ~TTo()); }
		static TTo To(wchar_t const* from, wchar_t** end, int radix)      { return static_cast<unsigned short>(::wcstol(from, end, radix) & ~TTo()); }
		static TTo To(std::string const& from, char** end, int radix)     { return static_cast<unsigned short>(::strtol(from.c_str(), end, radix) & ~TTo()); }
		static TTo To(std::wstring const& from, wchar_t** end, int radix) { return static_cast<unsigned short>(::wcstol(from.c_str(), end, radix) & ~TTo()); }
	};
	template <typename TFrom> struct Convert<char ,TFrom> :ConvertIntg<char , TFrom> {};
	template <typename TFrom> struct Convert<short,TFrom> :ConvertIntg<short, TFrom> {};
	template <typename TFrom> struct Convert<int  ,TFrom> :ConvertIntg<int  , TFrom> {};

	// To<UIntg>
	template <typename TTo, typename TFrom> struct ConvertUIntg
	{
		static TTo To(char const* from, int radix)               { return static_cast<TTo>(::strtoul(from, nullptr, radix) & ~TTo()); }
		static TTo To(wchar_t const* from, int radix)            { return static_cast<TTo>(::wcstoul(from, nullptr, radix) & ~TTo()); }
		static TTo To(std::string const& from, int radix)        { return static_cast<TTo>(::strtoul(from.c_str(), nullptr, radix) & ~TTo()); }
		static TTo To(std::wstring const& from, int radix)       { return static_cast<TTo>(::wcstoul(from.c_str(), nullptr, radix) & ~TTo()); }

		static TTo To(char const* from, char** end, int radix)            { return static_cast<unsigned short>(::strtoul(from, end, radix) & ~TTo()); }
		static TTo To(wchar_t const* from, wchar_t** end, int radix)      { return static_cast<unsigned short>(::wcstoul(from, end, radix) & ~TTo()); }
		static TTo To(std::string const& from, char** end, int radix)     { return static_cast<unsigned short>(::strtoul(from.c_str(), end, radix) & ~TTo()); }
		static TTo To(std::wstring const& from, wchar_t** end, int radix) { return static_cast<unsigned short>(::wcstoul(from.c_str(), end, radix) & ~TTo()); }
	};
	template <typename TFrom> struct Convert<unsigned char ,TFrom> :ConvertUIntg<unsigned char , TFrom> {};
	template <typename TFrom> struct Convert<unsigned short,TFrom> :ConvertUIntg<unsigned short, TFrom> {};
	template <typename TFrom> struct Convert<unsigned int  ,TFrom> :ConvertUIntg<unsigned int  , TFrom> {};

	// To<long long>
	template <typename TFrom> struct Convert<long long, TFrom>
	{
		static long long To(char const* from, int radix)               { return ::strtoll(from, nullptr, radix); }
		static long long To(wchar_t const* from, int radix)            { return ::wcstoll(from, nullptr, radix); }
		static long long To(std::string const& from, int radix)        { return ::strtoll(from.c_str(), nullptr, radix); }
		static long long To(std::wstring const& from, int radix)       { return ::wcstoll(from.c_str(), nullptr, radix); }

		static long long To(char const* from, char** end, int radix)            { return ::strtoll(from, end, radix); }
		static long long To(wchar_t const* from, wchar_t** end, int radix)      { return ::wcstoll(from, end, radix); }
		static long long To(std::string const& from, char** end, int radix)     { return ::strtoll(from.c_str(), end, radix); }
		static long long To(std::wstring const& from, wchar_t** end, int radix) { return ::wcstoll(from.c_str(), end, radix); }
	};

	// To<unsigned long long>
	template <typename TFrom> struct Convert<unsigned long long, TFrom>
	{
		static unsigned long long To(char const* from, int radix)         { return ::strtoull(from, nullptr, radix); }
		static unsigned long long To(wchar_t const* from, int radix)      { return ::wcstoull(from, nullptr, radix); }
		static unsigned long long To(std::string const& from, int radix)  { return ::strtoull(from.c_str(), nullptr, radix); }
		static unsigned long long To(std::wstring const& from, int radix) { return ::wcstoull(from.c_str(), nullptr, radix); }

		static unsigned long long To(char const* from, char** end, int radix)            { return ::strtoull(from, end, radix); }
		static unsigned long long To(wchar_t const* from, wchar_t** end, int radix)      { return ::wcstoull(from, end, radix); }
		static unsigned long long To(std::string const& from, char** end, int radix)     { return ::strtoull(from.c_str(), end, radix); }
		static unsigned long long To(std::wstring const& from, wchar_t** end, int radix) { return ::wcstoull(from.c_str(), end, radix); }
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

			PR_CHECK(pr::To<unsigned short>("12345",16), (unsigned short)0x2345);
			PR_CHECK(pr::To<char>(L"1",10), (char)1);

			PR_CHECK(pr::To<int>("1234",10), 1234);
			PR_CHECK(pr::To<int>(L"1234",10), 1234);
		}
	}
}
#endif
