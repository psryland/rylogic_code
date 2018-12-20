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
	// Forward declare Shape function for recursive shape types
	TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::Shape const& shape, m4_cref<> o2w = m4x4Identity);

	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeSphere const& shape, m4_cref<> o2w = m4x4Identity)
	{
		return Sphere(str, name, colour, shape.m_radius, o2w * shape.m_base.m_s2p);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeBox const& shape, m4_cref<> o2w = m4x4Identity)
	{
		return Box(str, name, colour, shape.m_radius * 2, o2w * shape.m_base.m_s2p);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeTriangle const& shape, m4_cref<> o2w = m4x4Identity)
	{
		return Triangle(str, name, colour, shape.m_v.x, shape.m_v.y, shape.m_v.z, o2w * shape.m_base.m_s2p);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeLine const& shape, m4_cref<> o2w_ = m4x4Identity)
	{
		auto o2w = o2w_ * shape.m_base.m_s2p;
		auto r = shape.m_radius * o2w.z;
		return Line(str, name, colour, o2w.pos - r, o2w.pos + r);
	}
	inline TStr& Shape(TStr& str, typename TStr::value_type const* name, Col colour, collision::ShapeArray const& shape, m4_cref<> o2w = m4x4Identity)
	{
		GroupStart(str, name, colour);
		for (collision::Shape const *s = shape.begin(), *s_end = shape.end(); s != s_end; s = next(s))
		{
			Shape(str, collision::ToString(s->m_type), colour, *s);
		}
		GroupEnd(str, o2w * shape.m_base.m_s2p);
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

