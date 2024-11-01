#pragma once
#include "src/forward.h"

static int BodyIndex;

struct Body :physics::RigidBody
{
	// Graphics for the object
	View3DObject m_gfx;

	Body()
		:physics::RigidBody()
		,m_gfx()
	{
		ShapeChange += [&](auto&,auto args)
		{
			if (args.before())
			{
				View3D_ObjectDelete(m_gfx);
				m_gfx = nullptr;
			}
			else
			{
				if (HasShape())
				{
					static std::default_random_engine rng;

					std::string str;
					ldr::RigidBody(str, "Body", RandomRGB(rng, 1.0f), *this);
					m_gfx = View3D_ObjectCreateLdr(::pr::Widen(str).c_str(), false, nullptr, nullptr);
				}
				UpdateGfx();
			}
		};
	}
	~Body()
	{
		if (m_gfx)
			View3D_ObjectDelete(m_gfx);
	}

	// Position the graphics at the rigid body location
	void UpdateGfx()
	{
		if (m_gfx)
			View3D_ObjectO2WSet(m_gfx, To<View3DM4x4>(m_o2w), nullptr);
	}
};