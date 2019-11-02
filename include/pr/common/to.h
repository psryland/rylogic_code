//***********************************************************************
// 'To' conversion
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************

#pragma once
#include <string>
#include <type_traits>

namespace pr
{
	// Convert 'from' to 'to'
	// Add conversions by specialising the template class 'Convert'
	// or by simply overloading the 'To' function
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&)                                      { static_assert(typename std::is_same_v<TTo, std::false_type>, "No conversion from this type available"); }
		template <typename... Args> static TTo To(TFrom const&, Args...) { static_assert(typename std::is_same_v<TTo, std::false_type>, "No conversion from this type available"); }
	};
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)
	{
		return Convert<TTo, TFrom>::To(from);
	}
	template <typename TTo, typename TFrom, typename... Args> inline TTo To(TFrom const& from, Args... args)
	{
		return Convert<TTo, TFrom>::To(from, std::forward<Args>(args)...);
	}

	// A dummy type used to disambiguate function signatures
	template <int N> struct DummyType { DummyType(int) {} };

	// Map standard type_traits into pr
	template <typename Ty> using no_cv  = typename std::remove_cv<Ty>::type;
	template <typename Ty> using no_ref = typename std::remove_reference<Ty>::type;
	template <typename Ty> using no_ptr = typename std::remove_pointer<Ty>::type;
	template <typename Ty1, typename Ty2> using is_same = std::is_same<Ty1,Ty2>;

	// Enable if and only if 'Test' is true.
	template <bool Test, int N = 0> using enable_if = typename std::enable_if_t<Test, DummyType<N>>;
	template <bool Test, typename TOut> using iif = typename std::enable_if_t<Test, TOut>;

	// True if 'Str' is a raw string
	template <typename Str> struct is_raw_str :std::false_type {};
	template <> struct is_raw_str<char const*> :std::true_type {};
	template <> struct is_raw_str<wchar_t const*> :std::true_type {};
	template <> struct is_raw_str<char const[]> :std::true_type {};
	template <> struct is_raw_str<wchar_t const[]> :std::true_type {};
	static_assert( is_raw_str<char const*>::value, "");
	static_assert( is_raw_str<wchar_t const*>::value, "");
	static_assert( is_raw_str<char const[]>::value, "");
	static_assert( is_raw_str<wchar_t const[]>::value, "");
	static_assert(!is_raw_str<std::string>::value, "");
	static_assert(!is_raw_str<std::wstring>::value, "");
	static_assert(!is_raw_str<std::string const&>::value, "");
	static_assert(!is_raw_str<std::wstring const&>::value, "");

	// True if 'Str' is a string class (as opposed to a raw string, or a vector<int>, etc)
	template <typename Str> struct is_str_class
	{
	private:
		template <typename U> static std::true_type  check(typename std::enable_if<std::is_class<U>::value && is_char<typename U::value_type>::value, int>::type);
		template <typename>   static std::false_type check(...);
	public:
		using type = decltype(check<Str>(0));
		static bool const value = type::value;
	};
	static_assert( is_str_class<std::string>::value, "");
	static_assert( is_str_class<std::wstring>::value, "");
	static_assert(!is_str_class<std::string const&>::value, "");
	static_assert(!is_str_class<std::wstring const&>::value, "");
	static_assert(!is_str_class<char const*>::value, "");
	static_assert(!is_str_class<wchar_t const*>::value, "");
	static_assert(!is_str_class<char const[]>::value, "");
	static_assert(!is_str_class<wchar_t const[]>::value, "");

	// Enable if 'Str' is a raw string
	template <typename Str> using enable_if_raw_str = enable_if<is_raw_str<Str>::value>;

	// Enable if 'Str' is a string class
	template <typename Str> using enable_if_str_class = enable_if<is_str_class<Str>::value>;
}
