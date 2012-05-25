//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

// Algorithm
// 1) Generate array of pairwise constraints (position, friction, joints, etc)
// 2) Each contact has a contact force associated with it, the magnitude of this force is 'lamda'
//    Initialise 'lamda' for each contact force from previous frame.
//    (Use 0 if first frame or first time we've seen contact)
// 3) Compute Jacobian for the constraints.
//    This is done by adding rows to the Jacobian matrix for each constraint. Each row has four
//    elements corresponding to the lin & ang velocities of each object required to maintain the constraint.
//    Achieved by differentiating (off-line) the position contraints or figuring out the velocity
//    constraints directly. Summary:
//		1. Determine each constraint equation as a function of body positions and rotations.
//		2. Differentiate the constraint equation with respect to time.
//		3. Identify the coefficients of {Vi, Wi, Vj, Wj}. These form a row of the Jacobian matrix (J).
//    The matrix looks like this:
//    {...0,0, Vi, Wi, 0,...,0 ,Vj, Wj,0...}
//    {...0,0, Vk, Wk, 0,...,0 ,Vl, Wl,0...}
//    {...0,0, Vm, Wm, 0,...,0 ,Vn, Wn,0...}
//    {...0,0, Vo, Wo, 0,...,0 ,Vp, Wp,0...} etc where num_rows = num_constraints
// 4) Compute the sum of the constraint velocities:
//		Algorithm 1 Compute C' = JV. J = Jacobian matrix, V = {Vi, Wi, Vj Wj}
//		for( i = 1 to num_constraints )
//		{
//			b1 = Jmap(i,1)
//			b2 = Jmap(i,2)
//			sum = 0
//			if b1 != 0						// b1 = zero used to represent static objects/terrain 
//			{
//				sum = sum + Jsp(i,1)V(b1)	
//			}
//			sum = sum + Jsp(i,2)V(b2)
//			C'(i) = sum
//		}
// 5) Compute the constraint force magnitudes
//		Algorithm 2 Compute Fc = Transpose(J) * lamda
//		for( i = 1 to num_bodies )
//		{
//			Fc(i) = 0	// Set the constraint forces in each body to zero
//		}
//		for( i = 1 to num_constraints )
//		{
//			b1 = Jmap(i,1)
//			b2 = Jmap(i,2)
//			Fc(b1) = Fc(b1) + Jsp(i,1) * lamda(i)
//			Fc(b2) = Fc(b2) + Jsp(i,2) * lamda(i)
//		}
//	This is an iterative algorithm, lamdas are stored for each constraint and improved with each iteration
//	Limits on the lamdas are imposed each iteration by clamping
//	Use the Projected Gauss-Seidel algorithm to iterative improve 'lamda's
// 6) Compute new velocities using 'lamdas'
// 7) Compute new positions using velocities
// 8) Maintain 'lamda's for next frame

#ifndef PR_PH_CONSTRAINT_SOLVER_H
#define PR_PH_CONSTRAINT_SOLVER_H

#include "PR/Common/StdVector.h"
#include "PR/Physics/Types/Types.h"
#include "PR/Physics/Types/Forward.h"

namespace pr
{
	namespace ph
	{
		struct v8
		{
			union {
			v4 x0;
			v4 lin_vel;
			v4 force;
			};
			union {
            v4 x1;
			v4 ang_vel;
			v4 torque;
			};
		};

		struct ConstraintPair
		{
			std::size_t			m_objectA_index;
			const Rigidbody*	m_objectA;
			std::size_t			m_objectB_index;
			const Rigidbody*	m_objectB;
		};
		
		
		struct ConstraintVelocity
		{
			v8	x0;
			v8	x1;
		};

		// This is the Jacobian matrix
		struct ConstraintMatrix
		{
			void AddCollisionConstraint(const Contact& pair);
			std::vector<ConstraintVelocity>	m_Jsparce;	// The velocities for the 1st and 2nd bodies involved in the constraint
			std::vector<ConstraintPair>		m_Jmap;		// The indices of the objects involved in the constraint 
			std::vector<float>				m_lamda;	// Estimates of lamda
		};

		// Approximately solve JBL = n given L0
		void SolveConstraints(ConstraintMatrix& matrix);

	}//namespace ph
}//namespace pr
#endif//PR_PH_CONSTRAINT_SOLVER_H



//struct PhysicsObject
//{
//	v4		m_ws_position;
//	Quat	m_ws_orientation;
//	v4		m_ws_lin_velocity;
//	v4		m_ws_ang_velocity;
//};
//
//struct Velocity
//{
//	v4	m_lin_velocity;
//	v4	m_ang_velocity;
//};
//
//struct Contact
//{
//	const PhysicsObject* m_obj0;
//	const PhysicsObject* m_obj1;
//	v4	m_ws_position_rel_to_obj0;
//	v4	m_ws_position_rel_to_obj1;
//	float m_lamda;
//};
//
//struct VelocityConstraint
//{
//	v4	m_v0;	// Linear velocity of object 0
//	v4	m_w0;	// Angular velocity of object 0
//	v4	m_v1;	// Linear velocity of object 1
//	v4	m_w1;	// Angular velocity of object 1
//};
//
//
//std::vector<pr::PhysicsObject>	g_physics_objects;	// V[n]
//
//struct ConstraintMatrix
//{
//	// Compute the constraint velocities vector
//	void ComputeConstraintVelocities
//	{
//        for( int i = 0, i_end = m_num_constraints; i != i_end; ++i )
//		{
//			const PhysicsObject* b1 = &g_physics_objects[m_rigid_body_indices[i].first ];
//			const PhysicsObject* b2 = &g_physics_objects[m_rigid_body_indices[i].second];
//			
//			v8 sum = v8Zero;
//			sum += m_constraint_velocities[i].first * V(b1)
//sum = sum+Jsp(i,2)V(b2)
//m_constraints[i] = sum
//end for
//	// Adds a constraint that ensures the distance between two points is maintained
//	void AddDistanceConstraint(const pr::Contact& contact)
//	{
//		v4 ws_point_0 = contact.m_obj0.m_ws_position + contact.m_ws_position_rel_to_obj0;
//		v4 ws_point_1 = contact.m_obj1.m_ws_position + contact.m_ws_position_rel_to_obj1;
//
//		v4 d = ws_point_1 - ws_point_0;
//
//		pr::velocity_constraint c;
//		c.m_v0 = -d;
//		c.m_w0 = -Cross3(contact.m_ws_position_rel_to_obj0, d);
//		c.m_v1 =  d;
//		c.m_w1 =  Cross3(contact.m_ws_position_rel_to_obj1, d);
//
//		m_constraints.push_back(c);
//	}
//
//	typedef std::pair<int, int> TIndexPair;
//	typedef std::pair<Velocity, Velocity> TVelocityPair;
//	
//	// Each [i] of this contains the indices of the pairs of rigid bodies
//	// involved in constraint [i]
//	std::vector<TIndexPair>	m_rigid_body_indices;
//
//	// Pairs of velocities added for each constraint
//	std::vector<pr::VelocityPair> m_constraint_velocities;
//
//	// The final sumed constraint velocities
//	std::vector<pr::VelocityConstraint> m_constraints;
//};
