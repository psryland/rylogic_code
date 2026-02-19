//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"
#include "src/core/input/actions.h"

namespace las::camera
{
	struct ICamera
	{
		// Notes:

	protected:

		Camera& m_cam;
		AutoSub m_sub_input;

		ICamera(Camera& cam, InputHandler& input);
		virtual ~ICamera() = default;
		ICamera(ICamera&&) = delete;
		ICamera(ICamera const&) = delete;
		ICamera& operator=(ICamera&&) = delete;
		ICamera& operator=(ICamera const&) = delete;

		virtual void OnAction(input::Action action);
	};
}
