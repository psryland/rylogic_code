//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************

#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/matrix4x4.h"

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
		static bool IsValid(AxisId axis_id)
		{
			return abs(axis_id) >= 1 && abs(axis_id) <= 3;
		}
	};

	// Return a transform from one basis axis to another
	inline m3x4 Rotation3x3(AxisId from_axis, AxisId to_axis)
	{
		// Get the rotation from Z to 'from_axis' = o2f
		// Get the rotation from Z to 'to_axis' = o2t
		// 'f2t' = o2t * Invert(o2f)
		m3x4 o2f, o2t;
		switch (from_axis)
		{
		default: assert(false && "axis_id must one of ±1, ±2, ±3"); o2f = pr::m3x4Identity; break;
		case -1: o2f = pr::Rotation3x3(0.0f, +pr::maths::tau_by_4, 0.0f); break;
		case +1: o2f = pr::Rotation3x3(0.0f, -pr::maths::tau_by_4, 0.0f); break;
		case -2: o2f = pr::Rotation3x3(+pr::maths::tau_by_4, 0.0f, 0.0f); break;
		case +2: o2f = pr::Rotation3x3(-pr::maths::tau_by_4, 0.0f, 0.0f); break;
		case -3: o2f = pr::Rotation3x3(0.0f, +pr::maths::tau_by_2, 0.0f); break;
		case +3: o2f = pr::m3x4Identity; break;
		}
		switch (to_axis)
		{
		default: assert(false && "axis_id must one of ±1, ±2, ±3"); o2t = pr::m3x4Identity; break;
		case -1: o2t = pr::Rotation3x3(0.0f, +pr::maths::tau_by_4, 0.0f); break;
		case +1: o2t = pr::Rotation3x3(0.0f, -pr::maths::tau_by_4, 0.0f); break;
		case -2: o2t = pr::Rotation3x3(+pr::maths::tau_by_4, 0.0f, 0.0f); break;
		case +2: o2t = pr::Rotation3x3(-pr::maths::tau_by_4, 0.0f, 0.0f); break;
		case -3: o2t = pr::Rotation3x3(0.0f, +pr::maths::tau_by_2, 0.0f); break;
		case +3: o2t = pr::m3x4Identity; break;
		}
		return o2t * InvertFast(o2f);
	}

	// Return a transform from one basis axis to another
	inline m4x4 Rotation4x4(AxisId from_axis, AxisId to_axis, v4 const& translation)
	{
		return m4x4::make(Rotation3x3(from_axis, to_axis), translation);
	}
}