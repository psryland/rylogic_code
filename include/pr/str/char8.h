//**********************************
// UTF-8 strings
//  Copyright (c) Rylogic Ltd 2019
//**********************************
#pragma once
#include <cuchar>
#include <string>
#include <string_view>
#include <type_traits>
#include "pr/common/to.h"
#include "pr/common/cast.h"

// Glue code for converting from C++17 strings to C++20 utf-8 strings
// Notes:
//  - This is a compatibility header for C++17 and below for char8_t
//  - char8_t and std::u8string are the way of the future. Always convert
//    code in that direction.
//  - It is legal to reinterpret cast anything to 'char*' but not the other
//    way. So:
//       auto bytes = reinterpret_cast<char*>(u8"utf-8 string"); // is legal
//       auto text = reinterpret_cast<char8_t*>("ambiguous text"); // is not.
//  - A 'code unit' is a byte within a 'code point'

// Tips:
//  - UTF-8 at the Console:
//      // Set console code page to UTF-8 so console known how to interpret string data
//      SetConsoleOutputCP(CP_UTF8);
//      
//      // Enable buffering to prevent VS from chopping up UTF-8 byte sequences
//      setvbuf(stdout, nullptr, _IOFBF, 1000);
//      
//      std::string test = u8"Greek: αβγδ; German: Übergrößenträger";
//      std::cout << test << std::endl;


#if !defined(__cpp_char8_t)

// char8_t should be a distinct type basically identical to unsigned char.
// Can't use an alias to 'char' because then overloads are not unique.
using char8_t = unsigned char;

namespace std
{
	using u8string = std::basic_string<char8_t>;
	using u8string_view = std::basic_string_view<char8_t>;
}

#endif

namespace pr
{
	// Cast a char pointer to a utf8 string pointer
	inline char8_t const* char8_ptr(char8_t const* str)
	{
		return str;
	}
	inline char8_t const* char8_ptr(char const* str)
	{
		return reinterpret_cast<char8_t const*>(str);
	}
	inline char8_t* char8_ptr(char8_t* str)
	{
		return str;
	}
	inline char8_t* char8_ptr(char* str)
	{
		return reinterpret_cast<char8_t*>(str);
	}

	namespace convert
	{
		struct ToU8
		{
			static std::u8string_view To(std::string_view str)
			{
				return std::u8string_view(char8_ptr(str.data()), str.size());
			}
		};
	}
	//template <> struct Convert<>
	//{
	//	std::u8string To
	//};
}
