//********************************************************
//
//	Physics Instance related methods
//
//********************************************************

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Physics/Model/Instance.h"

using namespace pr;
using namespace pr::ph;

//*****
Instance::Instance()
:m_model(0)
,m_object_to_world(0)
,m_collision_group(0)
,m_velocity(v4Zero)
,m_ang_momentum(v4Zero)
,m_ang_velocity(v4Zero)
,m_ws_inv_mass_tensor(m4x4Identity)
,m_force(v4Zero)
,m_torque(v4Zero)
,m_ws_bbox(BBoxUnit)
{}
Instance::Instance(Model* model, m4x4* object_to_world)
:m_model(model)
,m_object_to_world(object_to_world)
,m_collision_group(0)
,m_velocity(v4Zero)
,m_ang_momentum(v4Zero)
,m_ang_velocity(v4Zero)
,m_ws_inv_mass_tensor(m4x4Identity)
,m_force(v4Zero)
,m_torque(v4Zero)
,m_ws_bbox(BBoxUnit)
{}

//*****
// Set the angular velocity
void Instance::SetAngVelocity(const v4& ang_vel)
{
	m_ang_velocity = ang_vel;

	m4x4 o2w = m_object_to_world->GetRotation();
	m_ang_momentum = o2w * m_model->m_mass_tensor * o2w.GetTranspose() * m_ang_velocity;
}

//*****
// Evolved this object forward in time.
void Instance::Step(float elapsed_seconds)
{
	// Linear ***************
	// A = F/M
	v4 acceleration	= (1.0f / Mass()) * m_force;
	// V = Vo + AT
	m_velocity += acceleration * elapsed_seconds;
	// S = So + VoT + 0.5AT^2
	(*m_object_to_world)[3] += m_velocity * elapsed_seconds + (0.5f * elapsed_seconds * elapsed_seconds) * acceleration;

	// Angular ***************
	//static const float ORDER2_STEP_THRESHOLD = 1.0f;
	//static const float ORDER4_STEP_THRESHOLD = 1.0f;
	//float fang_velSq = m_ang_velocity.Length3Sq();
	//if( fang_velSq < ORDER2_STEP_THRESHOLD )
	//{
	//	StepOrder1(elapsed_seconds);
	//}
	//else if( fang_velSq < ORDER4_STEP_THRESHOLD )
	{
		StepOrder2(elapsed_seconds);
	}
	//else
	//{
	//	StepOrder4(elapsed_seconds);
	//}

	// May not need to do this every step...
	m_object_to_world->Orthonormalise();

	// Prepare this object for the next step
	m_force					.Zero();
	m_torque				.Zero();
	m_ws_bbox				= (*m_object_to_world) * m_model->m_bbox;

	//Iw = (o2w * Io * w2o)^-1  =  w2o^-1 * Io^-1 * o2w^-1  =  o2w * Io^-1 * w2o
	m4x4 o2w				= m_object_to_world->GetRotation();
	m_ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
}

//*****
// Calculate the energy of this object
float Instance::GetEnergy(const v4& gravity) const
{
	// mgh + 0.5mv^2 + 0.5wIw
	float potential			= gravity.Length3() * -Dot3(Normalise3(gravity), (*m_object_to_world)[3]);
	float linear_kinetic	= 0.5f * Mass() * m_velocity.Length3Sq();
	float ang_kinetic		= 0.5f * Dot3(m_ang_velocity, m_ang_momentum);
	return potential + linear_kinetic + ang_kinetic;
}

//*****
// Use Euler integration to advance the angular stuff.
// Note: m_ang_momentum is the ang mom from the last step
void Instance::StepOrder1(float elapsed_seconds)
{
	// Add the torque impulse to get the current angular momentum
	m_ang_momentum += m_torque * elapsed_seconds;

	// Determine the change in orientation due to the current angular momentum and current inv mass tensor
	m_ang_velocity = m_ws_inv_mass_tensor * m_ang_momentum;

	// Rotate the object_to_world by the change in orientation for this time step
	(*m_object_to_world) += elapsed_seconds * m_ang_velocity.CrossProductMatrix() * m_object_to_world->GetRotation();
	(*m_object_to_world)[3][3] = 1.0f;
}

//*****
// Use the midpoint algorithm to advance the angular stuff.
void Instance::StepOrder2(float elapsed_seconds)
{
	float half_dt = elapsed_seconds * 0.5f;

	// Add the torque impulse to get the current angular momentum
	m_ang_momentum += m_torque * elapsed_seconds;

	// Determine the change in orientation due to the current angular momentum and current inv mass tensor
	m_ang_velocity = m_ws_inv_mass_tensor * m_ang_momentum;

	// Find the orientation at the mid point
	m4x4 o2w = m_object_to_world->GetRotation();
	o2w += half_dt * m_ang_velocity.CrossProductMatrix() * o2w;
	o2w[3][3] = 1.0f;

	// Get the world space inv mass tensor at the mid point
	m4x4 mid_ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
	
	// Calculate the angular velocity at the mid point
	v4 mid_ang_velocity	= mid_ws_inv_mass_tensor * m_ang_momentum;

	// Rotate the object_to_world by the midpoint change in orientation for this time step
	(*m_object_to_world) += elapsed_seconds * mid_ang_velocity.CrossProductMatrix() * m_object_to_world->GetRotation();
	(*m_object_to_world)[3][3] = 1.0f;
}

//*****
// Use the Runge-Kutta 4th order algorithm to advance the angular stuff.
void Instance::StepOrder4(float elapsed_seconds)
{
	elapsed_seconds;
	//float half_dt = elapsed_seconds * 0.5f;
	//
	//// Add the torque impulse to get the current angular momentum
	//m_ang_momentum += m_torque * elapsed_seconds;
	//
	//// Determine the change in orientation due to the current angular momentum and current inv mass tensor
	//m_ang_velocity = m_ws_inv_mass_tensor * m_ang_momentum;
	//
	//// Find the orientation at the mid point
	//m4x4 o2w = m_object_to_world->GetRotation();
	//o2w += half_dt * m_ang_velocity.CrossProductMatrix() * o2w;
	//
	//// Get the world space inv mass tensor at the mid point
	//m4x4 mid_ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
	//
	//// Calculate the angular velocity at the mid point
	//v4 mid_ang_velocity	= mid_ws_inv_mass_tensor * m_ang_momentum;
	//
	//
	//// Determine the change in orientation due to the current angular momentum and mid inv mass tensor
	//m_ang_velocity = mid_ws_inv_mass_tensor * m_ang_momentum;
	//
	//// Find the orientation at the end point
	//o2w += half_dt * m_ang_velocity.CrossProductMatrix() * o2w;
	//
	//// Get the world space inv mass tensor at the mid point
	//m4x4 mid_ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
	//
	//// Calculate the angular velocity at the mid point
	//v4 mid_ang_velocity	= mid_ws_inv_mass_tensor * m_ang_momentum;
	//
	//
	//	int i;
	//	float xh,hh,h6,*dym,*dyt,*yt;
	//	dym=vector(1,n);
	//	dyt=vector(1,n);
	//	// 16.1 Runge-Kutta Method 713
	//	//	Sample page from NUMERICAL RECIPES IN C: THE ART OF SCIENTIFIC COMPUTING (ISBN 0-521-43108-5)
	//	//	Copyright (C) 1988-1992 by Cambridge University Press. Programs Copyright (C) 1988-1992 by Numerical Recipes Software.
	//	//	Permission is granted for internet users to make one paper copy for their own personal use. Further reproduction, or any copying of machinereadable
	//	//	files (including this one) to any server computer, is strictly prohibited. To order Numerical Recipes books or CDROMs, visit website
	//	//	http://www.nr.com or call 1-800-872-7423 (North America only), or send email to directcustserv@cambridge.org (outside North America).
	//	yt=vector(1,n);
	//	hh=h*0.5;
	//	h6=h/6.0;
	//	xh=x+hh;
	//	for (i=1;i<=n;i++)
	//	{
	//		yt[i]=y[i]+hh*dydx[i];// First step.
	//	}
	//	(*derivs)(xh,yt,dyt);// Second step.
	//	for (i=1;i<=n;i++)
	//	{
	//		yt[i]=y[i]+hh*dyt[i];
	//	}
	//	(*derivs)(xh,yt,dym); // Third step.
	//	for (i=1;i<=n;i++)
	//	{
	//		yt[i]=y[i]+h*dym[i];
	//		dym[i] += dyt[i];
	//	}
	//	(*derivs)(x+h,yt,dyt); // Fourth step.
	//	for (i=1;i<=n;i++) // Accumulate increments with proper weights.
	//	{
	//		yout[i]=y[i]+h6*(dydx[i]+dyt[i]+2.0*dym[i]);
	//	}
	//	free_vector(yt,1,n);
	//	free_vector(dyt,1,n);
	//	free_vector(dym,1,n);
	//}
}

//*****
// Move an instance by 'push_distance'. This is basically a hack to help solve
// the resting contact problem. This can add energy if the push direction opposes
// gravity. The extra energy is removed from the velocity and the angular momentum
void Instance::PushOut(const v4& push_distance)
{
	push_distance;
	//(*m_object_to_world)[3] += push_distance;
	//
	//// See if we need to remove energy
	//float energy_added = -Dot3(push_distance, m_gravity);
	//if( energy_added > 0.0f )
	//{
	//	//// E = 0.5 * m * v * v
	//	//// v = Sqrt(2E/m)
	//	//float dvelSq = 2.0f * energy_added / Mass();
	//	//float velSq  = m_velocity.Length3Sq();
	//	//if( dvelSq > velSq )
	//	//{
	//	//	m_velocity.Zero();
	//	//}
	//	//else
	//	//{
	//	//	m_velocity *= (1.0f - Sqrt(dvelSq / velSq));
	//	//}

	//	// E = 0.5 * w .dot( I * w )
	//	// m_ang_velocity *= (1.0f - Sqrt(dang_velSq / ang_velSq));
	//}
}







//PSR...//*****
//PSR...// Use the Leap Frog algorithm to integrate this instance forward in time.
//PSR...// Normally this algorithm has the object_to_world being half a time
//PSR...// step ahead of the momentum. However, I'm calculating the extra half step 
//PSR...// so that I can interchange between the order2 and order4 steppers.
//PSR...//	Algorithm: q = position, p = momentum
//PSR...//		temp_q	= q + dt/2 * p;
//PSR...//		p		= p + dt * f(temp_q);
//PSR...//		q		= temp_q + dt/2 * p;
//PSR...void Instance::StepOrder2(float elapsed_seconds)
//PSR...{
//PSR...	// Add the torque impulse to get the current angular momentum
//PSR...	m_ang_momentum += m_torque * elapsed_seconds;
//PSR...
//PSR...	float half_dt = elapsed_seconds / 2.0f;
//PSR...
//PSR...	// Step the position half a time step forward
//PSR...	// Determine the change in orientation due to the current angular momentum and current inv mass tensor
//PSR...	m_ang_velocity = m_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...	// Rotate the object_to_world by the change in orientation for this time step
//PSR...	(*m_object_to_world) += half_dt * m_ang_velocity.CrossProductMatrix() * m_object_to_world->GetRotation();
//PSR...	(*m_object_to_world)[3][3] = 1.0f;
//PSR...	
//PSR...	// Get the midpoint inertia tensor
//PSR...	m4x4 o2w					= m_object_to_world->GetRotation();
//PSR...	m4x4 mid_ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
//PSR...
//PSR...	m_ang_momentum += m_torque * elapsed_seconds;
//PSR...
//PSR...	// Get the midpoint ang_velocity
//PSR...	m_ang_velocity = mid_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...	// Step the position by the second half of the time step 
//PSR...	(*m_object_to_world) += half_dt * m_ang_velocity.CrossProductMatrix() * m_object_to_world->GetRotation();
//PSR...	(*m_object_to_world)[3][3] = 1.0f;
//PSR...}
//PSR...
//PSR...//*****
//PSR...// This method was proposed by Forest and Ruth in 1990.
//PSR...// From a book called "Numerical Hamiltonian Problems" by Sanz-Serna and Calvo.
//PSR...// It's basically two leapfrog steps per step (apparently)
//PSR...//	Algorithm:
//PSR...//		const float w = (2.0f + 2.0f^(1.0f/3.0f) + 2.0f^(-1.0f/3.0f)) / 3.0f;
//PSR...//		const float v = 1.0f - 2.0f * w;
//PSR...//		const float b[4] = {w, v, w, 0};
//PSR...//		const float B[4] = {w / 2.0f, (w + v) / 2.0f, (w + v) / 2.0f, w / 2.0f};
//PSR...//		for( int i = 0; i < 4; ++i )
//PSR...//		{
//PSR...//			q += dt * B[i] * invM*p;
//PSR...//			p += dt * b[i] * force(q);
//PSR...//		}
//PSR...void Instance::StepOrder4(float elapsed_seconds)
//PSR...{
//PSR...	// Add the torque impulse to get the current angular momentum
//PSR...	m_ang_momentum += m_torque * elapsed_seconds;
//PSR...
//PSR...	const float w = 1.35120f;//(2.0f + 2.0f^(1.0f/3.0f) + 2.0f^(-1.0f/3.0f)) / 3.0f;
//PSR...	const float v = 1.0f - 2.0f * w;
//PSR...	const float b[4] = {w, v, w, 0};
//PSR...	const float B[4] = {w / 2.0f, (w + v) / 2.0f, (w + v) / 2.0f, w / 2.0f};
//PSR...
//PSR...	m4x4 ws_inv_mass_tensor = m_ws_inv_mass_tensor;
//PSR...	for( uint i = 0; i < 4; ++i )
//PSR...	{
//PSR...		// Determine the change in orientation due to the current angular momentum and current inv mass tensor
//PSR...		m_ang_velocity = ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...		// Rotate the object_to_world by the change in orientation for this time step
//PSR...		(*m_object_to_world) += (elapsed_seconds * B[i]) * m_ang_velocity.CrossProductMatrix() * m_object_to_world->GetRotation();
//PSR...		(*m_object_to_world)[3][3] = 1.0f;
//PSR...		
//PSR...		// Get the inertia tensor at this point
//PSR...		m4x4 o2w			= m_object_to_world->GetRotation();
//PSR...		ws_inv_mass_tensor	= o2w * m_model->m_inv_mass_tensor * o2w.GetTranspose();
//PSR...	}
//PSR...}
//PSR...
//PSR...//	C = [w/2  w+v/2  3*w/2+v  1]
//PSR...//	Q[0] = q[n];
//PSR...//	P[1] = p[n];
//PSR...//	for i=1 ... 4
//PSR...//		Q[i]   = Q[i-1] + dt * B[i] * g(P[i]);
//PSR...//		P[i+1] = P[i]   + dt * f(Q[i], t[n]+C[i]*dt);
//PSR...//	q[n+1]=Q[4]:
//PSR...//	p[n+1]=P[5];
