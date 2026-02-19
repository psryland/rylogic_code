//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics-2/forward.h"
#include "pr/physics-2/rigid_body/rigid_body.h"

namespace pr::physics
{
	// Calculate the signed change in kinetic energy caused by applying 'force' for 'time_s'.
	// Assumes constant inertia over the timestep. Exact for symmetric bodies (sphere, etc.) at
	// the model origin. For the general case, KE change ≈ Dot(v_mid, f) * dt (power formula).
	template <typename = void>
	float KineticEnergyChange(v8force force, v8force momentum0, InertiaInv const& inertia_inv, float time_s)
	{
		// Kinetic energy change:
		//    0.5 * (v1*I*v1 - v0*I*v0)
		//  = 0.5 * (v1.h1 - v0.h0)

		// Initial velocity
		auto velocity0 = inertia_inv * momentum0;

		// 'force' causes a change in momentum
		auto dmomentum = force * time_s;
		auto momentum1 = momentum0 + dmomentum;

		// Which corresponds to a change in velocity
		auto dvelocity = inertia_inv * dmomentum;
		auto velocity1 = velocity0 + dvelocity;

		// Kinetic energy
		auto ke = 0.5f * (Dot(velocity1, momentum1) - Dot(velocity0, momentum0));
		return ke;
	}

	// Evolve a rigid body forward in time. 
	template <typename = void>
	void Evolve(RigidBody& rb, float elapsed_seconds)
	{
		// World-frame equation of motion:
		//   dh/dt = f
		// where:
		//   h = spatial momentum at the model origin (world coordinates)
		//   f = net external spatial force at the model origin (world coordinates)
		//
		// Note: The body-frame equation is dh^B/dt = f^B - v^B ×* h^B (Featherstone RBDA).
		// In the world frame, the gyroscopic/Euler effects are captured by the time-varying
		// world-space inertia (which changes as the body rotates). The mid-step inertia
		// refinement below handles this implicitly.

		#if PR_DBG
		auto ke_before = rb.KineticEnergy();
		#endif

		// Notes:
		//  - The WS inertia depends on orientation which changes throughout the step due to the angular velocity of the body.
		//    Assuming the WS force is constant for the step, then the average momentum for the step is 'h = h0 + 0.5*t*Force'.
		//    Angular velocity = Iinv.h but I depends on orientation, so we need to approximate I at t = 0.5.
		//  - WS spatial vectors are all measured at the model origin

		auto ws_force = rb.ForceWS();
		auto ws_inertia_inv = rb.InertiaInvWS();
		auto ws_momentum = rb.MomentumWS() + ws_force * elapsed_seconds * 0.5f;

		// Refine ws_inertia_inv by estimating mid-step orientation
		for (int i = 0; i != 1; ++i)
		{
			auto ws_velocity = ws_inertia_inv * ws_momentum;
			auto dpos = ws_velocity * elapsed_seconds * 0.5f;
			auto do2w = m3x4::Rotation(dpos.ang);
			ws_inertia_inv = Rotate(ws_inertia_inv, do2w);
		}

		// Mid-step velocity using refined inertia
		auto ws_velocity = ws_inertia_inv * ws_momentum;
		auto dpos = ws_velocity * elapsed_seconds;

		#if PR_DBG
		// KE change ≈ power × time = Dot(v_mid, f) × dt.
		// This is second-order accurate for the midpoint scheme.
		auto ke_change = Dot(ws_velocity, ws_force) * elapsed_seconds;
		#endif

		// Update the position/orientation and momentum
		// 'dpos' is in world space, but is object relative so it cannot be applied as a single transform
		auto o2w = m4x4
		{
			m3x4::Rotation(dpos.ang) * rb.O2W().rot,
			dpos.lin                 + rb.O2W().pos
		};

		rb.O2W(o2w);
		rb.MomentumWS(rb.MomentumWS() + ws_force * elapsed_seconds);
		rb.ZeroForces();

		#if PR_DBG
		auto ke_after = rb.KineticEnergy();
		assert("Evolve has caused an unexpected change in kinetic energy" && FEqlRelative(ke_before + ke_change, ke_after, 0.1f * elapsed_seconds));
		#endif

		// Do this after the KE test because changing the orientation changes the KE.
		rb.O2W(Orthonorm(rb.O2W()));
	}
}

