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
		float m_speed;     // Movement speed (m/s)
		float m_speed_min; // Minimum speed
		float m_speed_max; // Maximum speed

		FreeCamera(Camera& cam, InputHandler& input);
		void OnAction(input::Action action) override;
	};
}