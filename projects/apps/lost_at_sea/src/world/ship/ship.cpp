//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/forward.h"
#include "src/world/ship/ship.h"
#include "src/world/ocean/ocean.h"
#include "pr/physics-2/integrator/integrator.h"

namespace las
{
	// Gravity acceleration (m/s²)
	constexpr float Gravity = -9.81f;

	// Water density (kg/m³)
	constexpr float WaterDensity = 1025.0f;

	Ship::Ship(Renderer& rdr, Ocean const& ocean, v4 location)
		:m_col_shape(v4{1, 1, 1, 0})
		,m_body(&m_col_shape, m4x4::Identity(), Inertia::Box(v4{0.5f, 0.5f, 0.5f, 0}, 100.0f)) // Rigid body: mass = 100kg, box inertia with half-extents of 0.5
		,m_inst()
	{
		// Create a simple box model for visualisation
		ResourceFactory factory(rdr);
		auto opts = ModelGenerator::CreateOptions{}.bake(m4x4::Identity());
		m_inst.m_model = ModelGenerator::Box(factory, 0.5f, &opts);
		factory.FlushToGpu(EGpuFlush::Block);

		// Place the cube near the camera, sitting on the ocean surface.
		// The box is 1m tall with its origin at the centre, so raise it by 0.5m above the surface.
		location.z = ocean.HeightAt(location.x, location.y, 0.0f) + 0.5f;
		m_body.O2W(m4x4::Translation(location));
		m_inst.m_i2w = m_body.O2W();
	}

	void Ship::Step(float dt, Ocean const& ocean, float sim_time)
	{
		auto mass = m_body.Mass();

		// Apply gravity
		auto gravity_force = v4{0, 0, Gravity * mass, 0};
		m_body.ApplyForceWS(gravity_force, v4Zero);

		// Apply buoyancy force based on how much of the cube is below the ocean surface.
		// Submersion is measured from the bottom face of the cube, not the CoM.
		auto ws_com = m_body.CentreOfMassWS();
		auto surface_z = ocean.HeightAt(ws_com.x, ws_com.y, sim_time);
		auto bottom_z = ws_com.z - 0.5f; // bottom face of the 1x1x1 cube
		auto submerged_height = std::clamp(surface_z - bottom_z, 0.0f, 1.0f);
		if (submerged_height > 0)
		{
			// Buoyancy force = water_density * g * submerged_volume
			auto buoyancy = WaterDensity * (-Gravity) * submerged_height * 1.0f;
			auto buoyancy_force = v4{0, 0, buoyancy, 0};
			m_body.ApplyForceWS(buoyancy_force, v4Zero);

			// Linear drag to simulate water resistance and prevent endless oscillation
			auto velocity = m_body.VelocityWS();
			auto drag_lin = -200.0f * velocity.lin;
			auto drag_ang = -50.0f * velocity.ang;
			m_body.ApplyForceWS(drag_lin, drag_ang);
		}

		// Integrate the rigid body forward in time
		pr::physics::Evolve(m_body, dt);
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
