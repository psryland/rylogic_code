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
	//  - Convert::To_() is so that calling 'To<something>' from within a Convert struct works.
	//  - Include 'string_core' for string conversion

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom> struct Convert
	{
		constexpr static TTo To_(TFrom const&)
		{
			static_assert(std::is_same_v<TTo, std::false_type>, "No conversion from this type is available");
		}
		template <typename... Args> constexpr static TTo To_(TFrom const&, Args...)
		{
			static_assert(std::is_same_v<TTo, std::false_type>, "No conversion from this type is available");
		}
	};

	// Conversion function: auto b = To<B>(a);
	template <typename TTo, typename TFrom> constexpr inline TTo To(TFrom const& from)
	{
		return Convert<TTo, TFrom>::To_(from);
	}
	template <typename TTo, typename TFrom, typename... Args> constexpr inline TTo To(TFrom const& from, Args... args)
	{
		return Convert<TTo, TFrom>::To_(from, std::forward<Args>(args)...);
	}
}
