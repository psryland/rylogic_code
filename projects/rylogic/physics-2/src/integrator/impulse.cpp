//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#include "pr/physics-2/integrator/impulse.h"

namespace pr::physics
{
	// Calculate the restitution impulse that resolves a collision between two rigid bodies.
	//
	// Overview:
	//   Given a contact between objA and objB (all data in objA's frame), this function
	//   computes the impulse needed to prevent interpenetration and apply restitution.
	//   The result is a pair of spatial force wrenches — one for each body — expressed
	//   at each body's model origin in its own object space.
	//
	// The approach:
	//   1. Compute the relative velocity at the contact point.
	//   2. Build the "collision mass matrix" that relates an impulse at the contact point
	//      to the resulting velocity change, accounting for both translational and rotational
	//      degrees of freedom.
	//   3. Decompose the impulse into normal and tangential components.
	//   4. Scale the normal component by (1 + elasticity) for restitution.
	//   5. Clamp the tangential component to the friction cone.
	//   6. Convert the point impulse to a spatial wrench at each body's origin.
	//
	ImpulsePair RestitutionImpulse(Contact const& c)
	{
		auto& objA = *c.m_objA;
		auto& objB = *c.m_objB;
		auto& pt = c.m_point_at_t;

		// Sanity check: the contact should be approaching, not separating
		#if PR_DBG
		auto rel_normal_velocity = Dot(c.m_velocity.LinAt(pt), c.m_axis);
		assert("Point of contact is moving out of collision" && rel_normal_velocity <= 0);
		#endif

		// Lever arms from each body's origin to the contact point (in A's frame).
		// These determine how much of the impulse goes into rotation vs translation.
		auto rA = pt - v4::Origin();
		auto rB = pt - c.m_b2a.pos;

		// Relative velocity of B w.r.t. A at the contact point.
		// V_inv is the full 3D velocity; Vn_inv is the component along the collision normal.
		auto V_inv  = c.m_velocity.LinAt(pt);
		auto Vn_inv = Dot(V_inv, c.m_axis) * c.m_axis;

		// Get the inverse inertia tensors as 3x3 matrices in A's frame.
		// For objA: already in its own frame, so no transform needed.
		// For objB: rotate B's inertia into A's frame using the rotational part of b2a.
		//   Important: we must NOT pass the full b2a transform (including position) because
		//   InertiaInvOS(m4x4) interprets the position as a CoM offset and applies the
		//   parallel axis theorem. The collision mass matrix formula already accounts for
		//   the positional offset through the CPM(rB) terms below.
		auto Ia_inv_3x3 = objA.InertiaInvOS().To3x3();
		auto Ib_inv_3x3 = objB.InertiaInvOS(c.m_b2a.rot).To3x3();

		// Build the collision inverse-mass matrix for each body.
		// This is the 3x3 matrix M⁻¹ such that: Δv_at_contact = M⁻¹ · J
		// where J is the impulse applied at the contact point.
		//
		// Derivation: for a rigid body, the velocity change at point P due to impulse J at P:
		//   Δv_P = J/m + (I⁻¹ · (r × J)) × r
		//        = (1/m · I₃ - [r]× · I⁻¹ · [r]×) · J
		//
		// where [r]× is the cross-product (skew-symmetric) matrix of the lever arm r.
		// Note: [r]× · I⁻¹ · [r]× is negative semi-definite, so the minus sign makes
		// the rotational contribution ADD to the compliance (the body is "softer" at
		// off-centre points because it can rotate as well as translate).
		auto col_Ia_inv = (1/objA.Mass()) * m3x4Identity - CPM(rA) * Ia_inv_3x3 * CPM(rA);
		auto col_Ib_inv = (1/objB.Mass()) * m3x4Identity - CPM(rB) * Ib_inv_3x3 * CPM(rB);
		auto col_I_inv = col_Ia_inv + col_Ib_inv;

		// The collision mass matrix (inverse of the compliance) maps velocity to impulse.
		auto col_I = Invert(col_I_inv);

		// Decompose the impulse into normal and tangential components.
		// impulse0: the impulse needed to kill ALL relative velocity (zero restitution).
		// impulseN: the normal component only (for zero restitution).
		// impulseT: the tangential (friction) component.
		auto impulse0 = -(col_I * V_inv);
		auto denom = Dot(c.m_axis, col_I_inv * c.m_axis);
		auto impulseN = Abs(denom) > maths::tiny<float> 
			? -(Dot(c.m_axis, V_inv) / denom) * c.m_axis 
			: v4{};
		auto impulseT = impulse0 - impulseN;

		// Apply restitution to the normal component.
		// For elasticity e: the post-collision normal velocity is -e times the pre-collision value.
		// This requires scaling the normal impulse by (1 + e). e=0 is perfectly inelastic
		// (objects stick together), e=1 is perfectly elastic (kinetic energy conserved).
		auto impulse4 = (1 + c.m_mat.m_elasticity_norm) * impulseN + impulseT;

		// Coulomb friction cone: the tangential impulse cannot exceed μ · |Jn|.
		// If it does, we clamp it, which models sliding friction. When friction is zero,
		// this effectively removes all tangential impulse, making the collision frictionless.
		{
			auto clamped_friction = pr::Min(c.m_mat.m_friction_static, 0.9999f);
			auto static_friction = clamped_friction / (1.000001f - clamped_friction);
			auto Jn = Dot(impulse4, c.m_axis);
			auto Jt = Sqrt(pr::Max(0.0f, LengthSq(impulse4) - Sqr(Jn)));
			if (Jt > static_friction * Abs(Jn))
			{
				Jt = static_friction * Abs(Jn);
				auto impulseT_lenSq = LengthSq(impulseT);
				if (impulseT_lenSq > Sqr(maths::tiny<float>))
					impulseT = Jt * (impulseT / Sqrt(impulseT_lenSq));
				impulse4 = (1 + c.m_mat.m_elasticity_norm) * impulseN + impulseT;
			}
		}

		// Convert the linear impulse at the contact point into a spatial force wrench
		// at A's model origin. Shift translates the pure force into force + torque:
		//   wrench.lin = impulse4  (force unchanged)
		//   wrench.ang = r × impulse4  (torque from the lever arm)
		auto impulse = Shift(v8force{v4{}, impulse4}, v4Origin - pt);

		// Build the impulse pair: equal and opposite wrenches for each body.
		// For objA: receives the negative (reaction) impulse, already in A's frame.
		// For objB: receives the positive (action) impulse, transformed from A's
		//   frame to B's frame. The spatial force transform handles both rotation
		//   of the vector components and the change of reference point (A origin → B origin).
		auto impulse_pair = ImpulsePair{};
		impulse_pair.m_os_impulse_objA = -impulse;
		impulse_pair.m_os_impulse_objB = InvertAffine(c.m_b2a) * +impulse;
		impulse_pair.m_contact = &c;
		return impulse_pair;
	}
}

