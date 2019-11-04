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
	//  - Within a specialised Convert class, if you need 'To<something>' you need the 'pr::' to
	//    reach 'To' functions outside of the Convert class.
	//  - Include 'string_core' for string conversion

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&)
		{
			static_assert(std::is_same_v<TTo, std::false_type>, "No conversion from this type is available");
		}
		template <typename... Args> static TTo To(TFrom const&, Args...)
		{
			static_assert(std::is_same_v<TTo, std::false_type>, "No conversion from this type is available");
		}
	};
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)
	{
		return Convert<TTo, TFrom>::To(from);
	}
	template <typename TTo, typename TFrom, typename... Args> inline TTo To(TFrom const& from, Args... args)
	{
		return Convert<TTo, TFrom>::To(from, std::forward<Args>(args)...);
	}
}
