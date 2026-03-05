#include "src/forward.h"
#include "src/body.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::physics;

std::default_random_engine rng;

// Test scenarios for validating physics behaviour progressively
enum class EScenario : int
{
	HeadOnEqualMass  = 1, // Two equal-mass boxes, head-on along X
	HeadOnDiffMass   = 2, // Mass 10 vs Mass 5, head-on along X
	StationaryTarget = 3, // Moving box hits stationary box
	OffCenter        = 4, // Off-center hit (induces rotation)
	Oblique          = 5, // Bodies approaching at an angle
};

static char const* ScenarioName(EScenario s)
{
	switch (s)
	{
		case EScenario::HeadOnEqualMass:  return "Head-On Equal Mass";
		case EScenario::HeadOnDiffMass:   return "Head-On Diff Mass";
		case EScenario::StationaryTarget: return "Stationary Target";
		case EScenario::OffCenter:        return "Off-Center (Rotation)";
		case EScenario::Oblique:          return "Oblique Collision";
		default: return "Unknown";
	}
}

// Diagnostic output helper — writes to both OutputDebugString and a log file
static FILE* s_log_file = nullptr;
static void DbgLog(char const* fmt, ...)
{
	char buf[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(buf, sizeof(buf), fmt, args);
	va_end(args);
	OutputDebugStringA(buf);

	if (!s_log_file)
		s_log_file = fopen("dump\\physics_diag.log", "w");
	if (s_log_file)
	{
		fputs(buf, s_log_file);
		fflush(s_log_file);
	}
}

// Snapshot of a rigid body's state at a moment in time
struct BodySnapshot
{
	v4 pos;
	v4 lin_vel;
	v4 ang_vel;
	v8force momentum;
	float mass;
	float ke;

	static BodySnapshot Capture(physics::RigidBody const& rb)
	{
		auto vel = rb.VelocityWS();
		auto snap = BodySnapshot{};
		snap.pos = rb.O2W().pos;
		snap.lin_vel = vel.lin;
		snap.ang_vel = vel.ang;
		snap.momentum = rb.MomentumWS();
		snap.mass = rb.Mass();
		snap.ke = rb.KineticEnergy();
		return snap;
	}

	void Log(char const* label) const
	{
		DbgLog("  %s: pos=(%.4f, %.4f, %.4f) lvel=(%.4f, %.4f, %.4f) avel=(%.4f, %.4f, %.4f) mass=%.1f KE=%.6f\n",
			label,
			pos.x, pos.y, pos.z,
			lin_vel.x, lin_vel.y, lin_vel.z,
			ang_vel.x, ang_vel.y, ang_vel.z,
			mass, ke);
	}
};

// Diagnostic data captured during a collision event
struct CollisionDiag
{
	BodySnapshot before[2]; // State just before impulse (post-Evolve, pre-impulse)
	BodySnapshot after[2];  // State just after impulse
	v4 contact_point_ws;
	v4 contact_normal_ws;
	float depth;
	bool occurred;
	int count;

	CollisionDiag()
		: before{}
		, after{}
		, contact_point_ws{}
		, contact_normal_ws{}
		, depth{}
		, occurred{}
		, count{}
	{}

	void Reset()
	{
		occurred = false;
		count = 0;
	}
};

struct MainUI :Form
{
	using Physics = Engine<broadphase::Brute<Body>, MaterialMap>;

	StatusBar   m_status;
	View3DPanel m_view3d;
	double      m_clock;
	int         m_steps;

	Body        m_body[2];
	Physics     m_physics;
	ShapeBox    m_box;

	// Instrumentation
	EScenario       m_scenario;
	CollisionDiag   m_diag;
	std::vector<v4> m_trail[2];
	view3d::Object  m_trail_gfx;
	view3d::Object  m_diag_gfx;
	bool            m_pause_on_collision;

	MainUI()
		: Form(Params<>().name("main-ui").title(L"Rylogic Physics").start_pos(EStartPosition::Manual).xy(1000, 50).padding(0).wndclass(RegisterWndClass<MainUI>()))
		, m_status(StatusBar::Params<>().parent(this_).dock(EDock::Bottom))
		, m_view3d(View3DPanel::Params().parent(this_).error_cb(ReportErrorCB, this_).dock(EDock::Fill).border().show_focus_point())
		, m_clock()
		, m_steps()
		, m_body()
		, m_physics()
		, m_box(v4{2, 2, 2, 0}) // Axis-aligned box, no shape-to-parent rotation
		, m_scenario(EScenario::HeadOnEqualMass)
		, m_diag()
		, m_trail{}
		, m_trail_gfx()
		, m_diag_gfx()
		, m_pause_on_collision(true)
	{
		// Hook collision detection for diagnostics.
		// This fires AFTER Evolve but BEFORE impulse resolution, so the body velocities
		// reflect the pre-impulse state (unchanged by Evolve when no external forces are applied).
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

		Reset();
		m_steps = 0xFFFFFFFF; // Auto-start

		m_view3d.Key += [&](Control&, KeyEventArgs const& args)
		{
			if (!args.m_down)
				return;

			// 1-5: select scenario
			if (args.m_vk_key >= '1' && args.m_vk_key <= '5')
			{
				m_scenario = static_cast<EScenario>(args.m_vk_key - '0');
				Reset();
				m_steps = 0xFFFFFFFF;
			}
			if (args.m_vk_key == 'R')
				Reset();
			if (args.m_vk_key == 'S')
				m_steps = 1;
			if (args.m_vk_key == 'G')
				m_steps = 0xFFFFFFFF;
			if (args.m_vk_key == 'P')
				m_steps = 0;
			if (args.m_vk_key == 'C')
				m_pause_on_collision = !m_pause_on_collision;
			if (args.m_vk_key == 'T')
				RunAllTests();
		};
	}

	void Reset()
	{
		m_steps = 0;
		m_clock = 0;
		m_diag.Reset();
		m_trail[0].clear();
		m_trail[1].clear();

		// Clean up diagnostic graphics
		if (m_trail_gfx) { View3D_ObjectDelete(m_trail_gfx); m_trail_gfx = nullptr; }
		if (m_diag_gfx) { View3D_ObjectDelete(m_diag_gfx); m_diag_gfx = nullptr; }

		// Set up perfectly elastic, frictionless material for clean collision tests
		auto& mat = m_physics.m_materials(0);
		mat.m_elasticity_norm = 1.0f;  // Perfectly elastic
		mat.m_elasticity_tang = 0.0f;
		mat.m_elasticity_tors = 0.0f;
		mat.m_friction_static = 0.0f;  // No friction

		// Set up the scenario-specific initial conditions
		SetupScenario();

		// Reset the broad phase
		m_physics.m_broadphase.Clear();
		for (auto& body : m_body)
			m_physics.m_broadphase.Add(body);

		for (auto& body : m_body)
		{
			if (!body.m_gfx) continue;
			View3D_WindowAddObject(m_view3d.m_win, body.m_gfx);
		}

		Render();
		View3D_ResetView(m_view3d.m_win, view3d::Vec4{0, 0, -1, 0}, view3d::Vec4{0, 1, 0, 0}, 20, TRUE, TRUE);

		DbgLog("\n--- Reset: Scenario %d [%s] ---\n", static_cast<int>(m_scenario), ScenarioName(m_scenario));
		DbgLog("  Material: elasticity_norm=%.2f friction=%.2f\n", mat.m_elasticity_norm, mat.m_friction_static);
		for (int i = 0; i != 2; ++i)
		{
			auto snap = BodySnapshot::Capture(m_body[i]);
			snap.Log(pr::FmtS("Body %d (initial)", i));
		}
		DbgLog("  Total lin momentum: (%.4f, %.4f, %.4f)\n",
			(m_body[0].MomentumWS().lin + m_body[1].MomentumWS().lin).x,
			(m_body[0].MomentumWS().lin + m_body[1].MomentumWS().lin).y,
			(m_body[0].MomentumWS().lin + m_body[1].MomentumWS().lin).z);
		DbgLog("  Total KE: %.6f\n", m_body[0].KineticEnergy() + m_body[1].KineticEnergy());
	}

	// Configure bodies for the current scenario. All scenarios use no external forces
	// so that collisions can be validated against analytic predictions.
	void SetupScenario()
	{
		auto& objA = m_body[0];
		auto& objB = m_body[1];

		// Common setup: axis-aligned boxes, zero forces/momentum, identity orientation
		for (auto& body : m_body)
		{
			body.ZeroForces();
			body.ZeroMomentum();
		}

		switch (m_scenario)
		{
			case EScenario::HeadOnEqualMass:
			{
				// Two equal-mass boxes approaching each other along X.
				// Elastic collision should swap velocities exactly.
				objA.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(m4x4::Translation(v4{-5.0f, 0, 0, 1}));
				objB.O2W(m4x4::Translation(v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(v4Zero, v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(v4Zero, v4{-3.0f, 0, 0, 0});
				break;
			}
			case EScenario::HeadOnDiffMass:
			{
				// Mass 10 hits mass 5 head-on along X.
				// v1' = (m1-m2)/(m1+m2)*v1 + 2*m2/(m1+m2)*v2
				// v2' = 2*m1/(m1+m2)*v1 + (m2-m1)/(m1+m2)*v2
				objA.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 5.0f));
				objA.O2W(m4x4::Translation(v4{-5.0f, 0, 0, 1}));
				objB.O2W(m4x4::Translation(v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(v4Zero, v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(v4Zero, v4{-3.0f, 0, 0, 0});
				break;
			}
			case EScenario::StationaryTarget:
			{
				// Moving box hits a stationary box (classic billiard scenario)
				objA.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(m4x4::Translation(v4{-5.0f, 0, 0, 1}));
				objB.O2W(m4x4::Translation(v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(v4Zero, v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(v4Zero, v4Zero);
				break;
			}
			case EScenario::OffCenter:
			{
				// Off-center hit: boxes offset in Y, collision induces rotation.
				// Body A approaches along X but is offset in Y so the contact
				// point is not aligned with the centres of mass.
				objA.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(m4x4::Translation(v4{-5.0f, +0.8f, 0, 1}));
				objB.O2W(m4x4::Translation(v4{+5.0f, 0, 0, 1}));
				objA.VelocityWS(v4Zero, v4{+3.0f, 0, 0, 0});
				objB.VelocityWS(v4Zero, v4Zero);
				break;
			}
			case EScenario::Oblique:
			{
				// Oblique collision: bodies approaching at an angle
				objA.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objB.Shape(m_box, physics::Inertia::Box(v4{1, 1, 1, 0}, 10.0f));
				objA.O2W(m4x4::Translation(v4{-5.0f, -2.0f, 0, 1}));
				objB.O2W(m4x4::Translation(v4{+5.0f, +2.0f, 0, 1}));
				objA.VelocityWS(v4Zero, v4{+3.0f, +1.0f, 0, 0});
				objB.VelocityWS(v4Zero, v4{-3.0f, -1.0f, 0, 0});
				break;
			}
		}
	}

	// Step the main loop
	void Step(double elapsed_seconds)
	{
		m_clock += elapsed_seconds;
		SetWindowTextA(*this, pr::FmtS("Physics [%d: %s] t=%.3f col=%d",
			static_cast<int>(m_scenario), ScenarioName(m_scenario), m_clock, m_diag.count));

		if (m_steps == 0 || m_steps-- == 0)
			return;

		auto dt = float(elapsed_seconds);

		// No external forces for test scenarios — collision physics only.
		// Forces are already zero from the previous step's ZeroForces() call in Evolve.

		// Reset per-step collision flag
		m_diag.occurred = false;

		// Record trail positions
		m_trail[0].push_back(m_body[0].O2W().pos);
		m_trail[1].push_back(m_body[1].O2W().pos);

		// Step physics (Evolve → Broad Phase → Narrow Phase → PostCollisionDetection → Resolve)
		m_physics.Step(dt, m_body);

		// If a collision occurred this step, capture post-impulse state and log diagnostics
		if (m_diag.occurred)
		{
			m_diag.after[0] = BodySnapshot::Capture(m_body[0]);
			m_diag.after[1] = BodySnapshot::Capture(m_body[1]);
			LogCollisionDiagnostics();

			if (m_pause_on_collision && m_diag.count == 1)
				m_steps = 0;
		}

		// NaN guard
		for (auto& body : m_body)
		{
			auto pos = body.O2W().pos;
			if (!std::isfinite(pos.x) || !std::isfinite(pos.y) || !std::isfinite(pos.z))
			{
				DbgLog("!!! NaN detected, resetting !!!\n");
				Reset();
				return;
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
			auto orbital = Cross3(s.pos, s.momentum.lin);
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
		auto dL_pct = Length(pre_total_L) > 0.01f ? 100.0f * Length(dL) / Length(pre_total_L) : 0.0f;
		DbgLog("Conservation:\n");
		DbgLog("  Delta lin momentum: (%.6f, %.6f, %.6f) |dp|=%.6f\n", dp.x, dp.y, dp.z, Length(dp));
		DbgLog("  Delta ang momentum: (%.6f, %.6f, %.6f) |dL|=%.6f (%.2f%%)\n", dL.x, dL.y, dL.z, Length(dL), dL_pct);
		DbgLog("  Delta KE: %.6f (%.2f%%)\n", dke, pre_total_ke > 0 ? 100.0f * dke / pre_total_ke : 0.0f);

		// Pass/fail thresholds.
		// Angular momentum conservation is approximate due to sub-step time correction:
		// the impulse is computed at the estimated collision time but applied to body
		// positions after Evolve. The error is proportional to penetration depth.
		bool momentum_ok = Length(dp) < 0.01f;
		auto ang_tol = pr::Max(0.01f, Length(pre_total_L) * 0.05f); // 5% of total
		bool ang_momentum_ok = Length(dL) < ang_tol;
		bool ke_ok = Abs(dke) < 0.01f * pre_total_ke;
		DbgLog("  Lin Momentum conserved: %s\n", momentum_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  Ang Momentum conserved: %s%s\n", ang_momentum_ok ? "PASS" : "*** FAIL ***",
			(Length(dL) > 0.01f && ang_momentum_ok) ? " (within sub-step tolerance)" : "");
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
		DbgLog("  v1': predicted=%.4f  actual=%.4f  error=%.6f\n", v1_pred, v1_actual, Abs(v1_pred - v1_actual));
		DbgLog("  v2': predicted=%.4f  actual=%.4f  error=%.6f\n", v2_pred, v2_actual, Abs(v2_pred - v2_actual));

		// Check Y and Z components are still near-zero (should be for pure 1D collision)
		auto vy1 = m_diag.after[0].lin_vel.y;
		auto vz1 = m_diag.after[0].lin_vel.z;
		auto vy2 = m_diag.after[1].lin_vel.y;
		auto vz2 = m_diag.after[1].lin_vel.z;
		DbgLog("  v1_yz: (%.6f, %.6f)  v2_yz: (%.6f, %.6f)\n", vy1, vz1, vy2, vz2);

		// Check angular velocity is near-zero (should be for centered 1D collision)
		auto w1 = m_diag.after[0].ang_vel;
		auto w2 = m_diag.after[1].ang_vel;
		DbgLog("  ang_vel1: (%.6f, %.6f, %.6f)  ang_vel2: (%.6f, %.6f, %.6f)\n",
			w1.x, w1.y, w1.z, w2.x, w2.y, w2.z);

		bool x_ok = Abs(v1_pred - v1_actual) < 0.05f && Abs(v2_pred - v2_actual) < 0.05f;
		bool yz_ok = Abs(vy1) < 0.05f && Abs(vz1) < 0.05f && Abs(vy2) < 0.05f && Abs(vz2) < 0.05f;
		bool no_spin = Length(w1) < 0.05f && Length(w2) < 0.05f;
		DbgLog("  X velocity match: %s\n", x_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  Y/Z remain zero:  %s\n", yz_ok ? "PASS" : "*** FAIL ***");
		DbgLog("  No angular vel:   %s\n", no_spin ? "PASS" : "*** FAIL ***");
	}

	// Run all 5 scenarios in sequence without rendering, log results for each.
	// Press 'T' to invoke.
	void RunAllTests()
	{
		m_steps = 0; // Stop normal stepping

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
				m_trail[0].push_back(m_body[0].O2W().pos);
				m_trail[1].push_back(m_body[1].O2W().pos);
				m_physics.Step(dt, m_body);
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

	// Render a frame
	void Render()
	{
		for (auto& body : m_body)
			body.UpdateGfx();

		// Rebuild trail and diagnostic visualisation
		RebuildDiagGfx();

		// Track the system centre of mass so both bodies are always visible
		auto total_mass = m_body[0].Mass() + m_body[1].Mass();
		auto com = (m_body[0].O2W().pos * m_body[0].Mass() + m_body[1].O2W().pos * m_body[1].Mass()) / total_mass;
		com.w = 1;
		View3D_CameraFocusPointSet(m_view3d.m_win, *reinterpret_cast<view3d::Vec4*>(&com));
		View3D_CameraCommit(m_view3d.m_win);

		Invalidate(false, nullptr, true);
	}

	// Rebuild the LDraw objects for trails, velocity arrows, and collision markers
	void RebuildDiagGfx()
	{
		using namespace pr::rdr12::ldraw;

		// Delete old diagnostic graphics
		if (m_trail_gfx) { View3D_ObjectDelete(m_trail_gfx); m_trail_gfx = nullptr; }
		if (m_diag_gfx) { View3D_ObjectDelete(m_diag_gfx); m_diag_gfx = nullptr; }

		// Build trail line strips
		if (m_trail[0].size() >= 2 || m_trail[1].size() >= 2)
		{
			Builder trail_builder;

			// Body A trail (red)
			if (m_trail[0].size() >= 2)
			{
				auto& line = trail_builder.Line("trail_A", 0xFFFF4040);
				line.strip(m_trail[0][0]);
				for (size_t i = 1; i < m_trail[0].size(); ++i)
					line.line_to(m_trail[0][i]);
			}

			// Body B trail (blue)
			if (m_trail[1].size() >= 2)
			{
				auto& line = trail_builder.Line("trail_B", 0xFF4040FF);
				line.strip(m_trail[1][0]);
				for (size_t i = 1; i < m_trail[1].size(); ++i)
					line.line_to(m_trail[1][i]);
			}

			// System centre of mass trail (yellow)
			if (m_trail[0].size() >= 2 && m_trail[1].size() >= 2)
			{
				auto total_mass = m_body[0].Mass() + m_body[1].Mass();
				auto& line = trail_builder.Line("trail_CoM", 0xFFFFFF00);
				auto com0 = (m_trail[0][0] * m_body[0].Mass() + m_trail[1][0] * m_body[1].Mass()) / total_mass;
				com0.w = 1;
				line.strip(com0);
				auto n = std::min(m_trail[0].size(), m_trail[1].size());
				for (size_t i = 1; i < n; ++i)
				{
					auto com = (m_trail[0][i] * m_body[0].Mass() + m_trail[1][i] * m_body[1].Mass()) / total_mass;
					com.w = 1;
					line.line_to(com);
				}
			}

			auto trail_text = trail_builder.ToText(false);
			m_trail_gfx = View3D_ObjectCreateLdrA(trail_text.c_str(), false, nullptr, nullptr);
			if (m_trail_gfx)
				View3D_WindowAddObject(m_view3d.m_win, m_trail_gfx);
		}

		// Build diagnostic overlays: velocity arrows, contact marker, collision normal
		{
			Builder diag_builder;

			// Current velocity arrows for each body
			for (int i = 0; i != 2; ++i)
			{
				auto vel = m_body[i].VelocityWS();
				auto pos = m_body[i].O2W().pos;
				auto speed = Length(vel.lin);
				if (speed > 0.01f)
				{
					auto vel_name = std::string(pr::FmtS("vel_%d", i));
					auto& arrow = diag_builder.Line(vel_name, i == 0 ? 0xFFFF8080u : 0xFF8080FFu);
					arrow.line(pos, pos + vel.lin);
					arrow.arrow(EArrowType::Fwd, 8.0f);
				}

				// Angular velocity arrows (green)
				auto aspeed = Length(vel.ang);
				if (aspeed > 0.01f)
				{
					auto avel_name = std::string(pr::FmtS("avel_%d", i));
					auto& arrow = diag_builder.Line(avel_name, 0xFF00FF00);
					arrow.line(pos, pos + vel.ang);
					arrow.arrow(EArrowType::Fwd, 6.0f);
				}
			}

			// Collision markers (shown after collision has occurred)
			if (m_diag.count > 0)
			{
				// Contact point
				diag_builder.Sphere("contact_pt", 0xFFFFFFFF).radius(0.08f).pos(m_diag.contact_point_ws);

				// Contact normal
				auto& normal = diag_builder.Line("contact_normal", 0xFF00FFFF);
				normal.line(m_diag.contact_point_ws, m_diag.contact_point_ws + m_diag.contact_normal_ws * 2.0f);
				normal.arrow(EArrowType::Fwd, 8.0f);
			}

			auto diag_text = diag_builder.ToText(false);
			m_diag_gfx = View3D_ObjectCreateLdrA(diag_text.c_str(), false, nullptr, nullptr);
			if (m_diag_gfx)
				View3D_WindowAddObject(m_view3d.m_win, m_diag_gfx);
		}
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

	// Handle errors reported within view3d
	static void __stdcall ReportErrorCB(void* ctx, char const* msg, char const* filepath, int line, int64_t)
	{
		auto this_ = static_cast<MainUI*>(ctx);
		auto message = pr::FmtS(L"%s(%d): %s", filepath, line, msg);
		::MessageBoxW(this_->m_hwnd, message, L"Error", MB_OK);
	}
};

// Entry point
int __stdcall WinMain(HINSTANCE, HINSTANCE, LPTSTR lpCmdLine, int)
{
	pr::InitCom com;
	pr::GdiPlus gdi;

	try
	{
		pr::win32::LoadDll<struct View3d>(L"view3d-12.dll");
		InitCtrls();

		MainUI main;
		main.Show();

		// If launched with -autotest, run all tests immediately then exit
		if (lpCmdLine && strstr(lpCmdLine, "-autotest"))
		{
			main.RunAllTests();
			return 0;
		}

		WinGuiMsgLoop loop;
		loop.AddLoop(100.0, false, [&](double dt) { main.Step(dt); });
		loop.AddLoop(60.0, true, [&](double) { main.Render(); });
		loop.AddMessageFilter(main);
		return loop.Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA("Died: ");
		OutputDebugStringA(ex.what());
		OutputDebugStringA("\n");
		return -1;
	}
}
