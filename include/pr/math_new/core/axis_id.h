//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"

namespace pr::math
{
	// An integer that represents one of the basis axis: +/-X, +/-Y, +/-Z
	struct AxisId
	{
		static int const None = 0;
		static int const PosX = +1;
		static int const PosY = +2;
		static int const PosZ = +3;
		static int const NegX = -1;
		static int const NegY = -2;
		static int const NegZ = -3;

		int value;

		constexpr AxisId(int axis_id) noexcept
			:value(axis_id)
		{}
		constexpr operator int() const noexcept
		{
			return value;
		}
		constexpr operator int&() noexcept
		{
			return value;
		}
		static constexpr bool IsValid(AxisId axis_id)
		{
			return axis_id >= -3 && axis_id <= 3 && axis_id != 0;
		}
		friend constexpr AxisId Abs(AxisId axis_id)
		{
			return AxisId(axis_id.value < 0 ? -axis_id.value : axis_id.value);
		}
	};
}