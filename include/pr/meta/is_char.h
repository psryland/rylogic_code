#pragma once
#include <type_traits>

namespace pr
{
	// Type trait that identifies character types
	template <typename Ty> using is_char =
		std::integral_constant<bool,
		std::is_same<Ty, char    >::value ||
		std::is_same<Ty, char16_t>::value ||
		std::is_same<Ty, char32_t>::value ||
		std::is_same<Ty, wchar_t >::value>;

	template <typename T> using is_char_t = typename is_char<T>::type;
	template <typename T> constexpr bool is_char_v = is_char<T>::value;

	static_assert(is_char_v<char>   , "");
	static_assert(is_char_v<wchar_t>, "");
	static_assert(!is_char_v<int>   , "");
	static_assert(!is_char_v<short> , "");
	static_assert(!is_char_v<float> , "");
}
