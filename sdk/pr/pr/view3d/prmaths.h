//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
// Conversion from View3D maths types to pr lib maths types
#pragma once
#ifndef PR_VIEW3D_PR_MATHS_H
#define PR_VIEW3D_PR_MATHS_H

#include "pr/view3d/view3d.h"
#include "pr/maths/maths.h"

namespace view3d
{
	template <> struct Convert<View3DV2  , pr::v2  > { static View3DV2   To(pr::v2   const& v)  { return reinterpret_cast<View3DV2 const&>(v); } };
	template <> struct Convert<View3DV4  , pr::v4  > { static View3DV4   To(pr::v4   const& v)  { return reinterpret_cast<View3DV4 const&>(v); } };
	template <> struct Convert<View3DM4x4, pr::m4x4> { static View3DM4x4 To(pr::m4x4 const& m)  { return reinterpret_cast<View3DM4x4 const&>(m); } };
	template <> struct Convert<View3DBBox, pr::BBox> { static View3DBBox To(pr::BBox const& bb) { return reinterpret_cast<View3DBBox const&>(bb); } };

	template <> struct Convert<pr::v2  , View3DV2  > { static pr::v2   To(View3DV2   const& v)  { return pr::v2::make(v.x, v.y);           } };
	template <> struct Convert<pr::v4  , View3DV4  > { static pr::v4   To(View3DV4   const& v)  { return pr::v4::make(v.x, v.y, v.z, v.w); } };
	template <> struct Convert<pr::m4x4, View3DM4x4> { static pr::m4x4 To(View3DM4x4 const& m)  { return pr::m4x4::make(view3d::To<pr::v4>(m.x), view3d::To<pr::v4>(m.y), view3d::To<pr::v4>(m.z), view3d::To<pr::v4>(m.w)); } };
	template <> struct Convert<pr::BBox, View3DBBox> { static pr::BBox To(View3DBBox const& bb) { return pr::BBox::make(view3d::To<pr::v4>(bb.centre), view3d::To<pr::v4>(bb.radius)); } };
}

#endif
