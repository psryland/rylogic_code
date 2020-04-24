//**********************************************
// File path/File system operations
//  Copyright (c) Rylogic Ltd 2009
//**********************************************
#pragma once
#include <string>
#include <algorithm>
#include <type_traits>
#include <filesystem>
#include <fstream>
#include <locale>
#include <codecvt>
#include <ctime>
#include <cstdint>
#include <cassert>
//#include "pr/str/string_core.h"
//#include "pr/win32/win32.h"

namespace pr::filesys
{
	// Notes:
	//  - Unix Time = seconds since midnight January 1, 1970 UTC
	//  - FILETIME = 100-nanosecond intervals since January 1, 1601 UTC

	// Convert a UTC Unix time to a local time zone Unix time
	inline time_t UTCtoLocal(time_t t)
	{
		struct tm utc, local;
		if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert UTC time to local time");
		return t + (mktime(&local) - mktime(&utc));
	}

	// Convert local time-zone Unix time to UTC Unix time
	inline time_t LocaltoUTC(time_t t)
	{
		struct tm utc, local;
		if (gmtime_s(&utc, &t) != 0 || localtime_s(&local, &t) != 0) throw std::exception("failed to convert local time to UTC time");
		return t - (mktime(&local) - mktime(&utc));
	}

	// Convert between Unix time and i64. The resulting i64 can then be converted to FILETIME, SYSTEMTIME, etc
	constexpr int64_t UnixTimetoI64(time_t  t)
	{
		return t * 10000000LL + 116444736000000000LL;
	}
	constexpr time_t  I64toUnixTime(int64_t t)
	{
		return (t - 116444736000000000LL) / 10000000LL;
	}

	// Conversions between int64_t, FILETIME, and SYSTEMTIME
	// Requires <windows.h> to be included
	// Note: the 'int64_t's here are not the same as the timestamps in 'FileTime'
	// those values are in Unix time. Use 'UnixTimetoI64()'
	struct FILETIME;
	struct SYSTEMTIME;
	template <typename = void> inline int64_t FTtoI64(FILETIME ft)
	{
		int64_t  n = int64_t(ft.dwHighDateTime) << 32 | int64_t(ft.dwLowDateTime);
		return n;
	}
	template <typename = void> inline FILETIME I64toFT(int64_t n)
	{
		FILETIME ft = {DWORD(n & 0xFFFFFFFFULL), DWORD((n >> 32) & 0xFFFFFFFFULL)};
		return ft;
	}
	template <typename = void> inline SYSTEMTIME FTtoST(FILETIME const& ft)
	{
		SYSTEMTIME st = {};
		if (!::FileTimeToSystemTime(&ft, &st)) throw std::exception("FileTimeToSystemTime failed");
		return st;
	}
	template <typename = void> inline FILETIME STtoFT(SYSTEMTIME const& st)
	{
		FILETIME ft = {};
		if (!::SystemTimeToFileTime(&st, &ft)) throw std::exception("SystemTimeToFileTime failed");
		return ft;
	}
	template <typename = void> inline int64_t STtoI64(SYSTEMTIME const& st)
	{
		return FTtoI64(STtoFT(st));
	}
	template <typename = void> inline SYSTEMTIME I64toST(int64_t n)
	{
		return FTtoST(I64toFT(n));
	}
}