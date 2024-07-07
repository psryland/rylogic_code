#pragma once
#include "forward.h"
#include "utils/utils.h"

namespace lightz
{
	template <typename T> T ConvertTo(std::string_view value);
	template <> inline String ConvertTo<String>(std::string_view value)
	{
		return String(value.data(), value.size());
	}
	template <> inline CRGB ConvertTo<CRGB>(std::string_view value)
	{
		return std::stoul(std::string(value), nullptr, 16);
	}
	template <> inline int ConvertTo<int>(std::string_view value)
	{
		return std::stoi(std::string(value));
	}
	template <> inline bool ConvertTo<bool>(std::string_view value)
	{
		return MatchI(value, "true") || MatchI(value, "1"); 
	}

	template <typename T> String ToString(T value);
	template <> inline String ToString<String>(String value)
	{
		return value;
	}
	template <> inline String ToString<std::string_view>(std::string_view value)
	{
		return String(value.data(), value.size());
	}
	template <> inline String ToString<CRGB>(CRGB value)
	{
		return String(static_cast<uint32_t>(value), 16);
	}
	template <> inline String ToString<int>(int value)
	{
		return String(value);
	}
	template <> inline String ToString<bool>(bool value)
	{
		return value ? "true" : "false";
	}
}
