//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr
{
	namespace physics
	{
		// Evolve a rigid body forward in time. 
		// This advances the position/orientation of the provided physics object by 'elapsed_seconds'.
		// This function assumes the 'm_os_force' member of the rigid body has been set, so all constraint
		// forces, gravity, etc should have been applied before calling this function.
		template <typename = void>
		void Evolve(RigidBody& rb, double elapsed_seconds)
		{
			// Equation of Motion:
			//   f = d(Iv)/dt = I*a + v x* Iv
			// where:
			//   f = net spatial force acting
			//   I = spatial inertia tensor
			//   v = spatial velocity
			//   a = spatial acceleration
			//   Iv = momentum
			//   x* = cross product for force spatial vectors
			// So:
			//   f = I*a + v x* Iv
			//   I^-1 * f = a + I^-1 * (v x* Iv)
			//   a = I^-1 * f -  I^-1 * (v x* Iv)

			// Apply the change in momentum
			rb.MomentumOS(rb.MomentumOS() + rb.ForceOS() * float(elapsed_seconds));

			// Solve for the spatial acceleration
			//   a = I^-1 * f -  I^-1 * (v x* Iv)
			//CPM(rb.Velocity())





			//// Choose the integration method based on the magnitude of the rotation and step size,
			//// since the inertia tensor is dependent on orientation (which changes with time).
			//// Choose the 'Runge/Kutta/Merson' integrator order based on angular velocity.
			//auto avel_os = rb.InertiaInvOS() * rb.MomentumOS().ang;
			//// auto t = elapsed_seconds;

			// Integrate
			//for (;;) // break scope
			//{
			//	float const Order1Threshold = 1.8e-1f;
			//	float const Order2Threshold = 2.3e-3f;
			//	float const VelCapThreshold = 1.0e-6f;//1.0e-8f;
	
				// If the angular velocity is low, use an Euler step
			//	if (t * avel_os < Order1Threshold)
			//	{
			//		StepOrder1(rb, elapsed_seconds);
			//		break;
			//	}

				// Use a mid point step for mid range angular
			//	t *= elapsed_seconds; // t^2
			//	if (t * avel_os < Order2Threshold)
			//	{
			//		StepOrder2(rb, elapsed_seconds);
			//		break;
			//	}
			
				// Use a RKM step for high angular
			//	t *= t * elapsed_seconds; // t^5
			//	if (t * avel_os > VelCapThreshold)
			//	{
			//		// Cap the angular velocity for stability
			//
			//	}
			//}

			// Prepare the rigid body for the next step
			rb.UpdateDerivedState();
			rb.ForceOS((v8f)v8Zero);
		}

		// Euler first order step
		template <typename = void>
		void StepOrder1(RigidBody& rb, double elapsed_seconds)
		{
			(void)rb, elapsed_seconds;
		//	// Find the world space spatial velocity
		//	// Assume that the Inertia matrix is constant for the step (i.e. order1)
		//	auto vel_ws = rb.SpatialInertiaInvWS() * rb.MomentumWS();
		//
		//	
		//
		//	// 
		//	// Rotate the object_to_world by the change in orientation for this time step
		//	//'  q += qdot. qdot = rate of change of orientation CPM is a differential operator...
		//	auto cx = CPM(avel_ * elapsed_seconds);
		//	rb.O2W(rb.O2W() + cx * rb.O2W().rot);
		}

		template <typename = void>
		void StepOrder2(RigidBody& rb, double elapsed_seconds)
		{
			(void)rb,elapsed_seconds;
		}
	}
}
