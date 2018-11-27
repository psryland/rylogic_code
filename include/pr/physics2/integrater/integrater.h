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
		template <typename = void>
		void Evolve(RigidBody& rb, float elapsed_seconds)
		{
			// Equation of Motion:
			//   f = d(Iv)/dt = I*a + vx*.I.v
			// where:
			//   f = net spatial force acting
			//   I = spatial inertia tensor
			//   v = spatial velocity
			//   a = spatial acceleration
			//   Iv = momentum
			//   x* = cross product for force spatial vectors
			// So:
			//   f = I*a + vx*.I.v
			//   I^-1 * f = a + I^-1 * (vx*.I.v)
			//   a = I^-1 * f -  I^-1 * (vx*.I.v)

			// Notes:
			//  'rb' should not store momentum, because we need 'v' for 'vx*.I.v'
			//  Should a store ws_force and os_force separately so that higher order
			//  integrators can more accurately sum the forces?

			// Two options: update in world space or body space.
			// In world space, the WS inverse inertia is needed to calculate the
			// updated velocity. This changes with orientation and hence with time.
			// In body space, the OS force changes with orientation (because really
			// they are WS forces). 

			// Midpoint integration.
			auto half_dt = elapsed_seconds * 0.5f;

			// Calculate the dynamics values at time_step / 2
			auto ws_momentum = rb.MomentumWS() + rb.ForceWS() * half_dt;
			auto ws_velocity = rb.InertiaInvWS();
				
				//v8m{0,0,1, 0,0,-0.6f};

			// Estimate the o2w halfway through the time step
			auto rot = rb.O2W().rot + CPM(ws_velocity.ang * half_dt) * rb.O2W().rot;

			auto momentum = rb.MomentumWS() + 

			// Calculate the WS inverse inertia for the estimated orientation
			auto ws_inertia_inv = rot * os_inertia_inv * Transpose(rot);

			// Simple version:
			// A = F/M
			auto lin_a = rb.MassProps().InvMass() * rb.ForceOS().lin + gravity;




			// Solve for the spatial acceleration
			//   a = I^-1 * f -  I^-1 * (vx*.I.v)
			auto& os_inertia = rb.MassProps().InertiaOS();
			auto& os_inertia_inv = rb.MassProps().InertiaInvOS();
			auto& os_velocity = rb.VelocityOS();
			auto& os_force = rb.ForceOS();
			(os_inertia_inv * os_force) - 

			
			
			//CPM(rb.Velocity())
			(void)elapsed_seconds;



			// The body spatial velocity
			v8m vel();

			// S = So + VoT + 0.5AT^2
			vel = vel * elapsed_seconds;

//			rb.m_object_to_world.pos += (velocity + (0.5f * elapsed_seconds) * acceleration) * elapsed_seconds;
//

			//auto vel_com = vel.at(v4Origin);
			//auto do2w_dt = CPM(vel_com) * rb.O2W();
			auto do2w_dt = CPM(vel.ang, vel.lin);
			auto o2w = rb.O2W() + do2w_dt;
			rb.O2W(Orthonorm(o2w));

			// Prepare the rigid body for the next step
			rb.UpdateDerivedState();
			rb.ForceOS(v8f{});
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
		void StepOrder2(RigidBody& rb, v4 vel_ang, float elapsed_seconds)
		{
//			// Use the midpoint algorithm
//			auto dt_by_2 = elapsed_seconds * 0.5f;
//
//			// Calculate mid-point values
//			auto mid_orientation            = rb.O2W().rot + CPM(vel_ang * dt_by_2) * rb.O2W().rot;
//			auto mid_ws_inv_inertia_tensor  = mid_orientation * rb.m_os_inv_inertia_tensor * Transpose(mid_orientation);
//			auto mid_ang_momentum           = rb.m_ang_momentum + rb.m_torque * dt_by_2;
//			auto mid_ang_velocity           = mid_ws_inv_inertia_tensor * rb.m_inv_mass * mid_ang_momentum;
//	
//			// Perform step using mid-point angular velocity
//			//
//			auto do2w_dt = CPM(mid_ang_velocity * elapsed_seconds) * rb.O2W().rot;
//			
//			// S = So + VoT + 0.5AT^2
//			rb.m_object_to_world.pos += (velocity + (0.5f * elapsed_seconds) * acceleration) * elapsed_seconds;
//
//			rb.m_object_to_world.rot += ;
		}
	}
}
