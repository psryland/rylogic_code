//*****************************************************************************
// Procedural animation - target paths
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include "pr/animation/forward.h"

namespace pr::animation
{
	// The derivatives of a moving frame. Linear values are model-space per second (w = 0); angular values are model-space axis * radians per second.
	struct Motion
	{
		v4 vel = {};
		v4 acc = {};
		v4 ang_vel = {};
		v4 ang_acc = {};
	};

	// A frame that moves smoothly to a goal over a duration, and can be given a new goal at any time.
	//  - The motion is a quintic curve in position and rotation, so position, velocity and acceleration are continuous, including across 'Retarget'.
	//  - Once the duration has passed, the path holds its end frame. Its derivatives are then those at the end of the curve, so a path given a
	//    non-zero end motion should be retargeted before it finishes.
	struct TargetPath
	{
		math::Hermite5Transform<float> m_curve;
		float m_time;
		float m_duration;

		// Construct a stationary, finished path at 'start'
		explicit TargetPath(xform const& start = xform::Identity())
			: m_curve(start.pos, v4::Zero(), v4::Zero(), start.rot, v4::Zero(), v4::Zero(), start.pos, v4::Zero(), v4::Zero(), start.rot, v4::Zero(), v4::Zero(), 1.0f)
			, m_time(1.0f)
			, m_duration(1.0f)
		{
		}

		// Move from the current frame and motion to 'goal' over 'duration' seconds (> 0), arriving with 'end' motion
		void Retarget(xform const& goal, float duration, Motion const& end = {})
		{
			// The new curve starts from the current state so the motion stays continuous
			pr_assert(duration > 0 && "Duration must be positive");
			auto now = Frame();
			m_curve = math::Hermite5Transform<float>(
				now.pos, Velocity(), Acceleration(), now.rot, AngularVelocity(), AngularAcceleration(),
				goal.pos, end.vel, end.acc, goal.rot, end.ang_vel, end.ang_acc,
				duration);
			m_time = 0.0f;
			m_duration = duration;
		}

		// Advance time by 'dt' seconds (>= 0)
		void Step(float dt)
		{
			m_time = std::min(m_time + dt, m_duration);
		}

		// True once the path has reached its goal
		bool Finished() const
		{
			return m_time >= m_duration;
		}

		// The current frame
		xform Frame() const
		{
			return m_curve.Eval(m_time);
		}

		// The current linear velocity
		v4 Velocity() const
		{
			return m_curve.pos.EvalDerivative(m_time);
		}

		// The current linear acceleration
		v4 Acceleration() const
		{
			return m_curve.pos.EvalDerivative2(m_time);
		}

		// The current angular velocity
		v4 AngularVelocity() const
		{
			return m_curve.rot.EvalDerivative(m_time);
		}

		// The current angular acceleration
		v4 AngularAcceleration() const
		{
			return m_curve.rot.EvalDerivative2(m_time);
		}
	};

	// A value in [0,1] that moves smoothly to a goal over a duration, starting and stopping at rest. Used for layer weights.
	//  - Like 'TargetPath', a new goal can be given at any time and the value, rate and rate of change stay continuous.
	struct Fade
	{
		math::Hermite5Vector<float> m_curve;
		float m_time;
		float m_duration;

		// Construct a stationary fade at 'value'
		explicit Fade(float value = 0.0f)
			: m_curve(v4(value, 0, 0, 0), v4::Zero(), v4::Zero(), v4(value, 0, 0, 0), v4::Zero(), v4::Zero(), 1.0f)
			, m_time(1.0f)
			, m_duration(1.0f)
		{
		}

		// Move from the current value to 'goal' (in [0,1]) over 'duration' seconds (> 0), arriving at rest
		void Start(float goal, float duration)
		{
			// The new curve starts from the current state so the fade stays continuous
			pr_assert(duration > 0 && "Duration must be positive");
			m_curve = math::Hermite5Vector<float>(
				m_curve.Eval(m_time), m_curve.EvalDerivative(m_time), m_curve.EvalDerivative2(m_time),
				v4(goal, 0, 0, 0), v4::Zero(), v4::Zero(),
				duration);
			m_time = 0.0f;
			m_duration = duration;
		}

		// Advance time by 'dt' seconds (>= 0)
		void Step(float dt)
		{
			m_time = std::min(m_time + dt, m_duration);
		}

		// True once the fade has reached its goal
		bool Finished() const
		{
			return m_time >= m_duration;
		}

		// The current value. A retarget mid-fade can overshoot slightly, so the value is clamped to [0,1].
		float Value() const
		{
			return std::clamp(m_curve.Eval(m_time).x, 0.0f, 1.0f);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(TargetPathTests)
	{
		PRUnitTestMethod(ReachesGoal, Quick)
		{
			// The path arrives at its goal and holds it
			auto path = TargetPath(xform::Identity());
			auto goal = xform(v4(1, 2, 3, 1), quat(v4::ZAxis(), 1.0f));
			path.Retarget(goal, 0.5f);
			PR_EXPECT(!path.Finished());
			for (int i = 0; i != 10; ++i)
				path.Step(0.1f);

			PR_EXPECT(path.Finished());
			PR_EXPECT(FEqlAbsolute(path.Frame().pos, goal.pos, 1e-5f));
			PR_EXPECT(FEqlOrientation(path.Frame().rot, goal.rot, 1e-4f));
			PR_EXPECT(FEqlAbsolute(path.Velocity(), v4::Zero(), 1e-5f));
		}
		PRUnitTestMethod(RetargetIsContinuous, Quick)
		{
			// Changing the goal mid-path keeps the frame and its derivatives
			auto path = TargetPath(xform::Identity());
			path.Retarget(xform(v4(1, 0, 0, 1), quat(v4::ZAxis(), 1.0f)), 1.0f);
			path.Step(0.4f);

			auto frame = path.Frame();
			auto vel = path.Velocity();
			auto acc = path.Acceleration();
			auto avel = path.AngularVelocity();
			auto aacc = path.AngularAcceleration();
			path.Retarget(xform(v4(0, 2, 1, 1), quat(v4::XAxis(), -0.5f)), 0.7f);

			PR_EXPECT(FEqlAbsolute(path.Frame().pos, frame.pos, 1e-5f));
			PR_EXPECT(FEqlOrientation(path.Frame().rot, frame.rot, 1e-4f));
			PR_EXPECT(FEqlAbsolute(path.Velocity(), vel, 1e-4f));
			PR_EXPECT(FEqlAbsolute(path.Acceleration(), acc, 1e-3f));
			PR_EXPECT(FEqlAbsolute(path.AngularVelocity(), avel, 1e-3f));
			PR_EXPECT(FEqlAbsolute(path.AngularAcceleration(), aacc, 1e-2f));
		}
		PRUnitTestMethod(FadeInOut, Quick)
		{
			// A fade reaches its goal, and reversing it mid-way stays within [0,1]
			auto fade = Fade(0.0f);
			fade.Start(1.0f, 0.3f);
			fade.Step(0.15f);
			auto mid = fade.Value();
			PR_EXPECT(mid > 0.0f && mid < 1.0f);

			fade.Start(0.0f, 0.2f);
			PR_EXPECT(FEqlAbsolute(fade.Value(), mid, 1e-5f));
			for (int i = 0; i != 5; ++i)
			{
				fade.Step(0.05f);
				PR_EXPECT(fade.Value() >= 0.0f && fade.Value() <= 1.0f);
			}
			PR_EXPECT(fade.Finished());
			PR_EXPECT(FEqlAbsolute(fade.Value(), 0.0f, 1e-6f));
		}
	};
}
#endif
