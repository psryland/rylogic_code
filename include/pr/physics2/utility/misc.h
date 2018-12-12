//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/inertia.h"
#include "pr/collision/collision.h"

namespace pr::physics
{
	// Extrapolate the position based on the given momentum, forces, and inertia
	// All parameters must be in the same space. The accuracy of this extrapolation
	// decreases with larger angular momentum or greater 'dt'.
	inline m4x4 ExtrapolateO2W(m4_cref<> o2w, v8f const& momentum, v8f const& force, InertiaInv const& inertia_inv, float dt)
	{
		// S = So + Vt + 0.5At²
		//   = So + t * (V + 0.5At)
		//   = So + 0.5 * t * (2*I^h + (I^f)t)
		//   = So + 0.5 * t * I^(2*h + ft)
		auto h = 2.0f * momentum + dt * force;
		auto dx = 0.5f * dt * (inertia_inv * h);
		return m4x4
		{
			m3x4::Rotation(dx.ang) * o2w.rot,
			dx.lin + o2w.pos
		};
	}
}