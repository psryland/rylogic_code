//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/types/vector4.h"
#include "pr/math_new/types/quaternion.h"

namespace pr::math
{
	template <ScalarType S>
	struct Xform
	{
		Vec4<S> pos;
		Quat<S> rot;
		Vec4<S> scl;
	};

		#if 0
	inline bool FEql(transform const& lhs, transform const& rhs)
	{
		return
			FEqlOrientation(lhs.rotation, rhs.rotation) &&
			FEql(lhs.translation, rhs.translation) &&
			FEql(lhs.scale, rhs.scale);
	}
	#endif
	
	#if 0
	// Transform Operators
	constexpr bool operator == (Xform const& lhs, Xform const& rhs)
	{
		return
			lhs.rotation == rhs.rotation &&
			lhs.translation == rhs.translation &&
			lhs.scale == rhs.scale;
	}
	constexpr bool operator != (Xform const& lhs, Xform const& rhs)
	{
		return !(lhs == rhs);
	}
	#endif
}