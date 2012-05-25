//*****************************************************************************
// Maths library
//	(c)opyright Paul Ryland 2002
//*****************************************************************************

#ifndef PR_MATHS_PR_TO_ODE_H
#define PR_MATHS_PR_TO_ODE_H
#pragma once

#include "pr/maths/maths.h"
#include <ode/ode.h>

struct dV4
{
	dVector3 m_pos;
	operator dVector3 const&() const { return m_pos; }
};
struct dM4x4
{
	dMatrix3 m_rot;
	dVector3 m_pos;
};

namespace pr
{
	namespace impl
	{
		// Copy vectors/matrices
		template <typename Real1, typename Real2> inline void copy(Real1* dst, Real2 const* src, int count)
		{
			while (count--)
				*dst++ = Real1(*src++);
		}
	}
	
	// Convert a pr::v4 to a dVector3 
	inline dV4 ode(pr::v4 const& vec)
	{
		dV4 v = {{vec.x, vec.y, vec.z, vec.w}};
		return v;
	}
	
	// Convert a dVector3 to a pr::v4
	inline pr::v4 ode(dVector3 const& vec, float w)
	{
		return pr::v4::make(vec[0], vec[1], vec[2], w);
	}
	
	// Convert a pr::m4x4 to a dM4x4
	inline dM4x4 ode(pr::m4x4 const& o2w)
	{
		dM4x4 m;
		impl::copy(m.m_rot, GetTranspose3x3(o2w).x.ToArray(), 12);
		impl::copy(m.m_pos, o2w.pos.ToArray(), 4);
		return m;
	}
	
	// Convert a position and orientation to a pr::m4x4
	inline pr::m4x4 ode(dReal const* pos, dReal const* rot)
	{
		pr::m4x4 o2w;
		impl::copy(o2w.x.ToArray(), rot, 12);
		impl::copy(o2w.pos.ToArray(), pos, 4);
		o2w.x.w = o2w.y.w = o2w.z.w = 0.0f; o2w.w.w = 1.0f;
		return pr::Transpose3x3(o2w);
	}
	
	// Convert a dM4x4 to a pr::m4x4
	inline pr::m4x4 ode(dM4x4 const& o2w)
	{
		return ode(o2w.m_pos, o2w.m_rot);
	}
	
	// Old
	inline dVector3& ode_v3(pr::v4 const& vec, dVector3& out)
	{
		impl::copy(out, vec.ToArray(), 4);
		return out;
	}

	inline pr::v4 pr_v4(dVector3 const& vec, float w)
	{
		pr::v4 out; out.w = w;
		impl::copy(out.ToArray(), vec, 3);
		return out;
	}

	inline pr::m4x4 pr_m4x4(dReal const* pos, dReal const* rot)
	{
		return ode(pos, rot);
	}

	inline void ode_posrot(pr::m4x4 const& o2w, dVector3& pos, dMatrix3& rot)
	{
		impl::copy(pos, o2w.pos.ToArray(), 4);
		impl::copy(rot, GetTranspose3x3(o2w).x.ToArray(), 12);
	}
	// Old
}

#endif
