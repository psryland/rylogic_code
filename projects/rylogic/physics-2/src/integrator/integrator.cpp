//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#include "pr/physics-2/integrator/integrator.h"

namespace pr::physics
{
	// Calculate the signed change in kinetic energy caused by applying 'force' for 'time_s'.
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

	// Evolve a rigid body forward in time using Störmer-Verlet (kick-drift-kick)
	// symplectic integration.
	void Evolve(RigidBody& rb, float elapsed_seconds)
	{
		auto ws_force = rb.ForceWS();
		auto half_impulse = ws_force * (elapsed_seconds * 0.5f);

		// Step 1: Half-kick — advance momentum by half-step
		rb.MomentumWS(rb.MomentumWS() + half_impulse);

		// Step 2: Drift — advance position/orientation using the half-kicked momentum.
		// The velocity is computed from the current orientation's inertia and the
		// half-stepped momentum. This is the key property of the Verlet scheme:
		// the position update uses the "midpoint" momentum.
		auto ws_inertia_inv = rb.InertiaInvWS();
		auto ws_velocity = ws_inertia_inv * rb.MomentumWS();
		auto dpos = ws_velocity * elapsed_seconds;

		auto o2w = m4x4
		{
			m3x4::Rotation(dpos.ang) * rb.O2W().rot,
			dpos.lin                 + rb.O2W().pos
		};
		rb.O2W(Orthonorm(o2w));

		// Step 3: Half-kick — advance momentum by second half-step
		rb.MomentumWS(rb.MomentumWS() + half_impulse);
		rb.ZeroForces();

		#if PR_DBG
		{
			// Sanity checks on the post-integration state:
			// 1. Verify no NaN crept in during integration
			auto h = rb.MomentumWS();
			auto rb_o2w = rb.O2W();
			assert("Evolve: NaN in momentum" && !IsNaN(h.ang) && !IsNaN(h.lin));
			assert("Evolve: NaN in transform" && !IsNaN(rb_o2w));

			// 2. Verify the orientation is still orthonormal (Orthonorm shouldn't need
			//    to make large corrections — if it does, the angular velocity is too high
			//    for the timestep or there's an integration bug)
			auto rot = rb_o2w.rot;
			assert("Evolve: orientation not orthonormal" && IsOrthonormal(rot));

			// 3. Verify the inertia inverse is still valid after rotation
			auto ws_iinv = rb.InertiaInvWS();
			assert("Evolve: invalid inertia after rotation" && ws_iinv.Check());
		}
		#endif
	}
}
