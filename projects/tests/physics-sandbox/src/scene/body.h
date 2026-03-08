#pragma once
#include "src/forward.h"

namespace physics_sandbox
{
	// A rigid body with attached View3D graphics.
	// When the collision shape changes, the graphics object is automatically
	// rebuilt from LDraw. UpdateGfx() syncs the graphics transform to the physics transform.
	struct Body : physics::RigidBody
	{
		// The renderer used for creating graphics objects from LDraw script.
		// Set once during UI initialization, before any Body instances are created.
		static rdr12::Renderer* s_rdr;

		// Graphics for the object (ref-counted)
		rdr12::ldraw::LdrObjectPtr m_gfx;

		Body();
		~Body();

		// Position the graphics at the rigid body location
		void UpdateGfx();

		// Add the body's graphics to a scene for rendering
		void AddToScene(rdr12::Scene& scene);
	};
}