//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include "pr/maths/maths.h"
#include "pr/common/colour.h"
#include "pr/collision/shapes.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/collision/shapes.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace ldr
	{
		template <typename TStr> inline TStr& Shape(TStr& str, char const* name, Col c, pr::collision::ShapeSphere const& sph, pr::m4x4 const& o2w = pr::m4x4Identity)
		{
			if (!name) name = "";
			return Append(str,"*Sphere",name,c,"{",sph.m_radius,O2W(o2w),"}\n");
		}
		template <typename TStr> inline TStr& Shape(TStr& str, char const* name, Col c, pr::collision::ShapeBox const& box, pr::m4x4 const& o2w = pr::m4x4Identity)
		{
			if (!name) name = "";
			return Append(str,"*Box",name,c,"{",2*box.m_radius.xyz,O2W(o2w),"}\n");
		}
	}
}

