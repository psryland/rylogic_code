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

namespace pr
{
	// Narrow
	inline std::string Narrow(char const* from, std::size_t len = 0)
	{
		if (len == 0) len = strlen(from); 
		return std::string(from, from+len);
	}
	inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
	{
		if (len == 0) len = wcslen(from);
		std::locale const loc("");
		std::vector<char> buffer(len + 1);
		std::use_facet<std::ctype<wchar_t>>(loc).narrow(from, from + len, '_', &buffer[0]);
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
		std::locale const loc("");
		std::vector<wchar_t> buffer(len + 1);
		std::use_facet<std::ctype<wchar_t>>(loc).widen(from, from + len, &buffer[0]);
		return std::wstring(&buffer[0], &buffer[len]);
	}
	template <std::size_t Len> inline std::wstring Widen (wchar_t const (&from)[Len]) { return Widen(from, Len); }
	template <std::size_t Len> inline std::wstring Widen (char    const (&from)[Len]) { return Widen(from, Len); }

	// To<std::string>
	template <> inline std::string To<std::string>(unsigned long from, int radix)         { char buf[128]; _ultoa_s(from, buf, radix); return buf; }
	template <> inline std::string To<std::string>(unsigned int from, int radix)          { return To<std::string>(static_cast<unsigned long>(from), radix); }
	template <> inline std::string To<std::string>(unsigned short from, int radix)        { return To<std::string>(static_cast<unsigned long>(from), radix); }
	template <> inline std::string To<std::string>(unsigned char from, int radix)         { return To<std::string>(static_cast<unsigned long>(from), radix); }
	template <> inline std::string To<std::string>(long from, int radix)                  { char buf[128]; _ltoa_s (from, buf, radix); return buf; }
	template <> inline std::string To<std::string>(int from, int radix)                   { return To<std::string>(static_cast<long>(from), radix); }
	template <> inline std::string To<std::string>(short from, int radix)                 { return To<std::string>(static_cast<long>(from), radix); }
	template <> inline std::string To<std::string>(char from)                             { return std::string(1, from); }
	template <> inline std::string To<std::string>(bool from)                             { return from ? "true" : "false"; }
	template <> inline std::string To<std::string>(long double from)                      { return std::to_string(from); }//char buf[_CVTBUFSIZE]; _gcvt_s(buf, from, 12); return buf; }
	template <> inline std::string To<std::string>(double from)                           { return To<std::string>(static_cast<long double>(from)); }
	template <> inline std::string To<std::string>(float from)                            { return To<std::string>(static_cast<long double>(from)); }
	template <> inline std::string To<std::string>(char const* from)                      { return from; }
	template <> inline std::string To<std::string>(std::string const& from)               { return from; }
	template <> inline std::string To<std::string>(wchar_t const* from)                   { return Narrow(from, wcslen(from)); }
	template <> inline std::string To<std::string>(std::wstring const& from)              { return Narrow(from.c_str(), from.size()); }

	// To<std::wstring>
	template <> inline std::wstring To<std::wstring>(unsigned long from, int radix)       { wchar_t buf[128]; _ultow_s(from, buf, radix); return buf; }
	template <> inline std::wstring To<std::wstring>(unsigned int from, int radix)        { return To<std::wstring>(static_cast<unsigned long>(from), radix); }
	template <> inline std::wstring To<std::wstring>(unsigned short from, int radix)      { return To<std::wstring>(static_cast<unsigned long>(from), radix); }
	template <> inline std::wstring To<std::wstring>(unsigned char from, int radix)       { return To<std::wstring>(static_cast<unsigned long>(from), radix); }
	template <> inline std::wstring To<std::wstring>(long from, int radix)                { wchar_t buf[128]; _ltow_s(from, buf, radix); return buf; }
	template <> inline std::wstring To<std::wstring>(int from, int radix)                 { return To<std::wstring>(static_cast<long>(from), radix); }
	template <> inline std::wstring To<std::wstring>(short from, int radix)               { return To<std::wstring>(static_cast<long>(from), radix); }
	template <> inline std::wstring To<std::wstring>(char from)                           { return std::wstring(1, from); }
	template <> inline std::wstring To<std::wstring>(bool from)                           { return from ? L"true" : L"false"; }
	template <> inline std::wstring To<std::wstring>(long double from)                    { return std::to_wstring(from); }
	template <> inline std::wstring To<std::wstring>(double from)                         { return std::to_wstring(static_cast<long double>(from)); }
	template <> inline std::wstring To<std::wstring>(float from)                          { return std::to_wstring(static_cast<long double>(from)); }
	template <> inline std::wstring To<std::wstring>(wchar_t const* from)                 { return from; }
	template <> inline std::wstring To<std::wstring>(std::wstring const& from)            { return from; }
	template <> inline std::wstring To<std::wstring>(char const* from)                    { return Widen(from, strlen(from)); }
	template <> inline std::wstring To<std::wstring>(std::string const& from)             { return Widen(from.c_str(), from.size()); }
}

// Can't add unittests because the unit test framework uses this header

#endif
