//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PH_CONSTRAINT_H
#define PR_PH_CONSTRAINT_H

#include "pr/physics/types/forward.h"
#include "pr/physics/collision/contact.h"

namespace pr
{
	namespace ph
	{
		enum EConstraintType
		{
			// Collision between two objects. All collisions are potentially resting
			// contact constraints as well. On the first pass we'll resolve all of the
			// constraints, then re-evaluate the relative velocities in the constraints
			// and go through applying zero-elasticity impulses to those that will not be
			// out of collision by the next frame
			EConstraintType_Collision,

			EConstraintType_Joint
		};

		struct Constraint
		{
			// Collision matrix - Relates a change in velocity to an impulse
			m3x4 m_mass;

			// The object relative world space contact points on objectA and objectB
			// Add to the object positions to get world relative positions.
			v4 m_pointA;
			v4 m_pointB;

			// This member has a different meaning depending on the constraint type
			// Collision, RestingContact: Contact normal in world space (from objectB to objectA, or the direction objectA needs to move to stop penetration, or pointing away from objectB)
			// Joint: The main axis of the joint (to be sorted out.. should be prismatic joint direction, or rotation axis... depends if I want to represent all joints using 6 DoF mask or something)
			v4 m_normal;

			// The desired final relative velocity of points A and B for this constraint
			// if it is considered in isolation with friction and elasticity considerations included.
			v4 m_desired_final_rel_velocity;

			// The impulse calculated per iteration due to this constraint
			v4 m_impulse;

			//// The depth of penetration. +ve means penetration, -ve means out of penetration
			//float m_penetration;
			// The minimum separation speed this pair of objects should have
			float m_separation_speed_min;

			// Material properties
			float m_elasticity;
			float m_static_friction;
			float m_dynamic_friction;

			// The type of constraint this is.
			EConstraintType	m_type;

			// Shock propagation mask, used to set one of the objects to infinite mass
			int m_shock_propagation_mask;

			// Debugging
			float m_error;
		};

		struct ConstraintBlock
		{
			// The objects involved in the constraint
			Rigidbody* m_objA;
			Rigidbody* m_objB;

			// Some information about gravity in the local area of the constraint.
			//float m_grav_accel;   // Used to determine resting contact speeds and penetration correction
			float m_grav_potential; // Used to set the order in which constraints are processed

			// This is the velocity an object would have after one frame of acceleration
			// under gravity alone.
			float m_resting_contact_speed;

			// A count of the number of constraints in this block
			uint16_t m_num_constraints;

			// The constraint set that this constraint belongs to
			uint8_t m_constraint_set;
			uint8_t pad;

			// Debugging
			Constraint* m_constraints;

			// Return access to the constraints in this block
			Constraint& operator [](uint32_t i) { return reinterpret_cast<Constraint*>(this + 1)[i]; }
		};
	}
}

#endif
