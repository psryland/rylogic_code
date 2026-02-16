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
		v4 m_camera_pos; // Camera world position (will become player position)
		double m_sim_time; // Accumulated simulation time

		SimState()
			: m_camera_pos(v4::Origin())
			, m_sim_time(0.0)
		{}
	};
}
