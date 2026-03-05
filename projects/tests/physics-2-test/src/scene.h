#pragma once
#include "src/forward.h"
#include "src/body.h"
#include "src/diagnostics.h"

// Test scenarios for validating physics behaviour progressively.
// Each scenario configures two bodies with specific initial conditions.
enum class EScenario : int
{
	Sandbox          = 0, // Free-form sandbox (default)
	HeadOnEqualMass  = 1, // Two equal-mass boxes, head-on along X
	HeadOnDiffMass   = 2, // Mass 10 vs Mass 5, head-on along X
	StationaryTarget = 3, // Moving box hits stationary box
	OffCenter        = 4, // Off-center hit (induces rotation)
	Oblique          = 5, // Bodies approaching at an angle
};

inline char const* ScenarioName(EScenario s)
{
	switch (s)
	{
		case EScenario::Sandbox:          return "Sandbox";
		case EScenario::HeadOnEqualMass:  return "Head-On Equal Mass";
		case EScenario::HeadOnDiffMass:   return "Head-On Diff Mass";
		case EScenario::StationaryTarget: return "Stationary Target";
		case EScenario::OffCenter:        return "Off-Center (Rotation)";
		case EScenario::Oblique:          return "Oblique Collision";
		default: return "Unknown";
	}
}

// The physics simulation scene. Owns the rigid bodies, physics engine, and all
// simulation state. Deliberately UI-independent so it can be reused by both
// the interactive sandbox and the headless unit test mode.
struct Scene
{
	using Physics = pr::physics::Engine<pr::physics::broadphase::Brute<Body>, pr::physics::MaterialMap>;

	static constexpr int MaxBodies = 16;

	Body        m_body[MaxBodies];
	int         m_body_count;
	Physics     m_physics;
	pr::collision::ShapeBox m_box;

	// Simulation state
	double      m_clock;
	int         m_steps_remaining; // 0 = paused, -1 = running, N = step N times
	EScenario   m_scenario;

	// Diagnostics
	CollisionDiag   m_diag;
	std::vector<pr::v4> m_trail[MaxBodies];

	Scene()
		: m_body()
		, m_body_count(2)
		, m_physics()
		, m_box(pr::v4{2, 2, 2, 0})
		, m_clock()
		, m_steps_remaining(0)
		, m_scenario(EScenario::Sandbox)
		, m_diag()
		, m_trail{}
	{
		// Hook collision detection for diagnostics.
		// This fires AFTER Evolve but BEFORE impulse resolution.
		m_physics.PostCollisionDetection += [&](auto&, auto& collisions)
		{
			if (collisions.empty())
				return;

			m_diag.occurred = true;
			m_diag.count++;

			// Capture pre-impulse state
			m_diag.before[0] = BodySnapshot::Capture(m_body[0]);
			m_diag.before[1] = BodySnapshot::Capture(m_body[1]);

			// Capture contact info (collision data is in objA space, transform to world)
			auto const& c = collisions[0];
			m_diag.contact_point_ws = m_body[0].O2W() * c.m_point_at_t;
			m_diag.contact_normal_ws = (m_body[0].O2W().rot * c.m_axis).w0();
			m_diag.depth = c.m_depth;
		};
	}

	// Reset the simulation to the current scenario's initial conditions
	void Reset()
	{
		m_steps_remaining = 0;
		m_clock = 0;
		m_diag.Reset();
		for (int i = 0; i != MaxBodies; ++i)
			m_trail[i].clear();

		// Set up perfectly elastic, frictionless material for clean collision tests
		auto& mat = m_physics.m_materials(0);
		mat.m_elasticity_norm = 1.0f;
		mat.m_elasticity_tang = 0.0f;
		mat.m_elasticity_tors = 0.0f;
		mat.m_friction_static = 0.0f;

		// Configure bodies for the current scenario
		SetupScenario();

		// Rebuild the broadphase with the active bodies
		m_physics.m_broadphase.Clear();
		for (int i = 0; i != m_body_count; ++i)
			m_physics.m_broadphase.Add(m_body[i]);

		DbgLog("\n--- Reset: Scenario %d [%s] ---\n", static_cast<int>(m_scenario), ScenarioName(m_scenario));
		DbgLog("  Material: elasticity_norm=%.2f friction=%.2f\n", mat.m_elasticity_norm, mat.m_friction_static);
		for (int i = 0; i != m_body_count; ++i)
		{
			auto snap = BodySnapshot::Capture(m_body[i]);
			snap.Log(pr::FmtS("Body %d (initial)", i));
		}
		auto total_p = m_body[0].MomentumWS().lin + m_body[1].MomentumWS().lin;
		DbgLog("  Total lin momentum: (%.4f, %.4f, %.4f)\n", total_p.x, total_p.y, total_p.z);
		DbgLog("  Total KE: %.6f\n", m_body[0].KineticEnergy() + m_body[1].KineticEnergy());
	}

	// Advance the simulation by one time step.
	// Returns true if a collision occurred during this step.
	bool Step(double elapsed_seconds)
	{
		m_clock += elapsed_seconds;
		auto dt = float(elapsed_seconds);

		// Reset per-step collision flag
		m_diag.occurred = false;

		// Record trail positions for each active body
		for (int i = 0; i != m_body_count; ++i)
			m_trail[i].push_back(m_body[i].O2W().pos);

		// Step physics (Evolve → Broad Phase → Narrow Phase → PostCollisionDetection → Resolve)
		auto bodies = std::span<Body>(m_body, m_body_count);
		m_physics.Step(dt, bodies);

		// If a collision occurred this step, capture post-impulse state and log diagnostics
		if (m_diag.occurred)
		{
			m_diag.after[0] = BodySnapshot::Capture(m_body[0]);
			m_diag.after[1] = BodySnapshot::Capture(m_body[1]);
			LogCollisionDiagnostics();
		}

		// NaN guard
		for (int i = 0; i != m_body_count; ++i)
		{
			auto pos = m_body[i].O2W().pos;
			if (!std::isfinite(pos.x) || !std::isfinite(pos.y) || !std::isfinite(pos.z))
			{
				DbgLog("!!! NaN detected, resetting !!!\n");
				Reset();
				return false;
			}
		}

		return m_diag.occurred;
	}

	// Configure bodies for the current scenario. All test scenarios use no external
	// forces so that collisions can be validated against analytic predictions.
	void SetupScenario()
	{
		m_body_count = 2;
		auto& objA = m_body[0];
		auto& objB = m_body[1];

		// Common setup: zero forces/momentum
		for (int i = 0; i != m_body_count; ++i)
		{
			m_body[i].ZeroForces();
			m_body[i].ZeroMomentum();
		}

		switch (m_scenario)
		{
			case EScenario::Sandbox:
			{
				// Default sandbox: two boxes approaching each other gently
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, 0, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+2.0f, 0, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4{-2.0f, 0, 0, 0});
				break;
			}
			case EScenario::HeadOnEqualMass:
			{
				// Two equal-mass boxes approaching each other along X.
				// Elastic collision should swap velocities exactly.
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, 0, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4{-3.0f, 0, 0, 0});
				break;
			}
			case EScenario::HeadOnDiffMass:
			{
				// Mass 10 hits mass 5 head-on along X.
				// v1' = (m1-m2)/(m1+m2)*v1 + 2*m2/(m1+m2)*v2
				// v2' = 2*m1/(m1+m2)*v1 + (m2-m1)/(m1+m2)*v2
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 5.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, 0, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4{-3.0f, 0, 0, 0});
				break;
			}
			case EScenario::StationaryTarget:
			{
				// Moving box hits a stationary box (classic billiard scenario)
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, 0, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4Zero);
				break;
			}
			case EScenario::OffCenter:
			{
				// Off-center hit: boxes offset in Y, collision induces rotation.
				// Body A approaches along X but is offset in Y so the contact
				// point is not aligned with the centres of mass.
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, +0.8f, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4Zero);
				break;
			}
			case EScenario::Oblique:
			{
				// Oblique collision: bodies approaching at an angle
				objA.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, pr::physics::Inertia::Box(pr::v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(pr::m4x4::Translation(pr::v4{-5.0f, -2.0f, 0, 1}));
				objB.O2W(pr::m4x4::Translation(pr::v4{+5.0f, +2.0f, 0, 1}));
				objA.VelocityWS(pr::v4Zero, pr::v4{+3.0f, +1.0f, 0, 0});
				objB.VelocityWS(pr::v4Zero, pr::v4{-3.0f, -1.0f, 0, 0});
				break;
			}
		}
	}

	// Log comprehensive collision diagnostics and analytic comparisons
	void LogCollisionDiagnostics()
	{
		DbgLog("\n========================================\n");
		DbgLog("=== COLLISION #%d [%s] at t=%.4f ===\n", m_diag.count, ScenarioName(m_scenario), m_clock);
		DbgLog("========================================\n");

		// Contact info
		DbgLog("Contact:\n");
		DbgLog("  point_ws = (%.4f, %.4f, %.4f)\n",
			m_diag.contact_point_ws.x, m_diag.contact_point_ws.y, m_diag.contact_point_ws.z);
		DbgLog("  normal_ws = (%.4f, %.4f, %.4f)\n",
			m_diag.contact_normal_ws.x, m_diag.contact_normal_ws.y, m_diag.contact_normal_ws.z);
		DbgLog("  depth = %.6f\n", m_diag.depth);

		// Pre-impulse state
		DbgLog("Pre-impulse:\n");
		m_diag.before[0].Log("Body A");
		m_diag.before[1].Log("Body B");
		auto pre_total_p = m_diag.before[0].momentum.lin + m_diag.before[1].momentum.lin;
		auto pre_total_ke = m_diag.before[0].ke + m_diag.before[1].ke;
		DbgLog("  Total lin momentum: (%.4f, %.4f, %.4f)\n", pre_total_p.x, pre_total_p.y, pre_total_p.z);
		DbgLog("  Total KE: %.6f\n", pre_total_ke);

		// Angular momentum about world origin = spin (I*ω) + orbital (r × p).
		// MomentumWS().ang is the spin angular momentum at each body's model origin.
		// We must add the orbital term r × (m*v) for a correct system total.
		auto ang_mom_about_origin = [](BodySnapshot const& s)
		{
			auto orbital = Cross(s.pos, s.momentum.lin);
			return s.momentum.ang + orbital;
		};
		auto pre_total_L = ang_mom_about_origin(m_diag.before[0]) + ang_mom_about_origin(m_diag.before[1]);
		DbgLog("  Total ang momentum (about origin): (%.4f, %.4f, %.4f)\n", pre_total_L.x, pre_total_L.y, pre_total_L.z);

		// Post-impulse state
		DbgLog("Post-impulse:\n");
		m_diag.after[0].Log("Body A");
		m_diag.after[1].Log("Body B");
		auto post_total_p = m_diag.after[0].momentum.lin + m_diag.after[1].momentum.lin;
		auto post_total_ke = m_diag.after[0].ke + m_diag.after[1].ke;
		DbgLog("  Total lin momentum: (%.4f, %.4f, %.4f)\n", post_total_p.x, post_total_p.y, post_total_p.z);
		DbgLog("  Total KE: %.6f\n", post_total_ke);

		auto post_total_L = ang_mom_about_origin(m_diag.after[0]) + ang_mom_about_origin(m_diag.after[1]);
		DbgLog("  Total ang momentum (about origin): (%.4f, %.4f, %.4f)\n", post_total_L.x, post_total_L.y, post_total_L.z);

		// Conservation checks
		auto dp = post_total_p - pre_total_p;
		auto dL = post_total_L - pre_total_L;
		auto dke = post_total_ke - pre_total_ke;
		auto dL_pct = pr::Length(pre_total_L) > 0.01f ? 100.0f * pr::Length(dL) / pr::Length(pre_total_L) : 0.0f;
		DbgLog("Conservation:\n");
		DbgLog("  Delta lin momentum: (%.6f, %.6f, %.6f) |dp|=%.6f\n", dp.x, dp.y, dp.z, pr::Length(dp));
		DbgLog("  Delta ang momentum: (%.6f, %.6f, %.6f) |dL|=%.6f (%.2f%%)\n", dL.x, dL.y, dL.z, pr::Length(dL), dL_pct);
		DbgLog("  Delta KE: %.6f (%.2f%%)\n", dke, pre_total_ke > 0 ? 100.0f * dke / pre_total_ke : 0.0f);

		// Pass/fail thresholds.
		// Angular momentum conservation is approximate due to sub-step time correction.
		bool momentum_ok = pr::Length(dp) < 0.01f;
		auto ang_tol = pr::Max(0.01f, pr::Length(pre_total_L) * 0.05f);
		bool ang_momentum_ok = pr::Length(dL) < ang_tol;
		bool ke_ok = pr::Abs(dke) < 0.01f * pre_total_ke;
		DbgLog("  Lin Momentum conserved: %s\n", momentum_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  Ang Momentum conserved: %s%s\n", ang_momentum_ok ? "PASS" : "*** FAIL ***",
			(pr::Length(dL) > 0.01f && ang_momentum_ok) ? " (within sub-step tolerance)" : "");
		DbgLog("  KE conserved (elastic): %s\n", ke_ok ? "PASS" : "*** FAIL ***");

		// Analytic predictions for 1D head-on elastic collisions (scenarios 1-3)
		if (m_scenario == EScenario::HeadOnEqualMass ||
			m_scenario == EScenario::HeadOnDiffMass ||
			m_scenario == EScenario::StationaryTarget)
		{
			LogAnalyticComparison();
		}

		DbgLog("========================================\n\n");
	}

	// Compare post-collision velocities to the analytic solution for 1D elastic collision.
	// For perfectly elastic collision:
	//   v1' = ((m1-m2)*v1 + 2*m2*v2) / (m1+m2)
	//   v2' = ((m2-m1)*v2 + 2*m1*v1) / (m1+m2)
	void LogAnalyticComparison()
	{
		auto m1 = m_diag.before[0].mass;
		auto m2 = m_diag.before[1].mass;
		auto v1 = m_diag.before[0].lin_vel.x;
		auto v2 = m_diag.before[1].lin_vel.x;

		auto v1_pred = ((m1 - m2) * v1 + 2 * m2 * v2) / (m1 + m2);
		auto v2_pred = ((m2 - m1) * v2 + 2 * m1 * v1) / (m1 + m2);

		auto v1_actual = m_diag.after[0].lin_vel.x;
		auto v2_actual = m_diag.after[1].lin_vel.x;

		DbgLog("Analytic comparison (1D elastic, X component):\n");
		DbgLog("  v1': predicted=%.4f  actual=%.4f  error=%.6f\n", v1_pred, v1_actual, pr::Abs(v1_pred - v1_actual));
		DbgLog("  v2': predicted=%.4f  actual=%.4f  error=%.6f\n", v2_pred, v2_actual, pr::Abs(v2_pred - v2_actual));

		auto vy1 = m_diag.after[0].lin_vel.y;
		auto vz1 = m_diag.after[0].lin_vel.z;
		auto vy2 = m_diag.after[1].lin_vel.y;
		auto vz2 = m_diag.after[1].lin_vel.z;
		DbgLog("  v1_yz: (%.6f, %.6f)  v2_yz: (%.6f, %.6f)\n", vy1, vz1, vy2, vz2);

		auto w1 = m_diag.after[0].ang_vel;
		auto w2 = m_diag.after[1].ang_vel;
		DbgLog("  ang_vel1: (%.6f, %.6f, %.6f)  ang_vel2: (%.6f, %.6f, %.6f)\n",
			w1.x, w1.y, w1.z, w2.x, w2.y, w2.z);

		bool x_ok = pr::Abs(v1_pred - v1_actual) < 0.05f && pr::Abs(v2_pred - v2_actual) < 0.05f;
		bool yz_ok = pr::Abs(vy1) < 0.05f && pr::Abs(vz1) < 0.05f && pr::Abs(vy2) < 0.05f && pr::Abs(vz2) < 0.05f;
		bool no_spin = pr::Length(w1) < 0.05f && pr::Length(w2) < 0.05f;
		DbgLog("  X velocity match: %s\n", x_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  Y/Z remain zero:  %s\n", yz_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  No angular vel:   %s\n", no_spin ? "PASS" : "*** FAIL ***");
	}

	// Run all scenarios in sequence without rendering, log results for each.
	void RunAllTests()
	{
		DbgLog("\n################################################################\n");
		DbgLog("### AUTO-TEST: Running all scenarios\n");
		DbgLog("################################################################\n");

		auto const dt = 1.0f / 100.0f;
		auto const max_steps = 5000;

		for (int s = 1; s <= 5; ++s)
		{
			m_scenario = static_cast<EScenario>(s);
			Reset();

			for (int step = 0; step < max_steps && m_diag.count == 0; ++step)
			{
				m_diag.occurred = false;
				for (int i = 0; i != m_body_count; ++i)
					m_trail[i].push_back(m_body[i].O2W().pos);

				auto bodies = std::span<Body>(m_body, m_body_count);
				m_physics.Step(dt, bodies);
				m_clock += dt;

				if (m_diag.occurred)
				{
					m_diag.after[0] = BodySnapshot::Capture(m_body[0]);
					m_diag.after[1] = BodySnapshot::Capture(m_body[1]);
					LogCollisionDiagnostics();
				}

				// NaN guard
				auto pos = m_body[0].O2W().pos;
				if (!std::isfinite(pos.x))
				{
					DbgLog("!!! NaN detected in scenario %d at step %d\n", s, step);
					break;
				}
			}

			if (m_diag.count == 0)
				DbgLog("!!! Scenario %d: No collision after %d steps!\n", s, max_steps);
		}

		DbgLog("\n################################################################\n");
		DbgLog("### AUTO-TEST COMPLETE\n");
		DbgLog("################################################################\n");
	}

	// Export the scene as LDraw script
	void Dump()
	{
		using namespace pr::rdr12::ldraw;
		auto flags = ERigidBodyFlags::All;

		Builder builder;
		builder._<LdrRigidBody>("body0", 0x8000FF00).rigid_body(m_body[0]).flags(flags);
		builder._<LdrRigidBody>("body1", 0x10FF0000).rigid_body(m_body[1]).flags(flags);
		builder.Save(L"\\dump\\physics_dump.ldr");
	}
};
