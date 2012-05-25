//****************************************************
//
//	Structures used for collision detection
//
//****************************************************
// See notes for Terrain
//	Depth vs Fraction:
//		Depth is the penetration in meters in the direction of 'm_normal'.
//		Fraction is the fraction of a step before collision occurs.
//		When comparing contact points, fraction is tested first. If not 0.0
//		or 1.0 then the greatest penetration is the one that will collide
//		first. If 0.0 or 1.0 then m_depth is used.
//
#ifndef PR_PHYSICS_COLLISION_H
#define PR_PHYSICS_COLLISION_H

#include "PR/Physics/Physics.h"
#include "PR/Physics/Engine/PHObject.h"

namespace pr
{
	namespace ph
	{
		struct Contact
		{
			void	SetNoCollision()									{ m_depth = -maths::float_max; m_fraction = 1.0f; }
			bool	IsContact() const									{ return FLess(m_fraction, 1.0f); }
			bool	IsDeeperThan(float fraction, float depth) const		{ return (!FEql(m_fraction, fraction)) ? (m_fraction < fraction) : (m_depth > depth); }
			bool	IsDeeperThan(const Contact& other) const			{ return IsDeeperThan(other.m_fraction, other.m_depth); }

			v4		m_pointA;					// The contact point on objectA in world space but relative to objectA
			v4		m_pointB;					// The contact point on objectB in world space but relative to objectB
			v4		m_normal;					// The contact normal in world space (from objectA's point of view)
			float	m_depth;					// The depth of penetration. >= 0.0f indicates contact
			float	m_fraction;					// The fraction of a step before this contact occurs. Range: [0..1). Should be 0.0 if m_depth > 0.0
			uint	m_material_indexA;			// The material index for pointA
			uint	m_material_indexB;			// The material index for pointB

			// Post-detection data - This is filled out by the collision resolution
			v4		m_relative_velocity;		// The velocity of pointA into pointB
			v4		m_tangent;					// The tangent to the point of contact in world space in the direction of the tangential relative velocity.
			float	m_rel_norm_speed;			// The magnitude of the normal component of the relative velocity
			float	m_rel_tang_speed;			// The magnitude of the tangential component of the relative velocity
		};

		// Associates two objects and a deepest point of contact
		struct CollisionData
		{
			CollisionData(){} // Do not expect any initialisation to happen. Treaded as POD
			CollisionData(Instance*	objA, Instance*	objB) : m_objA(objA), m_objB(objB) {}
			void Reset()						{ m_contact.SetNoCollision(); }
			bool CalculateExtraContactData();	// Returns true if this an actual collision/contact

			Instance*		m_objA;				// Object A. For terrain collisions use m_objA only
			Instance*		m_objB;				// Object B
			Contact			m_contact;			// The deepest point of contact between objA and objB
			CollisionData*	m_next;				// Used to link resting contacts together
		};

	}//namespace ph
}//namespace pr
#endif//PR_PHYSICS_COLLISION_H
