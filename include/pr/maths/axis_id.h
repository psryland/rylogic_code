//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"

namespace pr
{
	// An integer that represents one of the basis axis: ±X, ±Y, ±Z
	struct AxisId
	{
		int value;

		AxisId(int axis_id = 3) :value(axis_id)
		{
			assert(IsValid(*this) && "axis_id must one of ±1, ±2, ±3");
		}
		operator int const&() const
		{
			return value;
		}
		operator int&()
		{
			return value;
		}
		operator v4() const // Convert an axis id to an axis
		{
			switch (value) {
			default: return  v4Zero;
			case +1: return  v4XAxis;
			case -1: return -v4XAxis;
			case +2: return  v4YAxis;
			case -2: return -v4YAxis;
			case +3: return  v4ZAxis;
			case -3: return -v4ZAxis;
			}
		}
		static bool IsValid(AxisId axis_id)
		{
			return abs(axis_id) >= 1 && abs(axis_id) <= 3;
		}
	};
}