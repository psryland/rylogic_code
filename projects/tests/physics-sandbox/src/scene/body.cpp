#include "src/forward.h"
#include "src/scene/body.h"

namespace physics_sandbox
{
	rdr12::Renderer* Body::s_rdr = nullptr;

	Body::Body()
		: physics::RigidBody()
		, m_gfx()
	{
		// Rebuild graphics whenever the collision shape changes.
		// The handler uses the sender reference (not a captured 'this') so that
		// it remains valid after the Body is moved by std::vector reallocation.
		ShapeChange += [](RigidBody& sender, auto args)
		{
			auto& self = static_cast<Body&>(sender);
			if (args.before())
			{
				// Release the old graphics object (ref-counted, so deletion is automatic)
				self.m_gfx = nullptr;
			}
			else
			{
				// Create new graphics from the physics shape using LDraw
				if (self.HasShape() && s_rdr)
				{
					using namespace pr::rdr12::ldraw;
					static std::default_random_engine rng;
					Builder builder;
					builder._<LdrRigidBody>("Body", RandomRGB(rng, 0.0f, 1.0f)).rigid_body(self);
					auto result = Parse(*s_rdr, builder.ToText(false));
					if (!result.m_objects.empty())
						self.m_gfx = result.m_objects.front();
				}
				self.UpdateGfx();
			}
		};
	}

	Body::~Body()
	{
		// LdrObjectPtr ref-counting handles cleanup automatically
		m_gfx = nullptr;
	}

	// Position the graphics at the rigid body location
	void Body::UpdateGfx()
	{
		if (m_gfx)
			m_gfx->O2W(m_o2w);
	}

	// Add the body's graphics to a scene for rendering this frame
	void Body::AddToScene(rdr12::Scene& scene)
	{
		if (m_gfx)
			m_gfx->AddToScene(scene);
	}
}
