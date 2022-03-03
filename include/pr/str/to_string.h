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
#include "pr/str/string_core.h"
#include "pr/str/string.h"

namespace pr
{
	namespace convert
	{
		// To string
		template <typename Str>
		struct ToStringType
		{
			using Char = typename string_traits<Str>::value_type;

			static Str& To(Str& from)
			{
				return from;
			}
			static Str To(bool from)
			{
				return from
					? PR_STRLITERAL(Char, "true")
					: PR_STRLITERAL(Char, "false");
			}
			static Str To(char from)
			{
				Str str;
				string_traits<Str>::resize(str, 1);
				str[0] = from;
				return str;
			}
			static Str To(long long from, int radix)
			{
				Char buf[128];
				return char_traits<Char>::itostr(from, buf, _countof(buf), radix);
			}
			static Str To(unsigned long long from, int radix)
			{
				Char buf[128];
				return char_traits<Char>::uitostr(from, buf, _countof(buf), radix);
			}
			static Str To(long long from)
			{
				return To(from, 10);
			}
			static Str To(long from, int radix)
			{
				return To(static_cast<long long>(from), radix);
			}
			static Str To(long from)
			{
				return To(from, 10);
			}
			static Str To(int from, int radix)
			{
				return To(static_cast<long long>(from), radix);
			}
			static Str To(int from)
			{
				return To(from, 10);
			}
			static Str To(short from, int radix)
			{
				return To(static_cast<long long>(from), radix);
			}
			static Str To(short from)
			{
				return To(from, 10);
			}
			static Str To(unsigned long long from)
			{
				return To(from, 10);
			}
			static Str To(unsigned long from, int radix)
			{
				return To(static_cast<unsigned long long>(from), radix);
			}
			static Str To(unsigned long from)
			{
				return To(from, 10);
			}
			static Str To(unsigned int from, int radix)
			{
				return To(static_cast<unsigned long long>(from), radix);
			}
			static Str To(unsigned int from)
			{
				return To(from, 10);
			}
			static Str To(unsigned short from, int radix)
			{
				return To(static_cast<unsigned long long>(from), radix);
			}
			static Str To(unsigned short from)
			{
				return To(from, 10);
			}
			static Str To(unsigned char from, int radix)
			{ 
				return To(static_cast<unsigned long long>(from), radix);
			}
			static Str To(unsigned char from)
			{
				return To(from, 10);
			}
			static Str To(double from)
			{
				Char buf[128];
				return char_traits<Char>::dtostr(from, buf, _countof(buf));
			}
			static Str To(float from)
			{
				return To(static_cast<double>(from));
			}
			static Str To(long double from)
			{
				// careful with long double, it's non-standard
				std::basic_stringstream<Char> ss;
				ss << from;
				return ss.str().c_str();
			}
			
			// Convert/Narrow/Widen string types
			template <typename Str2, typename = std::enable_if_t<is_string_v<Str2>>>
			static Str To(Str2 const& s)
			{
				// Notes:
				//  - Remember type deduction doesn't work for string views
				//  - If 'Str2' = 'char const* const&', then 'string_traits<Str2>::value_type' would be 'char const'

				using Char2 = std::remove_const_t<typename string_traits<Str2>::value_type>;

				if constexpr (std::is_convertible_v<Str2, Str>)
				{
					return s;
				}
				else if constexpr (std::is_assignable_v<Str, Str2>)
				{
					return s;
				}
				else if constexpr (std::is_same_v<Char, char> && std::is_same_v<Char2, wchar_t>)
				{
					return Narrow(s);
				}
				else if constexpr (std::is_same_v<Char, wchar_t> && std::is_same_v<Char2, char>)
				{
					return Widen(s);
				}
				else
				{
					static_assert(dependant_false<Str2>, "Cannot convert between string types");
				}
			}
		};

		// Integral
		template <typename Ty>
		struct ToIntegral
		{
			// Convert from strings to integral types
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static Ty To(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				auto ptr = string_traits<Str>::ptr(s);
				errno = 0;

				if constexpr (std::is_signed_v<Ty> && sizeof(Ty) <= sizeof(long))
					return CheckErrno(static_cast<Ty>(char_traits<Char>::strtol(ptr, end, radix)));
				if constexpr (!std::is_signed_v<Ty> && sizeof(Ty) <= sizeof(long))
					return CheckErrno(static_cast<Ty>(char_traits<Char>::strtoul(ptr, end, radix) & ~Ty()));
				if constexpr (std::is_signed_v<Ty> && sizeof(Ty) > sizeof(long))
					return CheckErrno(static_cast<Ty>(char_traits<Char>::strtoll(ptr, end, radix) & ~Ty()));
				if constexpr (!std::is_signed_v<Ty> && sizeof(Ty) > sizeof(long))
					return CheckErrno(static_cast<Ty>(char_traits<Char>::strtoui64(ptr, end, radix) & ~Ty()));
			}
		};

		// Floating point
		template <typename Ty>
		struct ToFloatingPoint
		{
			// String to floating point
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static Ty To(Str const& s, Char const** end = nullptr)
			{
				auto ptr = string_traits<Str>::ptr(s);
				errno = 0;

				return CheckErrno(static_cast<Ty>(char_traits<Char>::strtod(ptr, end)));
			}
		};
	}

	// From Whatever to std::basic_string
	template <typename TFrom, typename Char>
	struct Convert<std::basic_string<Char>, TFrom> :convert::ToStringType<std::basic_string<Char>>
	{};

	// From Whatever to pr::string
	template <typename TFrom, typename Char, int L, bool F>
	struct Convert<pr::string<Char,L,F>, TFrom> :convert::ToStringType<pr::string<Char,L,F>>
	{};

	// From String types to Integral types
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

	// From String types to Floating point types
	template <typename TFrom> struct Convert<float      , TFrom> :convert::ToFloatingPoint<float      > {};
	template <typename TFrom> struct Convert<double     , TFrom> :convert::ToFloatingPoint<double     > {};
	template <typename TFrom> struct Convert<long double, TFrom> :convert::ToFloatingPoint<long double> {};

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
