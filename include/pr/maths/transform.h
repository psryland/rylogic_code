//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/quaternion.h"

namespace pr
{
	template <Scalar S, typename A, typename B>
	struct Xform
	{
		Quat<S, A, B> rot;
		Vec4<S, A> pos;
		Vec4<S, void> scl;

		Xform() = default;
		constexpr explicit Xform(Quat<S, A, B> const& r, Vec4<S, A> const& p, Vec4<S, void> const& s = Vec4<S, void>::One())
			:rot(r)
			,pos(p)
			,scl(s)
		{
		}
		
		// Basic constants
		static constexpr Xform Identity()
		{
			return { Quat<S, A, B>::Identity(), Vec4<S, A>::Origin(), Vec4<S, void>::One() };
		}
	};
}
