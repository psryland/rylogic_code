//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/core/input/modes.h"
#include "src/core/input/actions.h"
#include "src/core/input/input_handler.h"

namespace las::input
{
	Mode_FreeCamera::Mode_FreeCamera(InputHandler& ih)
		: IMode(ih)
		, m_mouse_pos()
		, m_mouse_ref()
		, m_rmb_down(false)
		, m_lmb_down(false)
		, m_mmb_down(false)
	{}

	EMode Mode_FreeCamera::Mode() const
	{
		return EMode::FreeCamera;
	}

	void Mode_FreeCamera::HandleKeyEvent(KeyEventArgs& args)
	{
		// TODO: translate the args.m_vk_key and args.m_down states into actions
		switch (args.m_vk_key)
		{
			case VK_SHIFT:
			{
				if (args.m_down)
					m_ih.Action(m_ih, { input::EAction::FreeCamera_SpeedUp });
				
				break;
			}

			// todo: emit these actions based on the key events
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

	void Mode_FreeCamera::HandleMouseEvent(MouseEventArgs& args) {}
	void Mode_FreeCamera::HandleWheelEvent(MouseWheelArgs& args) {}
}
