//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"
#include "src/core/cameras/icamera.h"
#include "src/core/input/actions.h"

namespace las
{
	struct Ship;
}

namespace las::camera
{
	struct ShipCamera : ICamera
	{
		// Notes:
		//  - Third-person camera that follows the ship using a spring arm.
		//  - The desired camera position is computed from the ship position,
		//    orbit angles (yaw/pitch), and arm length.
		//  - A spring system smoothly interpolates from the current position
		//    to the desired position, creating a natural follow feel.

		Ship const& m_ship;

		// Orbit parameters
		float m_yaw;           // Orbit yaw around world Z (radians)
		float m_pitch;         // Orbit pitch above horizontal (radians)
		float m_arm_length;    // Distance from ship to camera (metres)
		float m_arm_min;       // Minimum arm length
		float m_arm_max;       // Maximum arm length

		// Spring dynamics
		v4 m_current_pos;      // Smoothed camera position
		v4 m_pos_velocity;     // Position velocity for spring damping
		float m_stiffness;     // Spring stiffness (higher = faster catch-up)
		float m_damping;       // Spring damping (higher = less oscillation)

		// Look target offset above ship origin
		float m_look_offset_z;

		ShipCamera(Camera& cam, InputHandler& input, Ship const& ship);
		void OnAction(input::Action action) override;
		void Update(float dt) override;
		char const* Name() const override { return "Ship Camera"; }
	};
}
