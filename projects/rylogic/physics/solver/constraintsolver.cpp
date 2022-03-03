//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Physics/Solver/ConstraintSolver.h"
#include "PR/Physics/Collision/Contact.h"

using namespace pr;
using namespace pr::ph;

void ConstraintMatrix::AddCollisionConstraint(const Contact& contact)
{contact;}
//{
//	// Add the normal constraint
//	ConstraintVelocity vn;
//	vn.m_lin_vel_0 = -contact.m_normal;
//	vn.m_ang_vel_0 = -Cross3(contact.m_pointA, contact.m_normal);
//	vn.m_lin_vel_1 =  contact.m_normal;
//	vn.m_ang_vel_1 =  Cross3(contact.m_pointB, contact.m_normal);
//	m_Jsparce.push_back(vn);
//
//	// Record the objects in the map
//	ConstraintPair pair;
//	pair.m_objectA = contact.m_objectA;
//	pair.m_objectB = contact.m_objectB;
//	m_Jmap.push_back(pair);
//
//	// Add friction constraints
//
//}

float Dot6(const v8& lhs, const v8& rhs)
{
	return Dot3(lhs.x0, rhs.x0) + Dot3(lhs.x1, rhs.x1);
}

// Approximately solve JBL = n given L0
void pr::ph::SolveConstraints(ConstraintMatrix& matrix)
{matrix;}
//{
//	PR_ASSERT(PR_DBG_PHYSICS, matrix.m_Jsparce.size() == matrix.m_Jmap.size());
//
//	std::size_t num_constriants = matrix.m_Jsparce.size();
//
//	// Compute the rate of change of constraint error C' = JV
//	// constraint error is the amount that the constraint is broken by. i.e. 0 = constraint satisfied.
//	std::vector<float> constraints(num_constriants);	// = C'
//	for( std::size_t i = 0; i != num_constriants; ++i )
//	{
//		v8 objA_vel;
//		objA_vel.lin_vel	= matrix.m_Jmap[i].m_objectA->Velocity();
//		objA_vel.ang_vel	= matrix.m_Jmap[i].m_objectA->AngVelocity();
//
//		v8 objB_vel;
//		objB_vel.lin_vel	= matrix.m_Jmap[i].m_objectB->Velocity();
//		objB_vel.ang_vel	= matrix.m_Jmap[i].m_objectB->AngVelocity();
//
//		v8 coeffsA	= matrix.m_Jsparce[i].x0;
//		v8 coeffsB	= matrix.m_Jsparce[i].x1;
//
//		constraints[i] = Dot6(objA_vel, coeffsA) + Dot6(objB_vel, coeffsB); 
//	}
//
//	// Compute the constraint forces Fc = J^Lamda (^ = transpose)
//	std::vector<v8> constraint_forces(max_objects, v8());	// = Fc
//	for( std::size_t i = 0; i != num_constriants; ++i )
//	{
//		constraint_forces[matrix.m_Jmap[i].m_objectA_index] += matrix.m_Jsparce[i].x0 * matrix.m_lamda[i];
//		constraint_forces[matrix.m_Jmap[i].m_objectB_index] += matrix.m_Jsparce[i].x0 * matrix.m_lamda[i];
//	}
//
//	// Precalculate a = B*lamda
//	//std::vector<>
//
//	// Chose an initial guess PHI[0]
//	// for k := 1 step 1 until convergence do
//	//	for i := 1 step until n do
//	//		s = 0
//	//		for j := 1 step until i-1 do
//	//			s = s + A[i][j]	* PHI[j](k)
//	//		end (j-loop)
//	//		for j := i+1 step until n do
//	//			s = s + A[i][j] * PHI[j](k-1)
//	//		end (j-loop)
//	//		PHI[i](k) = (b[i] - s) / A[i][i]
//	//	end (i-loop) 
//	//	check if convergence is reached 
//	// end (k-loop) 
//}

// Approximately solve Ax = b given x(0)
void Solve_Slow()
{
	

}




