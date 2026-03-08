//*********************************************
// Physics Engine — GPU Integration Compute Shader
//  Copyright (C) Rylogic Ltd 2025
//*********************************************
// Störmer-Verlet (kick-drift-kick) symplectic integrator running on the GPU.
// Each thread processes one rigid body. The algorithm is identical to the CPU
// Evolve() function in integrator.cpp — see that file for detailed comments.
//
// Buffer layout:
//   u0: RWStructuredBuffer<RigidBodyDynamics> — per-body dynamic state (read/write)
//   u1: RWStructuredBuffer<IntegrateOutput>   — per-body KE debug output (write)
//   b0: cbuffer with time step and body count
//
// Matrix convention (row-vector / DirectX-style):
//   C++ m4x4 stores columns contiguously as x, y, z, w members.
//   HLSL 'row_major float4x4' maps each row to one C++ column.
//   For vector transforms: mul(v, M) applies the rotation/transform.
//   For matrix compose:    mul(A, B) composes A then B (C++ equivalent: B_cpp * A_cpp).
//   See spatial_algebra.hlsli for full convention documentation.

#include "core.hlsli"
#include "spatial_algebra.hlsli"

// Must match the C++ RigidBodyDynamics struct exactly (176 bytes per element).
struct RigidBodyDynamics
{
	// Object-to-world transform. Each HLSL row = one C++ column vector.
	// Row 0-2 = rotation basis vectors, Row 3 = position (w=1).
	row_major float4x4 o2w;

	// World-space spatial momentum [angular, linear] at centre of mass
	float4 momentum_ang;
	float4 momentum_lin;

	// World-space force accumulator [angular, linear] at centre of mass
	float4 force_ang;
	float4 force_lin;

	// Object-space inverse inertia (compact symmetric 3x3)
	float4 inertia_inv_diagonal;  // {Ixx_inv, Iyy_inv, Izz_inv, 0}
	float4 inertia_inv_products;  // {Ixy_inv, Ixz_inv, Iyz_inv, 0}
	float4 os_com_and_invmass;    // {com_x, com_y, com_z, inv_mass}
};

// Per-body output for debug energy validation
struct IntegrateOutput
{
	float ke_before;
	float ke_after;
	float pad0;
	float pad1;
};

// Shader resources
RWStructuredBuffer<RigidBodyDynamics> g_bodies : register(u0);
RWStructuredBuffer<IntegrateOutput> g_output : register(u1);

// Integration parameters
cbuffer cbIntegrate : register(b0)
{
	float g_dt;
	uint g_body_count;
	uint g_pad0;
	uint g_pad1;
};

[numthreads(64, 1, 1)]
void CSIntegrate(uint3 dtid : SV_DispatchThreadID)
{
	uint idx = dtid.x;
	if (idx >= g_body_count)
		return;

	// Load the body's dynamic state
	RigidBodyDynamics body = g_bodies[idx];
	float half_dt = g_dt * 0.5f;
	float inv_mass = body.os_com_and_invmass.w;
	float3 os_com = body.os_com_and_invmass.xyz;

	// ---- Step 1: Half-kick — advance momentum by half the force impulse ----
	body.momentum_ang += body.force_ang * half_dt;
	body.momentum_lin += body.force_lin * half_dt;

	// ---- Step 2: Drift — update position and orientation ----

	// Build the object-space unit inverse inertia (not mass-scaled)
	float3x3 os_iinv_unit = build_symmetric_3x3(
		body.inertia_inv_diagonal.xyz,
		body.inertia_inv_products.xyz);

	// Extract the 3x3 rotation from the transform (rows = basis vectors in row-vector convention)
	float3x3 rot = (float3x3)body.o2w;

	// Rotate the inverse inertia from object space to world space:
	//   I⁻¹_ws = R * I⁻¹_os * Rᵀ
	float3x3 ws_iinv_unit = rotate_inertia_inv(os_iinv_unit, rot);

	// Mass-scaled world-space inverse inertia
	float3x3 ws_iinv = inv_mass * ws_iinv_unit;

	// Compute KE before drift (for debug output).
	// Momentum is at CoM, inertia is block-diagonal — no coupling.
	float3 vel_ang_pre = mul(ws_iinv, body.momentum_ang.xyz);
	float3 vel_lin_pre = inv_mass * body.momentum_lin.xyz;
	float ke_before = 0.5f * spatial_dot(vel_ang_pre, vel_lin_pre,
		body.momentum_ang.xyz, body.momentum_lin.xyz);

	// Compute velocity from momentum (block-diagonal — no coupling terms).
	// omega = Ic_inv * h_ang, v_com = h_lin / m.
	float3 vel_ang = mul(ws_iinv, body.momentum_ang.xyz);
	float3 vel_lin = inv_mass * body.momentum_lin.xyz;

	// Compute the angular displacement (axis-angle)
	float3 drot = vel_ang * g_dt;

	// Apply rotation: R_new = Rodrigues(drot) * R_old
	// In row-vector convention (rows = basis vectors): new_rot = mul(rot, dR)
	float3x3 dR = rodrigues_rotation(drot);
	float3x3 new_rot = mul(rot, dR);

	// Re-orthonormalize to prevent drift accumulation
	new_rot = orthonorm3x3(new_rot);

	// CoM-based position update: translate CoM, derive model origin from new rotation.
	float3 com_ws = mul(os_com, rot);         // world-space CoM offset
	float3 com_pos = body.o2w[3].xyz + com_ws; // world-space CoM position
	float3 new_com_pos = com_pos + vel_lin * g_dt;
	float3 new_pos = new_com_pos - mul(os_com, new_rot);

	// Write back the updated transform
	body.o2w[0] = float4(new_rot[0], 0);
	body.o2w[1] = float4(new_rot[1], 0);
	body.o2w[2] = float4(new_rot[2], 0);
	body.o2w[3] = float4(new_pos, 1);

	// ---- Step 3: Half-kick — advance momentum by second half ----
	body.momentum_ang += body.force_ang * half_dt;
	body.momentum_lin += body.force_lin * half_dt;

	// Zero forces (they must be re-applied each frame)
	body.force_ang = float4(0, 0, 0, 0);
	body.force_lin = float4(0, 0, 0, 0);

	// ---- Debug: compute KE after integration ----
	// Block-diagonal: no coupling, velocity is simple.
	float3x3 new_ws_iinv_unit = rotate_inertia_inv(os_iinv_unit, new_rot);
	float3x3 new_ws_iinv = inv_mass * new_ws_iinv_unit;
	float3 vel_ang_post = mul(new_ws_iinv, body.momentum_ang.xyz);
	float3 vel_lin_post = inv_mass * body.momentum_lin.xyz;
	float ke_after = 0.5f * spatial_dot(vel_ang_post, vel_lin_post,
		body.momentum_ang.xyz, body.momentum_lin.xyz);

	// Write results
	g_bodies[idx] = body;
	g_output[idx].ke_before = ke_before;
	g_output[idx].ke_after = ke_after;
	g_output[idx].pad0 = 0;
	g_output[idx].pad1 = 0;
}
