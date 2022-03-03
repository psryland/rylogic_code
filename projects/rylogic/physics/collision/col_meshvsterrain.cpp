//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/collider.h"
#include "pr/physics/collision/contactmanifold.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/shape/shapepolytope.h"
#include "pr/physics/shape/shapeterrain.h"
#include "pr/physics/terrain/iterrain.h"
#include "physics/utility/debug.h"
#include "physics/utility/profile.h"

using namespace pr;
using namespace pr::ph;

struct MeshVsTerrainContext
{
	Shape const*		m_objA;
	Shape const*		m_objB;
	ContactManifold*	m_manifold;
};

// Results from mesh vs terrain intercept test
bool MeshVsTerrainResult(terrain::Result const& result, void* context)
{
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_terrain_point, ph::OverflowValue), "");
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(result.m_normal       , ph::OverflowValue), "");
	PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::Box("Contact", "FFFF0000", result.m_terrain_point, 0.02f);)

	MeshVsTerrainContext& ctx = *static_cast<MeshVsTerrainContext*>(context);

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

// Detect collisions between a polytope and the terrain
void pr::ph::MeshVsTerrain(Shape const& mesh, m4x4 const& a2w, Shape const& terrain, m4x4 const&, ContactManifold& manifold, CollisionCache*)
{
	PR_DECLARE_PROFILE(PR_PROFILE_TERR_COLLISION, phMeshVsTerrain);
	PR_PROFILE_SCOPE  (PR_PROFILE_TERR_COLLISION, phMeshVsTerrain);

	ShapePolytope const& poly = shape_cast<ShapePolytope>(mesh);
	ShapeTerrain const&  terr = shape_cast<ShapeTerrain>(terrain);

	if( mesh.m_flags & EShapeFlags_WholeShapeTerrainCollision )
	{
		terr.m_terrain->CollideShape(mesh, a2w, manifold);
	}
	else
	{
		MeshVsTerrainContext ctx;
		ctx.m_objA = &mesh;
		ctx.m_objB = &terrain;
		ctx.m_manifold = &manifold;

		std::size_t const MaxSamples = 20;
		terrain::Sample point[MaxSamples];
		std::size_t		num_points = 0;

		// If the polytope has a small number of vertices then test all of them
		if( poly.m_vert_count <= MaxSamples )
		{
			terrain::Sample* pt = point;
			for( v4 const* vert = poly.vert_begin(), *vert_end = poly.vert_end(); vert != vert_end; ++vert, ++pt )
			{
				pt->m_point	 = a2w * *vert;
				pt->m_radius = 0.0f;
			}
			num_points = pt - point;

			PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupStart("TerrainContacts");)
			terr.m_terrain->CollideSpheres(point, num_points, MeshVsTerrainResult, &ctx);
			PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupEnd();)
			return;
		}

		ContactManifold local_manifold;
		ctx.m_manifold = &local_manifold;

		// The sampling the mesh algorithm below works pretty well for low frequency
		// terrain but it breaks down in high frequency 'V' shaped terrain. The following
		// is a fail safe to prevent the centre of mass from falling through the terrain.
		point[0].m_point  = a2w.pos;
		point[0].m_radius = 0.0f;
		terr.m_terrain->CollideSpheres(point, 1, MeshVsTerrainResult, &ctx);
		if( local_manifold.IsOverlap() )
		{
			manifold.Add(local_manifold[0]);
			return;
		}
		
		// Sample the polytope in a range of directions to approximate it's shape
		m3x4 w2a = InvertFast(a2w.rot);

		std::size_t const NumVerts = 17;
		num_points = NumVerts;
		std::size_t sup_vert_id = 0;
		static_assert(MaxSamples >= NumVerts, "");

		// Choose directions to sample the polytope
		v4 dir[4];
		dir[0] = w2a.x;
		dir[1] = w2a.z;
		dir[2] = w2a.x + w2a.z;
		dir[3] = w2a.x - w2a.z;
		point[0].m_point  = a2w * SupportVertex(poly, -w2a.y, sup_vert_id, sup_vert_id);
		point[0].m_radius = 0.0f;
		for( int i = 1, j = 0; j != 4; ++j )
		{
			point[i].m_point = a2w * SupportVertex(poly, -dir[j]         ,sup_vert_id, sup_vert_id); point[i].m_radius = 0.0f; ++i;
			point[i].m_point = a2w * SupportVertex(poly, -dir[j] - w2a.y ,sup_vert_id, sup_vert_id); point[i].m_radius = 0.0f; ++i;
			point[i].m_point = a2w * SupportVertex(poly,  dir[j] - w2a.y ,sup_vert_id, sup_vert_id); point[i].m_radius = 0.0f; ++i;
			point[i].m_point = a2w * SupportVertex(poly,  dir[j]         ,sup_vert_id, sup_vert_id); point[i].m_radius = 0.0f; ++i;
		}

		// Sample the terrain at the polytope vertices
		PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupStart("TerrainContacts");)
		terr.m_terrain->CollideSpheres(point, num_points, MeshVsTerrainResult, &ctx);
		PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupEnd();)

		if( local_manifold.IsOverlap() )
		{
			// THIS WILL NEED LOOKING AT NOW THAT ALL CONTACTS ARE COLLECTED, NOT JUST THE DEEPEST
			// I'm just using the first contact in the mean time
			Contact const& contact = local_manifold[0];
			float deepest = contact.m_depth;

			// Try to refine the current deepest by using the negative
			// terrain normal and trying to find a deeper support vertex.
			std::size_t prev_sup_vert_id  = (std::size_t)(-1);
			v4 axis = w2a * -contact.m_normal;
			for( int j = 0; j != 5; ++j ) // Fix the maximum number of iterations
			{
				// Find the support vertex in the direction of the axis
				point[0].m_point = a2w * SupportVertex(poly, axis, sup_vert_id, sup_vert_id);
				point[0].m_radius = 0.0f;

				// If the same support vertex is returned then give up
				if( sup_vert_id == prev_sup_vert_id ) break;
				
				// Sample the terrain below this point
				PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupStart("TerrainContacts");)
				terr.m_terrain->CollideSpheres(point, 1, MeshVsTerrainResult, &ctx);
				PR_EXPAND(PR_DBG_TERR_COLLISION, ldr::GroupEnd();)

				// If this point is deeper than the current deepest save it
				// If not, then give up
				float depth = (*ctx.m_manifold)[0].m_depth;
				if( depth <= deepest ) break;
				
				axis = w2a * -(*ctx.m_manifold)[0].m_normal;
				prev_sup_vert_id = sup_vert_id;
			}
			manifold.Add(local_manifold[0]);
		}
	}
}