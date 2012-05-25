//********************************************************
//
//	Physics Object related methods
//
//********************************************************

#include "PR/Physics/Engine/PHObject.h"

using namespace pr;
using namespace pr::ph;

//********************************************************
// ph::Instance methods
//*****
// Constructor
Instance::Instance()
:m_physics_object(0)
,m_collision_group(0)
,m_object_to_world(0)
,m_velocity(v4Zero)
,m_ang_velocity(v4Zero)
,m_ang_momentum(v4Zero)
,m_gravity(v4Zero)
,m_force(v4Zero)
,m_torque(v4Zero)
,m_world_bbox(BBoxZero)
,m_next(0)
,m_prev(0)
{}

//*****
// Set the angular velocity
void Instance::SetAngVelocity(const v4& ang_vel)
{
	m_ang_velocity = ang_vel;

	m4x4 o2w = m_object_to_world->GetRotation();
	m_ang_momentum = o2w * m_physics_object->m_mass_tensor * o2w.GetTranspose() * m_ang_velocity;
}

//*****
// Prepare this object for the next step
void Instance::Reset()
{
	m_force					= m_gravity;
	m_torque				= v4Zero;
	m_world_bbox			= (*m_object_to_world) * m_physics_object->m_bbox;

	//Iw = (o2w * Io * w2o)^-1  =  w2o^-1 * Io^-1 * o2w^-1  =  o2w * Io^-1 * w2o
	m4x4 o2w				= m_object_to_world->GetRotation();
	m_ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
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
	(*m_object_to_world)[3] += m_velocity * elapsed_seconds + 0.5f * elapsed_seconds * elapsed_seconds * acceleration;

	// Angular ***************
//PSR...	static const float ORDER2_STEP_THRESHOLD = 1.0f;
//PSR...	static const float ORDER4_STEP_THRESHOLD = 1.0f;
//PSR...	float fang_velSq = m_ang_velocity.Length3Sq();
//PSR...	if( fang_velSq < ORDER2_STEP_THRESHOLD )
//PSR...	{
//PSR...		StepOrder1(elapsed_seconds);
//PSR...	}
//PSR...	else if( fang_velSq < ORDER4_STEP_THRESHOLD )
	{
		StepOrder2(elapsed_seconds);
	}
//PSR...	else
//PSR...	{
//PSR...		StepOrder4(elapsed_seconds);
//PSR...	}

	// May not need to do this every step...
	m_object_to_world->Orthonormalise();

	Reset();
}

//*****
// Calculate the energy of this object
float Instance::GetEnergy() const
{
	m4x4 o2w = m_object_to_world->GetRotation();
	o2w;

	// mgh + 0.5mv^2 + 0.5wIw
	float potential			= m_gravity.Length3() * -Dot3(m_gravity.GetNormal3(), (*m_object_to_world)[3]);
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
	m4x4 mid_ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
	
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
//PSR...	float half_dt = elapsed_seconds * 0.5f;
//PSR...
//PSR...	// Add the torque impulse to get the current angular momentum
//PSR...	m_ang_momentum += m_torque * elapsed_seconds;
//PSR...
//PSR...	// Determine the change in orientation due to the current angular momentum and current inv mass tensor
//PSR...	m_ang_velocity = m_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...	// Find the orientation at the mid point
//PSR...	m4x4 o2w = m_object_to_world->GetRotation();
//PSR...	o2w += half_dt * m_ang_velocity.CrossProductMatrix() * o2w;
//PSR...
//PSR...	// Get the world space inv mass tensor at the mid point
//PSR...	m4x4 mid_ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
//PSR...
//PSR...	// Calculate the angular velocity at the mid point
//PSR...	v4 mid_ang_velocity	= mid_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...
//PSR...	// Determine the change in orientation due to the current angular momentum and mid inv mass tensor
//PSR...	m_ang_velocity = mid_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...	// Find the orientation at the end point
//PSR...	o2w += half_dt * m_ang_velocity.CrossProductMatrix() * o2w;
//PSR...
//PSR...	// Get the world space inv mass tensor at the mid point
//PSR...	m4x4 mid_ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
//PSR...
//PSR...	// Calculate the angular velocity at the mid point
//PSR...	v4 mid_ang_velocity	= mid_ws_inv_mass_tensor * m_ang_momentum;
//PSR...
//PSR...
	//PSR...		int i;
//PSR...		float xh,hh,h6,*dym,*dyt,*yt;
//PSR...		dym=vector(1,n);
//PSR...		dyt=vector(1,n);
//PSR...		// 16.1 Runge-Kutta Method 713
//PSR...		//	Sample page from NUMERICAL RECIPES IN C: THE ART OF SCIENTIFIC COMPUTING (ISBN 0-521-43108-5)
//PSR...		//	Copyright (C) 1988-1992 by Cambridge University Press. Programs Copyright (C) 1988-1992 by Numerical Recipes Software.
//PSR...		//	Permission is granted for internet users to make one paper copy for their own personal use. Further reproduction, or any copying of machinereadable
//PSR...		//	files (including this one) to any server computer, is strictly prohibited. To order Numerical Recipes books or CDROMs, visit website
//PSR...		//	http://www.nr.com or call 1-800-872-7423 (North America only), or send email to directcustserv@cambridge.org (outside North America).
//PSR...		yt=vector(1,n);
//PSR...		hh=h*0.5;
//PSR...		h6=h/6.0;
//PSR...		xh=x+hh;
//PSR...		for (i=1;i<=n;i++)
//PSR...		{
//PSR...			yt[i]=y[i]+hh*dydx[i];// First step.
//PSR...		}
//PSR...		(*derivs)(xh,yt,dyt);// Second step.
//PSR...		for (i=1;i<=n;i++)
//PSR...		{
//PSR...			yt[i]=y[i]+hh*dyt[i];
//PSR...		}
//PSR...		(*derivs)(xh,yt,dym); // Third step.
//PSR...		for (i=1;i<=n;i++)
//PSR...		{
//PSR...			yt[i]=y[i]+h*dym[i];
//PSR...			dym[i] += dyt[i];
//PSR...		}
//PSR...		(*derivs)(x+h,yt,dyt); // Fourth step.
//PSR...		for (i=1;i<=n;i++) // Accumulate increments with proper weights.
//PSR...		{
//PSR...			yout[i]=y[i]+h6*(dydx[i]+dyt[i]+2.0*dym[i]);
//PSR...		}
//PSR...		free_vector(yt,1,n);
//PSR...		free_vector(dyt,1,n);
//PSR...		free_vector(dym,1,n);
//PSR...	}
}

//*****
// Move an instance by 'push_distance'. This is basically a hack to help solve
// the resting contact problem. This can add energy if the push direction opposes
// gravity. The extra energy is removed from the velocity and the angular momentum
void Instance::PushOut(const v4& push_distance)
{
	(*m_object_to_world)[3] += push_distance;
	
	// See if we need to remove energy
	float energy_added = -Dot3(push_distance, m_gravity);
	if( energy_added > 0.0f )
	{
//PSR...		// E = 0.5 * m * v * v
//PSR...		// v = Sqrt(2E/m)
//PSR...		float dvelSq = 2.0f * energy_added / Mass();
//PSR...		float velSq  = m_velocity.Length3Sq();
//PSR...		if( dvelSq > velSq )
//PSR...		{
//PSR...			m_velocity.Zero();
//PSR...		}
//PSR...		else
//PSR...		{
//PSR...			m_velocity *= (1.0f - Sqrt(dvelSq / velSq));
//PSR...		}

		// E = 0.5 * w .dot( I * w )
		// m_ang_velocity *= (1.0f - Sqrt(dang_velSq / ang_velSq));
	}
}


//********************************************************
// Ph::Object methods


//********************************************************
// Ph::Primitive methods
//*****
// Returns a bounding box orientated to the primitive
BoundingBox Primitive::BBox() const
{
	switch( m_type )
	{
	case Box:		return BoundingBox::construct(-m_radius[0], -m_radius[1], -m_radius[2], m_radius[0], m_radius[1], m_radius[2]);
	case Cylinder:	return BoundingBox::construct(-m_radius[0], -m_radius[0], -m_radius[2], m_radius[0], m_radius[0], m_radius[2]);
	case Sphere:	return BoundingBox::construct(-m_radius[0], -m_radius[0], -m_radius[0], m_radius[0], m_radius[0], m_radius[0]);
	default:		PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
	}
	return BoundingBox();
}

//*****
// Returns the volume of the primitive
float Primitive::Volume() const
{
	switch( m_type )
	{
	case Box:		return 8.0f * m_radius[0] * m_radius[1] * m_radius[2];
	case Cylinder:	return maths::pi * m_radius[0] * m_radius[0] * m_radius[2];
	case Sphere:	return 4.0f * maths::pi * m_radius[0] * m_radius[0] * m_radius[0] / 3.0f;
	default:		PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
	}
	return 0.0f;
}

//*****
// Returns the moments of inertia about the primary axes for the primitive.
// Multiply by mass to get the mass moments of inertia
v4 Primitive::MomentOfInertia() const
{
	v4 moi = v4Zero;
	switch( m_type )
	{
	case Box:
		{
			moi[0] = (1.0f / 3.0f) * (m_radius[1]*m_radius[1] + m_radius[2]*m_radius[2]);	// (1/12)m(Y^2 + Z^2)
			moi[1] = (1.0f / 3.0f) * (m_radius[0]*m_radius[0] + m_radius[2]*m_radius[2]);	// (1/12)m(X^2 + Z^2)
			moi[2] = (1.0f / 3.0f) * (m_radius[1]*m_radius[1] + m_radius[0]*m_radius[0]);	// (1/12)m(Y^2 + Z^2)
		}break;
	case Cylinder:	// Note for shell, Ixx = Iyy = (1/2)mr^2 + (1/12)mL^2, Izz = mr^2
		{
			moi[0] = (1.0f / 4.0f) * (m_radius[0]*m_radius[0]) + (1.0f / 3.0f) * (m_radius[2]*m_radius[2]);	// (1/4)mr^2 + (1/12)mL^2
			moi[1] = moi[0];
			moi[2] = (1.0f / 2.0f) * (m_radius[0]*m_radius[0]);	// (1/2)mr^2
		}break;
	case Sphere:	// Note for a shell, Ixx = Iyy = Izz = 2/3mr^2
		{
			moi[0] = (2.0f / 5.0f) * (m_radius[0]*m_radius[0]);	// (2/5)mr^2
			moi[1] = moi[0];
			moi[2] = moi[0];
		}break;
	default:		PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
	}
	return moi;
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
//PSR...	m4x4 mid_ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
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
//PSR...		ws_inv_mass_tensor	= o2w * m_physics_object->m_inv_mass_tensor * o2w.GetTranspose();
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
