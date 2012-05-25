//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_IVECTOR2_IMPL_H
#define PR_MATHS_IVECTOR2_IMPL_H

#include "pr/maths/ivector2.h"

namespace pr
{
	inline iv2& iv2::operator = (v2 const& rhs)
	{
		x = int(rhs.x);
		y = int(rhs.y);
		return *this;
	}
	inline iv2::operator v2() const
	{
		return v2::make(*this);
	}
	inline iv2 Abs(iv2 const& v)
	{
		return iv2::make(Abs(v.x), Abs(v.y));
	}
	inline int Dot2(iv2 const& lhs, iv2 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}
}

#endif
