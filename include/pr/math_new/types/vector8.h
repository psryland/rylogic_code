//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/types/vector4.h"

namespace pr::math
{
	template <ScalarType S>
	struct Vec8
	{
		// Notes:
		//  - Spatial vectors describe a vector at a point plus the field of vectors around that point. 

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { S rx, ry, rz, rw, tx, ty, tz, tw; };
			struct { Vec4<S> ang, lin; };
			struct { S arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
	};
}
