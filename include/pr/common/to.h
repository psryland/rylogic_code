//***********************************************************************
// 'To' conversion
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************
#pragma once
#include <string>
#include <type_traits>

namespace pr
{
	// Notes:
	//  - Add conversions by specialising 'Convert' or by simply overloading the 'To' function
	//  - Include 'string_core' for string conversion

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom> struct Convert : std::false_type
	{
		struct no_conversion_for_these_types;
		constexpr static TTo Func(TFrom const&)
		{
			no_conversion_for_these_types error;
			static_assert(Convert<TTo, TFrom>::value, "No conversion from this type is available");
		}
		template <typename... Args> constexpr static TTo Func(TFrom const&, Args...)
		{
			no_conversion_for_these_types error;
			static_assert(Convert<TTo, TFrom>::value, "No conversion from this type is available");
		}
	};

	// Conversion function: auto b = To<B>(a);
	template <typename TTo, typename TFrom> constexpr inline TTo To(TFrom const& from)
	{
		return Convert<TTo, TFrom>::Func(from);
	}
	template <typename TTo, typename TFrom, typename... Args> constexpr inline TTo To(TFrom const& from, Args... args)
	{
		return Convert<TTo, TFrom>::Func(from, std::forward<Args>(args)...);
	}

	// No-op conversion
	template <typename T> struct Convert<T, T>
	{
		static constexpr T Func(T&& x) noexcept
		{
			return std::forward<T>(x);
		}
		static constexpr T const& Func(T const& x) noexcept
		{
			return x;
		}
		static constexpr T& Func(T& x) noexcept
		{
			return x;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(ToTests)
	{
		//struct TFrom {};
		//struct TTo {};
		//auto a = To<TTo>(TFrom{});
	}
}
#endif