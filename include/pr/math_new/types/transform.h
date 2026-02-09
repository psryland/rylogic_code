//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "../core/forward.h"
#include "../types/vector4.h"
#include "../types/quaternion.h"

namespace pr::math
{
	template <ScalarType S>
	struct Xform
	{
		Vec4<S> pos;
		Quat<S> rot;
		Vec4<S> scl;
	};
}