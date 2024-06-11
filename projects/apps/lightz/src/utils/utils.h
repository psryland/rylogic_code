#pragma once
#include "forward.h"

namespace lightz
{
	// Use with '%.*s' format string
	#define PRINTF_SV(str) static_cast<int>(str.size()), str.data()

	// Match a string against a pattern. 'len' is how much of 'str' needs to match
	inline bool Match(std::string_view str, std::string_view pattern, size_t len = ~size_t{})
	{
		return
			std::min(str.length(), len) == pattern.length() &&
			strncmp(str.data(), pattern.data(), pattern.length()) == 0;
	}
	inline bool MatchI(std::string_view str, std::string_view pattern, size_t len = ~size_t{})
	{
		return
			std::min(str.length(), len) == pattern.length() && 
			lwip_strnicmp(str.data(), pattern.data(), pattern.length()) == 0;
	}

	// Write out data in hex
	inline void HexDump(void const* data, size_t size)
	{
		uint8_t const* p = static_cast<uint8_t const*>(data);
		uint8_t const* z = static_cast<uint8_t const*>(nullptr);

		// Write out the data in the format:
		// 00000000 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 0123456789ABCDEF
		for (size_t i = 0; i < size; i += 16)
		{
			Serial.printf("%08X ", static_cast<unsigned int>((p - z) + i));

			for (size_t j = 0; j < 16; ++j)
			{
				if (i + j < size)
				{
					Serial.printf("%02X ", p[i + j]);
				}
				else
				{
					Serial.printf("   ");
				}
			}

			Serial.printf(" ");

			for (size_t j = 0; j < 16; ++j)
			{
				if (i + j < size)
				{
					char c = p[i + j];
					Serial.printf("%c", (c >= 32 && c < 127) ? c : '.');
				}
			}

			Serial.printf("\n");
		}
	}
}
