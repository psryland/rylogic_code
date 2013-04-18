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
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&)      { static_assert(false, "No conversion from this type available"); }
		static TTo To(TFrom const&, int) { static_assert(false, "No conversion from this type available"); }
	};

	// Convert 'from' to 'to'
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)            { return Convert<TTo,TFrom>::To(from); }
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from, int radix) { return Convert<TTo,TFrom>::To(from, radix); }
}

#endif
