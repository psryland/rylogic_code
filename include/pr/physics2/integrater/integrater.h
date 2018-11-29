//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"

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
			//   I = spatial inertia
			//   v = spatial velocity
			//   a = spatial acceleration
			//   Iv = momentum (h)
			//   x* = cross product for force spatial vectors
			// So:
			//   f = I*a + vx*.I.v
			//   I^ * f = a + I^ * (vx*.I.v)
			//   a = I^ * f -  I^ * (vx*.I.v)
			// where:
			//   I^ = inverse inertia

			#if PR_DBG
			auto ke_before = rb.KineticEnergy();
			auto ke_change = KineticEnergyChange(rb.ForceWS(), rb.InertiaInvWS(), elapsed_seconds);
			#endif

			// The WS inertia depends on orientation which changes throughout the step due to the angular velocity of the body.
			// Assuming the WS force is constant for the step, then the average momentum for the step is 'h = h0 + 0.5*t*Force'.
			// Angular velocity = I^.h but I depends on orientation, so we need to approximate I at t = 0.5.

			auto ws_force = rb.ForceWS();
			auto ws_inertia_inv = rb.InertiaInvWS();
			auto ws_momentum = rb.MomentumWS() + ws_force * elapsed_seconds * 0.5f;

			// Refine ws_inertia_inv
			for (int i = 0; i != 1; ++i)
			{
				auto ws_velocity = ws_inertia_inv * ws_momentum;
				auto dpos = ws_velocity * elapsed_seconds * 0.5f;
				auto do2w = m3x4::Rotation(dpos.ang);
				ws_inertia_inv = Transform(ws_inertia_inv, do2w);
			}

			// Apply the average momentum for the full step using the mid-step I
			auto ws_velocity = ws_inertia_inv * ws_momentum;
			auto dpos = ws_velocity * elapsed_seconds;
			auto do2w = m4x4::Transform(dpos.ang, dpos.lin.w1());

			// Update the position/orientation and momentum
			rb.O2W(rb.O2W() * do2w);
			rb.MomentumWS(rb.MomentumWS() + ws_force * elapsed_seconds);
			rb.ZeroForces();

			#if PR_DBG
			auto ke_after = rb.KineticEnergy();
			assert("Evolve has caused an unexpected change in kinetic energy" && FEql(abs(ke_after - ke_before), ke_change));
			#endif
		}

		// Calculate the change in kinetic energy caused by applying 'force' for 'time_s'
		inline float KineticEnergyChange(v8f const& force, InertiaInv const& inertia_inv, float time_s)
		{
			// Linear:
			//  KE = 0.5 m v² , F = m v/t => v = Ft/m
			//     = 0.5 m (Ft/m)²
			//     = 0.5 * t² * F² / m
			// Angular:
			//  KE = 0.5 w I w , T = I w/t => w = I^Tt
			//     = 0.5 I^Tt I I^Tt
			//     = 0.5 * t² * I^T T
			
			//auto ke_lin = 0.5f * Sqr(time_s) * Dot(force.lin,force.lin) / inertia_inv.Mass();
			//auto ke_ang = 0.5f * Sqr(time_s) * Dot(inertia_inv * force.ang, force.ang);
			//auto ke2 = ke_lin + ke_ang;
			
			auto ke = 0.5f * Sqr(time_s) * Dot(inertia_inv * force, force);
			return ke;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/physics2/shape/inertia_builder.h"

namespace pr::physics
{
	PRUnitTest(IntegratorTests)
	{
		auto mass = 5.0f;

		// Set up a rigid body at rest
		auto rb = RigidBody{};
		rb.SetMassProperties(InertiaBuilder::Sphere(1).ToInertia(mass));

		// Initial KE is zero because at rest
		auto ke0 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, 0), true);
		
		// Get it moving by applying forces/torques
		auto force = v8f{1,1,1, 1,1,-1};
		auto dke = KineticEnergyChange(force, rb.InertiaInvWS(), 1.0f);
		rb.ApplyForceWS(force);

		// Integrate
		Evolve(rb, 1.0f);

		ke0 += dke;
		auto ke1 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke1), true);

		// Integrate some more
		Evolve(rb, 1.0f);

		ke0 += 0;
		auto ke2 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke2), true);

		// Apply a force to stop the motion
		force = -rb.MomentumWS();
		dke = KineticEnergyChange(force, rb.InertiaInvWS(), 1.0f);
		rb.ApplyForceWS(force);

		// Integrate some more
		Evolve(rb, 1.0f);

		ke0 -= dke;
		auto ke3 = rb.KineticEnergy();
		PR_CHECK(FEql(ke0, ke3), true);

		PR_CHECK(FEql(ke0, 0), true);
	}
}
#endif
