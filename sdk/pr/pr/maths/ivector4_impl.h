//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_IVECTOR4_IMPL_H
#define PR_MATHS_IVECTOR4_IMPL_H

#include "pr/maths/ivector4.h"

namespace pr
{
	inline iv4& iv4::operator = (v4 const& vec)
	{
		return set(int(vec.x), int(vec.y), int(vec.z), int(vec.w));
	}
	inline iv4::operator v4 () const
	{
		return v4::make(float(x), float(y), float(z), float(w));
	}
	inline iv4& Zero(iv4& v)
	{
		return v = pr::iv4Zero;
	}
	inline iv4 Abs(iv4 const& v)
	{
		return iv4::make(Abs(v.x), Abs(v.y), Abs(v.z), Abs(v.w));
	}
	inline int Dot3(iv4 const& lhs, iv4 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
	}
	inline int Dot4(iv4 const& lhs, iv4 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
	}
	inline iv4 Cross3(iv4 const& lhs, iv4 const& rhs)
	{
		return iv4::make(lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x, 0);
	}
}

#endif
