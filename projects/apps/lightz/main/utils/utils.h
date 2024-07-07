#pragma once
#include "forward.h"
#include "utf8.h"

namespace lightz
{
	// Use with '%.*s' format string
	#define PRINTF_SV(str) static_cast<int>(str.size()), str.data()

	struct error_msg_pair_t
	{
		esp_err_t error;
		std::string_view msg;
	};

	// Check for ESP errors
	inline void Check(esp_err_t result, std::initializer_list<error_msg_pair_t const> messages)
	{
		if (likely(result == ESP_OK))
			return;

		std::string message;
		for (auto const& [code, msg] : messages)
		{
			if (code != 0 && code != result) continue;
			message.append("ERROR: ").append(msg).append("\n");
			break;
		}
		message.append("ERROR: ").append(esp_err_to_name(result)).append("\n");
		_esp_error_check_failed(result, __FILE__, __LINE__, __ASSERT_FUNC, message.c_str());
	}
	inline void Check(esp_err_t result, std::string_view msg = {})
	{
		return Check(result, {{0, msg}});
	}

	// Log levels
	enum class ELogLevel
	{
		Verbose = ESP_LOG_VERBOSE,
		Debug = ESP_LOG_DEBUG,
		Info = ESP_LOG_INFO,
		Warn = ESP_LOG_WARN,
		Error = ESP_LOG_ERROR,
		Silent = ESP_LOG_NONE,
	};

	// // Log a message a message
	// inline void Log(ELogLevel level, char const* tag, char const* format)
	// {
	// 	ESP_LOG_LEVEL(static_cast<esp_log_level_t>(level), tag, format, 0);
	// }

	// Match a string against a pattern. 'len' is how much of 'str' needs to match
	inline bool Match(std::string_view str, std::string_view pattern, size_t len = ~size_t{})
	{
		return
			std::min(str.length(), len) == pattern.length() &&
			std::strncmp(str.data(), pattern.data(), pattern.length()) == 0;
	}
	inline bool MatchI(std::string_view str, std::string_view pattern, size_t len = ~size_t{})
	{
		static auto strnicmp = [](char const* a, char const* b, size_t len)
		{
			for (; len-- != 0;)
			{
				auto ach = std::tolower(pr::str::utf8::CodePoint(a, a + len));
				auto bch = std::tolower(pr::str::utf8::CodePoint(b, b + len));
				if (ach == bch) continue;
				return ach - bch;
			}
			return 0;
		};

		return
			std::min(str.length(), len) == pattern.length() && 
			strnicmp(str.data(), pattern.data(), pattern.length()) == 0;
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
			printf("%08X ", static_cast<unsigned int>((p - z) + i));

			for (size_t j = 0; j < 16; ++j)
			{
				if (i + j < size)
				{
					printf("%02X ", p[i + j]);
				}
				else
				{
					printf("   ");
				}
			}

			printf(" ");

			for (size_t j = 0; j < 16; ++j)
			{
				if (i + j < size)
				{
					char c = p[i + j];
					printf("%c", (c >= 32 && c < 127) ? c : '.');
				}
			}

			printf("\n");
		}
	}
}
