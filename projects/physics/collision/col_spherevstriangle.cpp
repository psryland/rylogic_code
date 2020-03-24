//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/shape/shapesphere.h"
#include "pr/physics/shape/shapetriangle.h"
#include "physics/utility/debug.h"

using namespace pr;
using namespace pr::ph;

// Detect collisions between a sphere and a triangle
void pr::ph::SphereVsTriangle(Shape const& sphere, m4x4 const& a2w, Shape const& triangle, m4x4 const& b2w, ContactManifold& manifold, CollisionCache*)
{
	ShapeSphere   const& sphere_shape   = shape_cast<ShapeSphere>(sphere);
	ShapeTriangle const& triangle_shape = shape_cast<ShapeTriangle>(triangle);

	// Get the sphere in triangle space
	v4 pos = InvertFast(b2w) * a2w.pos;

	// Find the closest point on the triangle to the sphere
	v4 a = triangle_shape.m_v.x; a.w = 1.0f;
	v4 b = triangle_shape.m_v.y; b.w = 1.0f;
	v4 c = triangle_shape.m_v.z; c.w = 1.0f;
	v4 closest_point = ClosestPoint_PointToTriangle(pos, a, b, c);
	v4 sep = pos - closest_point;
	float dist = Length(sep);
	if( dist < sphere_shape.m_radius )
	{
		Contact contact;
		contact.m_normal		= b2w * (FEql(dist,0) ? ((dist>=0.0f)*2.0f-1.0f) * triangle_shape.m_v.w : (sep / dist));
		contact.m_pointA		= a2w.pos - contact.m_normal * sphere_shape.m_radius;
		contact.m_pointB		= b2w * closest_point;
		contact.m_material_idA	= sphere_shape.m_base.m_material_id;
		contact.m_material_idB	= triangle_shape.m_base.m_material_id;
		contact.m_depth			= sphere_shape.m_radius - dist;
		manifold.Add(contact);
	}
}
