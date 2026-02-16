//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Simulation state that is handed off from Step → Render each frame.
#pragma once
#include "src/forward.h"

namespace las
{
	// Per-frame simulation state snapshot.
	// Step writes this; Render reads the latest committed copy.
	struct SimState
	{
		v4 m_camera_pos;      // Camera world position
		double m_sim_time;    // Accumulated simulation time
		v4 m_sun_direction;   // Sun direction (normalised, points toward sun)
		v4 m_sun_colour;      // Sun light colour
		float m_sun_intensity; // Sun intensity (0=night, 1=noon)

		SimState()
			: m_camera_pos(v4::Origin())
			, m_sim_time(0.0)
			, m_sun_direction(Normalise(v4(0.5f, 0.3f, 0.8f, 0.0f)))
			, m_sun_colour(v4(1.0f, 0.95f, 0.85f, 1.0f))
			, m_sun_intensity(1.0f)
		{}
	};
}
