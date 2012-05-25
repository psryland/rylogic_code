//****************************************************
//
//	Structures that describe a physics object
//
//****************************************************
// Ph::Instance:
//	This object represents an instance of a physics object.
//	Instances are moved using impulses. Each impulse results
//	in a change of momentum and angular momentum
#ifndef PR_PHYSICS_OBJECT_H
#define PR_PHYSICS_OBJECT_H

#include "PR/Maths/Maths.h"
#include "PR/Physics/PhysicsAssertEnable.h"

namespace pr
{
	namespace ph
	{
		// Primitives that the collision model is made out of
		// Sphere:		m_radius[0] - radius of the sphere, other radii ignored
		// Cylinder:	m_radius[0] - radius of the cylinder, m_radius[2] - half height of the cylinder
		// Box:			m_radius[0], m_radius[1], m_radius[2] - half lengths of the box edges
		struct Primitive
		{
			enum Type { Box, Cylinder, Sphere, NumberOf };
			BoundingBox BBox() const;			// Returns a bounding box orientated to the primitive
			float	Volume() const;				// Returns the volume of the primitive
			v4		MomentOfInertia() const;	// Returns the moments of inertia about the primary axes for the primitive (x by mass for the mass moment of inertia)

			Type	m_type;					// The type of primitive this is. One of Primitive::Type
			float	m_radius[3];			// The dimensions of the primitive in primitive space
			m4x4	m_primitive_to_object;	// Transform from primitive space to physics object space
			uint	m_material_index;		// The physics material that this primitive is made out of
		};

		// Contains all of the properties of the physics object 
		struct Object
		{
			BoundingBox	m_bbox;					// The object-oriented bounding box for this object
			float		m_mass;					// The mass of the object
			m4x4		m_mass_tensor;			// The mass tensor
			m4x4		m_inv_mass_tensor;		// The inverse of the mass tensor
			
			// Collision model
			Primitive*	m_primitive;			// Pointer to an array of primitives forming the collision model
			uint		m_num_primitives;		// The number of primitives in the array
		};

		// Contains all of the properties of the physics instance
		struct Instance
		{
			Instance();

			// Accessors
			const BoundingBox&	BBox() const						{ return m_physics_object->m_bbox; }
			const BoundingBox&	WorldBBox() const					{ return m_world_bbox; }
			float				Mass() const						{ return m_physics_object->m_mass; }
			const m4x4&			InvMassTensorWS() const				{ return m_ws_inv_mass_tensor; }
			uint				NumPrimitives() const				{ return m_physics_object->m_num_primitives; }
			const Primitive&	Primitive(uint i) const				{ PR_ASSERT(PR_DBG_PHYSICS, i < NumPrimitives()); return m_physics_object->m_primitive[i]; }
			const m4x4&			ObjectToWorld() const				{ return *m_object_to_world; }
			uint				CollisionGroup() const				{ return m_collision_group; }
			const v4&			Velocity() const					{ return m_velocity; }
			const v4&			AngVelocity() const					{ return m_ang_velocity; }
			v4					VelocityAt(const v4& where) const	{ return m_velocity + Cross3(m_ang_velocity, where); }
			float				GetEnergy() const;

			// Set methods		
			void	SetGravity(const v4& gravity)					{ m_gravity = Mass() * gravity; }
			void	SetAngVelocity(const v4& ang_vel);

			// Impulses in the world frame.
			// Note: impulse = force * dt where dt = 'elapsed_seconds' when step is called.
			// F = (f1*dt + f2*dt + f3*dt + ... + fn*dt) == (f1 + f2 + f3 + ... + fn)*dt
			void	ApplyWorldImpulse	(const v4& force);
			void	ApplyWorldMoment	(const v4& torque);
			void	ApplyWorldImpulseAt	(const v4& force, const v4& where);

			// Collision impulse. These zero the component of 'm_force' and 'm_torque'
			// that opposes the direction of 'force' and 'where.Cross(force)'
			void	ApplyWorldCollisionImpulseAt	(const v4& force, const v4& where);

			// Evolve this instance forward in time
			void	Reset();
			void	Step(float elapsed_seconds);

			void	PushOut(const v4& push_distance);

		private:
			void	StepOrder1(float elapsed_seconds);
			void	StepOrder2(float elapsed_seconds);
			void	StepOrder4(float elapsed_seconds);

		public:

			Object*		m_physics_object;			// The physics object
			uint		m_collision_group;			// The collision group that this instance belongs to
			m4x4*		m_object_to_world;			// The transform from physics object space into world space
			v4			m_velocity;					// The velocity of the object in world space
			v4			m_ang_momentum;				// The angular momentum of the object in world space
			v4			m_ang_velocity;				// The angular velocity of the object in world space
			v4			m_gravity;					// The gravitational force for this object
			m4x4		m_ws_inv_mass_tensor;		// The world space inverse mass tensor
			v4			m_force;					// The accumulation of impulse forces in world space
			v4			m_torque;					// The accumulation of moments (impulse torques) in world space
			BoundingBox	m_world_bbox;				// The world space bounding box for this object. Calculated per step
			Instance*	m_next;						// Used to create a linked list of physics objects within the physics engine
			Instance*	m_prev;						// Used to create a linked list of physics objects within the physics engine
		};

		//***********************************************************
		// Implementation
		//*****
		// Apply an impulse to the centre of mass
		inline void Instance::ApplyWorldImpulse(const v4& force)
		{
			m_force += force;
		}

		//*****
		// Apply a moment to the centre of mass
		inline void Instance::ApplyWorldMoment(const v4& torque)
		{
			m_torque += torque;
		}

		//*****
		// Apply an impulse at a location relative to the centre of mass.
		// 'impulse' and 'where' are in world space although 'where' is relative
		// to the object centre of mass.
		inline void	Instance::ApplyWorldImpulseAt(const v4& force, const v4& where)
		{
			m_force	 += force;
			m_torque += Cross3(where, force);
		}

		//*****
		// Apply a collision impulse. These zero the current components of force
		// and torque in the direction of the force and torque we're about to apply
		inline void Instance::ApplyWorldCollisionImpulseAt(const v4& force, const v4& where)
		{
			float inward_force = Dot3(force, m_force);
			if( inward_force < 0.0f ) { m_force -= (inward_force / force.Length3Sq()) * force; }
			m_force += force;

			v4 torque = Cross3(where, force);
			float inward_torque = Dot3(torque, m_torque);
			if( inward_torque < 0.0f ) { m_torque -= (inward_torque / torque.Length3Sq()) * torque; }
			m_torque += torque;
		}
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_OBJECT_H

