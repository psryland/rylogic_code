//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/terrain/iterrain.h"
#include "pr/physics/shape/shapebox.h"
#include "pr/physics/shape/shapeterrain.h"
#include "physics/utility/debug.h"

using namespace pr;
using namespace pr::ph;

struct BoxVsTerrainContext
{
	Shape const*		m_objA;
	Shape const*		m_objB;
	ContactManifold*	m_manifold;
};

// Results from sphere vs terrain intercept test
bool BoxVsTerrainResult(terrain::Result const& result, void* context)
{
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_terrain_point, ph::OverflowValue), "");
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_normal       , ph::OverflowValue), "");

	BoxVsTerrainContext& ctx = *static_cast<BoxVsTerrainContext*>(context);
	
	Contact contact;
	contact.m_pointA		= result.m_sample->m_point;
	contact.m_pointB		= result.m_terrain_point;
	contact.m_normal		= result.m_normal;
	contact.m_material_idA	= ctx.m_objA->m_material_id;
	contact.m_material_idB	= result.m_material_id;
	contact.m_depth			= Length3(result.m_sample->m_point - result.m_terrain_point) - result.m_sample->m_radius;
	ctx.m_manifold->Add(contact);
	return true;
}

// Detect collisions between a box and a terrain object
void pr::ph::BoxVsTerrain(Shape const& box, m4x4 const& a2w, Shape const& terrain, m4x4 const&, ContactManifold& manifold, CollisionCache*)
{
	ShapeBox const&		box_shape     = shape_cast<ShapeBox    >(box);
	ShapeTerrain const& terrain_shape = shape_cast<ShapeTerrain>(terrain);

	if( box.m_flags & EShapeFlags_WholeShapeTerrainCollision )
	{
		terrain_shape.m_terrain->CollideShape(box, a2w, manifold);
	}
	else
	{
		BoxVsTerrainContext ctx;
		ctx.m_objA		= &box;
		ctx.m_objB		= &terrain;
		ctx.m_manifold	= &manifold;

		v4 radius[3];
		radius[0] = box_shape.m_radius.x * a2w.x;
		radius[1] = box_shape.m_radius.y * a2w.y;
		radius[2] = box_shape.m_radius.z * a2w.z;

		terrain::Sample corner[8];
		corner[0].m_point = a2w.pos - radius[0] - radius[1] - radius[2]; corner[0].m_radius = 0.0f;
		corner[1].m_point = a2w.pos + radius[0] - radius[1] - radius[2]; corner[1].m_radius = 0.0f;
		corner[2].m_point = a2w.pos - radius[0] + radius[1] - radius[2]; corner[2].m_radius = 0.0f;
		corner[3].m_point = a2w.pos + radius[0] + radius[1] - radius[2]; corner[3].m_radius = 0.0f;
		corner[4].m_point = a2w.pos - radius[0] - radius[1] + radius[2]; corner[4].m_radius = 0.0f;
		corner[5].m_point = a2w.pos + radius[0] - radius[1] + radius[2]; corner[5].m_radius = 0.0f;
		corner[6].m_point = a2w.pos - radius[0] + radius[1] + radius[2]; corner[6].m_radius = 0.0f;
		corner[7].m_point = a2w.pos + radius[0] + radius[1] + radius[2]; corner[7].m_radius = 0.0f;
		
		terrain_shape.m_terrain->CollideSpheres(corner, 8, BoxVsTerrainResult, &ctx);
	}
}
