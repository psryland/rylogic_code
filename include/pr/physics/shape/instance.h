//****************************************************
//
//	Physics Instance
//
//****************************************************
// ph::Instance & ph::StaticInstance
//	These objects represent a dynamic and static instance of a physics object.
//
// Notes:
//	Don't chain the instances together using pointers, this is a problem for
//	serialised instances and may cause multi-threading problems if the instances
//	are owned by different threads.
#ifndef PR_PHYSICS_INSTANCE_H
#define PR_PHYSICS_INSTANCE_H

#include "PR/Maths/Maths.h"
#include "PR/Physics/PhysicsAssertEnable.h"
#include "PR/Physics/Forward.h"
#include "PR/Physics/Broadphase/BroadphaseEntity.h"
#include "PR/Physics/Model/RigidBody.h"

namespace pr
{
	namespace ph
	{
		enum EInstanceType
		{
			EInstanceType_Static,
			EInstanceType_Dynamic,
		};

		struct InstanceBase
		{
			uint				m_type;				// A type to identify what kind of instance this is
			broadphase::Entity	m_broadphase;		// The location of this instance in the broadphase
		};
		
		struct StaticInstance
		{
			InstanceBase		m_base;				// A common "Base Class" for physics instances
			Shape*				m_shape;			// The collision shape for the instance
			m4x4*				m_object_to_world;	// The transform from physics object space into world space
		};

		struct Instance
		{
			InstanceBase		m_base;				// A common "Base Class" for physics instances
			RigidBody*			m_rigid_body;		// Mass properties and collision shape for the instance
		//	uint				m_collision_group;	// The collision group that this instance belongs to

			// State variables
			m4x4*				m_object_to_world;	// The transform from physics object space into world space
			v4					m_lin_velocity;		// The velocity of the object in world space
			v4					m_ang_velocity;		// The angular velocity of the object in world space
			v4					m_force;			// The accumulation of world space forces within a step
			v4					m_torque;			// The accumulation of world space torques within a step

		//	// Derived variables
		//	v4			m_ang_momentum;				// The angular momentum of the object in world space
		//	m4x4		m_ws_inv_mass_tensor;		// The world space inverse mass tensor
		//	BoundingBox	m_ws_bbox;					// The world space bounding box for this object. Calculated per step
		};

		//struct Instance : pr::chain::link<Instance, ManagedInstance>
		//{
		//	Instance();
		//	Instance(RigidBody* rigid_body, m4x4* object_to_world);
		//	
		//	// Accessors
		//	const BoundingBox&	BBox() const						{ return m_rigid_body->m_bbox; }
		//	const BoundingBox&	BBoxWS() const						{ return m_ws_bbox; }
		//	float				Mass() const						{ return m_rigid_body->m_mass; }
		//	const m4x4&			InvMassTensorWS() const				{ return m_ws_inv_mass_tensor; }
		//	uint				CollisionGroup() const				{ return m_collision_group; }
		//	std::size_t			NumPrimitives() const				{ return m_rigid_body->m_num_primitives; }
		//	const Model&		GetModel() const					{ return *m_rigid_body; }
		//	const Primitive*	prim_begin() const					{ return m_rigid_body->prim_begin(); }
		//	const Primitive*	prim_end() const					{ return m_rigid_body->prim_end(); }
		//	const m4x4&			ObjectToWorld() const				{ return *m_object_to_world; }
		//	const v4&			Velocity() const					{ return m_velocity; }
		//	const v4&			AngVelocity() const					{ return m_ang_velocity; }
		//	v4					VelocityAt(const v4& where) const	{ return m_velocity + Cross3(m_ang_velocity, where); }
		//	float				GetEnergy(const v4& gravity) const;

		//	// Set methods		
		//	void SetCollisionGroup(uint group)						{ m_collision_group = group; }
		//	void SetRigidBody(RigidBody* rigid_body)				{ m_rigid_body = rigid_body; }
		//	void SetObjectToWorld(m4x4* object_to_world)			{ m_object_to_world = object_to_world; }
		//	void SetVelocity(const v4& velocity)					{ m_velocity = velocity; }
		//	void SetAngVelocity(const v4& ang_vel);

		//	// Impulses in world frame.
		//	// Note: impulse = force * dt where dt = 'elapsed_seconds' when step is called.
		//	// F = (f1*dt + f2*dt + f3*dt + ... + fn*dt) == (f1 + f2 + f3 + ... + fn)*dt
		//	void ApplyWorldImpulse	(const v4& force);
		//	void ApplyWorldMoment	(const v4& torque);
		//	void ApplyWorldImpulseAt(const v4& force, const v4& where);

		//	// Collision impulse. These zero the component of 'm_force' and 'm_torque'
		//	// that opposes the direction of 'force' and 'where.Cross(force)'
		//	void ApplyWorldCollisionImpulseAt(const v4& force, const v4& where);

		//	// Evolve this instance forward in time. This applies the accumulated force and torque
		//	// for 'elapsed_seconds' then resets these members ready for the next step
		//	void Step(float elapsed_seconds);

		//	void PushOut(const v4& push_distance);

		//private:
		//	void StepOrder1(float elapsed_seconds);
		//	void StepOrder2(float elapsed_seconds);
		//	void StepOrder4(float elapsed_seconds);

		//private:
		//	RigidBody*	m_rigid_body;				// The collision shape and mass properties
		//	uint		m_collision_group;			// The collision group that this instance belongs to
		//	broadphase::Entity m_bp_entity;			// Allow these objects to be added to the broadphase

		//	// State variables
		//	m4x4*		m_object_to_world;			// The transform from physics object space into world space
		//	v4			m_lin_velocity;				// The velocity of the object in world space
		//	v4			m_ang_velocity;				// The angular velocity of the object in world space
		//	v4			m_force;					// The accumulation of world space forces within a step
		//	v4			m_torque;					// The accumulation of world space torques within a step

		//	// Derived variables
		//	v4			m_ang_momentum;				// The angular momentum of the object in world space
		//	m4x4		m_ws_inv_mass_tensor;		// The world space inverse mass tensor
		//	BoundingBox	m_ws_bbox;					// The world space bounding box for this object. Calculated per step
		//};

		//// Implementation ***********************************************
		//
		////*****
		//// Apply an impulse at the centre of mass
		//inline void Instance::ApplyWorldImpulse(const v4& force)
		//{
		//	m_force += force;
		//}

		////*****
		//// Apply a moment to the centre of mass
		//inline void Instance::ApplyWorldMoment(const v4& torque)
		//{
		//	m_torque += torque;
		//}

		////*****
		//// Apply an impulse at a location relative to the centre of mass.
		//// 'impulse' and 'where' are in world space although 'where' is relative
		//// to the object centre of mass.
		//inline void	Instance::ApplyWorldImpulseAt(const v4& force, const v4& where)
		//{
		//	m_force	 += force;
		//	m_torque += Cross3(where, force);
		//}

		////*****
		//// Apply a collision impulse. These zero the current components of force
		//// and torque in the direction of the force and torque we're about to apply
		//inline void Instance::ApplyWorldCollisionImpulseAt(const v4& force, const v4& where)
		//{
		//	float inward_force = Dot3(force, m_force);
		//	if( inward_force < 0.0f ) { m_force -= (inward_force / force.Length3Sq()) * force; }
		//	m_force += force;

		//	v4 torque = Cross3(where, force);
		//	float inward_torque = Dot3(torque, m_torque);
		//	if( inward_torque < 0.0f ) { m_torque -= (inward_torque / torque.Length3Sq()) * torque; }
		//	m_torque += torque;
		//}
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_INSTANCE_H

