//***********************************************************************
// Interpolation
//  Copyright (c) Rylogic Ltd 2014
//***********************************************************************

#pragma once

namespace pr
{
	// Specialise this struct for specific type interpolations
	template <typename T> struct Interpolate
	{
		struct Point
		{
			template <typename F> T operator()(T const& lhs, T const&, F, F) const
			{
				return lhs;
			}
		};
		struct Linear
		{
			template <typename F> T operator()(T const& lhs, T const& rhs, F n, F N) const
			{
				if (N == 0) return lhs;
				return (lhs*(N-n) + rhs*n) / N;
			}
		};
	};
}
