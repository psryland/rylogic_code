//***********************************************************************
// 'To' conversion
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************

#pragma once
#ifndef PR_COMMON_TO_H
#define PR_COMMON_TO_H

#include <string>

namespace pr
{
	// Specialise this struct for specific conversions
	// Use specialisations of 'TTo' otherwise abiguous overloads will be created
	// e.g.
	//  // To<X>
	//  template <typename TFrom> struct Convert<X,TFrom>
	//  {
	//     static X To(int from, int radix) { return To(static_cast<long long>(from), radix); }
	//  };
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&) { static_assert(false, "No conversion from this type available"); }
		
		template <typename... Args>
		static TTo To(TFrom const&, Args...) { static_assert(false, "No conversion from this type available"); }
	};

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom>
	inline TTo To(TFrom const& from)
	{
		return Convert<TTo,TFrom>::To(from);
	}
	template <typename TTo, typename TFrom, typename... Args>
	inline TTo To(TFrom const& from, Args... args)
	{
		return Convert<TTo,TFrom>::To(from, std::forward<Args>(args)...);
	}
}

#endif
