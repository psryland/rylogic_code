//***********************************************************************
// Interpolation
//  Copyright (c) Rylogic Ltd 2014
//***********************************************************************
#pragma once

#include "pr/common/interpolate.h"
#include "pr/maths/maths.h"

namespace pr
{
	// Interpolate<v4>
	template <> struct Interpolate<v4>
	{
		struct Point
		{
			template <typename F> v2 operator()(v2 lhs, v4_cref<>, F, F) const
			{
				return lhs;
			}
			template <typename F> v3 operator()(v3 lhs, v4_cref<>, F, F) const
			{
				return lhs;
			}
			template <typename F> v4 operator()(v4_cref<> lhs, v4_cref<>, F, F) const
			{
				return lhs;
			}
		};
		struct Linear
		{
			template <typename F> v2 operator()(v2 lhs, v2 rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
			template <typename F> v3 operator()(v3 lhs, v3 rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
			template <typename F> v4 operator()(v4_cref<> lhs, v4_cref<> rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
		};
	};

}
