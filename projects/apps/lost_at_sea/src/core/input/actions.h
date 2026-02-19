//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"

namespace las::input
{
	enum class EAction
	{
		// Global actions
		CycleCameraMode,
		ToggleDiagnostics,

		// Free camera
		FreeCamera_MoveForward,
		FreeCamera_MoveBack,
		FreeCamera_MoveLeft,
		FreeCamera_MoveRight,
		FreeCamera_MoveUp,
		FreeCamera_MoveDown,
		FreeCamera_SpeedUp,
		FreeCamera_SlowDown,
		FreeCamera_Yaw,
		FreeCamera_Pitch,

		// Ship control (stub)
		Ship_Accelerate,
		Ship_Decelerate,
		Ship_TurnLeft,
		Ship_TurnRight,

		// Menu navigation (stub)
		Menu_NavigateUp,
		Menu_NavigateDown,
		Menu_Select,
		Menu_Back,
		Max
	};

	struct Action
	{
		EAction m_action; // The action being raised
		float m_axis;     // Normalised axis value or rotation amount (radians)
		float m_dt;       // Time step for continuous actions (seconds)
	};
}
