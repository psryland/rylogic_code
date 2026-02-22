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

	public:

		// Per-frame update for physics-like behaviour (inertia, damping, etc.)
		virtual void Update(float dt) { (void)dt; }

		// Human-readable name for the debug UI
		virtual char const* Name() const { return "Camera"; }

		// Current movement speed (for diagnostics). Override in modes that have speed control.
		virtual float Speed() const { return 0.0f; }
	};
}
