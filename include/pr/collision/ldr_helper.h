//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/maths/maths.h"
#include "pr/common/colour.h"
#include "pr/collision/shapes.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace ldr
	{
		//template <typename TStr> inline TStr& Shape(TStr& str, char const* name, Col c, pr::collision::ShapeSphere const& sph, pr::m4x4 const& o2w = pr::m4x4Identity)
		//{
		//	if (!name) name = "";
		//	return Append(str,"*Sphere",name,c,"{",sph.m_radius,O2W(o2w),"}\n");
		//}
		//template <typename TStr> inline TStr& Shape(TStr& str, char const* name, Col c, pr::collision::ShapeBox const& box, pr::m4x4 const& o2w = pr::m4x4Identity)
		//{
		//	if (!name) name = "";
		//	return Append(str,"*Box",name,c,"{",2*box.m_radius.xyz,O2W(o2w),"}\n");
		//}

		template <typename Str, typename Char = Str::value_type> inline Str& Shape(Str& str, Char const* name, Col colour, collision::ShapeSphere const& shape, m4x4_cref o2w)
		{
			return Sphere(str, name, colour, o2w.pos, shape.m_radius);
		}
		template <typename Str, typename Char = Str::value_type> inline Str& Shape(Str& str, Char const* name, Col colour, collision::ShapeBox const& shape, m4x4_cref o2w)
		{
			return Box(str, name, colour, shape.m_radius * 2.0f, o2w);
		}
		template <typename Str, typename Char = Str::value_type> inline Str& Shape(Str& str, Char const* name, Col colour, collision::ShapeLine const& shape, m4x4_cref o2w)
		{
			auto r = shape.m_radius * o2w.z;
			return Line(str, name, colour, o2w.pos - r, o2w.pos + r);
		}
		template <typename Str, typename Char = Str::value_type> inline Str& Shape(Str& str, Char const* name, Col colour, collision::ShapeArray const& shape, m4x4_cref o2w)
		{
			GroupStart(str, name, colour);
			for (collision::Shape const *s = shape.begin(), *s_end = shape.end(); s != s_end; s = next(s))
			{
				Shape(str, collision::ToString(s->m_type), colour, *s, s->m_s2p);
			}
			Append(str, O2W(o2w));
			GroupEnd(str);
			return str;
		}
		template <typename Str, typename Char = Str::value_type> inline Str& Shape(Str& str, Char const* name, Col colour, collision::Shape const& shape, m4x4_cref o2w)
		{
			switch (shape.m_type)
			{
			default: assert("unsupported physics shape for LDR script"); return str;
			case collision::EShape::Sphere:   return Shape(str, name, colour, collision::shape_cast<collision::ShapeSphere>  (shape), o2w); break;
			case collision::EShape::Box:      return Shape(str, name, colour, collision::shape_cast<collision::ShapeBox>     (shape), o2w); break;
			//case collision::EShape::Polytope: return Shape(str, name, colour, collision::shape_cast<collision::ShapePolytope>(shape), o2w); break;
			//case collision::EShape::Cylinder: return Shape(str, name, colour, collision::shape_cast<collision::ShapeCylinder>(shape), o2w); break;
			//case collision::EShape::Triangle: return Shape(str, name, colour, collision::shape_cast<collision::ShapeTriangle>(shape), o2w); break;
			case collision::EShape::Line:     return Shape(str, name, colour, collision::shape_cast<collision::ShapeLine>    (shape), o2w); break;
			case collision::EShape::Array:    return Shape(str, name, colour, collision::shape_cast<collision::ShapeArray>   (shape), o2w); break;
			}
		}
	}
}

