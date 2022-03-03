//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/shape/shapetriangle.h"
#include "pr/physics/shape/shapeterrain.h"
#include "pr/physics/terrain/iterrain.h"
#include "physics/utility/debug.h"
#include "physics/utility/profile.h"

using namespace pr;
using namespace pr::ph;

struct TriangleVsTerrainContext
{
	Shape const*		m_objA;
	Shape const*		m_objB;
	ContactManifold*	m_manifold;
};

// Results from mesh vs terrain intercept test
bool TriangleVsTerrainResult(terrain::Result const& result, void* context)
{
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_terrain_point, ph::OverflowValue), "");
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_normal       , ph::OverflowValue), "");
	PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::Box("Contact", "FFFF0000", result.m_terrain_point, 0.02f);)

	TriangleVsTerrainContext& ctx = *static_cast<TriangleVsTerrainContext*>(context);

	Contact contact;
	contact.m_pointA		= result.m_sample->m_point;
	contact.m_pointB		= result.m_terrain_point;
	contact.m_normal		= result.m_normal;
	contact.m_material_idA	= ctx.m_objA->m_material_id;
	contact.m_material_idB	= result.m_material_id;
	contact.m_depth			= Length(result.m_sample->m_point - result.m_terrain_point) - result.m_sample->m_radius;
	ctx.m_manifold->Add(contact);
	return true;
}

// Detect collisions between a triangle and the terrain
void pr::ph::TriangleVsTerrain(Shape const& triangle, m4x4 const& a2w, Shape const& terrain, m4x4 const&, ContactManifold& manifold, CollisionCache*)
{
	PR_DECLARE_PROFILE(PR_PROFILE_TERR_COLLISION, phTriangleVsTerrain);
	PR_PROFILE_SCOPE  (PR_PROFILE_TERR_COLLISION, phTriangleVsTerrain);

	ShapeTriangle const& triangle_shape	= shape_cast<ShapeTriangle>(triangle);
	ShapeTerrain  const& terrain_shape	= shape_cast<ShapeTerrain>(terrain);

	if( triangle.m_flags & EShapeFlags_WholeShapeTerrainCollision )
	{
		terrain_shape.m_terrain->CollideShape(triangle, a2w, manifold);
	}
	else
	{
		TriangleVsTerrainContext ctx;
		ctx.m_objA		= &triangle;
		ctx.m_objB		= &terrain;
		ctx.m_manifold	= &manifold;

		terrain::Sample point[3];
		point[0].m_point = a2w.pos + a2w * triangle_shape.m_v.x; point[0].m_radius = 0.0f;
		point[1].m_point = a2w.pos + a2w * triangle_shape.m_v.y; point[1].m_radius = 0.0f;
		point[2].m_point = a2w.pos + a2w * triangle_shape.m_v.z; point[2].m_radius = 0.0f;
		terrain_shape.m_terrain->CollideSpheres(point, 3, TriangleVsTerrainResult, &ctx);
	}
}