//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// LDR script generating helper functions

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/shape.h"
#include "pr/physics2/shape/shape_box.h"
#include "pr/physics2/shape/shape_array.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace ldr
	{
		// Returns a string containing a description of a physics shape
		template <typename TStr> inline TStr& PhShape(TStr& str, typename TStr::value_type const* name, Col colour, physics::ShapeSphere const& shape, m4x4_cref o2w)
		{
			using namespace pr::physics;
			return Sphere(str, name, colour, o2w.pos, shape.m_radius);
		}
		template <typename TStr> inline TStr& PhShape(TStr& str, typename TStr::value_type const* name, Col colour, physics::ShapeBox const& shape, m4x4_cref o2w)
		{
			using namespace pr::physics;
			return Box(str, name, colour, o2w, shape.m_radius * 2.0f);
		}
		template <typename TStr> inline TStr& PhShape(TStr& str, typename TStr::value_type const* name, Col colour, physics::ShapeArray const& shape, m4x4_cref o2w)
		{
			using namespace pr::physics;
			GroupStart(str, name, colour);
			for (Shape const *s = arr.begin(), *s_end = arr.end(); s != s_end; s = next(s))
			{
				PhShape(str, ToString(s->m_type), colour, *s, s->m_shape_to_model);
			}
			Append(str, o2w);
			GroupEnd(str);
			return str;
		}
		template <typename TStr> inline TStr& PhShape(TStr& str, typename TStr::value_type const* name, Col colour, physics::Shape const& shape, m4x4_cref o2w)
		{
			using namespace pr::physics;
			switch (shape.m_type)
			{
			default: assert("unsupported physics shape for LDR script"); break;
			case EShape::Sphere:   return PhShape(str, name, colour, shape_cast<ShapeSphere>  (shape), o2w); break;
			case EShape::Box:      return PhShape(str, name, colour, shape_cast<ShapeBox>     (shape), o2w); break;
			//case EShape::Polytope: return PhShape(str, name, colour, shape_cast<ShapePolytope>(shape), o2w); break;
			//case EShape::Cylinder: return PhShape(str, name, colour, shape_cast<ShapeCylinder>(shape), o2w); break;
			//case EShape::Triangle: return PhShape(str, name, colour, shape_cast<ShapeTriangle>(shape), o2w); break;
			case EShape::Array:    return PhShape(str, name, colour, shape_cast<ShapeArray>   (shape), o2w); break;
			}
		}
		
	}
}