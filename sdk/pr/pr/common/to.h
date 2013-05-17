//***********************************************************************
// 'To' conversion
//  Copyright © Rylogic Ltd 2008
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
		static TTo To(TFrom const&)                 { static_assert(false, "No conversion from this type available"); }
		static TTo To(TFrom const&, int)            { static_assert(false, "No conversion from this type available"); }
		static TTo To(TFrom const&, char const*)    { static_assert(false, "No conversion from this type available"); }
		static TTo To(TFrom const&, wchar_t const*) { static_assert(false, "No conversion from this type available"); }
	};

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)                     { return Convert<TTo,TFrom>::To(from); }
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from, int radix)          { return Convert<TTo,TFrom>::To(from, radix); }
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from, char const* fmt)    { return Convert<TTo,TFrom>::To(from, fmt); }
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from, wchar_t const* fmt) { return Convert<TTo,TFrom>::To(from, fmt); }
}

#endif
