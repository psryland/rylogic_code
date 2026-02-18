//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// A rigid body that sits on the ocean surface.
// For now, the "ship" is a 1x1x1 cube constrained to rest on the ocean
// as though it were a solid surface (no penetration, orientation matches
// the local surface normal).
#pragma once
#include "src/forward.h"
#include "pr/physics-2/rigid_body/rigid_body.h"
#include "pr/physics-2/shape/inertia.h"
#include "pr/collision/shape_box.h"
#include "pr/container/byte_data.h"

namespace las
{
	struct Ocean;

	struct Ship
	{
		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		// Collision shape storage (value type, no heap allocation)
		pr::collision::ShapeBox m_col_shape;

		// Physics rigid body
		pr::physics::RigidBody m_body;

		// Graphics
		Instance m_inst;

		explicit Ship(Renderer& rdr, Ocean const& ocean);

		// Step the ship's physics: apply gravity, constrain to the ocean surface.
		void Step(float dt, Ocean const& ocean, float sim_time);

		// Prepare shader constant buffers for rendering (thread-safe).
		void PrepareRender(v4 camera_world_pos);

		// Add instance to the scene drawlist (NOT thread-safe).
		void AddToScene(Scene& scene);
	};
}
