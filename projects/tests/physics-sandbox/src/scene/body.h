#pragma once
#include "src/forward.h"

namespace physics_sandbox
{
	// A rigid body with attached View3D graphics.
	// When the collision shape changes, the graphics object is automatically
	// rebuilt from LDraw. UpdateGfx() syncs the graphics transform to the physics transform.
	struct Body : physics::RigidBody
	{
		// Graphics for the object
		view3d::Object m_gfx;

		Body();
		~Body();

		// Position the graphics at the rigid body location
		void UpdateGfx();
	};
}