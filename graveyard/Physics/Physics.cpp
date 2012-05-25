//*********************************************
//
//	Physics engine
//
//*********************************************

#include "PR/Physics/Physics.h"
#include "PR/Common/LineDrawerHelper.h"

using namespace pr;
using namespace pr::ph;

//*****
// Default call back functions
void DefaultGeneratePotentiallyCollidingObjectsCB(Instance*)	{}
bool DefaultGetCollisionDataCB(CollisionData&)					{ return false; }
void DefaultGetTerrainDataCB(Terrain& terrain_data)				{ terrain_data.m_collision = false; }
bool DefaultBBoxTerrainCollisionCB(GetTerrainDataCB, const Instance&, const float);	// Implemented in PHTerrainCollision.cpp

//*****
// Constructor
PhysicsEngine::PhysicsEngine()
:m_settings()
,m_time(0.0f)
,m_last_step_time(0.0f)
,m_inv_time_step(0.0f)
,m_instance(0)
,m_collision()
,m_collision_group()
{}

//*****
// Destructor
PhysicsEngine::~PhysicsEngine()
{
	UnInitialise();
}
	
//*****
// Initialise the physics engine
void PhysicsEngine::Initialise(const PhysicsEngineSettings& settings)
{
	m_settings = settings;
	m_time = 0.0f;
	m_last_step_time = 0.0f;

	// Validate required data
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_time_step				> 0.0f);
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_collision_container_size	> 0);
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_max_collision_groups		> 0);
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_material					!= 0);
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_max_physics_materials	> 0);
	PR_ASSERT(PR_DBG_PHYSICS, m_settings.m_max_resting_speed		> 0.0f);
	
	// Handle optional data
	if( !m_settings.m_GenerateCollisionPairs )	m_settings.m_GenerateCollisionPairs = DefaultGeneratePotentiallyCollidingObjectsCB;
	if( !m_settings.m_GetCollisionPair )		m_settings.m_GetCollisionPair		= DefaultGetCollisionDataCB;
	if( !m_settings.m_GetTerrainData )			m_settings.m_GetTerrainData			= DefaultGetTerrainDataCB;
	if( !m_settings.m_BBoxTerrainCollision )	m_settings.m_BBoxTerrainCollision	= DefaultBBoxTerrainCollisionCB;

	m_inv_time_step			= 1.0f / m_settings.m_time_step;
	m_instance				= 0;
	m_collision				.reserve(m_settings.m_collision_container_size);
	m_collision_group		.resize(m_settings.m_max_collision_groups);
	for( uint g = 0; g < m_settings.m_max_collision_groups; ++g )
		m_collision_group[g] .resize(m_settings.m_max_collision_groups);
}

//*****
// UnInitialise the physics engine
void PhysicsEngine::UnInitialise()
{
	m_collision_group.clear();
	m_collision.clear();
}	

//*****
// Add a physics object to the engine
void PhysicsEngine::Add(Instance* instance)
{
	if( m_instance ) m_instance->m_prev = instance;
	instance->m_next = m_instance;
	instance->m_prev = 0;
	m_instance = instance;
}

//*****
// Remove a physics object from the engine
void PhysicsEngine::Remove(Instance* instance)
{
	if( m_instance == instance ) m_instance = instance->m_next;
	if( instance->m_prev ) instance->m_prev->m_next = instance->m_next;
	if( instance->m_next ) instance->m_next->m_prev = instance->m_prev;
}

//*****
// Remove all objects from the physics engine
void PhysicsEngine::RemoveAll()
{
	m_instance = 0;
}

//*****
// Step all of the physics objects we know about
// Algorithm:
//	Have an array of collision_data objects. collision_data objects contain a next pointer
//	Get obj-obj collisions
//	Get terrain-obj collisions
//	Resolve collisions by applying impulses.
//		Collisions = relative velocity > MIN_VEL, contact = !collision
//		Link together the resting contacts
//	Resolve contacts by zeroing components of force/torque
//	Forward dynamics.
void PhysicsEngine::Step(float elapsed_seconds)
{
	m_time += elapsed_seconds;
	uint steps = (uint)((m_time - m_last_step_time) / m_settings.m_time_step);
	m_last_step_time += steps * m_settings.m_time_step;

	for( uint s = 0; s < steps; ++s )
	{
		// Reset the array of contact/collision points
		m_collision.clear();

		// Test for collisions with the terrain
		if( m_settings.m_use_terrain )
		{
			for( Instance* instance = m_instance; instance; instance = m_instance->m_next )
			{
				CollisionData collision(instance, 0);			
				TerrainCollisionDetection(collision);
				if( collision.CalculateExtraContactData() )
				{
					m_collision.push_back(collision);
				}
			}
		}

		// Ask the client code to build a list of potentially colliding objects
		m_settings.m_GenerateCollisionPairs(m_instance);

		// Detect actual object-object collisions 
		CollisionData collision;
		while( m_settings.m_GetCollisionPair(collision) )
		{
			CollisionDetection(collision);
			if( collision.CalculateExtraContactData() )
			{
				m_collision.push_back(collision);
			}
		}

		// Resolve collisions and collect resting contacts into a linked list
		for( TCollisionContainer::iterator c = m_collision.begin(), c_end = m_collision.end(); c != c_end; ++c )
		{
			ResolveCollision(*c);
		}

		// Evolve the instances forward in time
		for( Instance* instance = m_instance; instance; instance = m_instance->m_next )
		{
			instance->Step(m_settings.m_time_step);
		}
	}
}

//*****
// Detect collision between an object and the terrain system. On return,
// 'data' contains the deepest penetration.
void PhysicsEngine::TerrainCollisionDetection(CollisionData& data)
{
	data.Reset();

	// Check the collision group first
	if( CollisionGroup(data.m_objA->CollisionGroup(), m_settings.m_terrain_collision_group) == NoCollision )
		return;

	// Test the world space bbox against the terrain
	if( !m_settings.m_BBoxTerrainCollision(m_settings.m_GetTerrainData, *data.m_objA, m_settings.m_time_step) )
		return;

	// Do a more thorough terrain collision test; Test each
	// primitive against the terrain and find the contact points.
	for( uint pa = 0; pa < data.m_objA->NumPrimitives(); ++pa )
	{
		const Primitive& primA = data.m_objA->Primitive(pa);
		switch( primA.m_type )
		{
		case Primitive::Box:		BoxTerrainCollision		(primA, data);	break;
		case Primitive::Cylinder:	CylinderTerrainCollision(primA, data);	break;
		case Primitive::Sphere:		SphereTerrainCollision	(primA, data);	break;
		default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
		}
	}
}

//*****
// Detect collisions between physics objects. On return,
// 'data' contains the deepest penetration.
void PhysicsEngine::CollisionDetection(CollisionData& data)
{
	data.Reset();

	// Check the collision group first
	if( CollisionGroup(data.m_objA->CollisionGroup(), data.m_objB->CollisionGroup()) == NoCollision )
		return;

	// Test each primitive against every other primitive
	for( uint pa = 0; pa < data.m_objA->NumPrimitives(); ++pa )
	{
		const Primitive& primA = data.m_objA->Primitive(pa);
		for( uint pb = 0; pb < data.m_objB->NumPrimitives(); ++pb )
		{
			const Primitive& primB = data.m_objB->Primitive(pb);
			PrimitiveCollision(primA, primB, data);
		}
	}
}

//*****
// Resolve a collision between m_objA and m_objB in 'data'. If 'm_objB' is 0 then it is
// assumed to be an infinite mass object. The collision is resolved by _setting_ the impulses
// in objA and objB so that they will move out of collision.
void PhysicsEngine::ResolveCollision(CollisionData& data)
{
	PR_ASSERT_STR(PR_DBG_PHYSICS, data.CalculateExtraContactData(), "You must call 'data.CalculateExtraContactData()' before resolving a collision");

	// "mass" is a matrix defined as impulse = mass * drelative_velocity.
	// "inv_mass" is also called the 'K' matrix and is equal to:
	//	[(1/MassA + 1/MassB)*Identity - (pointA.CrossProductMatrix()*InvMassTensorWS()A*pointA.CrossProductMatrix() + pointB.CrossProductMatrix()*InvMassTensorWS()B*pointB.CrossProductMatrix())]
	// Say "inv_mass" = "inv_mass1" + "inv_mass2" then
	// "inv_mass1" = [(1/MassA)*Identity - (pointA.CrossProductMatrix()*InvMassTensorWS()A*pointA.CrossProductMatrix())] and
	// "inv_mass2" = [(1/MassB)*Identity - (pointB.CrossProductMatrix()*InvMassTensorWS()B*pointB.CrossProductMatrix())]
	m4x4 pointAcross = data.m_contact.m_pointA.CrossProductMatrix();
	m4x4 inv_mass1 = (1.0f / data.m_objA->Mass()) * m4x4Identity - (pointAcross * data.m_objA->InvMassTensorWS() * pointAcross);
	
	m4x4 inv_mass2 = m4x4Zero;
	if( data.m_objB )
	{
		m4x4 pointBcross = data.m_contact.m_pointB.CrossProductMatrix();
		inv_mass2 = (1.0f / data.m_objB->Mass()) * m4x4Identity - (pointBcross * data.m_objB->InvMassTensorWS() * pointBcross);
	}

	m4x4 inv_mass = inv_mass1 + inv_mass2; inv_mass[3][3] = 1.0f;
	m4x4 mass = inv_mass.GetInverse();

	const ph::Material& materialA = PhysicsMaterial(data.m_contact.m_material_indexA);
	const ph::Material& materialB = PhysicsMaterial(data.m_contact.m_material_indexB);

	float elasticity_n		= Minimum<float>(materialA.m_elasticity,			materialB.m_elasticity);
	float elasticity_t		= Minimum<float>(materialA.m_tangential_elasticity,	materialB.m_tangential_elasticity);
	float static_friction	= Maximum<float>(materialA.m_static_friction,		materialB.m_static_friction);
	float dynamic_friction	= Maximum<float>(materialA.m_dynamic_friction,		materialB.m_dynamic_friction);
	bool norm_resting_contact = data.m_contact.m_rel_norm_speed < m_settings.m_max_resting_speed;
	bool tang_resting_contact = data.m_contact.m_rel_tang_speed < m_settings.m_max_resting_speed;

	float rel_velocity_n = Dot3(data.m_contact.m_normal,  data.m_contact.m_relative_velocity);
	float rel_velocity_t = Dot3(data.m_contact.m_tangent, data.m_contact.m_relative_velocity);
	if( norm_resting_contact )
	{
		elasticity_n = 1.0f - (elasticity_n - 1.0f) * rel_velocity_n / m_settings.m_max_resting_speed;
	}
	if( tang_resting_contact )
	{
		elasticity_t = -1.0f + (elasticity_t + 1.0f) * rel_velocity_t / m_settings.m_max_resting_speed;
	}
	
	// Pi is the impulse required to reduce the normal component of rel_velocity to zero.
	// Pii is the impulse to reduce rel_velocity to zero
	// See article: A New Algebraic Rigid Body Collision Law Based On Impulse Space Considerations
	v4 Pi = -(rel_velocity_n / Dot3(data.m_contact.m_normal, inv_mass * data.m_contact.m_normal)) * data.m_contact.m_normal;
	v4 Pii = -(mass * data.m_contact.m_relative_velocity);
	v4 Pdiff = Pii - Pi;

	v4 impulse = (1.0f + elasticity_n) * Pi + (1.0f + elasticity_t) * Pdiff;

	// Clip this impulse to the friction cone
	float impulse_n = Dot3(data.m_contact.m_normal,  impulse);
	float impulse_t = Dot3(data.m_contact.m_tangent, impulse);
	if( Abs(impulse_t) > static_friction * impulse_n )
	{
		float kappa = dynamic_friction * (1.0f + elasticity_n) * Dot3(data.m_contact.m_normal, Pi) /
			(Abs(Dot3(data.m_contact.m_tangent, Pii)) - dynamic_friction * Dot3(data.m_contact.m_normal, Pdiff));
		
		impulse = (1.0f + elasticity_n) * Pi + kappa * Pdiff;
	}

	// Apply the collision impulses
	data.m_objA->ApplyWorldCollisionImpulseAt(-m_inv_time_step * impulse, data.m_contact.m_pointA);
	if( data.m_objB ) data.m_objB->ApplyWorldCollisionImpulseAt(m_inv_time_step * impulse, data.m_contact.m_pointB);

	// Push the objects out of penetration
	if( norm_resting_contact )
	{
		float dist = Minimum<float>(data.m_contact.m_depth, m_settings.m_max_push_out_distance);

		float fracA = 1.0f;
		float fracB = 0.0f;
		if( data.m_objB )
		{
			float total_mass = data.m_objA->Mass() + data.m_objB->Mass();
			fracA = data.m_objB->Mass() / total_mass;
			fracB = data.m_objA->Mass() / total_mass;
		}

		v4 distA = -dist * fracA * data.m_contact.m_normal;
		data.m_objA->PushOut(distA);

		if( data.m_objB )
		{
			v4 distB = dist * fracB * data.m_contact.m_normal;
			data.m_objB->PushOut(distB);
		}


		// Reduce the angular momentum in the direction of the normal
//PSR...		float ang_mom_dot_normal = data.m_contact.m_normal.Dot3(data.m_objA->m_ang_momentum);
//PSR...		if( ang_mom_dot_normal > 0.0f )
//PSR...		{
//PSR...			data.m_objA->m_ang_momentum -= 0.1f * ang_mom_dot_normal * data.m_contact.m_normal;
//PSR...		}
//PSR...		else
//PSR...		{
//PSR...			data.m_objA->m_ang_momentum += 0.1f * ang_mom_dot_normal * data.m_contact.m_normal;
//PSR...		}
	}

	// Test for sleeping condition
	if( norm_resting_contact && tang_resting_contact )
	{
		// Add support and test for sleeping
	}
}


//*****
// Resolve a resting contact. This is done by zeroing the components of force/torque
// in the ...
void PhysicsEngine::ResolveRestingContact(CollisionData& data)
{
	data;
//REMEMBER CONTACT FORCES...
// If a contact is not moving toward the contact normal but is accelerating toward the contact normal
// then this is a contact force.
}

//*****
// Detect collisions between two primitives
void PhysicsEngine::PrimitiveCollision(const Primitive& primA, const Primitive& primB, CollisionData& data)
{
	const m4x4& objA_to_world = data.m_objA->ObjectToWorld();
	const m4x4& objB_to_world = data.m_objB->ObjectToWorld();
	ldr::StartFile("C:\\Physics.txt");
	ldr::PhPrimitive("PrimA", "FFFF0000", primA, objA_to_world);
	ldr::PhPrimitive("PrimB", "FF0000FF", primB, objB_to_world);
//PSR...	m4x4 a2w = primA.m_primitive_to_object * objA_to_world;
//PSR...	m4x4 b2w = primB.m_primitive_to_object * objB_to_world;
//PSR...	v4 apos = a2w[3];
//PSR...	v4 bpos = b2w[3];
//PSR...	v4 aaxis = a2w[2];
//PSR...	v4 baxis = b2w[2];
//PSR...	v4 a_cross_b = aaxis.Cross(baxis);
//PSR...	ldr::Sphere("PrimA_centre", "FFFF0000", apos, 0.01f);
//PSR...	ldr::Sphere("PrimB_centre", "FF0000FF", bpos, 0.01f);
//PSR...	ldr::Line("PrimA_axis", "FFFF0000", apos + a2w[2] * -2.0f * primA.m_radius[2], apos + a2w[2] * 2.0f * primA.m_radius[2]);
//PSR...	ldr::Line("PrimB_axis", "FF0000FF", bpos + b2w[2] * -2.0f * primB.m_radius[2], bpos + b2w[2] * 2.0f * primB.m_radius[2]);
//PSR...	ldr::Vector("Cross", "FF00FF00", v4Zero, 4.0f * a_cross_b, 0.01f);
//PSR...	ldr::Line("Separation", "FF00FF00", apos, bpos);
	ldr::EndFile();
//return;

	switch( primA.m_type )
	{
	case Primitive::Box:
		switch( primB.m_type )
		{
		case Primitive::Box:		BoxToBoxCollision			(primA, primB, data, false);	break;
		case Primitive::Cylinder:	BoxToCylinderCollision		(primA, primB, data, false);	break;
		case Primitive::Sphere:		BoxToSphereCollision		(primA, primB, data, false);	break;
		default:					PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
		}break;
	case Primitive::Cylinder:
		switch( primB.m_type )
		{
		case Primitive::Box:		BoxToCylinderCollision		(primB, primA, data, true);		break;
		case Primitive::Cylinder:	CylinderToCylinderCollision	(primA, primB, data, false);	break;
		case Primitive::Sphere:		CylinderToSphereCollision	(primA, primB, data, false);	break;
		default:					PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
		}break;
	case Primitive::Sphere:
		switch( primB.m_type )
		{
		case Primitive::Box:		BoxToSphereCollision		(primB, primA, data, true);		break;
		case Primitive::Cylinder:	CylinderToSphereCollision	(primB, primA, data, true);		break;
		case Primitive::Sphere:		SphereToSphereCollision		(primA, primB, data, false);	break;
		default:					PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
		}break;
	default:
		PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
	}

	if( data.m_contact.IsContact() )
	{
		ldr::AppendFile("C:\\Physics.txt");
		v4 wa = objA_to_world * data.m_contact.m_pointA;
		v4 wb = objB_to_world * data.m_contact.m_pointB;
		ldr::Sphere("PointA", "FFFF0000", wa, 0.01f);
		ldr::Sphere("PointB", "FF0000FF", wb, 0.01f);
		ldr::Line("Normal_FromA", "FFFF0000", wa, wa + data.m_contact.m_normal);
		ldr::Line("Normal_FromB", "FF0000FF", wb, wb - data.m_contact.m_normal);
		ldr::EndFile();
	}
}

//******************************************************************************
// Global functions
//*****
// Transform an inertia tensor using the parallel axis theorem.
// 'offset' is the distance from (or toward) the centre of mass (determined by 'to_centre_of_mass')
// 'inertia' and 'offset' must be in the same frame.
void pr::ph::ParallelAxisTranslateInertia(m4x4& inertia, const v4& offset, float mass, bool to_centre_of_mass)
{
	if( to_centre_of_mass ) mass = -mass;

	for( uint i = 0; i < 3; ++i )
	{
		for( uint j = i; j < 3; ++j )
		{
			// For the diagonal elements I = Io + md^2 (away from CoM), Io = I - md^2 (toward CoM)
			// 'd' is the perpendicular component of 'offset'
			if( i == j )
			{
				uint i1 = (i + 1) % 3;
				uint i2 = (i + 2) % 3;
				inertia[i][i] += mass * (offset[i1]*offset[i1] + offset[i2]*offset[i2]);
			}

			// For off diagonal elements:
			//	Ixy = Ioxy + mdxdy	(away from CoM), Io = I - mdxdy (toward CoM)
			//	Ixz = Ioxz + mdxdz	(away from CoM), Io = I - mdxdz (toward CoM)
			//	Iyz = Ioyz + mdydz	(away from CoM), Io = I - mdydz (toward CoM)
			else
			{
				float delta = mass * (offset[i] * offset[j]);
				inertia[i][j] += delta;
				inertia[j][i] += delta;
			}
		}
	}
}
