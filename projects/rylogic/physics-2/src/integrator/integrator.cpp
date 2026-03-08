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
		// The velocity is at the CoM (inertia is block-diagonal, no coupling terms).
		// omega = Ic_inv * h_ang, v_com = h_lin / m.
		auto ws_inertia_inv = rb.InertiaInvWS();
		auto ws_velocity = ws_inertia_inv * rb.MomentumWS();

		// Compute CoM position before the step.
		// The body's O2W transform positions the model origin; CoM is offset from it.
		auto com_os = rb.CentreOfMassOS();
		auto com_ws = rb.O2W().pos + rb.O2W().rot * com_os;

		// Apply rotation
		auto drot = ws_velocity.ang * elapsed_seconds;
		auto new_rot = m3x4::Rotation(drot) * rb.O2W().rot;

		// Translate the CoM by the CoM velocity, then derive the model origin position
		// from the new rotation. This ensures the model origin orbits around the CoM
		// correctly when the body rotates.
		auto new_com_ws = com_ws + ws_velocity.lin * elapsed_seconds;
		auto new_pos = new_com_ws - new_rot * com_os;

		auto o2w = Orthonorm(m4x4{new_rot, new_pos});
		rb.O2W(o2w);

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
		auto inv_mass = dyn.os_com_and_invmass.w;
		auto os_com = v4{dyn.os_com_and_invmass.x, dyn.os_com_and_invmass.y, dyn.os_com_and_invmass.z, 0};

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

		// Compute velocity from momentum (block-diagonal — no coupling terms).
		// Since momentum/forces are about the CoM, the inverse inertia at the CoM
		// gives a simple decoupled relationship: omega = Ic_inv * h_ang, v = h_lin / m.
		auto vel_ang = ws_iinv * dyn.momentum_ang;
		auto vel_lin = inv_mass * dyn.momentum_lin;

		// CoM-based position update: translate CoM, derive model origin from new rotation.
		auto com_ws = rot * os_com;
		auto com_pos = pos + com_ws;

		auto dR = m3x4::Rotation(vel_ang * elapsed_seconds);
		auto new_rot = dR * rot;
		auto new_com_pos = com_pos + vel_lin * elapsed_seconds;
		auto new_pos = new_com_pos - new_rot * os_com;

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
