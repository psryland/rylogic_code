//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/math/math.h"
#include "pr/gfx/colour.h"
#include "pr/common/ldraw.h"
#include "pr/collision/shape.h"
#include "pr/collision/shape_sphere.h"
#include "pr/collision/shape_box.h"
#include "pr/collision/shape_line.h"
#include "pr/collision/shape_triangle.h"
#include "pr/collision/shape_polytope.h"
#include "pr/collision/shape_array.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"

namespace pr::ldraw
{
	// Add a collision shape as LDraw children of 'target'
	inline void AddShape(LdrBase& target, collision::Shape const& shape)
	{
		using namespace collision;
		switch (shape.m_type)
		{
			case EShape::Sphere:
			{
				auto& s = shape_cast<ShapeSphere>(shape);
				target.Sphere().radius(s.m_radius).o2w(s.m_base.m_s2p);
				break;
			}
			case EShape::Box:
			{
				auto& s = shape_cast<ShapeBox>(shape);
				target.Box().box(s.m_radius.x * 2, s.m_radius.y * 2, s.m_radius.z * 2).o2w(s.m_base.m_s2p);
				break;
			}
			case EShape::Triangle:
			{
				auto& s = shape_cast<ShapeTriangle>(shape);
				target.Triangle().tri(
					seri::Vec3{s.m_v.x.x, s.m_v.x.y, s.m_v.x.z},
					seri::Vec3{s.m_v.y.x, s.m_v.y.y, s.m_v.y.z},
					seri::Vec3{s.m_v.z.x, s.m_v.z.y, s.m_v.z.z}
				).o2w(s.m_base.m_s2p);
				break;
			}
			case EShape::Line:
			{
				auto& s = shape_cast<ShapeLine>(shape);
				auto r = s.m_radius * s.m_base.m_s2p.z;
				auto a = s.m_base.m_s2p.pos - r;
				auto b = s.m_base.m_s2p.pos + r;
				target.Line().line(seri::Vec3{a.x, a.y, a.z}, seri::Vec3{b.x, b.y, b.z});
				break;
			}
			case EShape::Polytope:
			{
				auto& s = shape_cast<ShapePolytope>(shape);
				auto& tri = target.Triangle();
				for (auto const& face : s.faces())
				{
					auto a = s.vertex(face.m_index[0]);
					auto b = s.vertex(face.m_index[1]);
					auto c = s.vertex(face.m_index[2]);
					tri.tri(pr::ldraw::seri::Vec3{a.x, a.y, a.z}, pr::ldraw::seri::Vec3{b.x, b.y, b.z}, pr::ldraw::seri::Vec3{c.x, c.y, c.z});
				}
				tri.o2w(s.m_base.m_s2p);
				break;
			}
			case EShape::Array:
			{
				auto& s = shape_cast<ShapeArray>(shape);
				auto& grp = target.Group();
				grp.o2w(s.m_base.m_s2p);
				for (auto const* sub = s.begin(), *end = s.end(); sub != end; sub = next(sub))
					AddShape(grp, *sub);
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown shape type");
			}
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::ldraw
{
	PRUnitTest(LdrPhysicsShapeTests)
	{
		Builder L;
		auto& grp = L.Group("Shape");
		(void)grp;
	}
}
#endif
