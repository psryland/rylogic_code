//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/terrain/iterrain.h"
#include "pr/physics/shape/shapesphere.h"
#include "pr/physics/shape/shapeterrain.h"
#include "physics/utility/debug.h"

using namespace pr;
using namespace pr::ph;

struct SphereVsTerrainContext
{
	Shape const*		m_objA;
	Shape const*		m_objB;
	ContactManifold*	m_manifold;
};

// Results from sphere vs terrain intercept test
bool SphereVsTerrainResult(terrain::Result const& result, void* context)
{
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_terrain_point, ph::OverflowValue), "");
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_normal       , ph::OverflowValue), "");

	SphereVsTerrainContext& ctx = *static_cast<SphereVsTerrainContext*>(context);

	Contact contact;
	contact.m_pointA		= result.m_sample->m_point - result.m_normal * result.m_sample->m_radius;
	contact.m_pointB		= result.m_terrain_point;
	contact.m_normal		= result.m_normal;
	contact.m_material_idA	= ctx.m_objA->m_material_id;
	contact.m_material_idB	= result.m_material_id;
	contact.m_depth			= -(Dot3(result.m_normal, result.m_sample->m_point - result.m_terrain_point) - result.m_sample->m_radius);
	ctx.m_manifold->Add(contact);
	return true;
}

// Detect collisions between a sphere and a terrain object
void pr::ph::SphereVsTerrain(Shape const& sphere, m4x4 const& a2w, Shape const& terrain, m4x4 const&, ContactManifold& manifold, CollisionCache*)
{
	ShapeTerrain const& terrain_shape = shape_cast<ShapeTerrain>(terrain);
	ShapeSphere const&  sphere_shape  = shape_cast<ShapeSphere >(sphere);

	if( sphere.m_flags & EShapeFlags_WholeShapeTerrainCollision )
	{
		terrain_shape.m_terrain->CollideShape(sphere, a2w, manifold);
	}
	else
	{
		SphereVsTerrainContext ctx;
		ctx.m_objA = &sphere;
		ctx.m_objB = &terrain;
		ctx.m_manifold = &manifold;

		terrain::Sample point;
		point.m_point  = a2w.pos;
		point.m_radius = sphere_shape.m_radius;
		terrain_shape.m_terrain->CollideSpheres(&point, 1, SphereVsTerrainResult, &ctx);
	}
}
