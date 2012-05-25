//****************************************************
//
//	Collision detection methods
//
//****************************************************

#include <malloc.h>		// for _alloca()
#include "PR/Physics/Physics.h"
#include "PR/Physics/Engine/PHCollision.h"
#include "PR/Physics/Engine/PHBoxCollider.h"
#include "PR/Physics/Engine/PHCollider.h"

using namespace pr;
using namespace pr::ph;

//************************************************************************
// CollisionData methods
//*****
// Determine the relative velocity and contact tangent
bool CollisionData::CalculateExtraContactData()
{
	// No penetration? no collision
	if( !m_contact.IsContact() ) return false;

	// Calculate the relative velocity
	if( m_objB ) m_contact.m_relative_velocity =  m_objB->VelocityAt(m_contact.m_pointB) - m_objA->VelocityAt(m_contact.m_pointA);
	else		 m_contact.m_relative_velocity = -m_objA->VelocityAt(m_contact.m_pointA);
	
	// If the relative velocity is not into the collision, no collision
	float rel_norm_velocity = Dot3(m_contact.m_normal, m_contact.m_relative_velocity);
	if( rel_norm_velocity > 0.0f ) return false;

	m_contact.m_rel_norm_speed = -rel_norm_velocity;

	// Calculate the tangent vector
	m_contact.m_tangent = m_contact.m_relative_velocity - rel_norm_velocity * m_contact.m_normal;
	float rel_tang_velocity = m_contact.m_tangent.Length3();
	if( !FEql(rel_tang_velocity, 0.0f) )	m_contact.m_tangent /= rel_tang_velocity;
	else									m_contact.m_tangent .Zero();

	m_contact.m_rel_tang_speed = rel_tang_velocity;

	// This is a collision
	return true;
}


//************************************************************************
// PhysicsEngine collision methods
//*****
// Cuboid vs. cuboid collision detection
void PhysicsEngine::BoxToBoxCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

	// Convert the primitives into world space
	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
	
	// Info needed by the box collider
	BoxCollider::Box boxA;
	BoxCollider::Box boxB;
	boxA.m_centre = primA_to_world[3];
	boxB.m_centre = primB_to_world[3];

	// Do a bounding sphere test
	v4 a2b			= boxB.m_centre - boxA.m_centre;
	v4 radii_sum	= { primA.m_radius[0] + primB.m_radius[0], primA.m_radius[1] + primB.m_radius[1], primA.m_radius[2] + primB.m_radius[2], 0.0f };
	if( a2b.Length3Sq() > radii_sum.Length3Sq() ) return;	// No Collision

	for( int i = 0; i < 3; ++i )
	{
		boxA.m_normal[i] = primA_to_world[i];
		boxA.m_radius[i] = primA.m_radius[i] * boxA.m_normal[i];
		
		boxB.m_normal[i] = primB_to_world[i];
		boxB.m_radius[i] = primB.m_radius[i] * boxB.m_normal[i];
	}

	// Test the boxes against each other
	Contact contact;
	BoxCollider(boxA, boxB, contact);
	if( !contact.IsContact() ) return; // No Collision

	if( !reverse )
	{
		contact.m_pointA = primA.m_primitive_to_object * contact.m_pointA;
		contact.m_pointB = primB.m_primitive_to_object * contact.m_pointB;
	}
	else
	{
		contact.m_normal = -contact.m_normal;
		contact.m_pointA = primB.m_primitive_to_object * contact.m_pointB;
		contact.m_pointB = primA.m_primitive_to_object * contact.m_pointA;
	}

	if( contact.IsDeeperThan(data.m_contact) )
		data.m_contact = contact;
}

//*****
// Cuboid vs. Cylinder collision detection
void PhysicsEngine::BoxToCylinderCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	primA;
	primB;
	data;
	reverse;

	//const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	//const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

	PR_STUB_FUNC();
}

//*****
// Cuboid vs. Sphere collision detection
void PhysicsEngine::BoxToSphereCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

	// Convert into primA space
	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
	m4x4 primB_to_primA = primB_to_world * primA_to_world.GetInverse();

	// Get the vector to primB
	const v4& a2b = primB_to_primA[3];

	// Get a vector from the sphere to the nearest point on the box
	v4 separation = v4Zero;
	v4 closest	  = a2b;
	for( int i = 0; i < 3; ++i )
	{
		if( a2b[i] > primA.m_radius[i] )
		{
			separation[i] = a2b[i] - primA.m_radius[i];
			closest[i]	  = primA.m_radius[i];
		}
		else if( a2b[i] < -primA.m_radius[i] )
		{
			separation[i] = a2b[i] + primA.m_radius[i];
			closest[i]	  = -primA.m_radius[i];
		}
	}
	
	// If the separation is greater than the radius of the sphere then no collision
	float fseparationSq = separation.Length3Sq();
	if( fseparationSq > primB.m_radius[0] * primB.m_radius[0] ) return;

	// Find the closest points
	Contact contact;
	v4& pointA = (!reverse) ? (contact.m_pointA) : (contact.m_pointB);
	v4& pointB = (!reverse) ? (contact.m_pointB) : (contact.m_pointA);

	// If the separation is zero then the centre of the sphere is inside the cuboid
	if( FEql(fseparationSq, 0.0f) )
	{
		// Find the closest point on primA
		int largest = 0;
		if( Abs(a2b[1]) > Abs(a2b[largest]) ) largest = 1;
		if( Abs(a2b[2]) > Abs(a2b[largest]) ) largest = 2;
		pointA = a2b;
		pointA[largest] = (a2b[largest] > 0.0f) ? (primA.m_radius[largest]) : (-primA.m_radius[largest]);
		pointA = primA.m_primitive_to_object * pointA;

		// Find the closest point on primB
		pointB.Set(0.0f, 0.0f, 0.0f, 1.0f);
		pointB[largest] -= primB.m_radius[0];
		pointB.Set(	Dot3(pointB, primB_to_primA[0]),
					Dot3(pointB, primB_to_primA[1]),
					Dot3(pointB, primB_to_primA[2]),
					1.0f);
		pointB = primB.m_primitive_to_object * pointB;

		// Get the normal in world space
		contact.m_normal = primA_to_world * (pointA - a2b); contact.m_normal.Normalise3();
		if( reverse ) contact.m_normal = -contact.m_normal;
		contact.m_depth = primB.m_radius[0] + primA.m_radius[largest] - a2b[largest];
	}
	else
	{	
		// Find the closest point on primA
		pointA = primA.m_primitive_to_object * closest;
		
		// Find the closest point on primB
		float fseparation = Sqrt(fseparationSq);
		pointB = separation * -primB.m_radius[0] / fseparation;
		pointB.Set(	Dot3(pointB, primB_to_primA[0]),
					Dot3(pointB, primB_to_primA[1]),
					Dot3(pointB, primB_to_primA[2]),
					1.0f);
		pointB = primB.m_primitive_to_object * pointB;

		// Get the normal in world space
		contact.m_normal = primA_to_world * separation; contact.m_normal.Normalise3();
		if( reverse ) contact.m_normal = -contact.m_normal;
		contact.m_depth = primB.m_radius[0] - fseparation;
	}

	if( contact.IsDeeperThan(data.m_contact) )
		data.m_contact = contact;
}

//*****
// Cylinder vs. Cylinder collision detection
void PhysicsEngine::CylinderToCylinderCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	primA;
	primB;
	data;
	reverse;
	//const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	//const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

//PSR...	// Convert into primA space
//PSR...	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
//PSR...	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
//PSR...	m4x4 primB_to_primA = primB_to_world * primA_to_world.GetInverse();
//PSR...
//PSR...	// Get the vector to primB
//PSR...	const v4& a2b = primB_to_primA[3];
//PSR...	float a2b_lengthSq = a2b.Length3Sq();
//PSR...	if( FEql(a2b_lengthSq, 0.0f) )
//PSR...	{
//PSR...		PR_ERROR_STR("Cylinder / cylinder exactly on top of each other");
//PSR...		return;
//PSR...	}
//PSR...
//PSR...	// Find the closest points
//PSR...	PR_ASSERT_STR(data.m_num_contacts < data.m_max_contacts, "Not enough room for results. Shouldn't be here if results are full");
//PSR...	v4& pointA = (!reverse) ? (data.m_contact[data.m_num_contacts].m_pointA) : (data.m_contact[data.m_num_contacts].m_pointB);
//PSR...	v4& pointB = (!reverse) ? (data.m_contact[data.m_num_contacts].m_pointB) : (data.m_contact[data.m_num_contacts].m_pointA);
//PSR...	v4  normal;
//PSR...	float& penetration = data.m_contact[data.m_num_contacts].m_penetration;
//PSR...
//PSR...	v4 a_cross_b = primA_to_world[2].Cross(primB_to_world[2]);
//PSR...
//PSR...	// The axes of the cylinders are aligned, possible collision types are face vs. face or wall vs. wall
//PSR...	if( a_cross_b.IsZero3() )
//PSR...	{
//PSR...		// Test the vertical radius
//PSR...		if( Abs(a2b[2]) > primA.m_radius[2] + primB.m_radius[2] ) return; // No Collision
//PSR...
//PSR...		// Test the horizontal radius
//PSR...		float separation_RSq	= a2b[0] * a2b[0] + a2b[1] * a2b[1];
//PSR...		float radii_sum			= primA.m_radius[0] + primB.m_radius[0];
//PSR...		if( separation_RSq > radii_sum * radii_sum ) return; // No Collision
//PSR...		float separation_R = Sqrt(separation_RSq);
//PSR...
//PSR...		float horizontal_penetration = primA.m_radius[0] + primB.m_radius[0] - separation_R;
//PSR...		float vertical_penetration = primA.m_radius[2] + primB.m_radius[2] - Abs(a2b[2]);
//PSR...
//PSR...		// Wall vs wall collision
//PSR...		if( horizontal_penetration < vertical_penetration )
//PSR...		{
//PSR...			float sign = (a2b[2] > 0.0f) ? (1.0f) : (-1.0f);
//PSR...
//PSR...			normal.Set(a2b[0], a2b[1], 0.0f, 0.0f);
//PSR...			normal *= 1.0f / separation_R;
//PSR...
//PSR...			pointA.Set(0.0f, 0.0f, sign * (primA.m_radius[2] - vertical_penetration / 2.0f), 1.0f);
//PSR...			pointA += normal * primA.m_radius[0];
//PSR...			pointA.w = 1.0f;
//PSR...
//PSR...			pointB = pointA - a2b;
//PSR...			pointB -= normal * horizontal_penetration;
//PSR...			pointB.w = 1.0f;
//PSR...
//PSR...			penetration = horizontal_penetration;
//PSR...		}
//PSR...
//PSR...		// Face vs face collision
//PSR...		else
//PSR...		{
//PSR...			float sign = (a2b[2] > 0.0f) ? (1.0f) : (-1.0f);
//PSR...			normal.Set(0.0f, 0.0f, sign, 0.0f);
//PSR...
//PSR...			pointA.Set(a2b[0] / 2.0f, a2b[1] / 2.0f, sign * primA.m_radius[2], 1.0f);
//PSR...			pointA.w = 1.0f;
//PSR...
//PSR...			pointB = pointA - a2b;
//PSR...			pointB[2] -= sign * vertical_penetration;
//PSR...			pointB.w = 1.0f;
//PSR...
//PSR...			penetration = vertical_penetration;
//PSR...		}
//PSR...	}
//PSR...
//PSR...	// One cylinder is at an angle to the other
//PSR...	else
//PSR...	{
//PSR...		// Do a bounding sphere test
//PSR...		v4 a2b			(primB_to_world[3] - primA_to_world[3]);
//PSR...		v4 radii_sum	(primA.m_radius[0] + primB.m_radius[0], 0.0f, primA.m_radius[2] + primB.m_radius[2], 0.0f);
//PSR...		if( a2b.Length3Sq() > radii_sum.Length3Sq() ) return;	// No Collision
//PSR...
//PSR...		// Find the closest point on each axis
//PSR...	}
//PSR...
//PSR...	pointA = primA.m_primitive_to_object * pointA;
//PSR...
//PSR...	pointB.Set(	pointB.Dot3(primB_to_primA[0]),
//PSR...				pointB.Dot3(primB_to_primA[1]),
//PSR...				pointB.Dot3(primB_to_primA[2]),
//PSR...				1.0f);
//PSR...	pointB = primB.m_primitive_to_object * pointB;
//PSR...
//PSR...	// Get the normal in world space
//PSR...	normal = primA_to_world * normal;
//PSR...	if( !reverse )	data.m_contact[data.m_num_contacts].m_normal = normal;
//PSR...	else			data.m_contact[data.m_num_contacts].m_normal = -normal;
//PSR...	++data.m_num_contacts;
//PSR...

//PSR...	// Convert the primitives into world space
//PSR...	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
//PSR...	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
//PSR...
//PSR...	// Do a bounding sphere test
//PSR...	v4 a2b			(primB_to_world[3] - primA_to_world[3]);
//PSR...	v4 radii_sum	(primA.m_radius[0] + primB.m_radius[0], 0.0f, primA.m_radius[2] + primB.m_radius[2], 0.0f);
//PSR...	if( a2b.Length3Sq() > radii_sum.Length3Sq() ) return;	// No Collision
//PSR...
//PSR...	// Info needed by the collider
//PSR...	Collider::Params params;
//PSR...	params.m_separating_axis = (v4*)_alloca(sizeof(v4) * 4);
//PSR...	params.m_separating_axis[0] = primA_to_world[2];
//PSR...	params.m_separating_axis[1] = primB_to_world[2];
//PSR...	v4 norm = (a2b - a2b.Dot3(primA_to_world[2]) * primA_to_world[2]).Normalise3();
//PSR...	if( !norm.IsZero3() )
//PSR...	{
//PSR...		params.m_separating_axis[2] = norm;
//PSR...        params.m_separating_axis[3] = (-a2b + a2b.Dot3(primB_to_world[2]) * primB_to_world[2]).Normalise3();
//PSR...		PR_ASSERT(!params.m_separating_axis[3].IsZero3());
//PSR...		params.m_num_separating_axes = 4;
//PSR...	}
//PSR...	else
//PSR...	{
//PSR...		params.m_num_separating_axes = 2;
//PSR...	}
//PSR...	// ObjectA
//PSR...	params.m_objectA.m_centre		= primA_to_world[3];
//PSR...	params.m_objectA.m_radius[0]	= primA.m_radius[2] * params.m_separating_axis[0];
//PSR...	params.m_objectA.m_radius[1]	= primA.m_radius[0] * params.m_separating_axis[2];
//PSR...	params.m_objectA.m_num_radii	= 2;
//PSR...	// ObjectB
//PSR...	params.m_objectB.m_centre		= primB_to_world[3];
//PSR...	params.m_objectB.m_radius[0]	= primB.m_radius[2] * params.m_separating_axis[1];
//PSR...	params.m_objectB.m_radius[1]	= primB.m_radius[0] * params.m_separating_axis[3];
//PSR...	params.m_objectB.m_num_radii	= 2;
//PSR...
//PSR...	// Test the cylinders against each other
//PSR...	Collider collider(params);
//PSR...	if( params.m_min_overlap.m_penetration < 0.0f ) return; // No Collision
//PSR...
//PSR...	Contact contact;
//PSR...	contact.m_pointA = params.m_min_overlap.m_A.m_point;
//PSR...	contact.m_pointB = params.m_min_overlap.m_B.m_point;
//PSR...	contact.m_normal = params.m_min_overlap.m_axis;
//PSR...	if( !reverse )
//PSR...	{
//PSR...		data.m_contact[data.m_num_contacts].m_normal = contact.m_normal;
//PSR...		data.m_contact[data.m_num_contacts].m_pointA = primA.m_primitive_to_object * contact.m_pointA;
//PSR...		data.m_contact[data.m_num_contacts].m_pointB = primB.m_primitive_to_object * contact.m_pointB;
//PSR...		data.m_contact[data.m_num_contacts].m_penetration = contact.m_penetration;
//PSR...	}
//PSR...	else
//PSR...	{
//PSR...		data.m_contact[data.m_num_contacts].m_normal = -contact.m_normal;
//PSR...		data.m_contact[data.m_num_contacts].m_pointA = primB.m_primitive_to_object * contact.m_pointB;
//PSR...		data.m_contact[data.m_num_contacts].m_pointB = primA.m_primitive_to_object * contact.m_pointA;
//PSR...		data.m_contact[data.m_num_contacts].m_penetration = contact.m_penetration;
//PSR...	}
//PSR...
//PSR...	// If the contact point is deeper than the deepest, record it
//PSR...	if( !data.m_deepest || data.m_contact[data.m_num_contacts].m_penetration > data.m_deepest->m_penetration )
//PSR...		data.m_deepest = &data.m_contact[data.m_num_contacts];
//PSR...
//PSR...	++data.m_num_contacts;
}

//*****
// Cylinder vs. Sphere detection
void PhysicsEngine::CylinderToSphereCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

	// Convert into primA space
	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
	m4x4 primB_to_primA = primB_to_world * primA_to_world.GetInverse();

	// Get the vector to primB
	const v4& a2b = primB_to_primA[3];
	float a2b_lengthSq = a2b.Length3Sq();
	if( FEql(a2b_lengthSq, 0.0f) )
	{
		PR_ERROR_STR(PR_DBG_PHYSICS, "Cylinder / sphere exactly on top of each other");
		return;
	}

	// Test the vertical radius
	if( Abs(a2b[2]) > primA.m_radius[2] + primB.m_radius[0] ) return; // No Collision

	// Test the horizontal radius
	float separation_RSq	= a2b[0] * a2b[0] + a2b[1] * a2b[1];
	float radii_sum			= primA.m_radius[0] + primB.m_radius[0];
	if( separation_RSq > radii_sum * radii_sum ) return; // No Collision

	// Calculate whether the sphere is closer to the walls or the end of the cylinder
	float cylinder_RSq		= primA.m_radius[0] * primA.m_radius[0];
	float cylinder_HSq		= primA.m_radius[2] * primA.m_radius[2];
	float separation_HSq	= a2b[2] * a2b[2];
	bool  closest_to_end	= cylinder_HSq * separation_RSq < cylinder_RSq * separation_HSq; 

	// Find the closest points
	Contact contact;
	v4& pointA = (!reverse) ? (contact.m_pointA) : (contact.m_pointB);
	v4& pointB = (!reverse) ? (contact.m_pointB) : (contact.m_pointA);
	v4& normal = contact.m_normal;
	float& penetration = contact.m_depth;
	
	// Three cases: Note, in the first two cases the centre of the sphere may be inside the cylinder
	// The centre of the sphere is within the range of the cylinder height
	if( Abs(a2b[2]) < primA.m_radius[2] && !closest_to_end )
	{
		float separation_R = Sqrt(separation_RSq);

		normal.Set(a2b[0], a2b[1], 0.0f, 0.0f);
		normal *= 1.0f / separation_R;

		pointA.Set(0.0f, 0.0f, a2b[2], 1.0f);
		pointA += normal * primA.m_radius[0];
		pointA.w = 1.0f;

		pointB = normal * -primB.m_radius[0];
		pointB.w = 1.0f;

		penetration = primA.m_radius[0] + primB.m_radius[0] - separation_R;
	}

	// The centre of the sphere is within the range of the cylinder radius
	else if( separation_RSq < cylinder_RSq && closest_to_end )
	{
		float sign = (a2b[2] > 0.0f) ? (1.0f) : (-1.0f);
		normal.Set(0.0f, 0.0f, sign * 1.0f, 0.0f);

		pointA = a2b;
		pointA[2] = sign * primA.m_radius[2];

		pointB.Set(0.0f, 0.0f, -sign * primB.m_radius[0], 1.0f);

		penetration = primA.m_radius[2] + primB.m_radius[0] - Abs(a2b[2]);
	}

	// The sphere is interacting with the edge of the cylinder. 
	else
	{
		// The centre should not be inside the cylinder here
		PR_ASSERT(PR_DBG_PHYSICS, cylinder_RSq < separation_RSq && cylinder_HSq < separation_HSq);
		float sign			= (a2b[2] > 0.0f) ? (1.0f) : (-1.0f);
		float separation_R	= Sqrt(separation_RSq);
		
		pointA .Set(a2b[0], a2b[1], 0.0f, 0.0f);
		pointA *= primA.m_radius[0] / separation_R;
		pointA[2] = sign * primA.m_radius[2];
		pointA.w = 1.0f;

		normal = a2b - pointA; normal.w = 0.0f;
		float length = normal.Length3();
		normal *= 1.0f / length;
		penetration = primB.m_radius[0] - length;

		pointB = normal * -primB.m_radius[0];
		pointB.w = 1.0f;
	}

	pointA = primA.m_primitive_to_object * pointA;

	pointB.Set(	Dot3(pointB, primB_to_primA[0]),
				Dot3(pointB, primB_to_primA[1]),
				Dot3(pointB, primB_to_primA[2]),
				1.0f);
	pointB = primB.m_primitive_to_object * pointB;

	// Get the normal in world space
	normal = primA_to_world * normal;
	if( reverse ) normal = -normal;
	
	if( contact.IsDeeperThan(data.m_contact) )
		data.m_contact = contact;
}

//*****
// Sphere vs. Sphere collision detection
void PhysicsEngine::SphereToSphereCollision(const Primitive& primA, const Primitive& primB, CollisionData& data, bool reverse)
{
	const m4x4& objA_to_world = (!reverse) ? (data.m_objA->ObjectToWorld()) : (data.m_objB->ObjectToWorld());
	const m4x4& objB_to_world = (!reverse) ? (data.m_objB->ObjectToWorld()) : (data.m_objA->ObjectToWorld());

	// Convert into primA space
	m4x4 primA_to_world = primA.m_primitive_to_object * objA_to_world;
	m4x4 primB_to_world = primB.m_primitive_to_object * objB_to_world;
	m4x4 primB_to_primA = primB_to_world * primA_to_world.GetInverse();

	// Get the vector to primB
	const v4& a2b = primB_to_primA[3];
	float a2b_lengthSq = a2b.Length3Sq();
	if( FEql(a2b_lengthSq, 0.0f) )
	{
		PR_ERROR_STR(PR_DBG_PHYSICS, "Two spheres exactly on top of each other");
		return;
	}

	// Test for collision
	float radii_sum = primA.m_radius[0] + primB.m_radius[0];
	if( a2b_lengthSq > radii_sum * radii_sum ) return; // No Collision
	
	float a2b_length = Sqrt(a2b_lengthSq);
	v4 norm_a2b = a2b / a2b_length;
	norm_a2b.w = 0.0f;

	// Find the closest points on the two spheres
	Contact contact;
	v4& pointA = (!reverse) ? (contact.m_pointA) : (contact.m_pointB);
	v4& pointB = (!reverse) ? (contact.m_pointB) : (contact.m_pointA);

	pointA = norm_a2b * primA.m_radius[0]; pointA.w = 1.0f;
	pointA = primA.m_primitive_to_object * pointA;

	pointB = norm_a2b * -primB.m_radius[0];
	pointB.Set(	Dot3(pointB, primB_to_primA[0]),
				Dot3(pointB, primB_to_primA[1]),
				Dot3(pointB, primB_to_primA[2]),
				1.0f);
	pointB = primB.m_primitive_to_object * pointB;

	// Get the normal in world space
	contact.m_normal = primA_to_world * norm_a2b;
	if( reverse ) contact.m_normal = -contact.m_normal;
	contact.m_depth = primA.m_radius[0] + primB.m_radius[0] - a2b_length;

	if( contact.IsDeeperThan(data.m_contact) )
		data.m_contact = contact;
}
