//**********************************
// String Core
//  Copyright (c) Rylogic Ltd 2015
//**********************************
// Fundamental string functions that operate on:
//'   std::string, std::wstring, pr::string<>, etc
//'   char[], wchar_t[], etc
//'   char*, wchar_t*, etc
// Note: char-array strings are not handled as special cases because there is no
// guarantee that the entire buffer is filled by the string, the null terminator
// may be midway through the buffer

#pragma once

#include <string>
#include <string_view>
#include <vector>
#include <functional>
#include <type_traits>
#include <locale>
#include <cstdlib>
#include <cassert>
#include "pr/str/encoding.h"

// Use this define to declare a string literal in a function templated on 'tchar'
#ifndef PR_STRLITERAL
#define PR_STRLITERAL(tchar,s)  pr::char_traits<tchar>::str(s, L##s)
#endif

namespace pr
{
	// Dealing with const in template functions:
	//  - If the function only requires a const parameter, use const in the parameter list:
	//    e.g.  template <typename T> void Read(T const& t) {}
	//    Within the function, use traits<T const>::...
	//  - If the function requires const and non-const overloads, just provide the non-const
	//    version and leave type deduction to handle the const cases:
	//    e.g. template <typename T> T::value* Ptr(T& t) { return t.ptr(); }
	//         Ptr<Thing const>(thing)
	//  - Functions that end with 'C' are used to get a const property from a potentially non
	//    const object.
	//  - T&& parameters cause T to be a reference type (mostly, except when an rvalue is passed)
	//    e.g. template <typename T> void f(T&& x) {} => T will be a reference type (so you might
	//    need to use std::decay_t when using traits)

	// Convert an int to a byte
	constexpr char operator "" _uc(unsigned long long arg) noexcept
	{
		return static_cast<unsigned char>(arg & 0xFF);
	}

	// Convert an int to a char8
	constexpr char operator "" _c8(unsigned long long arg) noexcept
	{
		return static_cast<char>(arg & 0xFF);
	}

	// Convert an int to a wchar_t
	constexpr wchar_t operator "" _wc(unsigned long long arg) noexcept
	{
		return static_cast<wchar_t>(arg & 0xFFFF);
	}

	#pragma region Char Traits

	// Extend char traits
	template <typename Char>
	struct char_traits;

	// Common functionality
	template <typename Char>
	struct char_traits_common :std::char_traits<Char>
	{
		static Char lwr(Char ch) { return static_cast<Char>(::tolower(ch)); }
		static Char upr(Char ch) { return static_cast<Char>(::toupper(ch)); }

		static size_t length(Char const* str)
		{
			size_t count = 0;
			for (; *str; ++str, ++count) {}
			return count;
		}
		static size_t length(Char const* str, size_t max_count)
		{
			size_t count = 0;
			for (; *str && max_count-- != 0; ++str, ++count) {}
			return count;
		}
	};

	// 'char' traits
	template <>
	struct char_traits<char> :char_traits_common<char>
	{
		static constexpr char const* str(char const* str, wchar_t const*) { return str; }

		static int strcmp(char const* lhs, char const* rhs)                     { return ::strcmp(lhs, rhs); }
		static int strncmp(char const* lhs, char const* rhs, size_t max_count)  { return ::strncmp(lhs, rhs, max_count); }
		static int strnicmp(char const* lhs, char const* rhs, size_t max_count) { return ::_strnicmp(lhs, rhs, max_count); }

		static double             strtod(char const* str, char const** end)                   { return ::strtod(str, (char**)end); }
		static long               strtol(char const* str, char const** end, int radix)        { return ::strtol(str, (char**)end, radix); }
		static unsigned long      strtoul(char const* str, char const** end, int radix)       { return ::strtoul(str, (char**)end, radix); }
		static long long          strtoi64(char const* str, char const** end, int radix)      { return ::_strtoi64(str, (char**)end, radix); }
		static unsigned long long strtoui64(char const* str, char const** end, int radix)     { return ::_strtoui64(str, (char**)end, radix); }

		static char* itostr(long long from, char* buf, int count, int radix)
		{
			// 'buf' should be at least 65 characters long
			if (::_i64toa_s(from, buf, count, radix) == 0) return buf;
			throw std::runtime_error("conversion from integral value to string failed");
		}
		static char* uitostr(unsigned long long from, char* buf, int count, int radix)
		{
			if (::_ui64toa_s(from, buf, count, radix) == 0) return buf;
			throw std::runtime_error("conversion from unsigned integral value to string failed");
		}
		static char* dtostr(double from, char* buf, int count)
		{
			// Have to use %g otherwise converting to a string loses precision
			auto n = ::_snprintf(buf, count, "%g", from);
			if (n <= 0) throw std::runtime_error("conversion from floating point value to string failed");
			if (n >= count) throw std::runtime_error("conversion from floating point value to string was truncated");
			buf[count - 1] = 0;
			return buf;
		}
	};

	// wchar_t traits
	template <>
	struct char_traits<wchar_t> :char_traits_common<wchar_t>
	{
		static constexpr wchar_t const* str(char const*, wchar_t const* str) { return str; }

		static int strcmp(wchar_t const* lhs, wchar_t const* rhs)                     { return ::wcscmp(lhs, rhs); }
		static int strncmp(wchar_t const* lhs, wchar_t const* rhs, size_t max_count)  { return ::wcsncmp(lhs, rhs, max_count); }
		static int strnicmp(wchar_t const* lhs, wchar_t const* rhs, size_t max_count) { return ::_wcsnicmp(lhs, rhs, max_count); }

		static double             strtod(wchar_t const* str, wchar_t const** end)                   { return ::wcstod(str, (wchar_t**)end); }
		static long               strtol(wchar_t const* str, wchar_t const** end, int radix)        { return ::wcstol(str, (wchar_t**)end, radix); }
		static long long          strtoi64(wchar_t const* str, wchar_t const** end, int radix)      { return ::_wcstoi64(str, (wchar_t**)end, radix); }
		static unsigned long      strtoul(wchar_t const* str, wchar_t const** end, int radix)       { return ::wcstoul(str, (wchar_t**)end, radix); }
		static unsigned long long strtoui64(wchar_t const* str, wchar_t const** end, int radix)     { return ::_wcstoui64(str, (wchar_t**)end, radix); }

		static wchar_t* itostr(long long from, wchar_t* buf, int count, int radix)
		{
			// 'buf' should be at least 65 characters long
			if (::_i64tow_s(from, buf, count, radix) == 0) return buf;
			throw std::runtime_error("conversion from integral value to string failed");
		}
		static wchar_t* uitostr(unsigned long long from, wchar_t* buf, int count, int radix)
		{
			if (::_ui64tow_s(from, buf, count, radix) == 0) return buf;
			throw std::runtime_error("conversion from unsigned integral value to string failed");
		}
		static wchar_t* dtostr(double from, wchar_t* buf, int count)
		{
			// Have to use %g otherwise converting to a string loses precision
			auto n = ::_snwprintf(buf, count, L"%g", from);
			if (n <= 0) throw std::runtime_error("conversion from floating point value to string failed");
			if (n >= count) throw std::runtime_error("conversion from floating point value to string was truncated");
			buf[count - 1] = 0;
			return buf;
		}
	};

	// char16_t traits
	template <>
	struct char_traits<char16_t> : char_traits_common<char16_t>
	{};

	// char32_t traits
	template <>
	struct char_traits<char32_t> : char_traits_common<char32_t>
	{};

	// References to chars
	template <typename Char>
	struct char_traits<Char const> :char_traits<std::decay_t<Char>>
	{};
	template <typename Char>
	struct char_traits<Char&> :char_traits<std::decay_t<Char>>
	{};

	// Is 'T' a char or wchar_t
	template <typename T> struct is_char :std::false_type {};
	template <> struct is_char<char> :std::true_type {};
	template <> struct is_char<wchar_t> :std::true_type {};
	template <> struct is_char<char16_t> :std::true_type {};
	template <> struct is_char<char32_t> :std::true_type {};
	template <typename T> constexpr bool is_char_v = is_char<std::decay_t<T>>::value;

	// Casting helper to catch overflow when casting from wide to narrow characters
	template <typename Char1, typename Char2, typename = std::enable_if_t<is_char_v<Char1> && is_char_v<Char2>>>
	inline Char1 char_cast(Char2 ch)
	{
		assert(static_cast<Char2>(static_cast<Char1>(ch)) == ch && "Character value overflow in cast");
		return static_cast<Char1>(ch);
	}

	#pragma endregion

	#pragma region String Traits

	// String traits
	template <typename TStr>
	struct string_traits;

	// std::basic_string
	template <typename Char>
	struct string_traits<std::basic_string<Char>> :char_traits<Char>
	{
		using value_type = Char;
		using string_type = std::basic_string<Char>;
		static bool const null_terminated = true;

		static value_type const* c_str(string_type const& str) { return str.c_str(); }
		static value_type const* ptr(string_type const& str)   { return str.data(); }
		static value_type* ptr(string_type& str)               { return str.data(); }
		static size_t size(string_type const& str)             { return str.size(); }
		static bool empty(string_type const& str)              { return str.empty(); }
		static void resize(string_type& str, size_t n)         { str.resize(n); }  // note: does not guarantee to fill for all string types
	};
	template <typename Char>
	struct string_traits<std::basic_string<Char> const> :char_traits<Char>
	{
		using value_type = Char const;
		using string_type = std::basic_string<Char> const;
		static bool const null_terminated = true;

		static value_type* c_str(string_type& str) { return str.c_str(); }
		static value_type* ptr(string_type& str)   { return str.data(); }
		static size_t size(string_type& str)       { return str.size(); }
		static bool empty(string_type& str)        { return str.empty(); }
		static void resize(string_type&, size_t)   { static_assert(false, "Immutable string cannot be resized"); }
	};

	// std::basic_string_view
	template <typename Char>
	struct string_traits<std::basic_string_view<Char>> :char_traits<Char>
	{
		using value_type = Char;
		using string_type = std::basic_string_view<Char>;
		static bool const null_terminated = false;

		static value_type const* c_str(string_type const& str) { static_assert(false, "String views cannot provide null terminated strings"); }
		static value_type const* ptr(string_type const& str)   { return str.data(); }
		static value_type* ptr(string_type& str)               { return str.data(); }
		static size_t size(string_type const& str)             { return str.size(); }
		static bool empty(string_type const& str)              { return str.empty(); }
		static void resize(string_type& str, size_t n)         { if (n <= size(str)) str = str.substr(0, n); else throw std::runtime_error("String views can only be made smaller"); }
	};
	template <typename Char>
	struct string_traits<std::basic_string_view<Char> const> :char_traits<Char>
	{
		using value_type = Char const;
		using string_type = std::basic_string_view<Char> const;
		static bool const null_terminated = false;

		static value_type* c_str(string_type& str)     { static_assert(false, "String views cannot provide null terminated strings"); }
		static value_type* ptr(string_type& str)       { return str.data(); }
		static size_t size(string_type& str)           { return str.size(); }
		static bool empty(string_type& str)            { return str.empty(); }
		static void resize(string_type& str, size_t n) { static_assert(false, "Immutable string cannot be resized"); }
	};

	// char*
	template <typename Char>
	struct string_traits<Char*> :char_traits<Char>
	{
		using value_type = Char;
		using string_type = Char*;
		static bool const null_terminated = true;

		static value_type const* c_str(string_type const str) { return str; }
		static value_type* ptr(string_type str)               { return str; }
		static size_t size(string_type const str)             { return length(str); }
		static bool empty(string_type const str)              { return *str == 0; }
		static void resize(string_type str, size_t n)         { str[n] = 0; }
	};
	template <typename Char>
	struct string_traits<Char const*> :char_traits<Char>
	{
		using value_type = Char const;
		using string_type = Char const*;
		static bool const null_terminated = true;

		static value_type* c_str(string_type str) { return str; }
		static value_type* ptr(string_type str)   { return str; }
		static size_t size(string_type str)       { return length(str); }
		static bool empty(string_type str)        { return *str == 0; }
		static void resize(string_type, size_t)   { static_assert(false, "Immutable strings cannot be resized"); }
	};
	template <typename Char>
	struct string_traits<Char* const> :string_traits<Char*>
	{};
	template <typename Char>
	struct string_traits<Char const* const> :string_traits<Char const*>
	{};

	// char[]
	template <typename Char, size_t Len>
	struct string_traits<Char[Len]> :char_traits<Char>
	{
		using value_type = Char;
		using string_type = Char[Len];
		static bool const null_terminated = true;

		static value_type const* c_str(string_type const& str) { return str; }
		static value_type const* ptr(string_type const& str)   { return str; }
		static value_type* ptr(string_type& str)               { return str; }
		static size_t size(string_type const& str)             { return length(str, Len); }
		static bool empty(string_type const& str)              { return *str == 0; }
		static void resize(string_type& str, size_t n)         { if (n < Len) str[n] = 0; else throw std::runtime_error("Resize exceeds fixed array size"); }
	};
	template <typename Char, size_t Len>
	struct string_traits<Char const[Len]> :char_traits<Char>
	{
		using value_type = Char const;
		using string_type = Char const[Len];
		static bool const null_terminated = true;

		static value_type* c_str(string_type& str) { return str; }
		static value_type* ptr(string_type& str)   { return str; }
		static size_t size(string_type& str)       { return length(str, Len); }
		static bool empty(string_type& str)        { return *str == 0; }
		static void resize(string_type&, size_t)   { static_assert(false, "Immutable string cannot be resized"); }
	};

	// std::array<char>
	template <typename Char, size_t Len>
	struct string_traits<std::array<Char, Len>> :char_traits<Char>
	{
		static_assert(Len > 0);
		using value_type = Char;
		using string_type = std::array<Char, Len>;
		static bool const null_terminated = true;

		static value_type const* c_str(string_type const& str)  { return str.data(); }
		static value_type const* ptr(string_type const& str)    { return str.data(); }
		static value_type* ptr(string_type& str)                { return str.data(); }
		static size_t size(string_type const& str)              { return length(str.data(), Len); }
		static bool empty(string_type const& str)               { return str[0] == 0; }
		static void resize(std::array<Char,Len>& str, size_t n) { if (n < Len) str[n] = 0; else throw std::runtime_error("Resize exceeds fixed array size"); }
	};
	template <typename Char, size_t Len>
	struct string_traits<std::array<Char, Len> const> :char_traits<Char>
	{
		static_assert(Len > 0);
		using value_type = Char const;
		using string_type = std::array<Char, Len> const;
		static bool const null_terminated = true;

		static value_type* c_str(string_type& str) { return str.data(); }
		static value_type* ptr(string_type& str)   { return str.data(); }
		static size_t size(string_type& str)       { return length(str.data(), Len); }
		static bool empty(string_type& str)        { return str[0] == 0; }
		static void resize(string_type&, size_t)   { static_assert(false, "Immutable string cannot be resized"); }
	};

	// References to strings
	template <typename TStr>
	struct string_traits<TStr&> :string_traits<std::remove_reference_t<TStr>>
	{};

	// Is string type
	template <typename Ty> struct is_string :std::false_type {};
	template <typename Char> struct is_string<Char*> : is_char<Char> {};
	template <typename Char> struct is_string<Char const*> : is_char<Char> {};
	template <typename Char> struct is_string<std::basic_string_view<Char>> : is_char<Char> {};
	template <typename Char> struct is_string<std::basic_string<Char>> : is_char<Char> {};
	template <typename Char, size_t Len> struct is_string<Char const[Len]> : is_char<Char> {};
	template <typename Char, size_t Len> struct is_string<Char[Len]> : is_char<Char> {};
	template <typename Ty> constexpr bool is_string_v = is_string<std::decay_t<Ty>>::value;

	// Checks
	static_assert(std::is_same_v<string_traits<std::wstring>::value_type, wchar_t>);
	static_assert(std::is_same_v<string_traits<std::string const&>::value_type, char const>);
	static_assert(is_string_v<char*>);
	static_assert(is_string_v<wchar_t* const>);
	static_assert(is_string_v<char16_t const*>);
	static_assert(is_string_v<std::string>);
	static_assert(is_string_v<std::wstring>);
	static_assert(is_string_v<std::wstring const&>);
	static_assert(!is_string_v<double>);

	#pragma endregion

	#pragma region Encoding

	// The largest code point defined in unicode 6.0
	constexpr int UnicodeMaxValue = 0x10FFFF;

	// A static instance of the locale, because this thing takes ages to construct
	inline std::locale const& locale()
	{
		static std::locale s_locale("");
		return s_locale;
	}

	// Return 'str_in' as type 'ToStr' avoiding the copy if possible
	template <typename ToStr, typename FromStr, int MaxValue = std::numeric_limits<string_traits<ToStr>::value_type>::max()>
	ToStr ReturnStr(FromStr const& str_in, [[maybe_unused]] int const max_value = MaxValue, [[maybe_unused]] char dflt = '_')
	{
		if constexpr (std::is_same_v<FromStr, ToStr>)
		{
			return str_in;
		}
		else if constexpr (std::is_convertible_v<FromStr, ToStr>)
		{
			return str_in;
		}
		else if constexpr (std::is_constructible_v<ToStr, FromStr>)
		{
			return ToStr(str_in);
		}
		else
		{
			auto length = string_traits<FromStr const>::size(str_in);

			ToStr str_out = {};
			string_traits<ToStr>::resize(str_out, length);

			// Copy characters using casting
			auto s = string_traits<FromStr const>::ptr(str_in);
			auto d = string_traits<ToStr>::ptr(str_out);
			for (; length-- != 0; ++s, ++d)
			{
				if (static_cast<int>(*s) <= max_value)
					*d = char_cast<typename string_traits<ToStr>::value_type>(*s);
				else
					*d = char_cast<typename string_traits<ToStr>::value_type>(dflt);
			}

			return str_out;
		}
	}

	// Apply 'converter' to 'str_in'
	template <typename ToStr, typename FromStr, typename Converter, typename = std::enable_if_t<std::is_base_of_v<std::codecvt_base, Converter>>>
	ToStr ConvertEncoding(FromStr const& str_in, Converter& converter)
	{
		using in_traits   = string_traits<FromStr const>;
		using out_traits  = string_traits<ToStr>;
		using in_char     = std::decay_t<in_traits::value_type>;
		using out_char    = std::decay_t<out_traits::value_type>;
		using intern_type = typename Converter::intern_type;
		using extern_type = typename Converter::extern_type;

		// Create the string to be returned
		ToStr str_out = {};
		
		// Set the initial string size based on the relative size of the characters
		// Use '15' as the min size so 16-byte arrays can be used as 'ToStr'.
		auto out_size = std::max<size_t>(15, in_traits::size(str_in));
		out_traits::resize(str_out, out_size);

		// Get pointers to the 'from' string
		auto in_beg = in_traits::ptr(str_in);
		auto in_end = in_beg + in_traits::size(str_in);
		auto in_next = in_beg;
		auto in = in_beg;

		// Get pointers to the 'to' string
		auto out_beg = out_traits::ptr(str_out);
		auto out_end = out_beg + out_size;
		auto out_next = out_beg;
		auto out = out_beg;

		// Loop until all of 'str_in' has been converted
		for (auto mb = std::mbstate_t{};;)
		{
			int r;

			// Convert as many characters as will fit in 'out'
			if constexpr (std::is_same_v<in_char, intern_type>)
			{
				r = converter.out(mb, in, in_end, in_next, out, out_end, out_next);
			}
			else if constexpr (std::is_same_v<out_char, intern_type>)
			{
				r = converter.in(mb, in, in_end, in_next, out, out_end, out_next);
			}
			else if constexpr (std::is_same_v<in_char, wchar_t> && std::is_same_v<intern_type, char16_t>)
			{
				r = converter.out(mb,
					reinterpret_cast<intern_type const*>(in), 
					reinterpret_cast<intern_type const*>(in_end), 
					reinterpret_cast<intern_type const*&>(in_next), 
					out, out_end, out_next);
			}
			else if constexpr (std::is_same_v<out_char, wchar_t> && std::is_same_v<intern_type, char16_t>)
			{
				r = converter.in(mb,
					in, in_end, in_next, 
					reinterpret_cast<intern_type*>(out),
					reinterpret_cast<intern_type*>(out_end),
					reinterpret_cast<intern_type*&>(out_next));
			}
			else
			{
				static_assert(false, "Converter type mismatch");
			}
			assert(in_next <= in_end);
			assert(out_next <= out_end);

			// 'str_in' has been completely converted
			if (r == std::codecvt_base::ok && in_next == in_end)
			{
				// Trim the out string to the correct length
				out_traits::resize(str_out, out_next - out_beg);
				break;
			}
			
			// 'Out' is not big enough, allocate more space and carry on
			if (r == std::codecvt_base::ok || r == std::codecvt_base::partial)
			{
				// The number of characters converted so far
				auto out_count = out_next - out_beg;

				// Grow 'str_out' can continue converting
				out_traits::resize(str_out, out_size *= 3 / 2);
				out_beg = out_traits::ptr(str_out);
				out_end = out_beg + out_size;
				out = out_beg + out_count;
				in = in_next;
				continue;
			}

			// 'str_in' contains an encoding error
			r == std::codecvt_base::error
				? throw std::runtime_error("Encoding error in input string")
				: throw std::runtime_error("Unexpected encoding return code");
		}

		return str_out;
	}

	// String encoding conversion
	template <EEncoding ToEnc, typename ToStr, EEncoding FromEnc, typename FromStr>
	ToStr ConvertEncoding(FromStr const& str_in, [[maybe_unused]] char dflt = '_')
	{
		// Notes:
		//  - The size of 'wchar_t' is platform specific (i.e. not portable)
		//  - UCS2 is a subset of utf-16.
		//  - Windows used UCS2 up to Windows 2000, after that utf-16 is used.
		//  - char16_t is used for utf-16
		//  - char8_t is used for utf-8, 
		//  - char can be ascii or utf-8
		//  - wchar_t can be utf-16, ucs2, or whatever
		//  - the codecvt class seems to infer the encoding from the character type
		//  - The windows API actually uses utf-16 but codecvt<wchar_t, char>,..> doesn't accept 
		// Old Methods:
		//   std::string buffer(from.size(), '\0');
		//   std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from.data(), from.data() + from.size(), '_', &buffer[0]);
		//   return std::move(buffer);
		//   
		//   std::wstring buffer(from.size(), '\0');
		//   std::use_facet<std::ctype<wchar_t>>(locale()).widen(from.data(), from.data() + from.size(), &buffer[0]);
		//   return std::move(buffer);
		//   
		//   std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>, wchar_t> convert;
		//   auto wide = convert.from_bytes(from.data(), from.data() + from.size());
		//   return std::move(wide);

		if constexpr (false) {}
		else if constexpr (ToEnc == EEncoding::ascii)
		{
			if constexpr (false) {}
			else if constexpr (FromEnc == EEncoding::ascii)
			{
				// ASCII to ASCII is a no-op
				return ReturnStr<ToStr>(str_in);
			}
			else if constexpr (FromEnc == EEncoding::utf8)
			{
				// UTF-8 to ASCII
				return ReturnStr<ToStr>(str_in, 127, dflt);
			}
			else if constexpr (FromEnc == EEncoding::utf16_le)
			{
				// UTF-16 to ASCII
				return ReturnStr<ToStr>(str_in, 127, dflt);
			}
			else if constexpr (FromEnc == EEncoding::utf32)
			{
				// UTF-32 to ASCII
				return ReturnStr<ToStr>(str_in, 127, dflt);
			}
			else
			{
				static_assert(false, "Unsupported encoding conversion");
			}
		}
		else if constexpr (ToEnc == EEncoding::utf8)
		{
			if constexpr (false) {}
			else if constexpr (FromEnc == EEncoding::ascii)
			{
				// ASCII to UTF-8 is a no-op
				return ReturnStr<ToStr>(str_in);
			}
			else if constexpr (FromEnc == EEncoding::utf8)
			{
				// UTF-8 to UTF-8 is a no-op
				return ReturnStr<ToStr>(str_in);
			}
			else if constexpr (FromEnc == EEncoding::utf16_le)
			{
				// UTF-16 to UTF-8
				struct convert_t :std::codecvt<char16_t, char, std::mbstate_t> {} converter;
				return ConvertEncoding<ToStr>(str_in, converter);
			}
			else if constexpr (FromEnc == EEncoding::utf32)
			{
				// UTF-32 to UTF-8
				throw std::runtime_error("not implemented");
			}
			else if constexpr (FromEnc == EEncoding::ucs2_le)
			{
				// UCS2 to UTF-8 
				struct convert_t :std::codecvt<char16_t, char, std::mbstate_t> {} converter;
				return ConvertEncoding<ToStr>(str_in, converter);
			}
			else
			{
				static_assert(false, "Unsupported encoding conversion");
			}
		}
		else if constexpr (ToEnc == EEncoding::utf16_le)
		{
			if constexpr (false) {}
			else if constexpr (FromEnc == EEncoding::ascii)
			{
				// ASCII to UTF-16
				return ReturnStr<ToStr>(str_in);
			}
			else if constexpr (FromEnc == EEncoding::utf8)
			{
				// UTF-8 to UTF-16
				struct convert_t :std::codecvt<char16_t, char, std::mbstate_t> {} converter;
				return ConvertEncoding<ToStr>(str_in, converter);
			}
			else if constexpr (FromEnc == EEncoding::utf16_le)
			{
				// UTF-16 to UTF-16 is a no-op
				return ReturnStr(from);
			}
			else if constexpr (FromEnc == EEncoding::utf32)
			{
				// UTF-32 to UTF-16
				throw std::runtime_error("not implemented");
			}
			else
			{
				static_assert(false, "Unsupported encoding conversion");
			}
		}
		else if constexpr (ToEnc == EEncoding::utf32)
		{
			if constexpr (false) {}
			else if constexpr (FromEnc == EEncoding::ascii)
			{
				// ASCII to UTF-32
				return ReturnStr<ToStr>(str_in);
			}
			else if constexpr (FromEnc == EEncoding::utf8)
			{
				// UTF-8 to UTF-32
				throw std::runtime_error("not implemented");
			}
			else if constexpr (FromEnc == EEncoding::utf16_le)
			{
				// UTF-16 to UTF-32
				throw std::runtime_error("not implemented");
			}
			else if constexpr (FromEnc == EEncoding::utf32)
			{
				// UTF-32 to UTF-32 is a no-op
				return ReturnStr(from);
			}
			else
			{
				static_assert(false, "Unsupported encoding conversion");
			}
		}
		else
		{
			static_assert(false, "Unsupported encoding conversion");
		}
	}

	// Narrow a string to utf-8
	inline std::string Narrow(std::string const& from) // from ascii, utf8
	{
		return from;
	}
	inline std::string Narrow(std::string_view from)
	{
		// Needed because string_view is not implicitly convertible to std::string
		return Narrow(std::string(from));
	}
	inline std::string Narrow(char const* from)
	{
		// Needed to disambiguate 'string_view' and 'std::string const&' overloads
		return Narrow(std::string_view(from));
	}
	inline std::string Narrow(std::wstring_view from) // from utf16
	{
		return ConvertEncoding<EEncoding::utf8, std::string, EEncoding::utf16_le>(from);
	}

	// Widen string to utf16
	inline std::wstring Widen(std::wstring const& from) // from usc2, utf-16
	{
		return from;
	}
	inline std::wstring Widen(std::wstring_view from)
	{
		// Needed because string_view is not implicitly convertible to std::string
		return Widen(std::wstring(from));
	}
	inline std::wstring Widen(wchar_t const* from)
	{
		// Needed to disambiguate 'wstring_view' and 'std::wstring const&' overloads
		return Widen(std::wstring_view(from));
	}
	inline std::wstring Widen(std::string_view from) // from utf-8
	{
		return ConvertEncoding<EEncoding::utf16_le, std::wstring, EEncoding::utf8>(from);
	}

	#pragma endregion

	namespace str
	{
		#pragma region Sprintf

		// Variable arg 'sprintf', overloaded for all char type combinations
		inline int vsprintf(char* buf, size_t buf_size_in_bytes, char const* format, va_list args)
		{
			return vsprintf_s(buf, buf_size_in_bytes, format, args);
		}
		inline int vsprintf(char* buf, size_t buf_size_in_bytes, wchar_t const* format, va_list args)
		{
			return vsprintf_s(buf, buf_size_in_bytes, Narrow(format).c_str(), args);
		}
		inline int vsprintf(wchar_t* buf, size_t buf_size_in_words, wchar_t const* format, va_list args)
		{
			return vswprintf_s(buf, buf_size_in_words, format, args);
		}
		inline int vsprintf(wchar_t* buf, size_t buf_size_in_words, char const* format, va_list args)
		{
			return vswprintf_s(buf, buf_size_in_words, Widen(format).c_str(), args);
		}

		// 'sprintf' accepting any char type
		template <typename Char1, typename Char2>
		inline int sprintf(Char1* buf, size_t buf_size_in_words, Char2 const* format, ...)
		{
			va_list arg_list;
			va_start(arg_list, format);
			auto r = vsprintf(buf, buf_size_in_words, format, arg_list);
			va_end(arg_list);
			return r;
		}

		#pragma endregion

		#pragma region Character classes

		template <typename Char> inline bool IsNewLine(Char ch)                 { return ch == '\n'; }
		template <typename Char> inline bool IsLineSpace(Char ch)               { return ch == ' ' || ch == '\t' || ch == '\r'; }
		template <typename Char> inline bool IsWhiteSpace(Char ch)              { return IsLineSpace(ch) || IsNewLine(ch) || ch == '\v' || ch == '\f'; }
		template <typename Char> inline bool IsDecDigit(Char ch)                { return (ch >= '0' && ch <= '9'); }
		template <typename Char> inline bool IsBinDigit(Char ch)                { return (ch >= '0' && ch <= '1'); }
		template <typename Char> inline bool IsOctDigit(Char ch)                { return (ch >= '0' && ch <= '7'); }
		template <typename Char> inline bool IsHexDigit(Char ch)                { return IsDecDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'); }
		template <typename Char> inline bool IsDigit(Char ch)                   { return IsDecDigit(ch); }
		template <typename Char> inline bool IsAlpha(Char ch)                   { return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'); }
		template <typename Char> inline bool IsIdentifier(Char ch, bool first)  { return ch == '_' || IsAlpha(ch) || (!first && IsDigit(ch)); }

		// Return a pointer to delimiters, either the ones provided or the default ones
		template <typename Char>
		inline Char const* Delim(Char const* delim = nullptr)
		{
			static Char const default_delim[] = {' ', '\t', '\n', '\r', 0};
			return delim ? delim : default_delim;
		}

		#pragma endregion

		#pragma region Size

		// Return true if 'str' is an empty string
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline bool Empty(Str const& str)
		{
			return string_traits<Str const>::empty(str);
		}

		// Return the size of the string, excluding the null terminator (same as 'strlen'). Not necessarily the length of a multibyte encoded string
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline size_t Size(Str const& str)
		{
			return string_traits<Str const>::size(str);
		}

		#pragma endregion

		#pragma region Range

		// Return a pointer to the start of the string
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* Begin(Str&& str)
		{
			// Compile error: "'return': cannot convert from 'const char *' to 'Char *'" means
			// the string_traits<Str>::value_type is missing a 'const'
			return string_traits<Str>::ptr(str);
		}
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char const* BeginC(Str&& str)
		{
			return Begin<Str const>(str);
		}

		// Return a pointer to the end of the string
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* End(Str&& str)
		{
			return Begin(str) + Size(str);
		}
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char const* EndC(Str&& str)
		{
			return End<Str const>(str);
		}

		// Return a pointer to the 'N'th character or the end of the string, whichever is less
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* End(Str&& str, size_t N)
		{
			return Begin(str) + std::min(N, Size(str));
		}
		template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char const* EndC(Str&& str, size_t N)
		{
			return Begin<Str const>(str) + std::min(N, Size(str));
		}

		#pragma endregion

		#pragma region Equal

		// Return true if the ranges '[i,iend)' and '[j,jend)' are equal
		template <typename Iter1, typename Iter2, typename Pred>
		inline bool Equal(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend, Pred pred)
		{
			for (; !(i == iend) && !(j == jend) && pred(*i, *j); ++i, ++j) {}
			return i == iend && j == jend;
		}
		template <typename Iter1, typename Iter2> 
		inline bool Equal(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend)
		{
			return Equal(i, iend, j, jend, [](auto l, auto r) { return l == r; });
		}

		// Return true if str1 and str2 are equal
		template <typename Str1, typename Str2, typename Pred, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool Equal(Str1 const& str1, Str2 const& str2, Pred pred)
		{
			return Equal(Begin(str1), End(str1), Begin(str2), End(str2), pred);
		}
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool Equal(Str1 const& str1, Str2 const& str2)
		{
			return Equal(Begin(str1), End(str1), Begin(str2), End(str2));
		}

		// Return true if str1 and str2 are equal
		template <typename Char1, typename Char2, typename Pred>
		inline bool Equal(Char1 const* str1, Char2 const* str2, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return *i == 0 && *j == 0;
		}
		template <typename Char1, typename Char2>
		inline bool Equal(Char1 const* str1, Char2 const* str2)
		{
			return Equal(str1, str2, [](auto l, auto r) { return l == r; });
		}

		// Specialisations for char/wchar_t
		inline bool Equal(char const* str1, char const* str2)
		{
			return ::strcmp(str1, str2) == 0;
		}
		inline bool Equal(wchar_t const* str1, wchar_t const* str2)
		{
			return ::wcscmp(str1, str2) == 0;
		}

		#pragma endregion

		#pragma region EqualI

		// Return true if the ranges '[i,iend)' and '[j,jend)' are equal, ignoring case
		template <typename Iter1, typename Iter2>
		inline bool EqualI(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend)
		{
			using Char1 = std::decay_t<decltype(*i)>;
			using Char2 = std::decay_t<decltype(*j)>;

			return Equal(i, iend, j, jend, [](Char1 lhs, Char2 rhs)
				{
					auto l = char_traits<Char1>::lwr(lhs);
					auto r = char_traits<Char2>::lwr(rhs);
					return l == r;
				});
		}

		// Return true if lhs and rhs are equal, ignoring case
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool EqualI(Str1 const& str1, Str2 const& str2)
		{
			return EqualI(Begin(str1), End(str1), Begin(str2), End(str2));
		}

		// Specialisations for char/wchar_t
		inline bool EqualI(char const* str1, char const* str2)
		{
			return ::_stricmp(str1, str2) == 0;
		}
		inline bool EqualI(wchar_t const* str1, wchar_t const* str2)
		{
			return ::_wcsicmp(str1, str2) == 0;
		}

		#pragma endregion

		#pragma region EqualN

		// Return true if lhs and rhs are equal, up to the given length
		template <typename Str1, typename Str2, typename Pred, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length, Pred pred)
		{
			return Equal(Begin(str1), End(str1, length), Begin(str2), End(str2, length), pred);
		}
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length)
		{
			return Equal(Begin(str1), End(str1, length), Begin(str2), End(str2, length));
		}
		template <typename Char1, typename Char2, typename Pred>
		inline bool EqualN(Char1 const* str1, Char2 const* str2, size_t length, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; length-- != 0 && *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return length == size_t(-1) || (*i == 0 && *j == 0);
		}
		inline bool EqualN(char const* str1, char const* str2, size_t length)
		{
			return ::strncmp(str1, str2, length) == 0;
		}
		inline bool EqualN(wchar_t const* str1, wchar_t const* str2, size_t length)
		{
			return ::wcsncmp(str1, str2, length) == 0;
		}

		#pragma endregion

		#pragma region EqualNI

		// Return true if lhs and rhs are equal, up to the given length
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline bool EqualNI(Str1 const& str1, Str2 const& str2, size_t length)
		{
			using Char1 = typename string_traits<Str1 const>::value_type;
			using Char2 = typename string_traits<Str2 const>::value_type;

			return EqualN(str1, str2, length, [](Char1 lhs, Char2 rhs)
				{
					auto l = char_traits<Char1>::lwr(lhs);
					auto r = char_traits<Char2>::lwr(rhs);
					return l == r; 
				});
		}
		template <typename Str, typename Char, int N, typename = std::enable_if_t<is_string_v<Str>>>
		inline bool EqualNI(Str const& str, Char const (&str2)[N])
		{
			return EqualNI(str, str2, N);
		}
		inline bool EqualNI(char const* str1, char const* str2, size_t length)
		{
			return ::_strnicmp(str1, str2, length) == 0;
		}
		inline bool EqualNI(wchar_t const* str1, wchar_t const* str2, size_t length)
		{
			return ::_wcsnicmp(str1, str2, length) == 0;
		}

		#pragma endregion

		#pragma region FindChar

		// Return a pointer to the first occurrence of 'ch' in a string
		template <typename Str1, typename Char2, typename Char1 = typename string_traits<Str1>::value_type, typename = std::enable_if_t<is_string_v<Str1>>>
		inline Char1* FindChar(Str1&& str, Char2 ch)
		{
			if constexpr (string_traits<Str1>::null_terminated)
			{
				auto ptr = string_traits<Str1>::ptr(str);
				for (; *ptr && static_cast<int>(*ptr) != static_cast<int>(ch); ++ptr) {}
				return ptr;
			}
			else
			{
				auto ptr = string_traits<Str1>::ptr(str);
				auto ptr_end = ptr + string_traits<Str1>::size(str);
				for (; ptr != ptr_end && static_cast<int>(*ptr) != static_cast<int>(ch); ++ptr) {}
				return ptr;
			}
		}

		// Return a pointer to the first occurrence of 'ch' in a string or the string null terminator or the 'length' character
		template <typename Str1, typename Char2, typename Char1 = typename string_traits<Str1>::value_type, typename = std::enable_if_t<is_string_v<Str1>>>
		inline Char1* FindChar(Str1&& str, Char2 ch, size_t length)
		{
			if constexpr (string_traits<Str1>::null_terminated)
			{
				auto ptr = string_traits<Str1>::ptr(str);
				for (; *ptr && length-- && static_cast<int>(*ptr) != static_cast<int>(ch); ++ptr) {}
				return ptr;
			}
			else
			{
				auto ptr = string_traits<Str1>::ptr(str);
				auto ptr_end = ptr + string_traits<Str1>::size(str, length);
				for (; ptr != ptr_end && static_cast<int>(*ptr) != static_cast<int>(ch); ++ptr) {}
				return ptr;
			}
		}

		#pragma endregion

		#pragma region FindStr

		// Find the sub string 'what' in the given range of characters.
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Iter, typename Str, typename Pred, typename = std::enable_if_t<is_string_v<Str>>>
		inline Iter FindStr(Iter first, Iter last, Str const& what, Pred pred)
		{
			if (Empty(what)) return last;
			auto what_len = static_cast<int>(Size(what));
			auto what_beg = Begin(what);
			for (; last - first >= what_len; ++first)
				if (pred(first, first + what_len, what_beg, what_beg + what_len))
					return first;

			return last;
		}
		template <typename Iter, typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Iter FindStr(Iter first, Iter last, Str const& what)
		{
			return FindStr(first, last, what, [](auto i, auto iend, auto j, auto jend)
			{
				return Equal(i, iend, j, jend);
			});
		}

		// Find the sub string 'what' in the given range of characters.
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Str1, typename Str2, typename Char1 = typename string_traits<Str1>::value_type, typename = std::enable_if_t<is_string_v<Str1>>>
		inline Char1* FindStr(Str1&& str, Str2 const& what)
		{
			return FindStr(Begin(str), End(str), what);
		}

		// Find the sub string 'what' in the range of characters provided (no case).
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Iter, typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Iter FindStrI(Iter first, Iter last, Str const& what)
		{
			return FindStr(first, last, what, [](auto i, auto iend, auto j, auto jend)
			{
				return EqualI(i, iend, j, jend);
			});
		}
		template <typename Str1, typename Str2, typename Char = typename string_traits<Str1>::value_type, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline Char* FindStrI(Str1&& str, Str2 const& what)
		{
			return FindStrI(Begin(str), End(str), what);
		}

		#pragma endregion

		#pragma region Find

		// Return an iterator to the first position that satisfies 'pred'
		template <typename Iter, typename Pred>
		inline Iter FindFirst(Iter beg, Iter end, Pred pred)
		{
			for (; beg != end && !pred(*beg); ++beg) {}
			return beg;
		}

		// Returns a pointer to the first character in '[offset, offset+count)' that satisfies 'pred', or a pointer to the end of the string or &str[offset+count]
		template <typename Str, typename Pred, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* FindFirst(Str&& str, size_t offset, size_t count, Pred pred)
		{
			return FindFirst(Begin(str) + offset, End(str, offset + count), pred);
		}

		// Returns a pointer to the first character in 'str' that satisfies 'pred', or a pointer to the end of the string
		template <typename Str, typename Pred, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* FindFirst(Str&& str, Pred pred)
		{
			return FindFirst(str, 0, ~size_t(), pred);
		}

		// Return an iterator to *one past* the last position that satisfies 'pred' or a pointer to the beginning of the string. Intended to be used to form a range with FindFirst/FindLast.
		template <typename Iter, typename Pred>
		inline Iter FindLast(Iter beg, Iter end, Pred pred)
		{
			for (; end != beg && !pred(*(end-1)); --end) {}
			return end;
		}

		// Returns a pointer to *one past* the last character in '[offset, offset+count)' that satisfies 'pred', or a pointer to the beginning of the string or &str[offset]
		template <typename Str, typename Pred, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* FindLast(Str&& str, size_t offset, size_t count, Pred pred)
		{
			return FindLast(Begin(str) + offset, End(str, offset + count), pred);
		}

		// Returns a pointer to *one past* the last character in 'str' that satisfies 'pred', or a pointer to the beginning of the string
		template <typename Str, typename Pred, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char* FindLast(Str&& str, Pred pred)
		{
			return FindLast(str, 0, ~size_t(), pred);
		}

		// Find the first occurrence of one of the chars in 'delim'
		template <typename Iter, typename Char>
		inline Iter FindFirstOf(Iter beg, Iter end, Char const* delim)
		{
			for (; beg != end && *FindChar(delim, *beg) == 0; ++beg) {}
			return beg;
		}
		template <typename Str, typename Char1, typename Char2 = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char2* FindFirstOf(Str&& str, Char1 const* delim)
		{
			return FindFirstOf(Begin(str), End(str), delim);
		}
		template <typename Iter, typename Char>
		inline size_t FindFirstOfAdv(Iter& str, Char const* delim)
		{
			auto count = size_t();
			for (; *str && *FindChar(delim, *str) == 0; ++str, ++count) {}
			return count;
		}
		template <typename Iter, typename Char>
		inline size_t FindFirstOfAdv(Iter& str, Iter end, Char const* delim)
		{
			auto count = size_t();
			for (; str != end && *FindChar(delim, *str) == 0; ++str, ++count) {}
			return count;
		}

		// Return a pointer to *one past* the last occurrence of one of the chars in 'delim'. Intended to be use with FindFirstOf to for a range
		template <typename Iter, typename Char>
		inline Iter FindLastOf(Iter beg, Iter end, Char const* delim)
		{
			for (; end != beg && *FindChar(delim, *(end-1)) == 0; --end) {}
			return end;
		}
		template <typename Str, typename Char1, typename Char2 = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char2* FindLastOf(Str&& str, Char1 const* delim)
		{
			return FindLastOf(Begin(str), End(str), delim);
		}

		// Find the first character not in the set 'delim'
		template <typename Iter, typename Char>
		inline Iter FindFirstNotOf(Iter beg, Iter end, Char const* delim)
		{
			for (; beg != end && *FindChar(delim, *beg) != 0; ++beg) {}
			return beg;
		}
		template <typename Str, typename Char1, typename Char2 = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char2* FindFirstNotOf(Str&& str, Char1 const* delim)
		{
			return FindFirstNotOf(Begin(str), End(str), delim);
		}
		template <typename Iter, typename Char>
		inline size_t FindFirstNotOfAdv(Iter& str, Char const* delim)
		{
			auto count = size_t();
			for (; *str && *FindChar(delim, *str) != 0; ++str, ++count) {}
			return count;
		}
		template <typename Iter, typename Char>
		inline size_t FindFirstNotOfAdv(Iter& str, Iter end, Char const* delim)
		{
			auto count = size_t();
			for (; str != end && *FindChar(delim, *str) != 0; ++str, ++count) {}
			return count;
		}

		// Return a pointer to *one past* the last character not in the set 'delim'
		template <typename Iter, typename Char>
		inline Iter FindLastNotOf(Iter beg, Iter end, Char const* delim)
		{
			for (; end != beg && *FindChar(delim, *(end-1)) != 0; --end) {}
			return end;
		}
		template <typename Str, typename Char1, typename Char2 = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
		inline Char2* FindLastNotOf(Str&& str, Char1 const* delim)
		{
			return FindLastNotOf(Begin(str), End(str), delim);
		}

		#pragma endregion

		#pragma region Resize

		// Resize a string. For pointers to fixed buffers, it's the callers responsibility to ensure sufficient space
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline void Resize(Str&& str, size_t new_size)
		{
			string_traits<Str>::resize(str, new_size);
		}
		template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
		inline void Resize(Str&& str, size_t new_size, Char const ch)
		{
			auto current_size = Size(str);
			Resize(str, new_size);

			auto const fill = char_cast<typename string_traits<Str>::value_type>(ch);
			for (; current_size < new_size; ++current_size)
				str[current_size] = fill;
		}

		#pragma endregion

		#pragma region Append

		// Append a char to the end of 'str'
		// 'len' is an optimisation for pointer-like strings so that Size() doesn't need to be called for each Append
		// Returns 'str' for method chaining
		template <typename Str1, typename Char2, typename = std::enable_if_t<is_string_v<Str1> && is_char_v<Char2>>>
		inline Str1&& Append(Str1&& str, Char2 ch, size_t& len)
		{
			Resize(str, len+1);
			str[len++] = char_cast<typename string_traits<Str1>::value_type>(ch);
			return std::forward<Str1>(str);
		}
		template <typename Str1, typename Char2, typename = std::enable_if_t<is_string_v<Str1> && is_char_v<Char2>>>
		inline Str1&& Append(Str1&& str, Char2 ch)
		{
			auto len = Size(str);
			return Append(str, ch, len);
		}

		// Append a string to the end of 'str'
		// 'len' is an optimisation for pointer-like strings so that Size() doesn't need to be called for each Append
		// Returns 'str' for method chaining
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline Str1&& Append(Str1&& str, Str2 const& s, size_t& len)
		{
			auto count = Size(s);
			auto ptr = string_traits<Str2 const>::ptr(s);
			Resize(str, len + count);
			for (; count-- != 0; )
				str[len++] = char_cast<typename string_traits<Str1>::value_type>(*ptr++);

			return std::forward<Str1>(str);
		}
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline Str1&& Append(Str1&& str, Str2 const& s)
		{
			auto len = Size(str);
			return Append(str, s, len);
		}
	
		#pragma endregion

		#pragma region Assign

		// Assign a range of characters to a sub-range within a string.
		// 'dest' is the string to be assigned to
		// 'offset' is the index position of where to start copying to
		// 'count' is the maximum number of characters to copy. The capacity of dest *must* be >= offset + count
		// 'first','last' the range of characters to assign to 'dest'
		// On return, dest will be resized to 'offset + min(count, last-first)'.
		template <typename Str, typename Iter, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str&& Assign(Str&& dest, size_t offset, size_t count, Iter first, Iter last)
		{
			// The number of characters to be copied to 'dest', clamped by 'count'
			auto size = std::min(count, size_t(last - first));

			// Set 'dest' to be the correct size. Assume 'dest' can be resized to 'offset + size'
			Resize(dest, offset + size);

			// Assign the characters
			for (auto out = Begin(dest) + offset; size--; ++out, ++first)
				*out = char_cast<typename string_traits<Str>::value_type>(*first);

			return std::forward<Str>(dest);
		}
		template <typename Str, typename Iter, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str&& Assign(Str&& dest, Iter first, Iter last)
		{
			return Assign(dest, 0, size_t(last - first), first, last);
		}

		// Assign a null terminated string to 'dest'
		template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str&& Assign(Str&& dest, size_t offset, size_t count, Char const* first)
		{
			Resize(dest, offset);
			for (;count-- != 0 && *first; ++first)
				Append(dest, *first, offset);

			return std::forward<Str>(dest);
		}
		template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str&& Assign(Str&& dest, Char const* first)
		{
			return Assign(dest, 0, ~size_t(), first);
		}

		#pragma endregion

		#pragma region Upper Case

		// Convert a string to upper case
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str& UpperCase(Str& str)
		{
			using traits = string_traits<Str>;

			auto i = Begin(str);
			auto iend = End(str);
			for (; i != iend; ++i)
				*i = traits::upr(*i);

			return str;
		}
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str UpperCaseC(Str const& str)
		{
			auto s = str;
			return UpperCase(s);
		}
		template <typename Char>
		inline std::basic_string<Char> UpperCaseC(Char const* str)
		{
			auto s = std::basic_string<Char>(str);
			return UpperCase(s);
		}

		#pragma endregion

		#pragma region Lower Case

		// Convert a string to lower case
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str& LowerCase(Str& str)
		{
			using traits = string_traits<Str>;

			auto i = Begin(str);
			auto iend = End(str);
			for (; i != iend; ++i)
				*i = traits::lwr(*i);

			return str;
		}
		template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str LowerCaseC(Str const& str)
		{
			auto s = str;
			return LowerCase(s);
		}
		template <typename Char>
		inline std::basic_string<Char> LowerCaseC(Char const* str)
		{
			auto s = std::basic_string<Char>(str);
			return LowerCase(s);
		}

		#pragma endregion

		#pragma region SubStr

		// Copy a substring from within 'src' to 'out'
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline Str2& SubStr(Str1 const& src, size_t offset, size_t count, Str2& out)
		{
			auto s = Begin(src) + offset;
			return Assign(out, 0, count, s, s + count);
		}
		template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
		inline Str2 SubStr(Str1 const& src, size_t offset, size_t count)
		{
			Str2 out;
			return SubStr(src, offset, count, out);
		}

		#pragma endregion

		#pragma region Split

		// Split a string at 'delims' outputting each sub string to 'out'
		// 'out' should have the signature out(tstr1 const& s, int i, int j, int n)
		// where [i,j) is the range in 's' containing the substring. 'n' is the index of the output subrange.
		// Returns the number of sub strings found.
		template <typename Str, typename Char, typename OutCB, typename = std::enable_if_t<is_string_v<Str>>>
		inline int Split(Str const& str, Char const* delims, OutCB out)
		{
			int i = 0, j = 0, jend = static_cast<int>(Size(str)), n = 0;
			for (; j != jend; ++j)
			{
				if (*FindChar(delims, str[j]) == 0) continue;
				out(str, i, j, n++);
				i = j + 1;
			}
			if (i != j)
			{
				out(str, i, j, n++);
			}
			return n;
		}

		#pragma endregion

		#pragma region Trim

		// Trim characters from a string
		// 'str' is the string to be trimmed
		// 'pred' should return true if the character should be trimmed
		// Returns 'str' for method chaining
		template <typename Str, typename Pred, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str& Trim(Str& str, Pred pred, bool front, bool back)
		{
			auto beg = Begin(str);
			auto end = End(str);
			auto first = front ? FindFirst(beg  , end, [&](auto ch){ return !pred(ch); }) : beg;
			auto last  = back  ? FindLast (first, end, [&](auto ch){ return !pred(ch); }) : end;

			// Move the non-trimmed characters to the front of the string and trim the tail
			auto out = beg;
			for (; first != last; ++first, ++out) { *out = *first; }
			Resize(str, out - beg);
			return str;
		}
		template <typename Str, typename Pred, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str Trim(Str const& str, Pred pred, bool front, bool back)
		{
			auto s = str;
			return Trim(s, pred, front, back);
		}
		template <typename Char, typename Pred>
		inline Char* Trim(Char* str, Pred pred, bool front, bool back)
		{
			return Trim<Char*,Pred,Char>(str, pred, front, back);
		}
		template <typename Char, typename Pred>
		inline std::basic_string<Char> Trim(Char const* str, Pred pred, bool front, bool back)
		{
			auto s = std::basic_string<Char>(str);
			return Trim(s, pred, front, back);
		}
		
		// Trim leading or trailing characters in 'chars' from 'str'.
		// Returns 'str' for method chaining
		template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str& TrimChars(Str& str, Char const* chars, bool front, bool back)
		{
			return Trim(str, [&](auto ch){ return *FindChar(chars, ch) != 0; }, front, back);
		}
		template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
		inline Str TrimChars(Str const& str, Char const* chars, bool front, bool back)
		{
			auto s = str;
			return TrimChars(s, chars, front, back);
		}
		template <typename Char1, typename Char2>
		inline Char1* TrimChars(Char1* str, Char2 const* chars, bool front, bool back)
		{
			return TrimChars<Char1*, Char2, Char1>(str, chars, front, back);
		}
		template <typename Char1, typename Char2>
		inline std::basic_string<Char1> TrimChars(Char1 const* str, Char2 const* chars, bool front, bool back)
		{
			auto s = std::basic_string<Char1>(str);
			return TrimChars(s, chars, front, back);
		}

		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::str
{
	PRUnitTest(StringCoreTests)
	{
		using namespace std::string_view_literals;

		// Notes:
		//  - The encoding of this file is expected to be UTF-8 so characters like
		//    "±" appear to the compiler as: ...,",-62,-79,",... in literal strings.
		//    Depending on the string type, the compiler turns each of these bytes
		//    into the character type, so: u"±" becomes char16_t[3] == {-62,-79,0}.
		//  - C# uses utf-16 for its strings and "".Length returns the array size,
		//    not the number of characters.

		{// Narrow
			{
				auto r = Narrow("Ab3");
				PR_CHECK(r.size(), 3U);
				PR_CHECK(r.c_str(), { 'A', 'b', '3', '\0' });
			}
			{
				auto r = Narrow("±1"sv);
				PR_CHECK(r.size(), 3U);
				PR_CHECK(r.c_str(), { -62, -79, 49, 0 });
			}
			{
				char const s[] = {0xe4_c8, 0xbd_c8, 0xa0_c8, 0xe5_c8, 0xa5_c8, 0xbd_c8, 0_c8}; // 'ni hao' in utf-8
				auto r = Narrow(s);
				PR_CHECK(r.size(), 6U);
				PR_CHECK(r.c_str(), {-28, -67, -96, -27, -91, -67, 0});
			}
			{
				wchar_t const s[] = {0x4f60, 0x597d, 0}; // 'ni hao' in utf-16
				auto r = Narrow(s);
				PR_CHECK(r.size(), 6U);
				PR_CHECK(r.c_str(), {-28, -67, -96, -27, -91, -67, 0});
			}
			{
				wchar_t const s0[] = L"z\u00df\u6c34\U0001f34c";
				char const s1[] = "zß水🍌";
				auto r0 = Narrow(s0);
				auto r1 = Narrow(s1);
				PR_CHECK(r0.size(), 10U);
				PR_CHECK(r1.size(), 10U);
				PR_CHECK(r0.c_str(), {0x7A_c8, 0xC3_c8, 0x9F_c8, 0xE6_c8, 0xB0_c8, 0xB4_c8, 0xF0_c8, 0x9F_c8, 0x8D_c8, 0x8C_c8, 0x00_c8});
				PR_CHECK(r1.c_str(), {0x7A_c8, 0xC3_c8, 0x9F_c8, 0xE6_c8, 0xB0_c8, 0xB4_c8, 0xF0_c8, 0x9F_c8, 0x8D_c8, 0x8C_c8, 0x00_c8});
			}
		}
		{// Widen
			{
				auto r = Widen("Ab3");
				PR_CHECK(r.size(), 3U);
				PR_CHECK(r.c_str(), { 'A', 'b', '3', '\0' });
			}
			{
				auto r = Widen("±1"sv);
				PR_CHECK(r.size(), 2U);
				PR_CHECK(r.c_str(), { 177, 49, 0 });
			}
			{
				char const s[] = {0xe4_c8, 0xbd_c8, 0xa0_c8, 0xe5_c8, 0xa5_c8, 0xbd_c8, 0_c8}; // 'ni hao' in utf-8
				auto r = Widen(s);
				PR_CHECK(r.size(), 2U);
				PR_CHECK(r.c_str(), {0x4f60, 0x597d, 0});
			}
			{
				wchar_t const s[] = {0x4f60, 0x597d, 0}; // 'ni hao' in utf-16
				auto r = Widen(s);
				PR_CHECK(r.size(), 2U);
				PR_CHECK(r.c_str(), {0x4f60, 0x597d, 0});
			}
			{
				wchar_t const s0[] = L"z\u00df\u6c34\U0001f34c";
				char const s1[] = "zß水🍌";
				auto r0 = Widen(s0);
				auto r1 = Widen(s1);
				PR_CHECK(r0.size(), 5U);
				PR_CHECK(r1.size(), 5U);
				PR_CHECK(r0.c_str(), {0x007a, 0x00df, 0x6c34, 0xd83c, 0xdf4c, 0});
				PR_CHECK(r1.c_str(), {0x007a, 0x00df, 0x6c34, 0xd83c, 0xdf4c, 0});
			}
		}
		{// ConvertEncoding
			{// ASCII to ASCII
				std::string s0 = "abc";
				auto r0 = ConvertEncoding<EEncoding::ascii, std::string, EEncoding::ascii>(s0);
				PR_CHECK(Equal(s0, r0), true);

				std::wstring s1 = L"abc";
				auto r1 = ConvertEncoding<EEncoding::ascii, std::string, EEncoding::ascii>(s1);
				PR_CHECK(Equal(s1, r1), true);
			}
			{
				char16_t s[] = u"\u00b1\U0001f34c";
				auto r = ConvertEncoding<EEncoding::utf8, std::string, EEncoding::utf16_le>(s);
				PR_CHECK(r.size(), 6U);
				PR_CHECK(r.c_str(), { -62, -79, -16, -97, -115, -116, 0 });
			}
			{
				char32_t s[] = U"\u00b1\U0001f34c";
				auto r = ConvertEncoding<EEncoding::ascii, std::wstring, EEncoding::utf32>(s, char(1));
				PR_CHECK(r.size(), 2U);
				PR_CHECK(r.c_str(), { 1, 1, 0 });
			}
			{//UCS2LE to UTF-8
				wchar_t const s[] = { 0x4f60, 0x597d }; // 'ni hao'
				auto r = ConvertEncoding<EEncoding::utf8, std::string, EEncoding::ucs2_le>(s);
				PR_CHECK(r.size(), 6U);
				PR_CHECK(r.c_str(), { 0xe4_c8, 0xbd_c8, 0xa0_c8, 0xe5_c8, 0xa5_c8, 0xbd_c8, 0_c8 });
			}
		}
		{// Empty
			char const*     aptr   = "full";
			char            aarr[] = "";
			std::string     astr   = "";
			wchar_t const*  wptr   = L"";
			wchar_t         warr[] = L"full";
			std::wstring    wstr   = L"full";

			PR_CHECK(!Empty(aptr), true);
			PR_CHECK( Empty(aarr), true);
			PR_CHECK( Empty(astr), true);
			PR_CHECK( Empty(wptr), true);
			PR_CHECK(!Empty(warr), true);
			PR_CHECK(!Empty(wstr), true);
		}
		{// Size
			char const*     aptr   = "length7";
			char            aarr[] = "length7";
			std::string     astr   = "length7";
			wchar_t const*  wptr   = L"length7";
			wchar_t         warr[] = L"length7";
			std::wstring    wstr   = L"length7";

			PR_CHECK(Size(aptr), size_t(7));
			PR_CHECK(Size(aarr), size_t(7));
			PR_CHECK(Size(astr), size_t(7));
			PR_CHECK(Size(wptr), size_t(7));
			PR_CHECK(Size(warr), size_t(7));
			PR_CHECK(Size(wstr), size_t(7));
		}
		{// Range
			char const*     aptr   =  "range";
			char            aarr[] =  "range";
			std::string     astr   =  "range";
			wchar_t const*  wptr   = L"range";
			wchar_t         warr[] = L"range";
			std::wstring    wstr   = L"range";

			PR_CHECK(*Begin(aptr) ==  'r' && *(End(aptr)-1) ==  'e', true);
			PR_CHECK(*Begin(aarr) ==  'r' && *(End(aarr)-1) ==  'e', true);
			PR_CHECK(*Begin(astr) ==  'r' && *(End(astr)-1) ==  'e', true);
			PR_CHECK(*Begin(wptr) == L'r' && *(End(wptr)-1) == L'e', true);
			PR_CHECK(*Begin(warr) == L'r' && *(End(warr)-1) == L'e', true);
			PR_CHECK(*Begin(wstr) == L'r' && *(End(wstr)-1) == L'e', true);
		}
		{// Equal
			char const*     aptr   =  "equal";
			char            aarr[] =  "equal";
			std::string     astr   =  "equal";
			wchar_t const*  wptr   = L"equal";
			wchar_t         warr[] = L"equal";
			std::wstring    wstr   = L"equal";

			PR_CHECK(Equal(aptr, aptr) && Equal(aptr, aarr) && Equal(aptr, astr) && Equal(aptr, wptr) && Equal(aptr, warr) && Equal(aptr, wstr), true);
			PR_CHECK(Equal(aarr, aptr) && Equal(aarr, aarr) && Equal(aarr, astr) && Equal(aarr, wptr) && Equal(aarr, warr) && Equal(aarr, wstr), true);
			PR_CHECK(Equal(astr, aptr) && Equal(astr, aarr) && Equal(astr, astr) && Equal(astr, wptr) && Equal(astr, warr) && Equal(astr, wstr), true);
			PR_CHECK(Equal(wptr, aptr) && Equal(wptr, aarr) && Equal(wptr, astr) && Equal(wptr, wptr) && Equal(wptr, warr) && Equal(wptr, wstr), true);
			PR_CHECK(Equal(warr, aptr) && Equal(warr, aarr) && Equal(warr, astr) && Equal(warr, wptr) && Equal(warr, warr) && Equal(warr, wstr), true);
			PR_CHECK(Equal(wstr, aptr) && Equal(wstr, aarr) && Equal(wstr, astr) && Equal(wstr, wptr) && Equal(wstr, warr) && Equal(wstr, wstr), true);
			PR_CHECK(Equal(aptr, "equal!"), false);
			PR_CHECK(Equal(aarr, "equal!"), false);
			PR_CHECK(Equal(astr, "equal!"), false);
			PR_CHECK(Equal(wptr, "equal!"), false);
			PR_CHECK(Equal(warr, "equal!"), false);
			PR_CHECK(Equal(wstr, "equal!"), false);
		}
		{// EqualI
			char const*     aptr   =  "Equal";
			char            aarr[] =  "eQual";
			std::string     astr   =  "eqUal";
			wchar_t const*  wptr   = L"equAl";
			wchar_t         warr[] = L"equaL";
			std::wstring    wstr   = L"EQUAL";

			PR_CHECK(EqualI(aptr, aptr) && EqualI(aptr, aarr) && EqualI(aptr, astr) && EqualI(aptr, wptr) && EqualI(aptr, warr) && EqualI(aptr, wstr), true);
			PR_CHECK(EqualI(aarr, aptr) && EqualI(aarr, aarr) && EqualI(aarr, astr) && EqualI(aarr, wptr) && EqualI(aarr, warr) && EqualI(aarr, wstr), true);
			PR_CHECK(EqualI(astr, aptr) && EqualI(astr, aarr) && EqualI(astr, astr) && EqualI(astr, wptr) && EqualI(astr, warr) && EqualI(astr, wstr), true);
			PR_CHECK(EqualI(wptr, aptr) && EqualI(wptr, aarr) && EqualI(wptr, astr) && EqualI(wptr, wptr) && EqualI(wptr, warr) && EqualI(wptr, wstr), true);
			PR_CHECK(EqualI(warr, aptr) && EqualI(warr, aarr) && EqualI(warr, astr) && EqualI(warr, wptr) && EqualI(warr, warr) && EqualI(warr, wstr), true);
			PR_CHECK(EqualI(wstr, aptr) && EqualI(wstr, aarr) && EqualI(wstr, astr) && EqualI(wstr, wptr) && EqualI(wstr, warr) && EqualI(wstr, wstr), true);
			PR_CHECK(EqualI(aptr, "equal!"), false);
			PR_CHECK(EqualI(aarr, "equal!"), false);
			PR_CHECK(EqualI(astr, "equal!"), false);
			PR_CHECK(EqualI(wptr, "equal!"), false);
			PR_CHECK(EqualI(warr, "equal!"), false);
			PR_CHECK(EqualI(wstr, "equal!"), false);
		}
		{// EqualN
			char const*     aptr   =  "equal1";
			char            aarr[] =  "equal2";
			std::string     astr   =  "equal3";
			wchar_t const*  wptr   = L"equal4";
			wchar_t         warr[] = L"equal5";
			std::wstring    wstr   = L"equal6";

			PR_CHECK(EqualN(aptr, aptr, 5) && EqualN(aptr, aarr, 5) && EqualN(aptr, astr, 5) && EqualN(aptr, wptr, 5) && EqualN(aptr, warr, 5) && EqualN(aptr, wstr, 5), true);
			PR_CHECK(EqualN(aarr, aptr, 5) && EqualN(aarr, aarr, 5) && EqualN(aarr, astr, 5) && EqualN(aarr, wptr, 5) && EqualN(aarr, warr, 5) && EqualN(aarr, wstr, 5), true);
			PR_CHECK(EqualN(astr, aptr, 5) && EqualN(astr, aarr, 5) && EqualN(astr, astr, 5) && EqualN(astr, wptr, 5) && EqualN(astr, warr, 5) && EqualN(astr, wstr, 5), true);
			PR_CHECK(EqualN(wptr, aptr, 5) && EqualN(wptr, aarr, 5) && EqualN(wptr, astr, 5) && EqualN(wptr, wptr, 5) && EqualN(wptr, warr, 5) && EqualN(wptr, wstr, 5), true);
			PR_CHECK(EqualN(warr, aptr, 5) && EqualN(warr, aarr, 5) && EqualN(warr, astr, 5) && EqualN(warr, wptr, 5) && EqualN(warr, warr, 5) && EqualN(warr, wstr, 5), true);
			PR_CHECK(EqualN(wstr, aptr, 5) && EqualN(wstr, aarr, 5) && EqualN(wstr, astr, 5) && EqualN(wstr, wptr, 5) && EqualN(wstr, warr, 5) && EqualN(wstr, wstr, 5), true);
			PR_CHECK(EqualN(aptr, "equal!", 6), false);
			PR_CHECK(EqualN(aarr, "equal!", 6), false);
			PR_CHECK(EqualN(astr, "equal!", 6), false);
			PR_CHECK(EqualN(wptr, "equal!", 6), false);
			PR_CHECK(EqualN(warr, "equal!", 6), false);
			PR_CHECK(EqualN(wstr, "equal!", 6), false);
		}
		{// EqualNI
			char const*     aptr   =  "Equal1";
			char            aarr[] =  "eQual2";
			std::string     astr   =  "eqUal3";
			wchar_t const*  wptr   = L"equAl4";
			wchar_t         warr[] = L"equaL5";
			std::wstring    wstr   = L"EQUAL6";

			PR_CHECK(EqualNI(aptr, aptr, 5) && EqualNI(aptr, aarr, 5) && EqualNI(aptr, astr, 5) && EqualNI(aptr, wptr, 5) && EqualNI(aptr, warr, 5) && EqualNI(aptr, wstr, 5), true);
			PR_CHECK(EqualNI(aarr, aptr, 5) && EqualNI(aarr, aarr, 5) && EqualNI(aarr, astr, 5) && EqualNI(aarr, wptr, 5) && EqualNI(aarr, warr, 5) && EqualNI(aarr, wstr, 5), true);
			PR_CHECK(EqualNI(astr, aptr, 5) && EqualNI(astr, aarr, 5) && EqualNI(astr, astr, 5) && EqualNI(astr, wptr, 5) && EqualNI(astr, warr, 5) && EqualNI(astr, wstr, 5), true);
			PR_CHECK(EqualNI(wptr, aptr, 5) && EqualNI(wptr, aarr, 5) && EqualNI(wptr, astr, 5) && EqualNI(wptr, wptr, 5) && EqualNI(wptr, warr, 5) && EqualNI(wptr, wstr, 5), true);
			PR_CHECK(EqualNI(warr, aptr, 5) && EqualNI(warr, aarr, 5) && EqualNI(warr, astr, 5) && EqualNI(warr, wptr, 5) && EqualNI(warr, warr, 5) && EqualNI(warr, wstr, 5), true);
			PR_CHECK(EqualNI(wstr, aptr, 5) && EqualNI(wstr, aarr, 5) && EqualNI(wstr, astr, 5) && EqualNI(wstr, wptr, 5) && EqualNI(wstr, warr, 5) && EqualNI(wstr, wstr, 5), true);
			PR_CHECK(EqualNI(aptr, "equal!", 6), false);
			PR_CHECK(EqualNI(aarr, "equal!", 6), false);
			PR_CHECK(EqualNI(astr, "equal!", 6), false);
			PR_CHECK(EqualNI(wptr, "equal!", 6), false);
			PR_CHECK(EqualNI(warr, "equal!", 6), false);
			PR_CHECK(EqualNI(wstr, "equal!", 6), false);
		}
		{// FindChar
			char const*     aptr   =  "find char";
			char            aarr[] =  "find char";
			std::string     astr   =  "find char";
			wchar_t const*  wptr   = L"find char";
			wchar_t         warr[] = L"find char";
			std::wstring    wstr   = L"find char";

			PR_CHECK(*FindChar(aptr, 'i') ==  'i' && *FindChar(aptr,  'b') == 0, true);
			PR_CHECK(*FindChar(aarr,L'i') ==  'i' && *FindChar(aarr, L'b') == 0, true);
			PR_CHECK(*FindChar(astr, 'i') ==  'i' && *FindChar(astr,  'b') == 0, true);
			PR_CHECK(*FindChar(wptr, 'i') == L'i' && *FindChar(wptr, L'b') == 0, true);
			PR_CHECK(*FindChar(warr,L'i') == L'i' && *FindChar(warr,  'b') == 0, true);
			PR_CHECK(*FindChar(wstr, 'i') == L'i' && *FindChar(wstr, L'b') == 0, true);
		}
		{// FindChar N
			char const*     aptr   =  "find char";
			char            aarr[] =  "find char";
			std::string     astr   =  "find char";
			wchar_t const*  wptr   = L"find char";
			wchar_t         warr[] = L"find char";
			std::wstring    wstr   = L"find char";

			PR_CHECK(*FindChar(aptr, 'i', 2) ==  'i' && *FindChar(aptr,  'c', 4) == ' ', true);
			PR_CHECK(*FindChar(aarr,L'i', 2) ==  'i' && *FindChar(aarr, L'c', 4) == ' ', true);
			PR_CHECK(*FindChar(astr, 'i', 2) ==  'i' && *FindChar(astr,  'c', 4) == ' ', true);
			PR_CHECK(*FindChar(wptr, 'i', 2) == L'i' && *FindChar(wptr, L'c', 4) == ' ', true);
			PR_CHECK(*FindChar(warr,L'i', 2) == L'i' && *FindChar(warr,  'c', 4) == ' ', true);
			PR_CHECK(*FindChar(wstr, 'i', 2) == L'i' && *FindChar(wstr, L'c', 4) == ' ', true);
		}
		{// FindStr
			char const*     aptr   =  "find in str";
			char            aarr[] =  "find in str";
			std::string     astr   =  "find in str";
			wchar_t const*  wptr   = L"find in str";
			wchar_t         warr[] = L"find in str";
			std::wstring    wstr   = L"find in str";

			PR_CHECK(*FindStr(aptr, "str") ==  's' && FindStr(aptr,  "bob") == End(aptr), true);
			PR_CHECK(*FindStr(aarr,L"str") ==  's' && FindStr(aarr, L"bob") == End(aarr), true);
			PR_CHECK(*FindStr(astr, "str") ==  's' && FindStr(astr,  "bob") == End(astr), true);
			PR_CHECK(*FindStr(wptr, "str") == L's' && FindStr(wptr, L"bob") == End(wptr), true);
			PR_CHECK(*FindStr(warr,L"str") == L's' && FindStr(warr,  "bob") == End(warr), true);
			PR_CHECK(*FindStr(wstr, "str") == L's' && FindStr(wstr, L"bob") == End(wstr), true);

			PR_CHECK(FindStr(aptr + 2, aptr + 9, "in") - Begin(aptr) == 5, true);
			PR_CHECK(FindStr(wptr + 2, wptr + 9, "in") - Begin(wptr) == 5, true);
		}
		{// FindFirst                  0123456789
			char const*     aptr   =  "find first";
			char            aarr[] =  "find first";
			std::string     astr   =  "find first";
			wchar_t const*  wptr   = L"find first";
			wchar_t         warr[] = L"find first";
			std::wstring    wstr   = L"find first";

			PR_CHECK(FindFirst(aptr, [](char    ch){ return ch ==  'i'; }) == &aptr[0] + 1, true);
			PR_CHECK(FindFirst(aarr, [](char    ch){ return ch ==  'i'; }) == &aarr[0] + 1, true);
			PR_CHECK(FindFirst(astr, [](char    ch){ return ch ==  'i'; }) == &astr[0] + 1, true);
			PR_CHECK(FindFirst(wptr, [](wchar_t ch){ return ch == L'i'; }) == &wptr[0] + 1, true);
			PR_CHECK(FindFirst(warr, [](wchar_t ch){ return ch == L'i'; }) == &warr[0] + 1, true);
			PR_CHECK(FindFirst(wstr, [](wchar_t ch){ return ch == L'i'; }) == &wstr[0] + 1, true);

			PR_CHECK(FindFirst(aptr, [](char    ch){ return ch ==  'x'; }) == &aptr[0] + 10, true);
			PR_CHECK(FindFirst(aarr, [](char    ch){ return ch ==  'x'; }) == &aarr[0] + 10, true);
			PR_CHECK(FindFirst(astr, [](char    ch){ return ch ==  'x'; }) == &astr[0] + 10, true);
			PR_CHECK(FindFirst(wptr, [](wchar_t ch){ return ch == L'x'; }) == &wptr[0] + 10, true);
			PR_CHECK(FindFirst(warr, [](wchar_t ch){ return ch == L'x'; }) == &warr[0] + 10, true);
			PR_CHECK(FindFirst(wstr, [](wchar_t ch){ return ch == L'x'; }) == &wstr[0] + 10, true);

			PR_CHECK(FindFirst(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  'i'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindFirst(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  't'; }) == &aptr[0] + 8, true);

			PR_CHECK(FindFirst(aptr, 2, 6, [](char ch){ return ch ==  'i'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindFirst(aptr, 2, 6, [](char ch){ return ch ==  't'; }) == &aptr[0] + 8, true);
		}
		{//FindLast                    0123456789
			char const*     aptr   =  "find flast";
			char            aarr[] =  "find flast";
			std::string     astr   =  "find flast";
			wchar_t const*  wptr   = L"find flast";
			wchar_t         warr[] = L"find flast";
			std::wstring    wstr   = L"find flast";

			PR_CHECK(FindLast(aptr, [](char    ch){ return ch ==  'f'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindLast(aarr, [](char    ch){ return ch ==  'f'; }) == &aarr[0] + 6, true);
			PR_CHECK(FindLast(astr, [](char    ch){ return ch ==  'f'; }) == &astr[0] + 6, true);
			PR_CHECK(FindLast(wptr, [](wchar_t ch){ return ch == L'f'; }) == &wptr[0] + 6, true);
			PR_CHECK(FindLast(warr, [](wchar_t ch){ return ch == L'f'; }) == &warr[0] + 6, true);
			PR_CHECK(FindLast(wstr, [](wchar_t ch){ return ch == L'f'; }) == &wstr[0] + 6, true);

			PR_CHECK(FindLast(aptr, [](char    ch){ return ch ==  'x'; }) == &aptr[0] + 0, true);
			PR_CHECK(FindLast(aarr, [](char    ch){ return ch ==  'x'; }) == &aarr[0] + 0, true);
			PR_CHECK(FindLast(astr, [](char    ch){ return ch ==  'x'; }) == &astr[0] + 0, true);
			PR_CHECK(FindLast(wptr, [](wchar_t ch){ return ch == L'x'; }) == &wptr[0] + 0, true);
			PR_CHECK(FindLast(warr, [](wchar_t ch){ return ch == L'x'; }) == &warr[0] + 0, true);
			PR_CHECK(FindLast(wstr, [](wchar_t ch){ return ch == L'x'; }) == &wstr[0] + 0, true);

			PR_CHECK(FindLast(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  'f'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindLast(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  't'; }) == &aptr[0] + 2, true);

			PR_CHECK(FindLast(aptr, 2, 6, [](char ch){ return ch ==  'f'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindLast(aptr, 2, 6, [](char ch){ return ch ==  't'; }) == &aptr[0] + 2, true);
		}
		{// FindFirstOf             0123456
			char         aarr[] =  "AaAaAa";
			wchar_t      warr[] = L"AaAaAa";
			std::string  astr   =  "AaAaAa";
			std::wstring wstr   = L"AaAaAa";

			PR_CHECK(FindFirstOf(aarr, "A") == &aarr[0] + 0, true);
			PR_CHECK(FindFirstOf(warr, "a") == &warr[0] + 1, true);
			PR_CHECK(FindFirstOf(astr, "B") == &astr[0] + 6, true);
			PR_CHECK(FindFirstOf(wstr, "B") == &wstr[0] + 6, true);
		}
		{//FindLastOf               0123456
			char         aarr[] =  "AaAaAa";
			wchar_t      warr[] = L"AaAaaa";
			std::string  astr   =  "AaAaaa";
			std::wstring wstr   = L"Aaaaaa";
			PR_CHECK(FindLastOf(aarr, L"A") == &aarr[0] + 5, true);
			PR_CHECK(FindLastOf(warr, L"A") == &warr[0] + 3, true);
			PR_CHECK(FindLastOf(astr, L"B") == &astr[0], true);
			PR_CHECK(FindLastOf(wstr, L"B") == &wstr[0], true);
		}
		{//FindFirstNotOf           01234567890123
			char         aarr[] =  "junk_str_junk";
			wchar_t      warr[] = L"junk_str_junk";
			std::string  astr   =  "junk_str_junk";
			std::wstring wstr   = L"junk_str_junk";
			PR_CHECK(FindFirstNotOf(aarr, "_knuj"   ) == &aarr[0] + 5, true);
			PR_CHECK(FindFirstNotOf(warr, "_knuj"   ) == &warr[0] + 5, true);
			PR_CHECK(FindFirstNotOf(astr, "_knujstr") == &astr[0] + 13, true);
			PR_CHECK(FindFirstNotOf(wstr, "_knujstr") == &wstr[0] + 13, true);
		}
		{//FindLastNotOf            01234567890123
			char         aarr[] =  "junk_str_junk";
			wchar_t      warr[] = L"junk_str_junk";
			std::string  astr   =  "junk_str_junk";
			std::wstring wstr   = L"junk_str_junk";
			PR_CHECK(FindLastNotOf(aarr, "_knuj"   ) == &aarr[0] + 8, true);
			PR_CHECK(FindLastNotOf(warr, "_knuj"   ) == &warr[0] + 8, true);
			PR_CHECK(FindLastNotOf(astr, "_knujstr") == &astr[0] + 0, true);
			PR_CHECK(FindLastNotOf(wstr, "_knujstr") == &wstr[0] + 0, true);
		}
		{// Resize
			char            aarr[] = {'a','a','a','a'};
			wchar_t         warr[] = {L'a',L'a',L'a',L'a'};
			std::string     astr   = "aaaa";
			std::wstring    wstr   = L"aaaa";

			Resize(aarr, 2); PR_CHECK(Equal(aarr, "aa"), true);
			Resize(warr, 2); PR_CHECK(Equal(warr, "aa"), true);
			Resize(astr, 2); PR_CHECK(Equal(astr, "aa"), true);
			Resize(wstr, 2); PR_CHECK(Equal(wstr, "aa"), true);

			Resize(aarr, 3, 'b'); PR_CHECK(Equal(aarr, "aab"), true);
			Resize(warr, 3, 'b'); PR_CHECK(Equal(warr, "aab"), true);
			Resize(astr, 3, 'b'); PR_CHECK(Equal(astr, "aab"), true);
			Resize(wstr, 3, 'b'); PR_CHECK(Equal(wstr, "aab"), true);
		}
		{// Append
			char         aarr[5] = {};
			wchar_t      warr[5] = {};
			std::string  astr;
			std::wstring wstr;

			Append(aarr, 'a'); Append(aarr, L'b'); Append(aarr, 'c'); PR_CHECK(Equal(aarr, "abc"), true);
			Append(warr, 'a'); Append(warr, L'b'); Append(warr, 'c'); PR_CHECK(Equal(warr, "abc"), true);
			Append(astr, 'a'); Append(astr, L'b'); Append(astr, 'c'); PR_CHECK(Equal(astr, "abc"), true);
			Append(wstr, 'a'); Append(wstr, L'b'); Append(wstr, 'c'); PR_CHECK(Equal(wstr, "abc"), true);
		}
		{// Append string
			char         aarr[7] = {};
			wchar_t      warr[7] = {};
			std::string  astr;
			std::wstring wstr;

			Append(aarr, "abc"); Append(aarr, L"def"); PR_CHECK(Equal(aarr, "abcdef"), true);
			Append(warr, "abc"); Append(warr, L"def"); PR_CHECK(Equal(warr, "abcdef"), true);
			Append(astr, "abc"); Append(astr, L"def"); PR_CHECK(Equal(astr, "abcdef"), true);
			Append(wstr, "abc"); Append(wstr, L"def"); PR_CHECK(Equal(wstr, "abcdef"), true);
		}
		{// Assign
			char const*     asrc = "string";
			wchar_t const*  wsrc = L"string";

			char            aarr[5];
			wchar_t         warr[5];
			std::string     astr;
			std::wstring    wstr;

			Assign(aarr, asrc, asrc+3); PR_CHECK(Equal(aarr, "str"), true);
			Assign(aarr, wsrc, wsrc+3); PR_CHECK(Equal(aarr, "str"), true);
			Assign(warr, asrc, asrc+3); PR_CHECK(Equal(warr, "str"), true);
			Assign(warr, wsrc, wsrc+3); PR_CHECK(Equal(warr, "str"), true);
			Assign(astr, asrc, asrc+3); PR_CHECK(Equal(astr, "str"), true);
			Assign(astr, wsrc, wsrc+3); PR_CHECK(Equal(astr, "str"), true);
			Assign(wstr, asrc, asrc+3); PR_CHECK(Equal(wstr, "str"), true);
			Assign(wstr, wsrc, wsrc+3); PR_CHECK(Equal(wstr, "str"), true);

			Assign(aarr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(aarr, "stst"), true);
			Assign(aarr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(aarr, "stst"), true);
			Assign(warr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(warr, "stst"), true);
			Assign(warr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(warr, "stst"), true);
			Assign(astr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(astr, "stst"), true);
			Assign(astr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(astr, "stst"), true);
			Assign(wstr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(wstr, "stst"), true);
			Assign(wstr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(wstr, "stst"), true);

			Assign(astr, 2, ~size_t(), asrc, asrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
			Assign(astr, 2, ~size_t(), wsrc, wsrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
			Assign(wstr, 2, ~size_t(), asrc, asrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);
			Assign(wstr, 2, ~size_t(), wsrc, wsrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);

			Assign(astr, 2, ~size_t(), "ab"); PR_CHECK(Equal(astr, "stab"), true);
			Assign(astr, 2, ~size_t(), "ab"); PR_CHECK(Equal(astr, "stab"), true);
			Assign(wstr, 2, ~size_t(), "ab"); PR_CHECK(Equal(wstr, "stab"), true);
			Assign(wstr, 2, ~size_t(), "ab"); PR_CHECK(Equal(wstr, "stab"), true);

			Assign(astr, "done"); PR_CHECK(Equal(astr, "done"), true);
			Assign(astr, "done"); PR_CHECK(Equal(astr, "done"), true);
			Assign(wstr, "done"); PR_CHECK(Equal(wstr, "done"), true);
			Assign(wstr, "done"); PR_CHECK(Equal(wstr, "done"), true);
		}
		{//UpperCase
			char         aarr[5] =  "CaSe";
			wchar_t      warr[5] = L"CaSe";
			std::string  astr    =  "CaSe";
			std::wstring wstr    = L"CaSe";

			PR_CHECK(Equal(UpperCaseC(aarr), L"CASE"), true); PR_CHECK(Equal(aarr, "CaSe"), true);
			PR_CHECK(Equal(UpperCase (warr), L"CASE"), true); PR_CHECK(Equal(warr, "CASE"), true);
			PR_CHECK(Equal(UpperCase (astr), L"CASE"), true); PR_CHECK(Equal(astr, "CASE"), true);
			PR_CHECK(Equal(UpperCaseC(wstr), L"CASE"), true); PR_CHECK(Equal(wstr, "CaSe"), true);
		}
		{//LowerCase
			char         aarr[5] =  "CaSe";
			wchar_t      warr[5] = L"CaSe";
			std::string  astr    =  "CaSe";
			std::wstring wstr    = L"CaSe";

			PR_CHECK(Equal(LowerCaseC(aarr), L"case"), true); PR_CHECK(Equal(aarr, "CaSe"), true);
			PR_CHECK(Equal(LowerCase (warr), L"case"), true); PR_CHECK(Equal(warr, "case"), true);
			PR_CHECK(Equal(LowerCase (astr), L"case"), true); PR_CHECK(Equal(astr, "case"), true);
			PR_CHECK(Equal(LowerCaseC(wstr), L"case"), true); PR_CHECK(Equal(wstr, "CaSe"), true);
		}
		{//SubStr
			char    asrc[] =  "SubstringExtract";
			wchar_t wsrc[] = L"SubstringExtract";

			char         aarr[10] = {};
			wchar_t      warr[10] = {};
			std::string  astr;
			std::wstring wstr;

			SubStr(asrc, 3, 6, aarr); PR_CHECK(Equal(aarr, "string"), true);
			SubStr(asrc, 3, 6, warr); PR_CHECK(Equal(warr, "string"), true);
			SubStr(asrc, 3, 6, astr); PR_CHECK(Equal(astr, "string"), true);
			SubStr(asrc, 3, 6, wstr); PR_CHECK(Equal(wstr, "string"), true);

			SubStr(wsrc, 3, 6, aarr); PR_CHECK(Equal(aarr, "string"), true);
			SubStr(wsrc, 3, 6, warr); PR_CHECK(Equal(warr, "string"), true);
			SubStr(wsrc, 3, 6, astr); PR_CHECK(Equal(astr, "string"), true);
			SubStr(wsrc, 3, 6, wstr); PR_CHECK(Equal(wstr, "string"), true);
		}
		{// Split
			char    astr[] = "1,,2,3,4";
			wchar_t wstr[] = L"1,,2,3,4";
			char    res[][2] = {"1","","2","3","4"};
			int i;

			std::vector<std::string> abuf;
			Split(astr, L",", [&](char const* s, size_t i, size_t iend, int)
				{
					abuf.push_back(std::string(s+i, s+iend));
				});
			i = 0; for (auto& s : abuf)
				PR_CHECK(Equal(s, res[i++]), true);

			std::vector<std::wstring> wbuf;
			Split(wstr, ",", [&](wchar_t const* s, size_t i, size_t iend, int)
				{
					wbuf.push_back(std::wstring(s+i, s+iend));
				});
			i = 0; for (auto& s : wbuf)
				PR_CHECK(Equal(s, res[i++]), true);
		}
		{// Trim
			char            aarr[] =  " \t,trim\n";
			std::string     astr   =  " \t,trim\n";
			wchar_t         warr[] = L" \t,trim\n";
			std::wstring    wstr   = L" \t,trim\n";
			auto aws = IsWhiteSpace<char>;
			auto wws = IsWhiteSpace<wchar_t>;

			PR_CHECK(Equal(Trim(aarr, aws, true, true), ",trim"), true);
			PR_CHECK(Equal(Trim(astr, aws, true, true), ",trim"), true);
			PR_CHECK(Equal(Trim(warr, wws, true, true), ",trim"), true);
			PR_CHECK(Equal(Trim(wstr, wws, true, true), ",trim"), true);

			PR_CHECK(Equal(Trim( " \t,trim\n", aws, true, false), ",trim\n") , true);
			PR_CHECK(Equal(Trim( " \t,trim\n", aws, true, false), ",trim\n") , true);
			PR_CHECK(Equal(Trim(L" \t,trim\n", wws, true, false), ",trim\n") , true);
			PR_CHECK(Equal(Trim(L" \t,trim\n", wws, true, false), ",trim\n") , true);

			PR_CHECK(Equal(Trim( " \t,trim\n", aws, false, true), " \t,trim"), true);
			PR_CHECK(Equal(Trim( " \t,trim\n", aws, false, true), " \t,trim"), true);
			PR_CHECK(Equal(Trim(L" \t,trim\n", wws, false, true), " \t,trim"), true);
			PR_CHECK(Equal(Trim(L" \t,trim\n", wws, false, true), " \t,trim"), true);

			PR_CHECK(Equal(TrimChars( " \t,trim\n",  " \t,\n" ,true  ,true) ,  "trim"    ), true);
			PR_CHECK(Equal(TrimChars( " \t,trim\n", L" \t,\n" ,true  ,true) , L"trim"    ), true);
			PR_CHECK(Equal(TrimChars(L" \t,trim\n",  " \t,\n" ,true  ,false),  "trim\n"  ), true);
			PR_CHECK(Equal(TrimChars(L" \t,trim\n", L" \t,\n" ,false ,true) , L" \t,trim"), true);

			PR_CHECK(Equal(Trim(" \t ", aws, false, true), ""), true);
		}
	}
}
#endif



