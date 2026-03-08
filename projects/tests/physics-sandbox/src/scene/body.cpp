#include "src/forward.h"
#include "src/scene/body.h"

namespace physics_sandbox
{
	Body::Body()
		: physics::RigidBody()
		, m_gfx()
	{
		// Rebuild graphics whenever the collision shape changes
		ShapeChange += [&](auto&, auto args)
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
					using namespace pr::rdr12::ldraw;
					static std::default_random_engine rng;
					Builder builder;
					builder._<LdrRigidBody>("Body", RandomRGB(rng, 0.0f, 1.0f)).rigid_body(*this);
					m_gfx = View3D_ObjectCreateLdrA(builder.ToText(false).c_str(), false, nullptr, nullptr); // TODO use binary
				}
				UpdateGfx();
			}
		};
	}

	Body::~Body()
	{
		if (m_gfx)
			View3D_ObjectDelete(m_gfx);
	}

	// Position the graphics at the rigid body location
	void Body::UpdateGfx()
	{
		if (m_gfx)
			View3D_ObjectO2WSet(m_gfx, To<view3d::Mat4x4>(m_o2w), nullptr);
	}
}
