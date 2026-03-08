#pragma once
#include "src/forward.h"
#include "src/scene/body.h"
#include "src/diagnostics/diagnostics.h"
#include "src/utils/scene_loader.h"

namespace physics_sandbox
{
	// Test scenarios for validating physics behaviour progressively.
	// Each scenario configures two bodies with specific initial conditions.
	enum class EScenario : int
	{
		Sandbox = 0, // Free-form sandbox (default)
		HeadOnEqualMass = 1, // Two equal-mass boxes, head-on along X
		HeadOnDiffMass = 2, // Mass 10 vs Mass 5, head-on along X
		StationaryTarget = 3, // Moving box hits stationary box
		OffCenter = 4, // Off-center hit (induces rotation)
		Oblique = 5, // Bodies approaching at an angle
	};

	char const* ScenarioName(EScenario s);

	// The physics simulation scene. Owns the rigid bodies, physics engine, and all
	// simulation state. Deliberately UI-independent so it can be reused by both
	// the interactive sandbox and the headless unit test mode.
	struct Scene
	{
		static constexpr int MaxBodies = 16;

		Body        m_body[MaxBodies];
		int         m_body_count;

		// Broadphase and materials are owned by the scene and passed by reference
		// to the engine. This keeps the engine decoupled from concrete implementations.
		pr::physics::broadphase::Brute m_broadphase;
		pr::physics::MaterialMap       m_materials;
		pr::physics::Engine            m_physics;
		pr::collision::ShapeBox m_box;

		// Shapes owned by a loaded scene file. When loading from JSON, each body
		// can have a unique shape, so we store them here to keep them alive for the
		// lifetime of the scene. The hardcoded scenarios use 'm_box' instead.
		// Uses variant because collision shapes are value types (no virtual destructor).
		using OwnedShape = std::variant<pr::collision::ShapeBox, pr::collision::ShapeSphere, pr::collision::ShapeLine, pr::collision::ShapeTriangle>;
		std::vector<OwnedShape> m_owned_shapes;

		// Polytope shapes are variable-sized (trailing vertex/face/neighbour data),
		// so they can't fit in the OwnedShape variant. Store them in separate byte
		// buffers and access via: buf.as<ShapePolytope>()
		std::vector<pr::byte_data<16>> m_owned_polytopes;

		// Gravity acceleration vector (direction and magnitude, e.g. [0, -9.81, 0]).
		// Applied each step to all non-static bodies as F = m * g.
		pr::v4 m_gravity;

		// Height below which bodies are frozen (zero velocity/momentum).
		// Prevents bodies that escape through the ground from falling to -infinity
		// and accumulating extreme float values that corrupt the simulation.
		float m_kill_zone_height;

		// Ground plane visual. This is an LDraw object rendered as a large textured
		// quad. The physics ground is a static body in m_body[] with a thin box shape.
		rdr12::ldraw::LdrObjectPtr m_ground_gfx;

		// Simulation state
		double      m_clock;
		int         m_steps_remaining; // 0 = paused, -1 = running, N = step N times
		EScenario   m_scenario;

		// Diagnostics
		CollisionDiag   m_diag;

		Scene();

		// Reset the simulation to the current scenario's initial conditions
		void Reset();

		// Load a scene from a JSON file.
		// Replaces the current scenario with bodies defined in the file.
		void LoadFromJson(std::filesystem::path const& filepath);

		// Advance the simulation by one time step.
		// Returns true if a collision occurred during this step.
		bool Step(double elapsed_seconds);

		// Configure bodies for the current scenario
		void SetupScenario();

		// Log comprehensive collision diagnostics and analytic comparisons
		void LogCollisionDiagnostics();

		// Compare post-collision velocities to the analytic solution for 1D elastic collision
		void LogAnalyticComparison();

		// Run all scenarios in sequence without rendering, log results for each
		void RunAllTests();

		// Export the scene as LDraw script
		void Dump();

		// Compute the maximum extent of all scene bodies from the origin
		float ComputeSceneExtent(int num_bodies) const;

		// Create the visual ground plane as a large textured LDraw plane
		void CreateGroundGfx(float height, float extent, std::string const& texture);

		// Clean up the ground plane visual
		void CleanupGroundGfx();
	};
}
