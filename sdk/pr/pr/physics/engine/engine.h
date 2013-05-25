//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

// This engine consists of the following parts
//	BPEntity - any object that can exist in the broadphase
//	Shape - The basic type used for narrow phase collision detection
//	Rigidbody - The combination of a shape and mass properties
//
// Broad phase collision detector
//	This is a standalone module whose job is to produce CollisionPairs
// Terrain collision detector
//	This is a small module that interacts with a standalone terrain module
//	It produces CollisionPairs between Instances and the terrain
// Narrow phase collision detector
//	This module receives CollisionPairs and generates ContactManifolds
// Constraint Solver
//	This module receives ContactManifolds and Joints and generates
//	constraint forces (lamda forces)
// Integrater
//	This module sums the internal and external forces for an Instance
//	and steps it forward in time
//
// Instances are responsible for collecting forces within a frame.
//	Impulses can be applied to an Instance to immediately change
//	its velocity.
//
// Physics Engine
//	The engine is a collection of global functions for performing the
//	processes described above. There is a helper object called the PhysicsEngine
//	that contains references to Instances, a reference to a broad phase, a reference
//	to a terrain system, plus typical world variables such as gravity.
//	The PhysicsEngine also provides step methods.
//
// Collision Filters
//	Collision groups are not needed, the client builds the list of collision pairs
//  they can ellimination the things that don't collide

#pragma once
#ifndef PR_PHYSICS_ENGINE_H
#define PR_PHYSICS_ENGINE_H

#include "pr/physics/types/forward.h"
#include "pr/physics/engine/settings.h"
//#include "pr/physics/engine/agentcache.h"
#include "pr/physics/shape/shapeterrain.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "pr/physics/solver/constraintaccumulator.h"
#include "pr/physics/collision/collisioncache.h"
#include "pr/physics/utility/events.h"

namespace pr
{
	namespace ph
	{
		class Engine : public pr::events::IRecv<RBEvent>
		{
			ph::Settings			m_settings;				// Settings for the engine
			Rigidbody::Link         m_rigid_bodies;         // Registered rigid bodies. Uses Rigidbody::m_engine_ref;
			ConstraintAccumulator	m_constraints;			// Collects constraints and sorts them into connected sets
			Rigidbody				m_terrain_object;		// A rigid body to represent the terrain during collisions
			ShapeTerrain			m_terrain_shape;		// A collision shape representing a terrain system
			CollisionCache			m_collision_cache;
			bool					m_stepping;				// True while the engine is stepping, no modifications can be made while this is true
			std::size_t				m_frame_number;			// Rolling frame number counter
			float					m_time;					// The current time

			Engine(Engine const&);							// no copying
			Engine& operator=(Engine const&);				// no copying
			void			ConstructCommon();				// Common constructor called from main constructors
			void			CollisionDetection(Rigidbody const& objA, Rigidbody const& objB);
			static void		ObjectVsObjectConstraints(BPPair const& pair, void* context);
			void			ObjectVsTerrainConstraints();
			void			JointConstraints();
			static void		RayCastCollisionDetection(BPPair const& pair, void* context);
			static void		NarrowPhaseCollisionDetection(BPPair const& pair, void* context);
			void			TerrainCollisionDetection();

		public:
			Engine(const ph::Settings& settings);
			Engine(
				IBroadphase*			broadphase				= 0,
				ITerrain*				terrain					= 0,
				IPreCollisionObserver*	pre_col					= 0,
				IPstCollisionObserver*	pst_col					= 0,
				std::size_t				constraint_buffer_size	= 65536,
				std::size_t				collision_cache_size	= pr::meta::prime_gtreq<1000>::value,
				AllocFunction			allocate				= pr::DefaultAlloc,
				DeallocFunction			deallocate				= pr::DefaultDealloc
				);

			// Instances
			void Register     (ph::Rigidbody& rigid_body);
			void Unregister   (ph::Rigidbody& rigid_body);
			Rigidbody::Link const& GetRegisteredObjects() const;

			// Main engine step
			void Step         (float elapsed_seconds);
			
			// Collision observers
			bool NotifyPreCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold& manifold);
			void NotifyPstCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold const& manifold);
		
			// Cast a ray into the physics world. Returns true if the ray hits something
			bool RayCast(Ray const& ray, RayVsWorldResult& result);

			// Events
			void OnEvent(RBEvent const& e);
		};
	}
}

#endif
