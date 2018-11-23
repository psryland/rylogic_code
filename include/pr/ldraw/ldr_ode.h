//************************************
// LineDrawerHelper
//  Copyright (c) Paul Ryland 2006
//************************************
#pragma once

#include <ode/ode.h>
#include "pr/common/prtypes.h"
#include "pr/maths/pr_to_ode.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace ldr
	{
		// Generate an ldr string that describes the geometry in 'geom'
		// Each geom object should have it's data pointer set as a pr::variant, with the unsigned int value being the colour
		template <typename TStr> inline TStr& Geom(TStr& str, dGeomID geom)
		{
			using Char = TStr::value_type;

			switch (dGeomGetClass(geom))
			{
			default:
				if (dGeomIsSpace(geom))
				{
					GroupStart(str, PR_STRLITERAL(Char, "submodel"), 0);
					for (int i = 0, i_end = dSpaceGetNumGeoms((dSpaceID)geom); i != i_end; ++i)
						Geom(str, dSpaceGetGeom((dSpaceID)geom, i));
					GroupEnd(str);
				}
				break;
			case dSphereClass:
				{
					float rad = dGeomSphereGetRadius(geom);
					pr::v4 pos = pr::v4(pr::v3(dGeomGetOffsetPosition(geom)), 1.0f);
					pr::variant v = { dGeomGetData(geom) };
					Sphere(str, PR_STRLITERAL(Char, "sphere"), v.ptr != nullptr ? v.ui : 0xFFFFFFFF, pos, rad);
				}break;
			case dBoxClass:
				{
					dVector3 box_size; dGeomBoxGetLengths(geom, box_size);
					pr::v4 dim = pr::v4(box_size, 0.0f);
					pr::m4x4 o2p = pr::ode(dGeomGetOffsetPosition(geom), dGeomGetOffsetRotation(geom));
					pr::variant v = { dGeomGetData(geom) };
					Box(str, PR_STRLITERAL(Char, "box"), v.ptr != nullptr ? v.ui : 0xFFFFFFFF, o2p, dim);
				}break;
			case dCapsuleClass:
				{
					float rad, len; dGeomCapsuleGetParams(geom, &rad, &len);
					pr::m4x4 o2p = pr::ode(dGeomGetOffsetPosition(geom), dGeomGetOffsetRotation(geom));
					pr::variant v = { dGeomGetData(geom) };
					Cylinder(str, PR_STRLITERAL(Char, "caps"), v.ptr != nullptr ? v.ui : 0xFFFFFFFF, o2p, 1, len, rad);
					//Capsule("caps", v.ptr == 0 ? 0xFFFF0000 : v.ui, o2p, rad, len, str);
				}break;
			}
			return str;
		}
	}
}
