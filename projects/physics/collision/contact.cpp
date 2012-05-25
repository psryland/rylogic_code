//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/collision/contact.h"
#include "pr/physics/rigidbody/rigidbody.h"

using namespace pr;
using namespace pr::ph;

//// Determine extra information needed to do collision resolution for this
//// contact, i.e. the relative velocity, contact tangent, etc.
//void Contact::CalculateExtraContactInfo(Rigidbody const& objA, Rigidbody const& objB)
//{
//	PR_ASSERT(PR_DBG_PHYSICS, FEql(m_pointA.w, 1.0f));
//	PR_ASSERT(PR_DBG_PHYSICS, FEql(m_pointB.w, 1.0f));
//
//	// Save the objects involved in the collision
//	m_objectA = &objA;
//	m_objectB = &objB;
//
//	// Relative velocity
//	m_relative_velocity = objB.VelocityAt(m_pointB - objB.Position()) - objA.VelocityAt(m_pointA - objA.Position());
//
//	// If the relative velocity is not into the collision
//	float rel_norm_velocity = Dot3(m_normal, m_relative_velocity);
//	
//	// Tangent to the collision normal
//	m_tangent = m_relative_velocity - rel_norm_velocity * m_normal;
//
//	float rel_tang_velocity = m_tangent.Length3();
//	if( !FEql(rel_tang_velocity, 0.0f) )	m_tangent /= rel_tang_velocity;
//	else									m_tangent .Zero();
//
//	// Record the component velocities
//	m_rel_norm_speed		= rel_norm_velocity;
//	m_rel_tang_speed		= rel_tang_velocity;
//}
