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
	// Two equal, but opposite, impulses in object space, measured at the object model origin
	struct ImpulsePair
	{
		v8f m_os_impulse_objA;
		v8f m_os_impulse_objB;
		Contact const* m_contact;
	};

	// Calculate the impulse that will resolve the collision between two objects.
	inline ImpulsePair RestitutionImpulseWS(Contact const& c)
	{
		// Debugging Tips:
		//  - Check the impulse for each object assuming the other object has infinite mass
		//    i.e. set one of Ia¯ or Ib¯ to zero.
		//  - We don't actually need the collision point. 'c.m_velocity' is the relative velocity
		//    between the objects **as a vector field**. We calculate the spatial impulse that would
		//    cause the required relative velocity change, and apply it to each object's momentum field.

		//auto& hack = const_cast<Contact&>(c);
		//hack.m_axis = v4{1,0,0,0};
		//hack.m_point = v4{0.5f,0,0,1};
		//hack.m_depth = 0;
		//auto vel_at_collision_point = c.m_velocity.LinAt(c.m_point);

		// Reflect the velocity through the collision normal
		auto Vin = c.m_velocity;
		auto Vout = Reflect(Vin, c.m_axis);

		// Decompose the velocity into normal and tangential parts
		auto Vn = Proj(Vout, c.m_axis);
		auto Vt = Vout - Vn;

		// Apply elasticity to the outbound velocity
		auto Vout_inelastic = c.m_mat.m_elasticity_norm * Vn + c.m_mat.m_elasticity_tang * Vt;

		// Get the change in velocity of the collision point (in objA space)
		auto Vdiff = Vout_inelastic - Vin;
		
		assert("Linear velocity out > velocity in" && Length(Vdiff.lin) <= 2*Length(Vin.lin));
		assert("Angular velocity out > velocity in" && Length(Vdiff.ang) <= 2*Length(Vin.ang));

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

		auto& objA = *c.m_objA;
		auto& objB = *c.m_objB;

		// Get the inertias in objA space and the collision inertia (a.k.a "K" matrix)
		auto inertiaA¯ = objA.InertiaInvOS(m3x4Identity).To6x6();
		auto inertiaB¯ = objB.InertiaInvOS(c.m_b2a).To6x6();
		auto inertia = Invert(inertiaB¯ + inertiaA¯);

		// Calculate the impulse needed to cause 'Vdiff'
		auto os_impulse = inertia * Vdiff;

		// Limit the normal and tangential components of 'os_impulse' to the friction cone

		//{
		//	std::string str;
		//	ldr::RigidBody(str, "body0", 0x8000FF00, *c.m_objA, 0.1f, ldr::ERigidBodyFlags::None, &m4x4Identity);
		//	ldr::RigidBody(str, "body1", 0x80FF0000, *c.m_objB, 0.1f, ldr::ERigidBodyFlags::None, &c.m_b2a);
		//	ldr::VectorField(str, "Velocity", 0xFFFFFF00, (v8)c.m_velocity * 0.1f, v4Origin, 2, 0.25f);
		//	ldr::VectorField(str, "ImpulseField", 0xFF00FFFF, (v8)os_impulse * 0.1f, v4Origin, 5, 0.25f);
		//	ldr::Arrow(str, "Impulse", 0xFF0000FF, ldr::EArrowType::Fwd, c.m_point - os_impulse.lin, os_impulse.lin, 3);
		//	ldr::Arrow(str, "Twist", 0xFF000080, ldr::EArrowType::Fwd, c.m_point - os_impulse.ang, os_impulse.ang, 3);
		//	ldr::Write(str, L"P:\\dump\\collision_restitution.ldr");
		//}

		auto imp_at_col = os_impulse.LinAt(c.m_point);

		auto impulse_pair = ImpulsePair{};
		impulse_pair.m_os_impulse_objA = -os_impulse;
		impulse_pair.m_os_impulse_objB = InvertFast(c.m_b2a) * +os_impulse;
		impulse_pair.m_contact = &c;
		return impulse_pair;
	}
}

#if 0

auto pt_ws = objA.O2W() * c.m_point;
auto rA = pt_ws - objA.O2W().pos;
auto rB = pt_ws - objB.O2W().pos;
auto im1 = (1/objA.Mass())*m3x4Identity - CPM(rA)*objA.InertiaInvWS().To3x3()*CPM(rA);
auto im2 = (1/objB.Mass())*m3x4Identity - CPM(rB)*objA.InertiaInvWS().To3x3()*CPM(rB);
auto im = im1 + im2;
auto m = Invert(im);
auto vel = c.m_velocity.LinAt(c.m_point);
auto P2 = m * vel;

// p1 is the impulse required to reduce the normal component of the relative velocity to zero.
// p2 is the impulse to reduce rel_velocity to zero
// See article: A New Algebraic Rigid Body Collision Law Based On Impulse Space Considerations
auto p1    = v8f{};//(c.m_rel_norm_speed / Dot3(c.m_normal, inv_mass * c.m_normal)) * c.m_normal;
auto p2    = inertia * c.m_velocity;
auto Pdiff = p2 - p1;

// Create an impulse 
auto& mat = c.m_mat;
auto impulse = v8f{v4{}, 1 * P2};
//auto impulse = (1.0f + mat.m_elasticity) * p1 + (1.0f + mat.m_tangential_elasticity) * Pdiff;

// Clip this impulse to the friction cone
//auto impulse_n = Dot3(contact.m_normal,  ws_impulse);
//v4 ws_impulse_t	= impulse_n * contact.m_normal - ws_impulse;
//float impulse_t = ws_impulse_t.Length3();
//if( Abs(impulse_t) > Abs(impulse_n * contact.m_static_friction) )
//{
//	float kappa = contact.m_dynamic_friction * (1.0f + contact.m_elasticity_n) * Dot3(contact.m_normal, Pi) / (impulse_t - contact.m_dynamic_friction * Dot3(Pdiff, contact.m_normal));
//	ws_impulse = (1.0f + contact.m_elasticity_n) * Pi + kappa * Pdiff;
//}
//PR_ASSERT(PR_DBG_PHYSICS, IsFinite(ws_impulse, ph::OverflowValue));
		
return impulse;
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::physics
{
	PRUnitTest(ImpulseTests)
	{
		// Notes:
		// - objects spaces not aligned with world space

		{// Tangential impulse
			{
			}
			{// ObjA and ObjB rotating in opposite directions, should stop after collision impulses
				ShapeSphere sph(0.5f);
				RigidBody objA(&sph, m4x4Identity, Inertia::Sphere(0.5f, 10.0f));
				RigidBody objB(&sph, m4x4Identity, Inertia::Sphere(0.5f, 10.0f));

				objA.O2W(m4x4::Transform(0, 0, maths::tau_by_8f, v4{-0.5f, 0, 1, 1}));
				objB.O2W(m4x4::Transform(0, 0, maths::tau_by_8f, v4{+0.5f, 0, 1, 1}));
				objA.VelocityWS(v4{0, 0, -1, 0}, v4{});
				objB.VelocityWS(v4{0, 0, +1, 0}, v4{});

				Contact c(objA, objB);
				c.m_axis = v4{1,0,0,0};
				c.m_point = v4{0.5f, 0, 0, 0};
				c.m_velocity = c.m_b2a * objB.VelocityOS() - objA.VelocityOS();
				c.m_mat.m_elasticity_norm = 1.0f;
				c.m_mat.m_elasticity_tang = 0.0f;

				auto impulse_pair = RestitutionImpulseWS(c);
				objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
				objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);

				// 
				PR_CHECK(FEql(objA.O2W().pos, v4{-0.5f, 0, 1, 1}), true);
				PR_CHECK(FEql(objB.O2W().pos, v4{+0.5f, 0, 1, 1}), true);
				PR_CHECK(FEql(objA.VelocityWS(), v8m{}), true);
				PR_CHECK(FEql(objB.VelocityWS(), v8m{}), true);
			}
		}
	}
}
#endif