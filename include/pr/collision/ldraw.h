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
#include "pr/ldraw/ldr_helper.h"

namespace pr::ldr
{
	TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::Shape const& shape, m4_cref<> o2w);
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeSphere const& shape, m4_cref<> o2w)
	{
		return Sphere(str, name, colour, shape.m_radius, o2w.pos);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeBox const& shape, m4_cref<> o2w)
	{
		return Box(str, name, colour, shape.m_radius * 2, o2w);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeTriangle const& shape, m4_cref<> o2w)
	{
		return Triangle(str, name, colour, shape.m_v.x, shape.m_v.y, shape.m_v.z, o2w);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeLine const& shape, m4_cref<> o2w)
	{
		auto r = shape.m_radius * o2w.z;
		return Line(str, name, colour, o2w.pos - r, o2w.pos + r);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeArray const& shape, m4_cref<> o2w)
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
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::Shape const& shape, m4_cref<> o2w)
	{
		using namespace collision;
		switch (shape.m_type)
		{
		default: throw std::runtime_error("Unknown shape type");
		case EShape::Sphere:     return Shape(str, name, colour, shape_cast<ShapeSphere>(shape), o2w);
		case EShape::Box:        return Shape(str, name, colour, shape_cast<ShapeBox>(shape), o2w);
		case EShape::Triangle:   return Shape(str, name, colour, shape_cast<ShapeTriangle>(shape), o2w);
		case EShape::Line:       return Shape(str, name, colour, shape_cast<ShapeLine>(shape), o2w);
		case EShape::Array:      return Shape(str, name, colour, shape_cast<ShapeArray>(shape), o2w);
		//case EShape::Polytope: return Shape(str, name, colour, shape_cast<ShapePolytope>(shape), o2w);
		//case EShape::Cylinder: return Shape(str, name, colour, shape_cast<ShapeCylinder>(shape), o2w);
		}
	}
}

