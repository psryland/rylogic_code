//***************************************************************************************************
// View 3D
// Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
// Conversion from View3D maths types to pr lib maths types
#pragma once
#include "pr/common/to.h"
#include "pr/maths/maths.h"
#include "pr/gfx/colour.h"
#include "pr/view3d/dll/view3d.h"

namespace pr
{
	template <> struct Convert<View3DV2, v2>
	{
		static View3DV2 To_(v2 const& v)
		{
			return reinterpret_cast<View3DV2 const&>(v);
		}
	};
	template <> struct Convert<View3DV4, v4>
	{
		static View3DV4 To_(v4 const& v)
		{
			return reinterpret_cast<View3DV4 const&>(v);
		}
	};
	template <> struct Convert<View3DM4x4, m4x4>
	{
		static View3DM4x4 To_(m4x4 const& m)
		{
			return reinterpret_cast<View3DM4x4 const&>(m);
		}
	};
	template <> struct Convert<View3DBBox, BBox>
	{
		static View3DBBox To_(BBox const& bb)
		{
			return reinterpret_cast<View3DBBox const&>(bb);
		}
	};
	template <> struct Convert<View3DColour, Colour32>
	{
		static View3DColour To_(Colour32 col)
		{
			return col.argb;
		}
	};

	template <> struct Convert<v2, View3DV2>
	{
		static v2 To_(View3DV2 const& v)
		{
			return v2{v.x, v.y};
		}
	};
	template <> struct Convert<v4, View3DV4>
	{
		static v4 To_(View3DV4 const& v)
		{
			return v4{v.x, v.y, v.z, v.w};
		}
	};
	template <> struct Convert<m4x4, View3DM4x4>
	{
		static m4x4 To_(View3DM4x4 const& m)
		{
			return m4x4
			{
				pr::To<v4>(m.x),
				pr::To<v4>(m.y),
				pr::To<v4>(m.z),
				pr::To<v4>(m.w)
			};
		}
	};
	template <> struct Convert<BBox, View3DBBox>
	{
		static BBox To_(View3DBBox const& bb)
		{
			return BBox
			{
				pr::To<v4>(bb.centre),
				pr::To<v4>(bb.radius)
			};
		}
	};
	template <> struct Convert<Colour32, View3DColour>
	{
		static Colour32 To_(View3DColour col)
		{
			return Colour32(col);
		}
	};
}
