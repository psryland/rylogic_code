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
	float KineticEnergyChange(v8force force, v8force momentum0, InertiaInv const& inertia_inv, float time_s);

	// Evolve a rigid body forward in time using Störmer-Verlet (kick-drift-kick)
	// symplectic integration. This scheme exactly conserves a modified Hamiltonian
	// H_dt = H + O(dt²), preventing secular energy drift while maintaining second-order
	// accuracy. The structure is:
	//   Half-kick:  h += f * dt/2       (advance momentum by half-step)
	//   Drift:      v = Iinv(R) * h     (velocity from half-kicked momentum)
	//               R *= Rot(v.ang*dt)  (update orientation)
	//               x += v.lin * dt     (update position)
	//   Half-kick:  h += f * dt/2       (advance momentum by second half-step)
	void Evolve(RigidBody& rb, float elapsed_seconds);
}
