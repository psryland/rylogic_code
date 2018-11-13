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
		static int const None = 0;
		static int const PosX = +1;
		static int const PosY = +2;
		static int const PosZ = +3;
		static int const NegX = -1;
		static int const NegY = -2;
		static int const NegZ = -3;

		int value;

		AxisId(int axis_id)
			:value(axis_id)
		{}
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
			//assert(IsValid(*this) && "axis_id must one of ±1, ±2, ±3");
			switch (value) {
			default: return v4{};
			case +1: return v4{+1,0,0,0};
			case -1: return v4{-1,0,0,0};
			case +2: return v4{0,+1,0,0};
			case -2: return v4{0,-1,0,0};
			case +3: return v4{0,0,+1,0};
			case -3: return v4{0,0,-1,0};
			}
		}
		static bool IsValid(AxisId axis_id)
		{
			return abs(axis_id) >= 1 && abs(axis_id) <= 3;
		}
	};
}