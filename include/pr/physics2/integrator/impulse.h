//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"
#include "pr/physics2/integrator/contact.h"

namespace pr::physics
{
	// Impulse Calculations:
	//'  Two objects; A and B, collide at 'p'
	//'  rA  = vector from A origin to 'p'
	//'  rB  = vector from B origin to 'p'
	//'  Va¯ = Velocity at 'p' before collision = VA + WA x rA = body A linear + angular velocity
	//'  Vb¯ = Velocity at 'p' before collision = VB + WB x rB = body B linear + angular velocity
	//'  Va† = Velocity at 'p' after collision = -J(1/ma + rA²/Ia) - Va¯    (in 3D rA²/Ia = -rA x Ia¯ x rA)
	//'  Vb† = Velocity at 'p' after collision = +J(1/mb + rB²/Ib) - Vb¯    (ma,mb = mass, Ia,Ib = inertia)
	//'  V¯  = Relative velocity at 'p' before collision = Vb¯ - Va¯
	//'  V†  = Relative velocity at 'p' after collision = Vb† - Va† = eV¯   (e = elasticity)
	//'      = J(1/mb + rB²/Ib) - Vb¯ + J(1/ma + rA²/Ia) + Va¯              (J = impulse)
	//'      = J(1/ma + 1/mb + rA²/Ia + rB²/Ib) - V¯= eV¯
	//'      = J(1/ma + 1/mb + rA²/Ia + rB²/Ib) = eV¯ + V¯ = (e + 1)V¯
	//'  J   = (e + 1) * (1/ma + 1/mb + rA²/Ia + rB²/Ib)¯¹ * V¯

	// Elasticity and friction:
	//  Elasticity is how bouncy a material is in the normal direction.
	//  Friction is how sticky a material is in the tangential direction.
	//  The normal and torsion components of the outbound velocity are controlled by elasticity.
	//  Friction is used to limit the size of the tangential component of the impulse which effects the
	//  outbound tangential velocity.
	//  See comments in the implementation below

	// Two equal, but opposite, impulses in object space, measured at the object model origin
	struct ImpulsePair
	{
		v8f m_os_impulse_objA;
		v8f m_os_impulse_objB;
		Contact const* m_contact;
	};

	// Calculate the impulse that will resolve the collision between two objects.
	inline ImpulsePair RestitutionImpulse(Contact const& c)
	{
		// Calculate the effective inertia at 'c.m_point'. This is not the sum of inertias
		// because, even though the bodies are in contact at 'c.m_point', the point has a
		// different velocity on each body.
		// Let:
		//'   +p, -p = the restitution impulse for each object (equal but opposite)
		//' dVa, dVb = the change in velocities for the objects
		//' Ia¯, Ib¯ = the inverse inertia for each object expressed at the collision point (in objA space) '
		//'  impulse = change in momentum; p = dH = I.dV '
		//'    Vdiff = dVb - dVa          '
		//'      dVa = -Ia¯.p             '
		//'      dVb = +Ib¯.p             '
		//'    Vdiff = (Ib¯.p + Ia¯.p)    '
		//'    Vdiff = (Ib¯ + Ia¯).p      '
		//' => p = (Ib¯ + Ia¯)¯.Vdiff     '

		// Debugging Tips:
		//  - Check the impulse for each object assuming the other object has infinite mass
		//    i.e. set one of Ia¯ or Ib¯ to zero.

		auto& objA = *c.m_objA;
		auto& objB = *c.m_objB;
		auto& pt = c.m_point_at_t;

		// Check the relative velocity is into the collision
		#if PR_DBG
		auto rel_normal_velocity = Dot(c.m_velocity.LinAt(pt), c.m_axis);
		assert("Point of contact is moving out of collision" && rel_normal_velocity <= 0);
		Dump(c);
		#endif

		// rA = vector from objA origin to 'p'
		auto rA = pt - v4Origin;

		// rB = vector from objB origin to 'p'
		auto rB = pt - c.m_b2a.pos;

		// V¯ = Relative velocity at 'p' before collision = Vb¯ - Va¯
		auto V¯  = c.m_velocity.LinAt(pt);

		// Vn¯ = Relative normal velocity at 'p' before the collision
		auto Vn¯ = Dot(V¯, c.m_axis) * c.m_axis;

		// The collision inertia contribution by each object
		auto col_Ia¯ = (1/objA.Mass()) * m3x4Identity - CPM(rA) * objA.InertiaInvOS().To3x3() * CPM(rA);
		auto col_Ib¯ = (1/objB.Mass()) * m3x4Identity - CPM(rB) * objB.InertiaInvOS(c.m_b2a).To3x3() * CPM(rB);
		auto col_I¯ = col_Ia¯ + col_Ib¯;
		auto col_I = Invert(col_I¯);
		
		// Get the impulse that would change the relative velocity at 'pt' to zero
		auto impulse0 = -(col_I * V¯);

		// Get the impulse that would reduce the normal component of the relative velocity at 'pt' to zero
		auto impulseN = -(Dot(c.m_axis, V¯) / Dot(c.m_axis, col_I¯ * c.m_axis)) * c.m_axis;

		// The difference is the impulse that would reduce the tangential component of the relative velocity at 'pt' to zero
		auto impulseT = impulse0 - impulseN;

		#if PR_DBG
		//{
		//	// Assert that impulse0 would kill the relative velocity
		//	auto imp = Shift(v8f{v4{}, impulse0}, v4Origin - pt);
		//	auto velA_ws = (objA.InertiaInvWS() * (objA.O2W().rot * (objA.MomentumOS() +                       -imp)));
		//	auto velB_ws = (objB.InertiaInvWS() * (objB.O2W().rot * (objB.MomentumOS() + InvertFast(c.m_b2a) * +imp)));
		//	
		//	auto pt_ws = objA.O2W() * pt;
		//	auto velA_at_pt_ws = velA_ws.LinAt(pt_ws - objA.O2W().pos);
		//	auto velB_at_pt_ws = velB_ws.LinAt(pt_ws - objB.O2W().pos);
		//	auto dvel_ws = (velB_at_pt_ws - velA_at_pt_ws);
		//	assert(FEqlRelative(Length(dvel_ws), 0, 0.0001f));
		//}
		#endif
		// TODO optimise...

		// Calculate the restitution impulse
		// Apply elasticity to the normal component of the impulse
		auto impulse4 = (1 + c.m_mat.m_elasticity_norm) * impulseN + (1 + c.m_mat.m_elasticity_norm) * impulseT;

		// Limit the normal and tangential components of 'impulse' to the friction cone.
		{
			// Scale 0->1 to 0->inf, 0.5->1.0f
			auto static_friction = c.m_mat.m_friction_static / (1.000001f - c.m_mat.m_friction_static);

			// If '|Jt|/|Jn|' (the ratio of tangential to normal magnitudes) is greater than static friction
			// then the contact 'slips' and the impulse is reduced in the tangential direction.
			auto Jn = Dot(impulse4, c.m_axis);
			auto Jt = Sqrt(LengthSq(impulse4) - Sqr(Jn));
			if (Jt > static_friction * Abs(Jn))
			{
				// Reduce the tangential component of the impulse
				Jt = static_friction * Abs(Jn);
				impulseT = Jt * Normalise(impulseT);
				impulse4 = (1 + c.m_mat.m_elasticity_norm) * impulseN + impulseT;
			}
		}

		auto impulse = Shift(v8f{v4{}, impulse4}, v4Origin - pt);

		auto impulse_pair = ImpulsePair{};
		impulse_pair.m_os_impulse_objA = -impulse;
		impulse_pair.m_os_impulse_objB = InvertFast(c.m_b2a) * +impulse;
		impulse_pair.m_contact = &c;
		return impulse_pair;
	}
}

#if 0
		auto& objA = *c.m_objA;
		auto& objB = *c.m_objB;

		// Scale 0->1 to 0->inf, 0.5->1.0f
		auto static_friction = c.m_mat.m_friction_static / (1.000001f - c.m_mat.m_friction_static);

		#if PR_DBG
		auto J = v4{};
		auto col_I = m3x4{};
		{// Calculate collision inertia the old way in objA space
			 //'  rA  = vector from A origin to 'p'
			auto rA = c.m_point.w0();
			
			//'  rB  = vector from B origin to 'p'
			auto rB = c.m_point - c.m_b2a.pos;

			//'  Va¯ = Velocity at 'p' before collision = VA + WA x rA = body A linear + angular velocity
			auto Va¯ = c.m_objA->VelocityOS().lin + Cross(c.m_objA->VelocityOS().ang, rA);

			//'  Vb¯ = Velocity at 'p' before collision = VB + WB x rB = body B linear + angular velocity
			auto Vb¯ = c.m_b2a * c.m_objB->VelocityOS().lin + Cross(c.m_b2a * c.m_objB->VelocityOS().ang, rB);

			//'  V¯  = Relative velocity at 'p' before collision = Vb¯ - Va¯
			auto V¯  = Vb¯ - Va¯;

			//'  Va† = Velocity at 'p' after collision = -J(1/ma + rA²/Ia) - Va¯    (in 3D rA²/Ia = -rA x Ia¯ x rA)
			//'  Vb† = Velocity at 'p' after collision = +J(1/mb + rB²/Ib) - Vb¯    (ma,mb = mass, Ia,Ib = inertia)
			//'  V†  = Relative velocity at 'p' after collision = Vb† - Va† = eV¯   (e = elasticity)
			//'      = J(1/mb + rB²/Ib) - Vb¯ + J(1/ma + rA²/Ia) + Va¯              (J = impulse)
			//'      = J(1/ma + 1/mb + rA²/Ia + rB²/Ib) - V¯= eV¯
			//'      = J(1/ma + 1/mb + rA²/Ia + rB²/Ib) = eV¯ + V¯ = (e + 1)V¯
			//'  J   = (e + 1) * (1/ma + 1/mb + rA²/Ia + rB²/Ib)¯¹ * V¯
			auto col_Ia¯ = (1/c.m_objA->Mass()) * m3x4Identity - CPM(rA) * c.m_objA->InertiaInvOS().To3x3() * CPM(rA);
			auto col_Ib¯ = (1/c.m_objB->Mass()) * m3x4Identity - CPM(rB) * c.m_objB->InertiaInvOS(c.m_b2a).To3x3() * CPM(rB);
			auto col_I¯ = col_Ia¯ + col_Ib¯;
			col_I = Invert(col_I¯);
			J = col_I * V¯;
		}
		#endif

		// Get the inertias in objA space and the collision inertia (a.k.a "K" matrix)
		auto inertiaA¯ = objA.InertiaInvOS(m3x4Identity).To6x6();
		auto inertiaB¯ = objB.InertiaInvOS(c.m_b2a).To6x6();
		auto inertia = Invert(inertiaB¯ + inertiaA¯);

		// Check the relative velocity is into the collision
		#if PR_DBG
		auto rel_normal_velocity = Dot(c.m_velocity.LinAt(c.m_point), c.m_axis);
		assert("Point of contact is moving out of collision" && rel_normal_velocity < 0);
		Dump(c);
		#endif

		// Assert that impulse0 would kill the relative velocity
		#if PR_DBG
		{
			auto imp = -(inertia * c.m_velocity);
			auto pt_ws = c.m_objA->O2W() * c.m_point;
			auto velA_ws = (c.m_objA->InertiaInvWS() * (c.m_objA->O2W().rot * (c.m_objA->MomentumOS() +                       -imp)));
			auto velB_ws = (c.m_objB->InertiaInvWS() * (c.m_objB->O2W().rot * (c.m_objB->MomentumOS() + InvertFast(c.m_b2a) * +imp)));
			auto velA_at_pt_ws = velA_ws.LinAt(pt_ws);
			auto velB_at_pt_ws = velB_ws.LinAt(pt_ws);
			auto dvel_ws = (velB_at_pt_ws - velA_at_pt_ws);
			assert(FEqlRelative(Length(dvel_ws), 0, 0.0001f));
		}
		#endif


		// Remove any tangential component of the relative angular velocity, as that
		// is already represented in the linear velocity. Leave only the torsional part.
		auto Vin = c.m_velocity;
		Vin = Shift(Vin, +c.m_point);
		Vin.ang = Dot(Vin.ang, c.m_axis) * c.m_axis;
		Vin = Shift(Vin, -c.m_point);

		//// Reflect the velocity through the collision normal
		//auto Vout = Reflect(Vin, c.m_axis);

		// Get the normal component of the inbound velocity
		auto Vn = Proj(Vin, c.m_axis);
		
		// Get the impulse needed to reduce the entire inbound velocity to zero
		auto impulse0 = -(inertia * Vin);

		// Assert that impulse0 would kill the relative velocity
		#if PR_DBG
		auto velA_ws = c.m_objA->O2W().rot * (c.m_objA->InertiaInvOS() * (c.m_objA->MomentumOS() + -impulse0));
		auto velB_ws = c.m_objB->O2W().rot * (c.m_objB->InertiaInvOS() * (c.m_objB->MomentumOS() + InvertFast(c.m_b2a) * +impulse0));
		auto vel_at_point = (velB_ws - velA_ws).LinAt(c.m_objA->O2W() * c.m_point);
		assert(FEqlRelative(Length(vel_at_point), 0, 0.0001f));
		#endif

		// Get the impulse needed to reduce the normal component of the inbound velocity to zero
		auto impulse_n = -(inertia * Vn);

		// The difference is the impulse that would reduce the tangential component of the outbound velocity to zero
		auto impulse_t = impulse0 - impulse_n;

		// Apply elasticity to the normal component of the impulse
		impulse_n = v8f{
			(1 + c.m_mat.m_elasticity_tors) * impulse_n.ang,
			(1 + c.m_mat.m_elasticity_norm) * impulse_n.lin};

		// Calculate the restitution impulse
		auto impulse = impulse_n + impulse_t;

		// Limit the normal and tangential components of 'impulse' to the friction cone.
		// If '|Jt|/|Jn|' (the ratio of tangential to normal magnitudes) is greater than static friction
		// then the contact 'slips' and the impulse is reduced in the tangential direction.
		auto impulse_at_contact = Shift(impulse, c.m_point);
		auto Jn = Dot(impulse_at_contact.lin, c.m_axis);
		auto Jt = Sqrt(LengthSq(impulse_at_contact.lin) - Sqr(Jn));
		if (Jt > static_friction * Abs(Jn))
		{
			// Reduce the tangential component of the impulse
			Jt = static_friction * Abs(Jn);

			// Get the tangent direction
			auto tang = Normalise(impulse_at_contact.lin - Jn * c.m_axis);
			impulse_at_contact.lin = Jn * c.m_axis + Jt * tang;

			// Shift the impulse back to objA space.
			impulse = Shift(impulse_at_contact, -c.m_point);
		}

		{
			std::string str;
			ldr::RigidBody(str, "body0", 0x80FF0000, *c.m_objA, ldr::ERigidBodyFlags::None, &m4x4Identity);
			ldr::RigidBody(str, "body1", 0x8000FF00, *c.m_objB, ldr::ERigidBodyFlags::None, &c.m_b2a);
			ldr::Arrow(str, "Normal", 0xFFFFFFFF, ldr::EArrowType::Fwd, c.m_point, c.m_axis * 0.1f, 5);
			ldr::VectorField(str, "VelocityBefore", 0xFFFFFF00, (v8)c.m_velocity * 0.1f, v4Origin, 2, 0.25f);
			//ldr::VectorField(str, "VelocityAfter", 0xFF00FFFF, (v8)(c.m_velocity + Vdiff) * 0.1f, v4Origin, 2, 0.25f);
			ldr::VectorField(str, "ImpulseField", 0xFF007F00, (v8)impulse * 0.1f, v4Origin, 5, 0.25f);
			ldr::Arrow(str, "Impulse", 0xFF0000FF, ldr::EArrowType::Fwd, c.m_point - impulse.lin, impulse.lin, 3);
			ldr::Arrow(str, "Twist", 0xFF000080, ldr::EArrowType::Fwd, c.m_point - impulse.ang, impulse.ang, 3);
			ldr::Write(str, L"\\dump\\collision.ldr");
		}

		auto impulse_pair = ImpulsePair{};
		impulse_pair.m_os_impulse_objA = -impulse;
		impulse_pair.m_os_impulse_objB = InvertFast(c.m_b2a) * +impulse;
		impulse_pair.m_contact = &c;
		return impulse_pair;
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::physics
{
	PRUnitTest(ImpulseTests)
	{
		ShapeSphere sph(0.5f);
		ShapeBox    box(v4{maths::inv_root2f, maths::inv_root2f, maths::inv_root2f, 0}, m4x4::Transform(0, 0, maths::tau_by_8f, v4Origin));
		RigidBody objA(&box, m4x4::Transform(0, 0, 0, v4{-0.5f, 0, 1, 1}), Inertia::Box(v4{0.5f,0.5f,0.5f,0}, 10.0f));
		RigidBody objB(&box, m4x4::Transform(0, 0, 0, v4{+0.5f, 0, 1, 1}), Inertia::Box(v4{0.5f,0.5f,0.5f,0}, 10.0f));

		Contact c(objA, objB);
		ImpulsePair impulse_pair;

		{
			// Normal collision through the CoM for both objects, unequal masses.
			objA.Mass(10);
			objB.Mass(5);
			objA.VelocityWS(v4{0, 0, 0, 0}, v4{+0, 0, 0, 0});
			objB.VelocityWS(v4{0, 0, 0, 0}, v4{-1, 0, 0, 0});
			c.m_axis = v4{1,0,0,0};
			c.m_point = v4{0.5f, 0, 0, 0};
			c.m_mat.m_friction_static =  0.0f; // frictionless
			c.m_mat.m_elasticity_norm = +1.0f; // elastic
			c.m_mat.m_elasticity_tors = +1.0f; // elastic
			c.update(0);
			Dump(c);

			impulse_pair = RestitutionImpulse(c);
			objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);
			auto velA = objA.VelocityWS();
			auto velB = objB.VelocityWS();
			PR_CHECK(FEql(velA, v8m{0,0,0, -2.0f/3.0f,0,0}), true);
			PR_CHECK(FEql(velB, v8m{0,0,0, +1.0f/3.0f,0,0}), true);
		}
		{
			// Normal collision, not through the CoM for both objects, unequal masses.
			objA.Mass(10);
			objB.Mass(5);
			objA.VelocityWS(v4{0, 0, 0, 0}, v4{+0, 0, 0, 0});
			objB.VelocityWS(v4{0, 0, 0, 0}, v4{-1,-1, 0, 0});
			c.m_axis = Normalise(v4{1,1,0,0});
			c.m_point = v4{0.5f, 0, 0, 0};
			c.m_mat.m_friction_static = 1.0f; // sticky
			c.m_mat.m_elasticity_norm = 1.0f; // elastic
			c.m_mat.m_elasticity_tors = 1.0f; // elastic
			c.update(0);
			Dump(c);

			// Tangential component is frictionless so velocity should be reflected
			impulse_pair = RestitutionImpulse(c);
			objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);
			auto velA = objA.VelocityWS();
			auto velB = objB.VelocityWS();
			PR_CHECK(FEql(velA, v8m{0,0,-1.14286f, -2.0f/3.0f,-0.285714f, 0}), true);
			PR_CHECK(FEql(velB, v8m{0,0,-1.14286f, +1.0f/3.0f,-0.428572f, 0}), true);
		}
		{
			// Off-Normal collision, not through the CoM for both objects, equal masses.
			objA.Mass(10);
			objB.Mass(10);
			objA.VelocityWS(v4{0, 0, 0, 0}, v4{+0, 0, 0, 0});
			objB.VelocityWS(v4{0, 0, 0, 0}, v4{-1,-1, 0, 0});
			c.m_axis = Normalise(v4{Cos(maths::tauf/16), Sin(maths::tauf/16),0,0});
			c.m_point = v4{0.5f, 0, 0, 0};
			c.m_mat.m_friction_static = 0.0f; // frictionless
			c.m_mat.m_elasticity_norm = 1.0f; // elastic
			c.m_mat.m_elasticity_tors = -1.0f; // anti-elastic
			c.update(0);
			Dump(c);

			// Tangential component is frictionless so velocity should be reflected
			impulse_pair = RestitutionImpulse(c);
			objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);
			auto velA = objA.VelocityWS();
			auto velB = objB.VelocityWS();
			//PR_CHECK(FEql(velA, v8m{0,0,-0.228571f, -0.733333f,-0.228571f, 0}), true);
			//PR_CHECK(FEql(velB, v8m{0,0,-2.28571f, +0.466666f,-0.542857f, 0}), true);

			auto dvel = c.m_b2a * objB.VelocityOS() - objA.VelocityOS();
			auto vout = dvel.LinAt(c.m_point);
			auto expected_vout = v4{-1,-1,0,0} - 2 * Dot(v4{-1,-1,0,0}, c.m_axis) * c.m_axis;
			PR_CHECK(FEql(vout, expected_vout), true);
		}
		{
			// Tangential impulse. Glancing collision from two rotating, but not translating, objects.
			objA.Mass(10);
			objB.Mass(10);
			objA.VelocityWS(v4{0, 0, -1, 0}, v4{});
			objB.VelocityWS(v4{0, 0, +1, 0}, v4{-maths::tiny, 0, 0, 0});
			c.m_axis = v4{1,0,0,0};
			c.m_point = v4{0.5f, 0, 0, 0};
			c.m_mat.m_friction_static = 1.0f; // sticky
			c.m_mat.m_elasticity_norm = 1.0f; // elastic
			c.m_mat.m_elasticity_tors = 1.0f; // elastic
			c.update(0);
			Dump(c);

			// Collision should produce a near-zero impulse because the collision is
			// friction less, there is a tiny normal component, and the relative
			// velocity at the contact point is nearly zero.
			impulse_pair = RestitutionImpulse(c);
			PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objA, v8f{}, 0.001f), true);
			PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objB, v8f{}, 0.001f), true);
		}
		{
			// Change the angular velocity so that the collision point now does have opposing velocity.
			objA.Mass(10);
			objB.Mass(10);
			objA.VelocityWS(v4{0, 0, +1, 0}, v4{});
			objB.VelocityWS(v4{0, 0, +1, 0}, v4{-maths::tiny, 0, 0, 0});
			c.m_axis = v4{1,0,0,0};
			c.m_point = v4{0.5f, 0, 0, 0};
			c.m_mat.m_friction_static = 1.0f; // sticky
			c.m_mat.m_elasticity_norm = 1.0f; // elastic
			c.m_mat.m_elasticity_tors = 1.0f; // elastic
			c.update(0);
			Dump(c);

			// Impulses are still near zero because the normal component of the impulse is zero
			impulse_pair = RestitutionImpulse(c);
			PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objA, v8f{0,0,-2, 0,-4,0}, 0.001f), true);
			PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objB, v8f{0,0,-2, 0,+4,0}, 0.001f), true);
		}
		//{
		//	// Same setup, but with elastic tangential should still be zero because
		//	// the relative velocity is nearly zero at the contact point
		//	objA.Mass(10);
		//	objB.Mass(10);
		//	objA.VelocityWS(v4{0, 0, -1, 0}, v4{});
		//	objB.VelocityWS(v4{0, 0, +1, 0}, v4{-maths::tiny, 0, 0, 0});
		//	c.m_axis = v4{1,0,0,0};
		//	c.m_point = v4{0.5f, 0, 0, 0};
		//	c.m_mat.m_friction_static = 1.0f;
		//	c.m_mat.m_elasticity_norm = 1.0f; // elastic
		//	c.m_mat.m_elasticity_tors = -1.0f; // elastic
		//	c.update();
		//	Dump(c);

		//	impulse_pair = RestitutionImpulse(c);
		//	PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objA, v8f{}, 0.001f), true);
		//	PR_CHECK(FEqlRelative(impulse_pair.m_os_impulse_objB, v8f{}, 0.001f), true);
		//}
			//{
			//	// Glancing collision from two rotating, but not translating, objects.
			//	// Collision should reverse the rotations because the collision is tangentially elastic.
			//	RigidBody objA(&sph, m4x4::Transform(0, 0,                0, v4{-0.5f, 0, 1, 1}), Inertia::Sphere(0.5f, 10.0f));
			//	RigidBody objB(&box, m4x4::Transform(0, 0, maths::tau_by_8f, v4{+0.5f, 0, 1, 1}), Inertia::Sphere(0.5f, 10.0f));//Inertia::Box(v4{0.5f,0.5f,0.5f,0}, 10.0f));
			//
			//	// Spin the same way so that the contact point has opposing velocity.
			//	objA.VelocityWS(v4{0, 0, +1, 0}, v4{});
			//	objB.VelocityWS(v4{0, 0, +1, 0}, v4{});
			//
			//	Contact c(objA, objB);
			//	c.m_axis = v4{1,0,0,0};
			//	c.m_point = v4{0.5f, 0, 0, 0};
			//	c.m_velocity = c.m_b2a * objB.VelocityOS() - objA.VelocityOS();
			//	c.m_mat.m_elasticity_norm = 1.0f; // elastic
			//	c.m_mat.m_elasticity_tang = -1.0f; // elastic
			//
			//	Dump(c);
			//
			//	auto impulse_pair = RestitutionImpulse(c);
			//	objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			//	objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);
			//
			//	auto velA = objA.VelocityWS();
			//	auto velB = objB.VelocityWS();
			//	PR_CHECK(FEql(velA, v8m{0, 0, -0.428571f,  0, -0.285714f, +0}), true);
			//	PR_CHECK(FEql(velB, v8m{0, 0, -0.428571f,  0, +0.285714f, +0}), true);
			//}
			//{// ObjA and ObjB rotating in opposite directions, should stop after collision impulses
			//	ShapeSphere sph(0.5f);
			//	RigidBody objA(&sph, m4x4::Transform(0, 0, maths::tau_by_8f, v4{-0.5f, 0, 1, 1}), Inertia::Sphere(0.5f, 10.0f));
			//	RigidBody objB(&sph, m4x4::Transform(0, 0, maths::tau_by_8f, v4{+0.5f, 0, 1, 1}), Inertia::Sphere(0.5f, 10.0f));
			//
			//	objA.VelocityWS(v4{0, 0, -1, 0}, v4{});
			//	objB.VelocityWS(v4{0, 0, +1, 0}, v4{});
			//
			//	Contact c(objA, objB);
			//	c.m_axis = v4{1,0,0,0};
			//	c.m_point = v4{0.5f, 0, 0, 0};
			//	c.m_velocity = c.m_b2a * objB.VelocityOS() - objA.VelocityOS();
			//	c.m_mat.m_elasticity_norm = 1.0f;
			//	c.m_mat.m_elasticity_tang = 0.0f;
			//
			//	auto impulse_pair = RestitutionImpulse(c);
			//	objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			//	objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);
			//
			//	// 
			//	PR_CHECK(FEql(objA.O2W().pos, v4{-0.5f, 0, 1, 1}), true);
			//	PR_CHECK(FEql(objB.O2W().pos, v4{+0.5f, 0, 1, 1}), true);
			//	PR_CHECK(FEql(objA.VelocityWS(), v8m{}), true);
			//	PR_CHECK(FEql(objB.VelocityWS(), v8m{}), true);
			//}
	}
}
#endif