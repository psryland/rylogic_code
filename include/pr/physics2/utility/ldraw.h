#pragma once
#include "pr/physics2/forward.h"
#include "pr/collision/ldraw.h"
#include "pr/physics2/rigid_body/rigid_body.h"

namespace pr::ldr
{
	enum class ERigidBodyFlags
	{
		None   = 0,
		AVel   = 1 << 0,
		LVel   = 1 << 1,
		AMom   = 1 << 2,
		LMom   = 1 << 3,
		Force  = 1 << 4,
		Torque = 1 << 5,
		All    = ~0,
		_bitwise_operators_allowed,
	};
	inline TStr& RigidBody(TStr& str, typename TStr::value_type const* name, Col colour, physics::RigidBody const& rb, float scale = 1.0f, ERigidBodyFlags flags = ERigidBodyFlags::None)
	{
		Shape(str, name, colour, rb.Shape(), rb.O2W());
		auto n = Nest(str);
		CoordFrame(str, "Origin", 0xFFFFFFFF, m4x4Identity, 0.1f);
		CoordFrame(str, "CoM", 0xFF404040, m4x4::Translation(rb.CentreOfMassOS().w1()), 0.1f);
		if (flags != ERigidBodyFlags::None)
		{
			auto os_momentum = scale * rb.MomentumOS();
			auto os_velocity = scale * rb.VelocityOS();
			auto os_force    = scale * rb.ForceOS();
			if (bool(flags & ERigidBodyFlags::LVel))
				Arrow(str, "LVel", 0xFF00FFFF, v4Origin, os_velocity.lin, 2);
			if (bool(flags & ERigidBodyFlags::AVel))
				Arrow(str, "AVel", 0xFFFF00FF, v4Origin, os_velocity.ang, 2);
			if (bool(flags & ERigidBodyFlags::LMom))
				Arrow(str, "LMom", 0xFF008080, v4Origin, os_momentum.lin, 5);
			if (bool(flags & ERigidBodyFlags::AMom))
				Arrow(str, "AMom", 0xFF800080, v4Origin, os_momentum.ang, 5);
			if (bool(flags & ERigidBodyFlags::Force))
				Arrow(str, "Force", 0xFF0000FF, -os_force.lin.w1(), os_force.lin, 8);
			if (bool(flags & ERigidBodyFlags::Torque))
				Arrow(str, "Torque", 0xFF000080, v4Origin, os_force.ang.w1(), 8);
		}
		return str;
	}
}