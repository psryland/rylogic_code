//*********************************************
//
//	Physics engine
//
//*********************************************
//
// Usage:
//	The physics engine is designed to be used in two ways; firstly as a
//	manager for physics objects and secondly as a machine for detecting
//	and resolving collisions between ph::Instances. Not all physics objects
//	need to be managed by the physics engine.
//
//	ph::Instances are responsible for collecting impulses within a frame.
//
//	Expected usage in the first case:
//		1) Create physics objects/instances and add them to the physics
//			engine and an external broadphase system (quad tree, oct
//			tree, or dynamic object map).
//		2) Apply any external forces/impulses.
//		3) Call PhysicsEngine::Step:
//			a. calls GeneratePotentiallyCollidingObjects so that the
//				client code can use any method of Broadphase it wants.
//			b. calls GetCollisionData until it returns false. For each
//				collision pair CollisionDetection is called followed by
//				ResolveCollision.
//			c. detects and resolves collisions between instances and the
//				terrain by calling GetTerrainDataCB
//			d. evolves all dynamic objects forward in time
//	Note: all or some of the call back functions can be null. This results
//			in no collision detection.
//
//	The physics engine can also be used with objects not managed by the
//	engine.
//
// Collision Groups:
//	The collision group for terrain is zero. 

#ifndef PR_PHYSICS_H
#define PR_PHYSICS_H

#include "PR/Common/StdVector.h"
#include "PR/Maths/Maths.h"
#include "PR/Physics/PhysicsAssertEnable.h"
#include "PR/Physics/Engine/PHForward.h"
#include "PR/Physics/Engine/PHTypes.h"
#include "PR/Physics/Engine/PHMaterial.h"
#include "PR/Physics/Engine/PHObject.h"
#include "PR/Physics/Engine/PHCollision.h"
#include "PR/Physics/Engine/PHTerrain.h"

#ifndef NDEBUG
#pragma comment(lib, "PhysicsD.lib")
#else//NDEBUG
#pragma comment(lib, "Physics.lib")
#endif//NDEBUG

namespace pr
{
	namespace ph
	{
		// Use this function to build up a collection of ph::CollisionData for the overlapping
		// objects. These data will be asked for one at a time by 'GetCollisionData'
		// The instance list passed contains the instances that the physics engine knows about.
		// There is nothing stopping other physics objects being added to the potential collision list.
		typedef void (*GeneratePotentiallyCollidingObjectsCB)(ph::Instance* instance_list);
		
		// The physics engine calls this function to get the potentially overlapping pairs.
		// 'GeneratePotentiallyCollidingObjects' will always be called first.
		// Return 'true' if 'collision_pair' contains a potential collision pair
		typedef bool (*GetCollisionDataCB)(ph::CollisionData& collision_pair);

		// The physics engine calls this function to determine the terrain height at a point.
		// This function is called frequently so make it fast.
		typedef void (*GetTerrainDataCB)(ph::Terrain& terrain_data);

		// The physics engine calls this during terrain collision as a quick-out for object vs.
		// terrain collisions. The default for this function does not assume an up direction.
		// Writing a specialised version of this may help performance. This function should return
		// true if the bounding box will intersect the terrain in the next frame.
		typedef bool (*BBoxTerrainCollisionCB)(GetTerrainDataCB GetTerrainData, const ph::Instance& object, const float time_step);
	}//namespace physics

	// Settings for the physics engine
	struct PhysicsEngineSettings
	{
		PhysicsEngineSettings()
		{
			m_time_step					= 1.0f / 120.0f;	// 120 times per second
			m_collision_container_size	= 10000;
			m_max_collision_groups		= 0;
			m_material					= 0;
			m_max_physics_materials		= 0;
			m_terrain_collision_group	= 0;
			m_use_terrain				= false;
			m_max_resting_speed			= 0.01f;
			m_max_push_out_distance		= 0.001f;
			m_GenerateCollisionPairs	= 0;
			m_GetCollisionPair			= 0;
			m_GetTerrainData			= 0;
			m_BBoxTerrainCollision		= 0;
		};

		float				m_time_step;				// The rate that the engine is stepped.
		uint				m_collision_container_size;	// The space to reserve in the collision container
		uint				m_max_collision_groups;		// The number of different collision groups to use
		const ph::Material*	m_material;					// Pointer to an array of physics materials
		uint				m_max_physics_materials;	// The maximum size of the physics material array
		uint				m_terrain_collision_group;	// The collision group to use for the terrain
		bool				m_use_terrain;				// Set to true for collision resolution with terrain
		float				m_max_resting_speed;		// The magnitude of the maximum velocity that a resting object can have. m/s
		float				m_max_push_out_distance;	// When pushing objects out of penetration this is the maximum distance to move them per step

		// Call back functions
		ph::GeneratePotentiallyCollidingObjectsCB	m_GenerateCollisionPairs;
		ph::GetCollisionDataCB						m_GetCollisionPair;
		ph::GetTerrainDataCB						m_GetTerrainData;
		ph::BBoxTerrainCollisionCB					m_BBoxTerrainCollision;
	};

	//*****
	// A class used to give physical behaviour to physics objects
	class PhysicsEngine
	{
	public:
		PhysicsEngine();
		~PhysicsEngine();
		void	Initialise(const PhysicsEngineSettings& settings);
		void	UnInitialise();

		// Step
		void	Add(ph::Instance* instance);
		void	Remove(ph::Instance* instance);
		void	RemoveAll();
		void	Step(float elapsed_seconds);

		// Collision
		void	CollisionDetection			(ph::CollisionData& data);
		void	TerrainCollisionDetection	(ph::CollisionData& data);
		void	ResolveCollision			(ph::CollisionData& data);
		void	ResolveRestingContact		(ph::CollisionData& data);

		// Collision groups
		ph::CollisionResponce& CollisionGroup(uint group1, uint group2);

		// Physics Materials
		const ph::Material& PhysicsMaterial(uint material_index);

		// Bouyancy
		void	Float(ph::Instance& /*instance*/, const v4& /*fluid_plane*/) { PR_STUB_FUNC(); }

	private:
		void	PrimitiveCollision			(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data);

		// Implemented in PHCollision.cpp
		void	BoxToBoxCollision			(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		void	BoxToCylinderCollision		(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		void	BoxToSphereCollision		(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		void	CylinderToCylinderCollision	(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		void	CylinderToSphereCollision	(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		void	SphereToSphereCollision		(const ph::Primitive& primA, const ph::Primitive& primB, ph::CollisionData& data, bool reverse);
		
		// Implemented in PHTerrainCollision.cpp
		void	BoxTerrainCollision			(const ph::Primitive& primA, ph::CollisionData& data);
		void	CylinderTerrainCollision	(const ph::Primitive& primA, ph::CollisionData& data);
		void	SphereTerrainCollision		(const ph::Primitive& primA, ph::CollisionData& data);
		
	private:
		typedef std::vector<ph::CollisionData>						TCollisionContainer;
		typedef std::vector< std::vector<ph::CollisionResponce> >	TCollisionResponce;
		
		PhysicsEngineSettings	m_settings;
		float					m_time;					// What the physics engine thinks the time is
		float					m_last_step_time;		// The time value when the physics engine last "stepped"
		float					m_inv_time_step;		// == 1.0f / m_settings.m_time_step
		ph::Instance*			m_instance;				// A pointer to the first physics object
		TCollisionContainer		m_collision;			// An array of collisions
		TCollisionResponce		m_collision_group;		// A 2D array of collision groups. size = m_settings.m_max_collision_groups x m_settings.m_max_collision_groups
	};

	//********************************************************************

	//*****
	// Return the collision responce for two collision groups
	inline ph::CollisionResponce& PhysicsEngine::CollisionGroup(uint group1, uint group2)
	{
		PR_ASSERT(PR_DBG_PHYSICS, group1 < m_settings.m_max_collision_groups);
		PR_ASSERT(PR_DBG_PHYSICS, group2 < m_settings.m_max_collision_groups);
		if( group1 >= group2 )	return m_collision_group[group1][group2];
		else					return m_collision_group[group2][group1];
	}

	//*****
	// Return the material corresponding to 'material_index'
	inline const ph::Material& PhysicsEngine::PhysicsMaterial(uint material_index)
	{
		PR_ASSERT(PR_DBG_PHYSICS, material_index < m_settings.m_max_physics_materials);
		return m_settings.m_material[material_index];
	}

	//********************************************************************
	// Global helper functions
	namespace ph
	{
		//*****
		// Translate an inertia tensor to or from the centre of mass
		void ParallelAxisTranslateInertia(m4x4& inertia, const v4& offset, float mass, bool to_centre_of_mass);
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_H
