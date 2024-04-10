#pragma once
#include <string>
#include <string_view>
#include <format>
#include <variant>
#include <algorithm>

namespace pr::json
{
	//enum class EValueType
	//{
	//	String,
	//	Number,
	//	Object,
	//	Array,
	//	True,
	//	False,
	//	Null
	//};
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

	struct Value;
	struct Null
	{
		operator nullptr_t () const { return nullptr; }
	};
	struct Boolean
	{
		bool data;

		operator bool () const { return data; }
	};
	struct String
	{
		std::string_view data;

		operator std::string_view() const { return data; }
	};
	struct Number
	{
		std::string_view data;

		operator double () const { return std::stod(std::string(data)); }
		operator int64_t () const { return std::stoll(std::string(data)); }
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
		Value const& operator [] (std::string_view key) const
		{
			static constexpr Value null_value = { {}, Null{} };
			auto iter = std::find_if(items.begin(), items.end(), [key](auto& v) { v.key == key; });
			if (iter == items.end()) return null_value;
			return *iter;
		}
		Value const& operator [] (size_t index) const
		{
			return items[index];
		}
		std::optional<Value> find(std::string_view key) const;
	};
	struct Value
	{
		std::string_view key;
		std::variant<Null, Boolean, String, Number, Object, Array> value;

		template <typename T> T const& to() const
		{
			return as<T>.value();
		}
		template <typename T> std::optional<T const&> as() const
		{
			return std::holds_alternative<T>(value) ? std::get<T>(value) : std::nullopt;
		}
		Value const& operator [] (std::string_view k) const
		{
			if (std::holds_alternative<Object>(value))
				return std::get<Object>(value)[k];

			throw std::runtime_error("Not an object");
		}
		Value const& operator [] (size_t index) const
		{
			if (std::holds_alternative<Object>(value))
				return std::get<Object>(value)[index];
			if (std::holds_alternative<Array>(value))
				return std::get<Array>(value)[index];

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

	inline std::optional<Value> Object::find(std::string_view key) const
	{
		auto iter = std::find_if(items.begin(), items.end(), [key](auto& v) { v.key == key; });
		return iter != items.end() ? std::optional<Value>{*iter} : std::nullopt;
	}


	struct Options
	{
		bool AllowComments;
	};

	namespace impl
	{
		struct Token
		{
			EToken token;
			std::string_view data;
		};

		// Advance the string pointer to the next non-whitespace character
		inline void EatWS(std::string_view& src, bool allow_eos)
		{
			for (;!src.empty() && std::isspace(src[0]); )
				src.remove_prefix(1);
			if (src.empty() && !allow_eos)
				throw std::runtime_error("Unexpected end of string");
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
					return Value{ key, Null{} };
				}
				case EToken::True:
				{
					return Value{ key, Boolean{ true } };
				}
				case EToken::False:
				{
					return Value{ key, Boolean{ false } };
				}
				case EToken::String:
				{
					return Value{ key, String{ tok.data } };
				}
				case EToken::Number:
				{
					return Value{ key, Number{ tok.data } };
				}
				case EToken::OpenBracket:
				{
					auto list = Array{};
					auto require_comma = false;
					for (;;)
					{
						EatWS(src, false);
						if (src[0] == ']')
						{
							std::ignore = NextToken(src, opts);
							break;
						}
						if (require_comma && NextToken(src, opts).token != EToken::Comma)
						{
							throw std::runtime_error("Expected comma");
						}

						// Not the end of the array, so add the next value
						list.items.push_back(NextValue(src, {}, opts));
						require_comma = true;
					}
					return Value{ key, list };
				}
				case EToken::OpenBrace:
				{
					auto obj = Object{};
					auto require_comma = false;
					for (;;)
					{
						EatWS(src, false);
						if (src[0] == '}')
						{
							std::ignore = NextToken(src, opts);
							break;
						}
						if (require_comma && NextToken(src, opts).token != EToken::Comma)
						{
							throw std::runtime_error("Expected comma");
						}

						// Not the end of the object, so add the next key/value pair
						auto k = NextKey(src, opts);

						if (NextToken(src, opts).token != EToken::Colon)
							throw std::runtime_error("Expected colon");

						obj.items.push_back(NextValue(src, k, opts));
						require_comma = true;
					}
					return Value{ key, obj };
				}
				default:
				{
					throw std::runtime_error("Unknown token");
				}
			}
		}
	}

	/// <summary>Parse a UTF-8 JSON string into a DOM tree.</summary>
	Value Parse(std::string_view src, Options const& opts)
	{
		using namespace impl;

		auto start = src;
		try
		{
			return NextValue(src, {}, opts);
		}
		catch (std::runtime_error& ex)
		{
			ex = std::runtime_error(std::format("{}\nParsing failed at offset {}", ex.what(), start.data() - src.data()));
			throw;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::storage
{
	PRUnitTest(JsonTests)
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
		null
	]
})";
		#pragma endregion

		auto root = json::Parse(test_data, {.AllowComments = true});
		PR_CHECK(root["key1"].to<json::String>() == "value1"_sv, true);
		PR_CHECK(root["key2"].to<json::Number>() == 123, true);
		PR_CHECK(root["key3"].to<json::Boolean>() == true, true);
		PR_CHECK(root["key4"].to<json::Boolean>() == false, true);
		PR_CHECK(root["key5"] == nullptr, true);
		PR_CHECK(root["key6"] != nullptr, true);
		PR_CHECK(root["key20"].as != nullptr, true);
	}
}
#endif
