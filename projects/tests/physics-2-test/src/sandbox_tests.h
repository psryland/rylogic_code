//************************************
// Physics-2 Sandbox
//  Copyright (c) Rylogic Ltd 2016
//************************************
// Embedded unit tests for the physics-2-test application.
// Run with the `-unittest` command line argument.
// These tests use the PR_UNITTESTS framework and run headlessly
// (no View3D, no window) — they operate directly on RigidBody objects.
//
// For a broader set of collision tests, see also:
//   projects/rylogic/physics-2/src/unittests/test_collision.h
//
#pragma once

// Force PR_UNITTESTS to be defined so the test macros are available
// even in Release builds (when running from -unittest mode).
#ifndef PR_UNITTESTS
#define PR_UNITTESTS 1
#define PR_UNITTESTS_DEFINED_HERE
#endif

#include "pr/common/unittests.h"
#include "pr/collision/shape_box.h"
#include "pr/physics-2/rigid_body/rigid_body.h"
#include "pr/physics-2/shape/inertia.h"
#include "pr/physics-2/broadphase/brute.h"
#include "pr/physics-2/integrator/engine.h"
#include "pr/physics-2/material/material_map.h"

namespace pr::physics
{
	PRUnitTestClass(SandboxCollisionTests)
	{
		using TestEngine = Engine<broadphase::Brute<RigidBody>, MaterialMap>;

		// Snapshot of conserved system quantities
		struct SystemState
		{
			v4 total_lin_momentum;
			v4 total_ang_momentum;
			float total_ke;

			static SystemState Capture(RigidBody const& a, RigidBody const& b)
			{
				auto s = SystemState{};
				s.total_lin_momentum = a.MomentumWS().lin + b.MomentumWS().lin;
				auto La = a.MomentumWS().ang + Cross3(a.O2W().pos, a.MomentumWS().lin);
				auto Lb = b.MomentumWS().ang + Cross3(b.O2W().pos, b.MomentumWS().lin);
				s.total_ang_momentum = La + Lb;
				s.total_ke = a.KineticEnergy() + b.KineticEnergy();
				return s;
			}
		};

		struct CollisionResult
		{
			SystemState before;
			SystemState after;
			v8motion vel_a;
			v8motion vel_b;
			bool collision_occurred;
		};

		// Run a two-body elastic collision scenario to completion
		static CollisionResult RunScenario(
			collision::ShapeBox& box,
			v4 pos_a, v4 vel_a, float mass_a,
			v4 pos_b, v4 vel_b, float mass_b)
		{
			auto result = CollisionResult{};
			result.collision_occurred = false;

			auto inertia_a = Inertia::Box(box.m_radius, mass_a);
			auto inertia_b = Inertia::Box(box.m_radius, mass_b);
			RigidBody bodies[2] = {
				RigidBody{&box, m4x4::Translation(pos_a), inertia_a},
				RigidBody{&box, m4x4::Translation(pos_b), inertia_b},
			};
			auto& body_a = bodies[0];
			auto& body_b = bodies[1];
			body_a.VelocityWS(v4Zero, vel_a);
			body_b.VelocityWS(v4Zero, vel_b);

			auto engine = TestEngine{};
			auto& mat = engine.m_materials(0);
			mat.m_elasticity_norm = 1.0f;
			mat.m_elasticity_tang = 0.0f;
			mat.m_elasticity_tors = 0.0f;
			mat.m_friction_static = 0.0f;

			engine.m_broadphase.Add(body_a);
			engine.m_broadphase.Add(body_b);

			engine.PostCollisionDetection += [&](auto&, auto& collisions)
			{
				if (collisions.empty()) return;
				result.before = SystemState::Capture(body_a, body_b);
				result.collision_occurred = true;
			};

			auto const dt = 1.0f / 100.0f;
			for (int step = 0; step < 5000; ++step)
			{
				body_a.ZeroForces();
				body_b.ZeroForces();
				engine.Step(dt, bodies);

				if (result.collision_occurred)
				{
					result.after = SystemState::Capture(body_a, body_b);
					result.vel_a = body_a.VelocityWS();
					result.vel_b = body_b.VelocityWS();
					break;
				}
			}
			return result;
		}

		// Head-on equal mass: velocities should swap exactly
		PRUnitTestMethod(HeadOnEqualMass)
		{
			auto box = collision::ShapeBox(v4{2, 2, 2, 0});
			auto r = RunScenario(box,
				v4{-5, 0, 0, 1}, v4{+3, 0, 0, 0}, 10.0f,
				v4{+5, 0, 0, 1}, v4{-3, 0, 0, 0}, 10.0f);

			PR_EXPECT(r.collision_occurred);
			PR_EXPECT(FEqlRelative(r.after.total_lin_momentum, r.before.total_lin_momentum, 0.001f));
			PR_EXPECT(FEqlRelative(r.after.total_ke, r.before.total_ke, 0.001f));
			PR_EXPECT(FEqlRelative(r.vel_a.lin.x, -3.0f, 0.001f));
			PR_EXPECT(FEqlRelative(r.vel_b.lin.x, +3.0f, 0.001f));
		}

		// Head-on different mass: analytic prediction
		PRUnitTestMethod(HeadOnDiffMass)
		{
			auto box = collision::ShapeBox(v4{2, 2, 2, 0});
			auto r = RunScenario(box,
				v4{-5, 0, 0, 1}, v4{+3, 0, 0, 0}, 10.0f,
				v4{+5, 0, 0, 1}, v4{-3, 0, 0, 0}, 5.0f);

			PR_EXPECT(r.collision_occurred);
			PR_EXPECT(FEqlRelative(r.after.total_lin_momentum, r.before.total_lin_momentum, 0.001f));
			PR_EXPECT(FEqlRelative(r.after.total_ke, r.before.total_ke, 0.001f));
			PR_EXPECT(FEqlRelative(r.vel_a.lin.x, -1.0f, 0.001f));
			PR_EXPECT(FEqlRelative(r.vel_b.lin.x, +5.0f, 0.001f));
		}

		// Stationary target: moving body stops, target takes velocity
		PRUnitTestMethod(StationaryTarget)
		{
			auto box = collision::ShapeBox(v4{2, 2, 2, 0});
			auto r = RunScenario(box,
				v4{-5, 0, 0, 1}, v4{+3, 0, 0, 0}, 10.0f,
				v4{+5, 0, 0, 1}, v4{ 0, 0, 0, 0}, 10.0f);

			PR_EXPECT(r.collision_occurred);
			PR_EXPECT(FEqlRelative(r.after.total_lin_momentum, r.before.total_lin_momentum, 0.001f));
			PR_EXPECT(FEqlRelative(r.after.total_ke, r.before.total_ke, 0.001f));
			PR_EXPECT(FEqlRelative(r.vel_a.lin.x, 0.0f, 0.05f));
			PR_EXPECT(FEqlRelative(r.vel_b.lin.x, 3.0f, 0.001f));
		}
	};
}

// Clean up our conditional define
#ifdef PR_UNITTESTS_DEFINED_HERE
#undef PR_UNITTESTS
#undef PR_UNITTESTS_DEFINED_HERE
#endif
