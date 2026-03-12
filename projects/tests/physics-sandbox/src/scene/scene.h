#pragma once
#include "src/forward.h"
#include "src/scene/body.h"
#include "src/diagnostics/diagnostics.h"
#include "src/utils/scene_loader.h"
#include "src/scene/scenario.h"

namespace physics_sandbox
{
	// The physics simulation scene. Owns the rigid bodies, physics engine, and all
	// simulation state. Deliberately UI-independent so it can be reused by both
	// the interactive sandbox and the headless unit test mode.
	struct Scene
	{
		// Broadphase — either brute-force (CPU) or GPU sort-and-sweep.
		// Owned via unique_ptr to allow runtime selection based on GPU availability.
		physics::MaterialMap m_materials;
		physics::Engine m_physics;
		collision::ShapeBox m_box;

		// Bodies in the scene
		std::vector<Body> m_body;

		// Storage for shapes loaded in the scene file.
		byte_data<16> m_shape_buffer;


		//// Shapes owned by a loaded scene file. When loading from JSON, each body
		//// can have a unique shape, so we store them here to keep them alive for the
		//// lifetime of the scene. The hardcoded scenarios use 'm_box' instead.
		//// Uses variant because collision shapes are value types (no virtual destructor).
		//using OwnedShape = std::variant<collision::ShapeBox, collision::ShapeSphere, collision::ShapeLine, collision::ShapeTriangle>;
		//std::vector<OwnedShape> m_owned_shapes;

		//// Polytope shapes are variable-sized (trailing vertex/face/neighbour data),
		//// so they can't fit in the OwnedShape variant. Store them in separate byte
		//// buffers and access via: buf.as<ShapePolytope>()
		//std::vector<byte_data<16>> m_owned_polytopes;

		// Gravity acceleration vector (direction and magnitude, e.g. [0, -9.81, 0]).
		// Applied each step to all non-static bodies as F = m * g.
		v4 m_gravity;

		// Height below which bodies are frozen (zero velocity/momentum).
		// Prevents bodies that escape through the ground from falling to -infinity
		// and accumulating extreme float values that corrupt the simulation.
		float m_kill_zone_height;

		// Ground plane visual. This is an LDraw object rendered as a large textured
		// quad. The physics ground is a static body in m_body[] with a thin box shape.
		rdr12::ldraw::LdrObjectPtr m_ground_gfx;

		// Simulation state
		double m_clock;
		int m_steps_remaining; // 0 = paused, -1 = running, N = step N times
		EScenario m_scenario;

		// Diagnostics
		CollisionDiag m_diag;

		explicit Scene(ID3D12Device4* existing_device = nullptr);

		// Reset the simulation to the current scenario's initial conditions
		void Reset(rdr12::Renderer* rdr);

		// Load a scene from a JSON file.
		// Replaces the current scenario with bodies defined in the file.
		void LoadFromJson(rdr12::Renderer* rdr, std::filesystem::path const& filepath);

		// Advance the simulation by one time step.
		// Returns true if a collision occurred during this step.
		bool Step(double elapsed_seconds);

		// Configure bodies for the current scenario
		void SetupScenario(rdr12::Renderer* rdr);

		// Log comprehensive collision diagnostics and analytic comparisons
		void LogCollisionDiagnostics();

		// Compare post-collision velocities to the analytic solution for 1D elastic collision
		void LogAnalyticComparison();

		// Run all scenarios in sequence without rendering, log results for each
		void RunAllTests();

		// Export the scene as LDraw script
		void Dump();

		// Calculate the bounding box for the scene (excluding terrain)
		BBox CalculateSceneBBox(scene_loader::SceneDesc const& scene_desc) const;
	};
}
