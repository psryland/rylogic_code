//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"
#include "src/core/cameras/icamera.h"
#include "src/core/input/actions.h"

namespace las::camera
{
	struct FreeCamera : ICamera
	{
		float m_speed;     // Target movement speed (m/s)
		float m_speed_min; // Minimum speed
		float m_speed_max; // Maximum speed

		// Inertia
		v4 m_velocity;     // Current velocity (world space, m/s)
		v4 m_move_wish;    // Per-frame accumulated desired movement direction
		float m_accel;     // Acceleration time constant (higher = snappier)
		float m_damping;   // Deceleration time constant (higher = faster stop)

		FreeCamera(Camera& cam, InputHandler& input);
		void OnAction(input::Action action) override;
		void Update(float dt) override;
		char const* Name() const override { return "Free Camera"; }
		float Speed() const override { return m_speed; }
	};
}