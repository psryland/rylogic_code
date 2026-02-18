//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/forward.h"
#include "src/world/ship/ship.h"
#include "src/world/ocean/ocean.h"

namespace las
{
	Ship::Ship(Renderer& rdr, Ocean const& ocean)
		// Collision shape: a 1x1x1 box (dimensions, not half-extents — ShapeBox halves internally)
		:m_col_shape(v4{1, 1, 1, 0})
		// Rigid body: mass = 100kg, box inertia with half-extents of 0.5
		,m_body(&m_col_shape, m4x4::Identity(), pr::physics::Inertia::Box(v4{0.5f, 0.5f, 0.5f, 0}, 100.0f))
		,m_inst()
	{
		// Create a simple box model for visualisation
		ResourceFactory factory(rdr);
		auto opts = ModelGenerator::CreateOptions{}.bake(m4x4::Identity());
		m_inst.m_model = ModelGenerator::Box(factory, 0.5f, &opts);
		factory.FlushToGpu(EGpuFlush::Block);

		// Place the cube near the camera, sitting on the ocean surface.
		// The box is 1m tall with its origin at the centre, so raise it by 0.5m above the surface.
		auto x = 10.0f;
		auto y = 0.0f;
		auto z = ocean.HeightAt(x, y, 0.0f) + 0.5f;
		m_body.O2W(m4x4::Translation(x, y, z));
		m_inst.m_i2w = m_body.O2W();
	}

	void Ship::Step(float, Ocean const& ocean, float sim_time)
	{
		// For now, simply constrain the cube to sit on the ocean surface.
		// No real physics integration yet — the ocean surface is treated as a hard floor.

		auto o2w = m_body.O2W();
		auto pos = o2w.pos;

		// Query the ocean surface at the body's XY position
		auto surface_z = ocean.HeightAt(pos.x, pos.y, sim_time);
		auto surface_normal = ocean.NormalAt(pos.x, pos.y, sim_time);

		// The box is 1m tall, origin at centre, so the bottom face is 0.5m below the origin.
		// Place the origin 0.5m above the surface so the bottom face rests on the wave.
		pos.z = surface_z + 0.5f;

		// Orient the cube so its local Z axis aligns with the surface normal.
		// Build a rotation frame from the surface normal (up = surface_normal).
		auto up = Normalise(surface_normal);
		auto forward = Normalise(Cross3(v4{0, 1, 0, 0}, up)); // Arbitrary forward direction
		if (LengthSq(forward) < 0.01f)
			forward = Normalise(Cross3(v4{1, 0, 0, 0}, up));
		auto right = Cross3(up, forward);

		o2w.x = Normalise(forward);
		o2w.y = Normalise(right);
		o2w.z = up;
		o2w.pos = pos;

		m_body.O2W(o2w);
	}

	void Ship::PrepareRender(v4)
	{
		// Sync the graphics instance transform with the physics body
		m_inst.m_i2w = m_body.O2W();
	}

	void Ship::AddToScene(Scene& scene)
	{
		scene.AddInstance(m_inst);
	}
}
