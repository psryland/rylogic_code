//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/core/cameras/free_camera.h"

namespace las::camera
{
	FreeCamera::FreeCamera(Camera& cam, InputHandler& input)
		: ICamera(cam, input)
		, m_speed(20.0f)
		, m_speed_min(0.5f)
		, m_speed_max(2000.0f)
	{
	}

	void FreeCamera::OnAction(input::Action action)
	{
		switch (action.m_action)
		{
			case input::EAction::FreeCamera_MoveForward:
			{
				//auto c2w = m_cam.CameraToWorld();
				//c2w.pos += -c2w.z * m_speed * dt;
				//m_cam.CameraToWorld(c2w);
				break;
			}
			
			// todo: handle these actions
			// FreeCamera_MoveForward,
			// FreeCamera_MoveBack,
			// FreeCamera_MoveLeft,
			// FreeCamera_MoveRight,
			// FreeCamera_MoveUp,
			// FreeCamera_MoveDown,
			// FreeCamera_SpeedUp,
			// FreeCamera_SlowDown,
		}
	}
}

