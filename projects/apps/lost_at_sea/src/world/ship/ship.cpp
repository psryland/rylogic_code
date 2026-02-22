//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/forward.h"
#include "src/world/ship/ship.h"
#include "src/world/ocean/ocean.h"
#include "src/world/terrain/height_field.h"
#include "pr/physics-2/integrator/integrator.h"
#include "pr/physics-2/integrator/contact.h"
#include "pr/physics-2/integrator/impulse.h"

namespace las
{
	// Gravity acceleration (m/s²)
	constexpr float Gravity = -9.81f;

	// Water density (kg/m³)
	constexpr float WaterDensity = 1025.0f;

	Ship::Ship(Renderer& rdr, HeightField const& height_field, v4 location)
		:m_col_shape(v4{1, 1, 1, 0})
		,m_body(&m_col_shape, m4x4::Identity(), Inertia::Box(v4{0.5f, 0.5f, 0.5f, 0}, 100.0f)) // Rigid body: mass = 100kg, box inertia with half-extents of 0.5
		,m_inst()
	{
		// Create a simple box model for visualisation
		ResourceFactory factory(rdr);
		auto opts = ModelGenerator::CreateOptions{}.bake(m4x4::Identity());
		m_inst.m_model = ModelGenerator::Box(factory, 0.5f, &opts);
		factory.FlushToGpu(EGpuFlush::Block);

		// Find a high terrain point near the requested location so the ship spawns above land
		auto peak = height_field.FindHighPoint(location.x, location.y);
		location = v4{peak.x, peak.y, peak.z + 10.0f, 1}; // 10m above the terrain peak
		m_body.O2W(m4x4::Translation(location));
		m_inst.m_i2w = m_body.O2W();
	}

	void Ship::Step(float dt, Ocean const& ocean, HeightField const& height_field, float sim_time)
	{
		auto mass = m_body.Mass();

		// Apply gravity
		auto gravity_force = v4{0, 0, Gravity * mass, 0};
		m_body.ApplyForceWS(gravity_force, v4Zero);

		// Ship world position (model origin). CoM is at the model origin for this box.
		auto ws_pos = m_body.O2W().pos;

		// Apply buoyancy force based on how much of the cube is below the ocean surface.
		auto surface_z = ocean.HeightAt(ws_pos.x, ws_pos.y, sim_time);
		auto bottom_z = ws_pos.z - 0.5f; // bottom face of the 1x1x1 cube
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

		// Terrain collision: detect penetration and apply impulse response
		{
			auto terrain_z = height_field.HeightAt(ws_pos.x, ws_pos.y);
			auto penetration = terrain_z - bottom_z; // positive = overlap
			if (penetration > 0)
			{
				auto terrain_normal_ws = height_field.NormalAt(ws_pos.x, ws_pos.y);
				auto w2o = m_body.W2O();

				// Create a static terrain body at the contact point (infinite mass, zero velocity)
				auto terrain_surface_pos = v4{ws_pos.x, ws_pos.y, terrain_z, 1};
				auto terrain_o2w = m4x4::Translation(terrain_surface_pos);
				auto terrain_body = RigidBody{nullptr, terrain_o2w, Inertia::Infinite()};

				// Build the contact in objA (ship) space
				// m_axis = collision normal from A to B (ship to terrain) = -terrain_normal in ship space
				auto os_normal = w2o * (-terrain_normal_ws);
				os_normal.w = 0;

				// Contact point in ship space = bottom of the box projected to the terrain surface
				auto ws_contact_pt = v4{ws_pos.x, ws_pos.y, terrain_z + penetration * 0.5f, 1};
				auto os_contact_pt = w2o * ws_contact_pt;
				os_contact_pt.w = 0; // Contact point is an offset from objA origin

				// Check that objects are approaching (not separating)
				auto contact = physics::Contact{m_body, terrain_body};
				contact.m_axis = os_normal;
				contact.m_point = os_contact_pt;
				contact.m_point_at_t = os_contact_pt;
				contact.m_depth = penetration;

				// Material: rocky terrain with moderate bounce and friction
				contact.m_mat = physics::Material{
					physics::Material::DefaultID,
					/*friction_static=*/ 0.7f,
					/*elasticity_norm=*/ 0.3f,
					/*elasticity_tang=*/ 0.0f,
					/*elasticity_tors=*/ 0.0f,
					/*density=*/ 2500.0f
				};

				// Only apply impulse if objects are approaching
				auto rel_vel_at_pt = contact.m_velocity.LinAt(contact.m_point_at_t);
				auto approach_speed = Dot(rel_vel_at_pt, contact.m_axis);

				if (approach_speed < 0)
				{
					auto impulse_pair = physics::RestitutionImpulse(contact);
					m_body.MomentumOS(m_body.MomentumOS() + impulse_pair.m_os_impulse_objA);
				}

				// Position correction: push the ship out of the terrain
				auto correction = terrain_normal_ws * penetration;
				auto o2w = m_body.O2W();
				o2w.pos += correction;
				m_body.O2W(o2w);
			}
		}

		// Integrate the rigid body forward in time
		pr::physics::Evolve(m_body, dt);
	}

	void Ship::PrepareRender(v4)
	{
		// The standard forward renderer transforms vertices via m_o2s (= c2s * w2c * o2w)
		// which already handles the camera position via w2c. No manual camera-relative
		// subtraction needed — that would cause double-subtraction.
		m_inst.m_i2w = m_body.O2W();
	}

	void Ship::AddToScene(Scene& scene)
	{
		scene.AddInstance(m_inst);
	}
}
