//****************************************************
//
//	Terrain collision detection methods
//
//****************************************************

#include "physics/utility/stdafx.h"
// Ideas:
//	Maybe have a max edge length and subdivide edges with more sample points
//
//#include "pr/Physics/Terrain/Terrain.h"
//#include "pr/Physics/Model/Primitive.h"
//#include "pr/Physics/Collision/Contact.h"

//using namespace pr;
//using namespace pr::ph;
//
//void pr::ph::TerrainCollisionBox(const Primitive& prim, const m4x4& obj2w, Contact& contact)
//{
//	prim;
//	obj2w;
//	contact;
//}
//void pr::ph::TerrainCollisionCylinder(const Primitive& prim, const m4x4& obj2w, Contact& contact)
//{
//	prim;
//	obj2w;
//	contact;
//}
//void pr::ph::TerrainCollisionSphere(const Primitive& prim, const m4x4& obj2w, Contact& contact)
//{
//	prim;
//	obj2w;
//	contact;
//}

////*****
//// Test an object-orientated bounding box against the terrain. This
//// function is used as a default for the quick-out test for terrain
//// collisions. Returns true if any corner is below ground level.
//bool DefaultBBoxTerrainCollisionCB(GetTerrainDataCB GetTerrainData, const Instance& object, const float time_step)
//{
//	const m4x4& bbox_to_world	= object.ObjectToWorld();
//	const BBox& bbox		= object.BBox();
//
//	Terrain terrain;
//	terrain.m_lookup_type		= Terrain::eQuickOut | Terrain::eCheck;
//	for( uint c = BBox::FirstCorner; c < BBox::NumberOfCorners; ++c )
//	{
//		terrain.m_position		= bbox_to_world * bbox.GetCorner(c, 1.0f);
//		terrain.m_direction		= object.VelocityAt(terrain.m_position) * time_step;
//		GetTerrainData(terrain);
//		if( terrain.m_collision ) return true;
//	}
//	return false;
//}
//
////*****
//// Test a box primitive against the terrain and record the first contact
//void PhysicsEngine::TerrainCollisionBox(const Primitive& primA, CollisionData& data)
//{
//	// Find the axis vectors of the box in world space
//	m4x4 primA_to_world = data.m_objA->ObjectToWorld() * primA.m_primitive_to_object;
//	v4 X = primA_to_world[0] * primA.m_radius[0];
//	v4 Y = primA_to_world[1] * primA.m_radius[1];
//	v4 Z = primA_to_world[2] * primA.m_radius[2];
//
//	v4 top = primA_to_world[3] + Z;
//	v4 bot = primA_to_world[3] - Z;
//	v4 top_left = top - X;
//	v4 top_rite = top + X;
//	v4 bot_left = bot - X;
//	v4 bot_rite = bot + X;
//
//	v4 vertex[8];
//	vertex[0] = top_left - Y;
//	vertex[1] = top_rite - Y;
//	vertex[2] = top_left + Y;
//	vertex[3] = top_rite + Y;
//	vertex[4] = bot_left - Y;
//	vertex[5] = bot_rite - Y;
//	vertex[6] = bot_left + Y;
//	vertex[7] = bot_rite + Y;
//
//	// Test the test points of the box against the terrain
//	Terrain terrain[2];
//	Terrain* current	= &terrain[0];
//	Terrain* deepest	= &terrain[1];
//	current->m_lookup_type	= Terrain::eFullQuickOut;
//	deepest->m_lookup_type	= Terrain::eFullQuickOut;
//	deepest->SetNoCollision();
//	for( uint v = 0; v < 8; ++v )
//	{
//		current->m_position		= vertex[v];
//		current->m_direction	= data.m_objA->VelocityAt(vertex[v]) * m_settings.m_time_step;
//		m_settings.m_GetTerrainData(*current);
//
//		// If this is the first (deepest) contact with the terrain then record it
//		if( current->m_collision && current->IsDeeperThan(*deepest) )
//		{
//			Terrain* swap = deepest;
//			deepest = current;
//			current = swap;
//		}
//	}
//
//	// If there was a contact and the contact is deeper than the current deepest, record it.
//	if( deepest->m_collision && !data.m_contact.IsDeeperThan(deepest->m_fraction, deepest->m_depth) )
//	{
//		data.m_contact.m_normal				= -deepest->m_normal;	// Remember its from 'A's point of view.
//		data.m_contact.m_pointA				=  deepest->m_position - data.m_objA->ObjectToWorld()[3];
//		data.m_contact.m_pointA.w			= 1.0f;
//		data.m_contact.m_depth				= deepest->m_depth;
//		data.m_contact.m_fraction			= deepest->m_fraction;
//		data.m_contact.m_material_indexA	= primA.m_material_index;
//		data.m_contact.m_material_indexB	= deepest->m_material_index;
//	}
//}
//
//
////*****
//// Test a cylinder primitive against the terrain and record the first contact
//void PhysicsEngine::CylinderTerrainCollision(const Primitive& primA, CollisionData& data)
//{
//	// Test a box around the cylinder to find a contact
//	const m4x4	primA_to_world	= data.m_objA->ObjectToWorld() * primA.m_primitive_to_object;
//
//	v4 top, bottom, left, up;
//	top		= primA_to_world[3] + primA.m_radius[2] * primA_to_world[2];
//	bottom	= primA_to_world[3] - primA.m_radius[2] * primA_to_world[2];
//	left	= primA.m_radius[0] * primA_to_world[0];
//	up		= primA.m_radius[0] * primA_to_world[1];
//
//	v4 vertex[8];
//	vertex[0] = top + left + up;
//	vertex[1] = top - left + up;
//	vertex[2] = top + left - up;
//	vertex[3] = top - left - up;
//	vertex[4] = bottom + left + up;
//	vertex[5] = bottom - left + up;
//	vertex[6] = bottom + left - up;
//	vertex[7] = bottom - left - up;
//
//	// Test for a collision
//	Terrain terrain[2];
//	Terrain* current	= &terrain[0];
//	Terrain* deepest	= &terrain[1];
//	current->m_lookup_type	= Terrain::eQuickOut | Terrain::eCheck | Terrain::eNormal | Terrain::eDepth;
//	deepest->m_lookup_type	= Terrain::eQuickOut | Terrain::eCheck | Terrain::eNormal | Terrain::eDepth;
//	deepest->SetNoCollision();
//	uint deepest_v = 0;
//	for( uint v = 0; v < 8; ++v )
//	{
//		current->m_position		= vertex[v];
//		current->m_direction	= data.m_objA->VelocityAt(vertex[v]);
//		m_settings.m_GetTerrainData(*current);
//
//		// If this is the first (deepest) contact with the terrain then record it
//		if( current->m_collision && current->IsDeeperThan(*deepest) )
//		{
//			Terrain* swap = deepest;
//			deepest = current;
//			current = swap;
//			deepest_v = v;
//		}
//	}
//
//	// Use the normal from the deepest collision to get a more accurate contact on the cylinder
//	if( deepest->m_collision )
//	{
//		deepest->SetNoCollision();
//		deepest->m_lookup_type	= Terrain::eFullQuickOut;
//		if( deepest_v < 4 ) deepest->m_position		= top	 + Cross3(primA_to_world[2], Cross3(primA_to_world[2], deepest->m_normal));
//		else				deepest->m_position		= bottom + Cross3(primA_to_world[2], Cross3(primA_to_world[2], deepest->m_normal));
//		deepest->m_direction	= data.m_objA->VelocityAt(deepest->m_position);
//		m_settings.m_GetTerrainData(*deepest);
//
//		// If there was a contact and the contact is deeper than the current deepest, record it.
//		if( deepest->m_collision && !data.m_contact.IsDeeperThan(deepest->m_fraction, deepest->m_depth) )
//		{
//			data.m_contact.m_normal				= -deepest->m_normal;	// Remember its from 'A's point of view.
//			data.m_contact.m_pointA				=  deepest->m_position - data.m_objA->ObjectToWorld()[3];
//			data.m_contact.m_pointA.w			= 1.0f;
//			data.m_contact.m_depth				= deepest->m_depth;
//			data.m_contact.m_fraction			= deepest->m_fraction;
//			data.m_contact.m_material_indexA	= primA.m_material_index;
//			data.m_contact.m_material_indexB	= deepest->m_material_index;
//		}
//	}
//}
//
////*****
//// Test a sphere primitive against the terrain and record the first contact
//void PhysicsEngine::SphereTerrainCollision(const Primitive& primA, CollisionData& data)
//{
//	primA;
//	data;
////PSR...	v4 centre = data.m_objA->ObjectToWorld() * primA.m_primitive_to_object[3];
////PSR...
////PSR...	// Sample in the direction of travel using 6 points.
////PSR...	v4 vertex[6];
////PSR...	vertex[0] = centre;	vertex[0][0] += primA.m_radius[0];
////PSR...	vertex[1] = centre;	vertex[1][0] -= primA.m_radius[0];
////PSR...	vertex[2] = centre;	vertex[2][1] += primA.m_radius[0];
////PSR...	vertex[3] = centre;	vertex[3][1] -= primA.m_radius[0];
////PSR...	vertex[4] = centre;	vertex[4][2] += primA.m_radius[0];
////PSR...	vertex[5] = centre;	vertex[5][2] -= primA.m_radius[0];
////PSR...
////PSR...	const uint	collision_flags	=	Terrain::eQuickOut	|
////PSR...									Terrain::eCheck		|
////PSR...									Terrain::eNormal	|
////PSR...									Terrain::eDepth		|
////PSR...									Terrain::eFraction	|
////PSR...									Terrain::eMaterial;
////PSR...
////PSR...	Terrain terrain[2];
////PSR...	Terrain* current	= &terrain[0];
////PSR...	Terrain* deepest	= &terrain[1];
////PSR...	current->m_lookup_type	= Terrain::eDepth | Terrain::eNormal;
////PSR...	deepest->m_lookup_type	= Terrain::eDepth | Terrain::eNormal;
////PSR...	deepest->SetNoCollision();
////PSR...	for( uint v = 0; v < 6; ++v )
////PSR...	{
////PSR...		current->m_position = vertex[v];
////PSR...		m_settings.m_GetTerrainData(*current);
////PSR...
////PSR...		// If this is the first (deepest) contact with the terrain then record it
////PSR...		if( current->m_collision && current->IsDeeperThan(*deepest) )
////PSR...		{
////PSR...			Terrain* swap = deepest;
////PSR...			deepest = current;
////PSR...			current = swap;
////PSR...		}
////PSR...	}
////PSR...
////PSR...	// Use the -ve of normal of the 'deepest' point of contact
////PSR...	// to find the point of contact on the sphere
////PSR...	deepest->SetNoCollision();
////PSR...	deepest->m_position		= centre - deepest->m_normal * primA.m_radius[0];
////PSR...	deepest->m_lookup_type	= collision_flags;
////PSR...	m_settings.m_GetTerrainData(*deepest);
////PSR...
////PSR...	// If there was a contact and the contact is deeper than the current deepest, record it.
////PSR...	if( deepest->m_collision && !data.m_contact.IsDeeperThan(deepest->m_fraction, deepest->m_depth) )
////PSR...	{
////PSR...		data.m_contact.m_normal				= -deepest->m_normal;	// Remember its from 'A's point of view.
////PSR...		data.m_contact.m_pointA				=  deepest->m_position - data.m_objA->ObjectToWorld()[3];
////PSR...		data.m_contact.m_pointA				. SetW1();
////PSR...		data.m_contact.m_depth				= deepest->m_depth;
////PSR...		data.m_contact.m_fraction			= deepest->m_fraction;
////PSR...		data.m_contact.m_material_indexA	= primA.m_material_index;
////PSR...		data.m_contact.m_material_indexB	= deepest->m_material_index;
////PSR...	}
//}
//
//
////PSR...
////PSR...
////PSR...	// Only use test points that are facing the given direction of travel
////PSR...	v4 direction = data.m_objA->Velocity();
////PSR...	PR_ASSERT(!direction.IsZero3());
////PSR...	//if( direction.IsZero3() ) direction = data.m_objA->m_force;	??
////PSR...
////PSR...	// Sort the axes from longest to shortest
////PSR...	uint X = 0, Y = 1, Z = 2;
////PSR...	if( Abs(direction[X]) < Abs(direction[Y]) ) { uint swap = X; X = Y; Y = swap; }
////PSR...	if( Abs(direction[Y]) < Abs(direction[Z]) ) { uint swap = Y; Y = Z; Z = swap; }
////PSR...	if( Abs(direction[X]) < Abs(direction[Z]) ) { uint swap = X; X = Z; Z = swap; }
////PSR...
////PSR...	v4 radius[3];
////PSR...	radius[0] = primA_to_world[0] * primA.m_radius[0];
////PSR...	radius[1] = primA_to_world[1] * primA.m_radius[1];
////PSR...	radius[2] = primA_to_world[2] * primA.m_radius[2];
////PSR...	float D_dot_R[3];
////PSR...	D_dot_R[0] = direction.Dot3(radius[0]);
////PSR...	D_dot_R[1] = direction.Dot3(radius[1]);
////PSR...	D_dot_R[2] = direction.Dot3(radius[2]);
////PSR...
////PSR...	const uint MAX_TEST_POINTS = 10;
////PSR...	v4 vertex_buffer[MAX_TEST_POINTS];
////PSR...	v4* vertex = vertex_buffer;
////PSR...
////PSR...	// Add 5 points in the direction of the longest axis
////PSR...	if( !FEql(D_dot_R[X], 0.0f) )
////PSR...	{
////PSR...		float sign0 = (D_dot_R[X] < 0.0f) ? (-1.0f) : (1.0f);
////PSR...		v4 pos_radius0 = primA_to_world[3] + sign0 * radius[X];
////PSR...		vertex->Set(pos_radius0);													++vertex;
////PSR...		vertex->Set(pos_radius0 + radius[Y] + radius[Z]);							++vertex;
////PSR...		vertex->Set(pos_radius0 - radius[Y] + radius[Z]);							++vertex;
////PSR...		vertex->Set(pos_radius0 + radius[Y] - radius[Z]);							++vertex;
////PSR...		vertex->Set(pos_radius0 - radius[Y] - radius[Z]);							++vertex;
////PSR...
////PSR...		// Add 3 points in the direction of the next longest axis
////PSR...		if( !FEql(D_dot_R[Y], 0.0f) )
////PSR...		{
////PSR...			float sign1 = (D_dot_R[Y] < 0.0f) ? (-1.0f) : (1.0f);
////PSR...			v4 pos_radius1 = primA_to_world[3] + sign1 * radius[Y];
////PSR...			vertex->Set(pos_radius1);												++vertex;
////PSR...			vertex->Set(pos_radius1 - sign0 * radius[X] + radius[Z]);				++vertex;
////PSR...			vertex->Set(pos_radius1 - sign0 * radius[X] - radius[Z]);				++vertex;
////PSR...
////PSR...			// Add 2 more points in the direction of the last axis
////PSR...			if( !FEql(D_dot_R[Z], 0.0f) )
////PSR...			{
////PSR...				float sign2 = (D_dot_R[Z] < 0.0f) ? (-1.0f) : (1.0f);
////PSR...				v4 pos_radius2 = primA_to_world[3] + sign2 * radius[Z];
////PSR...				vertex->Set(pos_radius2);											++vertex;
////PSR...				vertex->Set(pos_radius2 - sign0 * radius[X] - sign1 * radius[Y]);	++vertex;
////PSR...			}
////PSR...		}
////PSR...	}
////PSR...	PR_ASSERT(vertex - vertex_buffer <= MAX_TEST_POINTS);
////PSR...
