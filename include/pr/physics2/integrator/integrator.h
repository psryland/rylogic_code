//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"

namespace pr::physics
{
	// Calculate the signed change in kinetic energy caused by applying 'force' for 'time_s'
	template <typename = void>
	float KineticEnergyChange(v8f force, v8f momentum0, InertiaInv const& inertia_inv, float time_s)
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
		// Equation of Motion:
		//   f = d(Iv)/dt = I*a + vx*.I.v
		// where:
		//   f = net spatial force acting
		//   I = spatial inertia
		//   v = spatial velocity
		//   a = spatial acceleration
		//   Iv = momentum (h)
		//   x* = cross product for force spatial vectors
		// So:
		//   f = I*a + vx*.I.v
		//   I¯ * f = a + I¯ * (vx*.I.v)
		//   a = I¯ * f -  I¯ * (vx*.I.v)
		// where:
		//   I¯ = inverse inertia

		#if PR_DBG
		auto ke_before = rb.KineticEnergy();
		auto ke_change = KineticEnergyChange(rb.ForceWS(), rb.MomentumWS(), rb.InertiaInvWS(), elapsed_seconds);
		#endif

		// Notes:
		//  - The WS inertia depends on orientation which changes throughout the step due to the angular velocity of the body.
		//    Assuming the WS force is constant for the step, then the average momentum for the step is 'h = h0 + 0.5*t*Force'.
		//    Angular velocity = I¯.h but I depends on orientation, so we need to approximate I at t = 0.5.
		//  - WS spatial vectors are all measured at the model origin

		auto ws_force = rb.ForceWS();
		auto ws_inertia_inv = rb.InertiaInvWS();
		auto ws_momentum = rb.MomentumWS() + ws_force * elapsed_seconds * 0.5f;

		// Refine ws_inertia_inv
		for (int i = 0; i != 1; ++i)
		{
			auto ws_velocity = ws_inertia_inv * ws_momentum;
			auto dpos = ws_velocity * elapsed_seconds * 0.5f;
			auto do2w = m3x4::Rotation(dpos.ang);
			ws_inertia_inv = Rotate(ws_inertia_inv, do2w);
		}

		// Apply the average momentum for the full step using the mid-step I
		auto ws_velocity = ws_inertia_inv * ws_momentum;
		auto dpos = ws_velocity * elapsed_seconds;

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
		assert("Evolve has caused an unexpected change in kinetic energy" && FEqlRelative(ke_before + ke_change, ke_after, 0.01f * elapsed_seconds));
		#endif

		// Do this after the KE test because changing the orientation changes the KE.
		rb.O2W(Orthonorm(rb.O2W()));
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/physics2/shape/inertia.h"

namespace pr::physics
{
	PRUnitTest(IntegratorTests)
	{
		auto mass = 5.0f;
		auto force = v8f{1,1,1, 1,1,-1};

		// Set up a rigid body at rest
		auto rb = RigidBody{};
		rb.SetMassProperties(Inertia::Sphere(1, mass));

		// Initial KE should be zero
		auto ke0 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, 0), true);
		
		// Get it moving by applying forces/torques
		auto dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
		rb.ApplyForceWS(force);
		Evolve(rb, 1.0f);

		// KE Gained
		ke0 += dke;
		auto ke1 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke1), true);

		// More force
		dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
		rb.ApplyForceWS(force);
		Evolve(rb, 1.0f);

		// KE Gained again
		ke0 += dke;
		auto ke2 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke2), true);

		// No force
		dke = 0;
		Evolve(rb, 1.0f);

		// KE unchanged
		ke0 += dke;
		auto ke3 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke3), true);

		// Apply a force to stop the motion
		force = -rb.MomentumWS();
		dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
		rb.ApplyForceWS(force);
		Evolve(rb, 1.0f);

		// KE lost
		ke0 += dke;
		auto ke4 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke4), true);

		// KE back to zero
		PR_CHECK(FEql(ke0, 0), true);
	}
}
#endif
