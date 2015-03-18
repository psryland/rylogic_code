//************************************
// LineDrawerHelper
//  (c)opyright Paul Ryland 2006
//************************************
#ifndef PR_LINEDRAWER_ODE_H
#define PR_LINEDRAWER_ODE_H
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
		template <typename TStr> inline void Geom(dGeomID geom, TStr& str)
		{
			switch (dGeomGetClass(geom))
			{
			default:
				if (dGeomIsSpace(geom))
				{
					GroupStart("submodel", 0, str);
					for (int i = 0, i_end = dSpaceGetNumGeoms((dSpaceID)geom); i != i_end; ++i)
						Geom(dSpaceGetGeom((dSpaceID)geom, i), str);
					GroupEnd(str);
				}
				break;
			case dSphereClass:
				{
					float rad = dGeomSphereGetRadius(geom);
					pr::v4 pos = pr::v4::make(dGeomGetOffsetPosition(geom), 1.0f);
					pr::variant v = { dGeomGetData(geom) };
					Sphere("sphere", v.ptr != nullptr ? v.ui : 0xFFFFFFFF, pos, rad, str);
				}break;
			case dBoxClass:
				{
					dVector3 box_size; dGeomBoxGetLengths(geom, box_size);
					pr::v4 dim = pr::v4::make(box_size, 0.0f);
					pr::m4x4 o2p = pr::pr_m4x4(dGeomGetOffsetPosition(geom), dGeomGetOffsetRotation(geom));
					pr::variant v = { dGeomGetData(geom) };
					Box("box", v.ptr != nullptr ? v.ui : 0xFFFFFFFF, o2p, dim, str);
				}break;
			case dCapsuleClass:
				{
					float rad, len; dGeomCapsuleGetParams(geom, &rad, &len);
					pr::m4x4 o2p = pr::pr_m4x4(dGeomGetOffsetPosition(geom), dGeomGetOffsetRotation(geom));
					pr::variant v = { dGeomGetData(geom) };
					Cylinder("caps", v.ptr != nullptr ? v.ui : 0xFFFFFFFF, o2p, 1, len, rad, str);
					//Capsule("caps", v.ptr == 0 ? 0xFFFF0000 : v.ui, o2p, rad, len, str);
				}break;
			}
		}
	}
}

#endif
