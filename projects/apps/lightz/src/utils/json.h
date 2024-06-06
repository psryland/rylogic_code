#pragma once
#include <string>
#include <string_view>
#include <variant>
#include <algorithm>
#include <filesystem>
#include <fstream>

namespace pr::json
{
	struct Value;
	Value const& null_value();

	struct Null
	{
		operator nullptr_t () const { return nullptr; }
	};
	struct Boolean
	{
		bool data;
		operator bool() const
		{
			return data;
		}
	};
	struct String
	{
		std::string data;
		operator std::string_view() const
		{
			return data;
		}
		operator std::string const& () const
		{
			return data;
		}
		operator std::filesystem::path() const
		{
			return data;
		}
		friend bool operator == (String const& lhs, std::string_view rhs)
		{
			return lhs.data == rhs;
		}
		friend bool operator != (String const& lhs, std::string_view rhs)
		{
			return !(lhs == rhs);
		}
	};
	struct Number : String
	{
		operator long long () const
		{
			return std::stoll(data);
		}
		operator double () const
		{
			return std::stod(data);
		}
		friend bool operator == (Number const& lhs, long long rhs)
		{
			return static_cast<long long>(lhs) == rhs;
		}
		friend bool operator == (Number const& lhs, double rhs)
		{
			return static_cast<double>(lhs) == rhs;
		}
		friend bool operator != (Number const& lhs, long long rhs)
		{
			return !(lhs == rhs);
		}
		friend bool operator != (Number const& lhs, double rhs)
		{
			return !(lhs == rhs);
		}
	};
	struct Array
	{
		std::vector<Value> items;

		bool empty() const
		{
			return items.empty();
		}
		size_t size() const
		{
			return items.size();
		}
		auto begin() const
		{
			return items.begin();
		}
		auto end() const
		{
			return items.end();
		}
		Value const& operator [] (size_t index) const
		{
			if (index >= items.size()) return null_value();
			return items[index];
		}
	};
	struct Object
	{
		std::vector<Value> items;

		bool empty() const
		{
			return items.empty();
		}
		size_t size() const
		{
			return items.size();
		}
		auto begin() const
		{
			return items.begin();
		}
		auto end() const
		{
			return items.end();
		}
		Value const* find(std::string_view key) const
		{
			auto iter = std::find_if(items.begin(), items.end(), [key](auto& v) { return v.key == key; });
			return iter != items.end() ? &*iter : nullptr;
		}
		Value const& operator [] (std::string_view key) const
		{
			auto value = find(key);
			return value ? *value : null_value();
		}
		Value const& operator [] (size_t index) const
		{
			if (index >= items.size()) return null_value();
			return items[index];
		}
	};

	struct Value
	{
		std::string key;
		std::variant<Null, Boolean, String, Number, Array, Object> value;

		// Convert to a json type
		template <typename T> T const* as() const
		{
			return std::get_if<T>(&value);
		}

		// Convert to a value
		bool to_bool() const
		{
			return std::get<Boolean>(value);
		}
		std::string_view to_string() const
		{
			return std::get<String>(value);
		}
		long long to_int() const
		{
			return std::get<Number>(value);
		}
		double to_float() const
		{
			return std::get<Number>(value);
		}
		Array const& to_array() const
		{
			return std::get<Array>(value);
		}
		Object const& to_object() const
		{
			return std::get<Object>(value);
		}
		Value const& operator [] (std::string_view k) const
		{
			if (auto object = as<Object>())
				return (*object)[k];

			throw std::runtime_error("Not an object");
		}
		Value const& operator [] (size_t index) const
		{
			if (auto object = as<Object>())
				return (*object)[index];
			if (auto list = as<Array>())
				return (*list)[index];

			throw std::runtime_error("Not an object or array");
		}
		friend bool operator == (Value const& lhs, nullptr_t)
		{
			return lhs.value.valueless_by_exception() || std::holds_alternative<Null>(lhs.value);
		}
		friend bool operator != (Value const& lhs, nullptr_t)
		{
			return !(lhs == nullptr);
		}
	};

	// Static null value returned for non-existent keys/indices
	inline Value const& null_value()
	{
		static Value s_null_value = { {}, Null{} };
		return s_null_value;
	}
	inline std::string UnescapeString(std::string_view str);

	// Json parsing options
	struct Options
	{
		bool AllowComments;
		bool AllowTrailingCommas;
	};

	// Implementation
	namespace impl
	{
		enum class EToken
		{
			EndOfString,
			Null,
			False,
			True,
			String,
			Number,
			OpenBrace,
			CloseBrace,
			OpenBracket,
			CloseBracket,
			Colon,
			Comma,
		};
		struct Token
		{
			EToken token;
			std::string_view data;
		};

		// Format a string with arguments
		template <typename... Args>
		inline std::string Fmt(char const* fmt, Args... args)
		{
			std::string result;
			result.resize(std::snprintf(nullptr, 0, fmt, args...));
			std::snprintf(result.data(), result.size() + 1, fmt, args...);
			return result;
		}
		
		// Advance the string pointer to the next non-whitespace character
		inline bool EatWS(std::string_view& src, bool allow_eos)
		{
			for (;!src.empty() && std::isspace(src[0]); )
				src.remove_prefix(1);
			if (src.empty() && !allow_eos)
				throw std::runtime_error("Unexpected end of string");
			
			// Always return true to allow if chaining
			return true;
		}

		// Return the next token in the json string
		inline Token NextToken(std::string_view& src, Options const& opts)
		{
			EatWS(src, true);
			if (src.empty())
				return { EToken::EndOfString, {} };

			switch (src[0])
			{
				case '{':
				{
					src.remove_prefix(1);
					return { EToken::OpenBrace, {} };
				}
				case '}':
				{
					src.remove_prefix(1);
					return { EToken::CloseBrace, {} };
				}
				case '[':
				{
					src.remove_prefix(1);
					return { EToken::OpenBracket, {} };
				}
				case ']':
				{
					src.remove_prefix(1);
					return { EToken::CloseBracket, {} };
				}
				case ':':
				{
					src.remove_prefix(1);
					return { EToken::Colon, {} };
				}
				case ',':
				{
					src.remove_prefix(1);
					return { EToken::Comma, {} };
				}
				case 't':
				case 'T':
				{
					if (src.size() < 4 || tolower(src[1]) != 'r' || tolower(src[2]) != 'u' || tolower(src[3]) != 'e')
						throw std::runtime_error("Unknown token");

					src.remove_prefix(4);
					return { EToken::True, {} };
				}
				case 'f':
				case 'F':
				{
					if (src.size() < 5 || tolower(src[1]) != 'a' || tolower(src[2]) != 'l' || tolower(src[3]) != 's' || tolower(src[4]) != 'e')
						throw std::runtime_error("Unknown token");

					src.remove_prefix(5);
					return { EToken::False, {} };
				}
				case 'n':
				{
					if (src.size() < 4 || tolower(src[1]) != 'u' || tolower(src[2]) != 'l' || tolower(src[3]) != 'l')
						throw std::runtime_error("Unknown token");

					src.remove_prefix(4);
					return { EToken::Null, {} };
				}
				case '"':
				case '\'':
				{
					auto quote = src[0];

					auto ptr = src.data();
					auto end = src.data() + src.size();

					// Find the end of the string literal
					for (bool esc = true; ptr != end && (esc || *ptr != quote); esc = !esc && *ptr == '\\', ++ptr) {}
					if (ptr == end)
						throw std::runtime_error("Incomplete literal string or character");

					// Unescape the string
					auto str = src.substr(1, ptr - src.data() - 1);
					src.remove_prefix(ptr - src.data() + 1);
					return { EToken::String, str };
				}
				case '-':
				case '+':
				{
					if (src.size() < 2 || !std::isdigit(src[1]))
						throw std::runtime_error("Unknown token");

					[[fallthrough]];
				}
				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
				{
					auto ptr = src.data();
					auto end = src.data() + src.size();

					// Find the end of the number
					auto allow_sign = true;
					auto allow_decimal_point = true;
					auto allow_exponent = true;
					for (; ptr != end; ++ptr)
					{
						if (std::isdigit(*ptr)) continue;
						if (allow_sign && (*ptr == '-' || *ptr == '+')) { allow_sign = false; continue; }
						if (allow_decimal_point && *ptr == '.') { allow_decimal_point = false; continue; }
						if (allow_exponent && (*ptr == 'e' || *ptr == 'E')) { allow_sign = true; allow_decimal_point = false; allow_exponent = false; continue; }
						break;
					}
					if (ptr == end)
						throw std::runtime_error("Incomplete number");

					auto str = src.substr(0, ptr - src.data());
					src.remove_prefix(ptr - src.data());
					return { EToken::Number, str };
				}
				case '/':
				{
					if (src.size() < 2 || src[1] != '/')
						throw std::runtime_error("Unknown token");

					if (!opts.AllowComments)
						throw std::runtime_error("Comments not allowed");

					auto ptr = src.data();
					auto end = src.data() + src.size();

					// Eat up to the end of the line
					for (; ptr != end && *ptr != '\n'; ++ptr) {}
					if (ptr == end)
						return { EToken::EndOfString, {} };

					// Tail recursion optimisation should prevent stack overflow
					src.remove_prefix(ptr - src.data());
					return NextToken(src, opts);
				}
				default:
				{
					throw std::runtime_error("Unknown token");
				}
			}
		}

		// Return the next key in the json string
		inline std::string_view NextKey(std::string_view& src, Options const& opts)
		{
			auto tok = NextToken(src, opts);
			if (tok.token != EToken::String)
				throw std::runtime_error("Expected key");

			return tok.data;
		}

		// Return the next value in the json string
		inline Value NextValue(std::string_view& src, std::string_view key, Options const& opts)
		{
			auto tok = NextToken(src, opts);
			switch (tok.token)
			{
				case EToken::EndOfString:
				{
					throw std::runtime_error("Unexpected end of string");
				}
				case EToken::Null:
				{
					return Value{ std::string(key), Null{} };
				}
				case EToken::True:
				{
					return Value{ std::string(key), Boolean{ true } };
				}
				case EToken::False:
				{
					return Value{ std::string(key), Boolean{ false } };
				}
				case EToken::String:
				{
					return Value{ std::string(key), String{ UnescapeString(tok.data) } };
				}
				case EToken::Number:
				{
					return Value{ std::string(key), Number{ std::string(tok.data) } };
				}
				case EToken::OpenBracket:
				{
					auto list = Array{};
					auto require_comma = false;
					for (;;)
					{
						if (EatWS(src, false) && src[0] == ']')
						{
							std::ignore = NextToken(src, opts);
							break;
						}
						if (require_comma && NextToken(src, opts).token != EToken::Comma)
						{
							throw std::runtime_error("Expected comma");
						}
						if (opts.AllowTrailingCommas && EatWS(src, false) && src[0] == ']')
						{
							std::ignore = NextToken(src, opts);
							break;
						}

						// Not the end of the array, so add the next value
						list.items.push_back(NextValue(src, {}, opts));
						require_comma = true;
					}
					return Value{ std::string(key), list };
				}
				case EToken::OpenBrace:
				{
					auto obj = Object{};
					auto require_comma = false;
					for (;;)
					{
						if (EatWS(src, false) && src[0] == '}')
						{
							std::ignore = NextToken(src, opts);
							break;
						}
						if (require_comma && NextToken(src, opts).token != EToken::Comma)
						{
							throw std::runtime_error("Expected comma");
						}
						if (opts.AllowTrailingCommas && EatWS(src, false) && src[0] == '}')
						{
							std::ignore = NextToken(src, opts);
							break;
						}

						// Not the end of the object, so add the next key/value pair
						auto k = NextKey(src, opts);

						if (NextToken(src, opts).token != EToken::Colon)
							throw std::runtime_error("Expected colon");

						obj.items.push_back(NextValue(src, k, opts));
						require_comma = true;
					}
					return Value{ std::string(key), obj };
				}
				default:
				{
					throw std::runtime_error("Unknown token");
				}
			}
		}
	}
	
	// Convert an escaped string to a normal string
	inline std::string UnescapeString(std::string_view str)
	{
		auto ptr = str.data();
		auto end = str.data() + str.size();
			
		std::string result(str.size(), '\0');
		auto out = result.data();

		for (; ptr != end; ++ptr)
		{
			if (*ptr != '\\')
			{
				*out++ = *ptr;
				continue;
			}
			switch (*++ptr)
			{
				case '"': *out++ = '"'; break;
				case '\\': *out++ = '\\'; break;
				case '/': *out++ = '/'; break;
				case 'b': *out++ = '\b'; break;
				case 'f': *out++ = '\f'; break;
				case 'n': *out++ = '\n'; break;
				case 'r': *out++ = '\r'; break;
				case 't': *out++ = '\t'; break;
				case 'u':
				{
					if (end - ptr < 5)
						throw std::runtime_error("Incomplete unicode escape sequence");
					
					char const digits[] = { ptr[1], ptr[2], ptr[3], ptr[4] };
					auto code = std::strtoul(&digits[0], nullptr, 16);
					if (code < 0x80)
					{
						*out++ = static_cast<char>(code);
					}
					else if (code < 0x800)
					{
						*out++ = static_cast<char>(0xC0 | (code >> 6));
						*out++ = static_cast<char>(0x80 | (code & 0x3F));
					}
					else
					{
						*out++ = static_cast<char>(0xE0 | (code >> 12));
						*out++ = static_cast<char>(0x80 | ((code >> 6) & 0x3F));
						*out++ = static_cast<char>(0x80 | (code & 0x3F));
					}
					ptr += 4;
					break;
				}
				default:
				{
					throw std::runtime_error("Unknown escape sequence");
				}
			}
		}

		result.resize(out - result.data());
		return result;
	}

	/// <summary>Parse a UTF-8 JSON string into a DOM tree.</summary>
	inline Value Parse(std::string_view src, Options const& opts)
	{
		using namespace impl;

		auto start = src;
		try
		{
			return NextValue(src, {}, opts);
		}
		catch (std::runtime_error& ex)
		{
			ex = std::runtime_error(impl::Fmt("Parsing failed at offset %zu - %s", src.data() - start.data(), ex.what()));
			throw;
		}
	}

	/// <summary>Read a JSON file into a DOM tree.</summary>
	inline Value Read(std::filesystem::path const& path, Options const& opts)
	{
		std::ifstream file(path);
		if (!file.is_open())
			throw std::runtime_error(impl::Fmt("Failed to open file '%s'", path.string().c_str()));

		auto size = std::filesystem::file_size(path);
		if (size > std::numeric_limits<size_t>::max())
			throw std::runtime_error(impl::Fmt("File '%s' is too large", path.string().c_str()));

		std::string data(static_cast<size_t>(size), '\0');
		file.read(data.data(), data.size());
		return Parse(data, opts);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::storage
{
	PRUnitTest(JsonTests)
	{
		{
			char const test_data[] =
#pragma region TestData
R"({
	"key1": "value1",
	"key2": 123,
	"key3": true,
	"key4": false,
	"key5": null,
	"key6": {
		"key7": "value7",
		"key8": 456,
		"key9": true,
		"key10": false,
		"key11": null,
		"key12": {
			"key13": "value13",
			"key14": 789,
			"key15": true,
			"key16": false,
			"key17": null
		},
		// Comments allowed
		"key18": [
			"value19",
			123,
			true,
			false,
			null
		]
	},
	"key20": [
		"value21",
		123,
		true,
		false,
		null,
	]
})";
#pragma endregion

			auto root = json::Parse(test_data, { .AllowComments = true, .AllowTrailingCommas = true });
			PR_CHECK(root["key1"].to_string() == "value1", true);
			PR_CHECK(root["key2"].to_int() == 123, true);
			PR_CHECK(root["key3"].to_bool() == true, true);
			PR_CHECK(root["key4"].to_bool() == false, true);
			PR_CHECK(root["key5"] == nullptr, true);
			PR_CHECK(root["key6"] != nullptr, true);
			PR_CHECK(root["key6"]["key12"]["key14"].to_int() == 789, true);
			PR_CHECK(root["key6"]["key12"].to_object().size() == 5, true);
			PR_CHECK(root["key6"]["key18"].to_array().empty(), false);
		}
		{
			char const test_data[] =
#pragma region TestData
R"({
	"EscapedString": "This is a string with a \"quote\" in it",
	"SearchPaths": [
		"C:\\Work\\Path",
	],
})";
#pragma endregion

			auto root = json::Parse(test_data, { .AllowComments = true, .AllowTrailingCommas = true });
			PR_CHECK(root["SearchPaths"][0].to_string() == "C:\\Work\\Path", true);
			PR_CHECK(root["EscapedString"].to_string() == "This is a string with a \"quote\" in it", true);
		}
	}
}
#endif
