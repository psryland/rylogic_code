//***********************************************************************
// ToString functions
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************

#pragma once

#include <string>
#include <sstream>
#include <clocale>
#include <cstdio>
#include <cstdlib>
#include <locale>
#include <vector>
#include "pr/common/to.h"
#include "pr/common/hresult.h"
#include "pr/str/string.h"

namespace pr
{
	namespace convert
	{
		// Convert a signed/unsigned int to a string. 'buf' should be at least 65 characters long
		inline char*    itostr (long long from         , char*    buf, int count, int radix)
		{
			if (::_i64toa_s (from, buf, count, radix) == 0) return buf;
			throw std::exception("conversion from integral value to string failed");
		}
		inline wchar_t* itostr (long long from         , wchar_t* buf, int count, int radix)
		{
			if (::_i64tow_s (from, buf, count, radix) == 0) return buf;
			throw std::exception("conversion from integral value to string failed");
		}
		inline char*    uitostr(unsigned long long from, char*    buf, int count, int radix)
		{
			if (::_ui64toa_s(from, buf, count, radix) == 0) return buf;
			throw std::exception("conversion from unsigned integral value to string failed");
		}
		inline wchar_t* uitostr(unsigned long long from, wchar_t* buf, int count, int radix)
		{
			if (::_ui64tow_s(from, buf, count, radix) == 0) return buf;
			throw std::exception("conversion from unsigned integral value to string failed");
		}
		inline char*    dtostr (double from, char*    buf, int count)
		{
			// Have to use %g otherwise converting to a string loses precision
			auto n = ::_snprintf (buf, count,  "%g", from);
			if (n <= 0) throw std::exception("conversion from floating point value to string failed");
			if (n >= count) throw std::exception("conversion from floating point value to string was truncated");
			buf[count-1] = 0;
			return buf;
		}
		inline wchar_t* dtostr (double from, wchar_t* buf, int count)
		{
			// Have to use %g otherwise converting to a string loses precision
			auto n = ::_snwprintf(buf, count, L"%g", from);
			if (n <= 0) throw std::exception("conversion from floating point value to string failed");
			if (n >= count) throw std::exception("conversion from floating point value to string was truncated");
			buf[count-1] = 0;
			return buf;
		}

		// To string
		template <typename Str, typename Char = typename Str::value_type>
		struct BasicToString
		{
			static Str To(Char const* from)                    { return from; }
			static Str To(bool from)                           { return from ? PR_STRLITERAL(Char, "true") : PR_STRLITERAL(Char, "false"); }
			static Str To(char from)                           { return Str(1, from); }
			static Str To(long long from, int radix)           { Char buf[128]; return itostr(from, buf, _countof(buf), radix); }
			static Str To(unsigned long long from, int radix)  { Char buf[128]; return uitostr(from, buf, _countof(buf), radix); }
			static Str To(long long from)                      { return To(from, 10); }
			static Str To(long from, int radix)                { return To(static_cast<long long>(from), radix); }
			static Str To(long from)                           { return To(from, 10); }
			static Str To(int from, int radix)                 { return To(static_cast<long long>(from), radix); }
			static Str To(int from)                            { return To(from, 10); }
			static Str To(short from, int radix)               { return To(static_cast<long long>(from), radix); }
			static Str To(short from)                          { return To(from, 10); }
			static Str To(unsigned long long from)             { return To(from, 10); }
			static Str To(unsigned long from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
			static Str To(unsigned long from)                  { return To(from, 10); }
			static Str To(unsigned int from, int radix)        { return To(static_cast<unsigned long long>(from), radix); }
			static Str To(unsigned int from)                   { return To(from, 10); }
			static Str To(unsigned short from, int radix)      { return To(static_cast<unsigned long long>(from), radix); }
			static Str To(unsigned short from)                 { return To(from, 10); }
			static Str To(unsigned char from, int radix)       { return To(static_cast<unsigned long long>(from), radix); }
			static Str To(unsigned char from)                  { return To(from, 10); }
			static Str To(double from)                         { Char buf[128]; return dtostr(from, buf, _countof(buf)); }
			static Str To(float from)                          { return To(static_cast<double>(from)); }
			static Str To(long double from)                    { std::basic_stringstream<Char> ss; ss << from; return ss.str().c_str(); } // careful with long double, it's non-standard
			
			// Narrow/Widen raw string types
			template <typename = enable_if<is_same<Char, char>::value>>
			static Str To(wchar_t const* from)
			{
				return Narrow(from, wcslen(from));
			}
			template <typename = enable_if<is_same<Char, wchar_t>::value>>
			static Str To(char const* from)
			{
				return Widen(from, strlen(from));
			}

			// Convert between strings with the same character type
			template <typename Str2, typename Char2 = Str2::value_type, typename = enable_if<is_same<Char,Char2>::value>>
			static Str To(Str2 const& from)
			{
				return Str(std::begin(from), std::end(from));
			}

			// Convert between strings with different underlying character types
			template <typename Str2, typename Char2 = Str2::value_type>
			static Str To(Str2 const& from, enable_if<is_same<Char,char>::value && is_same<Char2,wchar_t>::value> = 0)
			{
				return Narrow(from.c_str(), from.size());
			}
			template <typename Str2, typename Char2 = Str2::value_type>
			static Str To(Str2 const& from, enable_if<is_same<Char,wchar_t>::value && is_same<Char2,char>::value> = 0)
			{
				return Widen(from.c_str(), from.size());
			}
		};

		// Integral
		template <typename Ty, bool Signed = std::is_signed<Ty>::value, bool I32 = sizeof(Ty) <= sizeof(long)>
		struct ToIntegral
		{
			// Convert from raw strings to integral types
			template <typename = enable_if<Signed && I32>> static Ty To(char const* from, int radix = 10, char** end = nullptr, DummyType<1> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::strtol(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<Signed && !I32>> static Ty To(char const* from, int radix = 10, char** end = nullptr, DummyType<2> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::strtoll(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<!Signed && I32>> static Ty To(char const* from, int radix = 10, char** end = nullptr, DummyType<3> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::strtoul(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<!Signed && !I32>> static Ty To(char const* from, int radix = 10, char** end = nullptr, DummyType<4> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::strtoull(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<Signed && I32>> static Ty To(wchar_t const* from, int radix = 10, wchar_t** end = nullptr, DummyType<1> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::wcstol(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<Signed && !I32>> static Ty To(wchar_t const* from, int radix = 10, wchar_t** end = nullptr, DummyType<2> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::wcstoll(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<!Signed && I32>> static Ty To(wchar_t const* from, int radix = 10, wchar_t** end = nullptr, DummyType<3> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::wcstoul(from, end, radix) & ~Ty()));
			}
			template <typename = enable_if<!Signed && !I32>> static Ty To(wchar_t const* from, int radix = 10, wchar_t** end = nullptr, DummyType<4> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::wcstoull(from, end, radix) & ~Ty()));
			}

			// String class to integral
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static Ty To(Str const& s, int radix = 10, Char** end = nullptr)
			{
				return To(s.c_str(), radix, end);
			}
		};

		// Floating point
		template <typename Ty, bool F64 = sizeof(Ty) <= sizeof(double)>
		struct ToFloatingPoint
		{
			// Convert raw string to floating point
			template <typename = enable_if<F64>> static Ty To(char const* s, char** end = nullptr, DummyType<1> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(strtod(s, end)));
			}
			template <typename = enable_if<F64>> static Ty To(wchar_t const* s, wchar_t** end = nullptr, DummyType<1> = 0)
			{
				errno = 0;
				return CheckErrno(static_cast<Ty>(::wcstod(s, end)));
			}

			// String class to floating point
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static Ty To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
		};
	}
	template <typename TFrom, typename Char>                struct Convert<std::basic_string<Char>, TFrom> :convert::BasicToString<std::basic_string<Char>> {};
	template <typename TFrom, typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    TFrom> :convert::BasicToString<pr::string<Char,L,F>> {};

	template <typename TFrom> struct Convert<char                , TFrom> :convert::ToIntegral<char                > {};
	template <typename TFrom> struct Convert<wchar_t             , TFrom> :convert::ToIntegral<wchar_t             > {};
	template <typename TFrom> struct Convert<short               , TFrom> :convert::ToIntegral<short               > {};
	template <typename TFrom> struct Convert<int                 , TFrom> :convert::ToIntegral<int                 > {};
	template <typename TFrom> struct Convert<long                , TFrom> :convert::ToIntegral<long                > {};
	template <typename TFrom> struct Convert<long long           , TFrom> :convert::ToIntegral<long long           > {};
	template <typename TFrom> struct Convert<unsigned char       , TFrom> :convert::ToIntegral<unsigned char       > {};
	template <typename TFrom> struct Convert<unsigned short      , TFrom> :convert::ToIntegral<unsigned short      > {};
	template <typename TFrom> struct Convert<unsigned int        , TFrom> :convert::ToIntegral<unsigned int        > {};
	template <typename TFrom> struct Convert<unsigned long       , TFrom> :convert::ToIntegral<unsigned long       > {};
	template <typename TFrom> struct Convert<unsigned long long  , TFrom> :convert::ToIntegral<unsigned long long  > {};

	template <typename TFrom> struct Convert<float      , TFrom> :convert::ToFloatingPoint<float      > {};
	template <typename TFrom> struct Convert<double     , TFrom> :convert::ToFloatingPoint<double     > {};
	template <typename TFrom> struct Convert<long double, TFrom> :convert::ToFloatingPoint<long double> {};

	// Convert an integer to a string of 0s and 1s
	template <typename Str, typename Int, typename Char = Str::value_type, typename = enable_if<std::is_integral<Int>::value>>
	inline Str ToBinary(Int n)
	{
		int const bits = sizeof(Int) * 8;
		Str str; str.reserve(bits);
		for (int i = bits; i-- != 0;)
			str.push_back((n & Bit64(i)) ? '1' : '0');
		return str;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::str
{
	PRUnitTest(ToStringTests)
	{
		using std_cstr = std::string;
		using std_wstr = std::wstring;
		using pr_cstr  = pr::string<char>;
		using pr_wstr  = pr::string<wchar_t>;

		char     narr[] =  "junk_str_junk";
		wchar_t  wide[] = L"junk_str_junk";
		std_cstr scstr  =  "junk_str_junk";
		std_wstr swstr  = L"junk_str_junk";
		pr_cstr  pcstr  =  "junk_str_junk";
		pr_wstr  pwstr  = L"junk_str_junk";
		std_cstr tau    = "6.28";

		PR_CHECK(pr::To<std::string>(narr), scstr);
		PR_CHECK(pr::To<std::string>(wide), scstr);
		PR_CHECK(pr::To<std::string>(scstr), scstr);
		PR_CHECK(pr::To<std::string>(swstr), scstr);
		PR_CHECK(pr::To<std::string>(pcstr), scstr);
		PR_CHECK(pr::To<std::string>(pwstr), scstr);

		PR_CHECK(pr::To<std::wstring>(narr), swstr);
		PR_CHECK(pr::To<std::wstring>(wide), swstr);
		PR_CHECK(pr::To<std::wstring>(scstr), swstr);
		PR_CHECK(pr::To<std::wstring>(swstr), swstr);
		PR_CHECK(pr::To<std::wstring>(pcstr), swstr);
		PR_CHECK(pr::To<std::wstring>(pwstr), swstr);

		PR_CHECK(pr::To<std::string>(3.14), "3.14");
		PR_CHECK(pr::To<std::wstring>(42), L"42");
		PR_CHECK(pr::To<std_cstr>("literal cstr"), "literal cstr");
		PR_CHECK(pr::To<std_wstr>("literal cstr"), L"literal cstr");
		PR_CHECK(pr::To<pr_cstr>("literal cstr"), "literal cstr");
		PR_CHECK(pr::To<pr_wstr>("literal cstr"), L"literal cstr");

		PR_CHECK(pr::To<int>("1234"), 1234);
		PR_CHECK(pr::To<int>("1234", 10), 1234);
		PR_CHECK(pr::To<int>(L"1234", 10), 1234);
		PR_CHECK(pr::To<unsigned short>("12345",16), (unsigned short)0x2345);
		PR_CHECK(pr::To<char>(L"1"), (char)1);
		PR_CHECK(pr::To<int>(L"1234"), 1234);
	}
}
#endif
