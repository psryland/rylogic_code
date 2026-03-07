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

	// CPU fallback for GPU integration path.
	// Performs the same Störmer-Verlet kick-drift-kick on a RigidBodyDynamics buffer entry.
	// This mirrors the GPU compute shader exactly, operating only on the flat buffer fields
	// so results can be compared 1:1 between CPU and GPU paths.
	void EvolveCPU(RigidBodyDynamics& dyn, float elapsed_seconds)
	{
		auto half_dt = elapsed_seconds * 0.5f;
		auto inv_mass = dyn.inertia_inv_com_and_invmass.w;

		// ---- Step 1: Half-kick ----
		dyn.momentum_ang += dyn.force_ang * half_dt;
		dyn.momentum_lin += dyn.force_lin * half_dt;

		// ---- Step 2: Drift ----

		// Build the object-space unit inverse inertia 3x3 from compact storage
		auto const& dia = dyn.inertia_inv_diagonal;
		auto const& off = dyn.inertia_inv_products;
		auto os_iinv_unit = m3x4
		{
			v4{dia.x, off.x, off.y, 0},
			v4{off.x, dia.y, off.z, 0},
			v4{off.y, off.z, dia.z, 0},
		};

		// Extract the current rotation and position from the transform
		auto rot = dyn.o2w.rot;
		auto pos = dyn.o2w.pos;

		// Rotate the inverse inertia from object space to world space: R * I * R^T
		auto b2a = InvertAffine(rot);
		auto ws_iinv_unit = rot * os_iinv_unit * b2a;

		// Symmetrize to counteract float drift
		ws_iinv_unit.x.y = ws_iinv_unit.y.x = 0.5f * (ws_iinv_unit.x.y + ws_iinv_unit.y.x);
		ws_iinv_unit.x.z = ws_iinv_unit.z.x = 0.5f * (ws_iinv_unit.x.z + ws_iinv_unit.z.x);
		ws_iinv_unit.y.z = ws_iinv_unit.z.y = 0.5f * (ws_iinv_unit.y.z + ws_iinv_unit.z.y);

		// Mass-scaled world-space inverse inertia
		auto ws_iinv = m3x4
		{
			ws_iinv_unit.x * inv_mass,
			ws_iinv_unit.y * inv_mass,
			ws_iinv_unit.z * inv_mass,
		};

		// Rotate centre of mass to world space
		auto os_com = v4{dyn.inertia_inv_com_and_invmass.x, dyn.inertia_inv_com_and_invmass.y, dyn.inertia_inv_com_and_invmass.z, 0};
		auto ws_com = rot * os_com;

		// Compute velocity from momentum using the spatial inverse inertia.
		// When CoM == 0, the 6x6 inverse inertia is block-diagonal and simplifies.
		// When CoM != 0, we need the full 6x6 spatial multiply:
		//   Io_inv = | Ic_inv       , -Ic_inv * cx       |
		//            | cx * Ic_inv  , 1/m - cx*Ic_inv*cx |
		// where cx is the cross-product matrix of 'com' (cx * v = cross(com, v)).
		v4 vel_ang, vel_lin;
		if (LengthSq(ws_com) < 1e-12f)
		{
			// Block-diagonal: no coupling between angular and linear
			vel_ang = ws_iinv * dyn.momentum_ang;
			vel_lin = inv_mass * dyn.momentum_lin;
		}
		else
		{
			// Full 6x6 spatial inverse inertia multiply.
			// Compute two intermediates to share work between angular and linear:
			auto Ic_inv_tau = ws_iinv * dyn.momentum_ang;         // Ic_inv * tau
			auto Ic_inv_cxf = ws_iinv * Cross(ws_com, dyn.momentum_lin); // Ic_inv * (com x f)

			// omega = Ic_inv * tau - Ic_inv * (com x f) = Ic_inv * (tau - com x f)
			vel_ang = Ic_inv_tau - Ic_inv_cxf;

			// v = com x (Ic_inv * tau) + f/m - com x (Ic_inv * (com x f))
			vel_lin = Cross(ws_com, Ic_inv_tau) + inv_mass * dyn.momentum_lin - Cross(ws_com, Ic_inv_cxf);
		}

		// Compute displacement
		auto drot_vec = vel_ang * elapsed_seconds;
		auto dpos_vec = vel_lin * elapsed_seconds;

		// Apply rotation and position update
		auto dR = m3x4::Rotation(drot_vec);
		auto new_rot = dR * rot;
		auto new_pos = pos + v4{dpos_vec.x, dpos_vec.y, dpos_vec.z, 0};

		// Orthonormalize the rotation and write back to the transform
		dyn.o2w = Orthonorm(m4x4{new_rot, new_pos});

		// ---- Step 3: Half-kick ----
		dyn.momentum_ang += dyn.force_ang * half_dt;
		dyn.momentum_lin += dyn.force_lin * half_dt;

		// Zero forces
		dyn.force_ang = v4{};
		dyn.force_lin = v4{};
	}
}
