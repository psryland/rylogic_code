//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/core/input/input_handler.h"

namespace las
{
	InputHandler::InputHandler()
		:m_events()
		,m_mode(new input::Mode_FreeCamera(*this))
	{
	}

	// Get/Set input mode
	input::EMode InputHandler::Mode() const
	{
		return m_mode->Mode();
	}
	void InputHandler::Mode(input::EMode mode)
	{
		if (Mode() == mode)
			return;

		switch (mode)
		{
			case input::EMode::FreeCamera:
			{
				m_mode = std::shared_ptr<input::Mode_FreeCamera>{ new input::Mode_FreeCamera(*this) };
				break;
			}
			case input::EMode::ShipControl:
			case input::EMode::MenuNavigation:
			default:
			{
				throw std::runtime_error("Unsupported input mode");
			}
		}
		
		ModeChanged(*this, Mode());
	}

	// Sim step: process buffered input events and raise game actions as needed
	void InputHandler::Step(float dt)
	{
		for (auto& event : m_events)
		{
			switch (event.index())
			{
				case 0: // KeyEventArgs
				{
					auto& args = std::get<KeyEventArgs>(event);
					KeyEventDispatch(args);
					break;
				}
				case 1: // MouseEventArgs
				{
					auto& args = std::get<MouseEventArgs>(event);
					(void)args; //todo
					break;
				}
				case 2: // MouseWheelArgs
				{
					auto& args = std::get<MouseWheelArgs>(event);
					(void)args; //todo
					break;
				}
			}
		}

		// Reset the event buffer for the next frame
		m_events.resize(0);
	}

	// Process a key events and raise game actions as needed
	void InputHandler::KeyEventDispatch(KeyEventArgs& args)
	{
		// Handle global key bindings (e.g. mode switching)
		switch (args.m_vk_key)
		{
			// Cycle camera modes
			case VK_F1:
			{
				Action(*this, { input::EAction::CycleCameraMode });
				args.m_handled = true;
				break;
			}

			// Display diagnostic UIs
			case VK_F3:
			{
				Action(*this, { input::EAction::ToggleDiagnostics });
				args.m_handled = true;
				break;
			}
		}

		// Forward the event to the active mode's handler
		m_mode->HandleKeyEvent(args);
	}
	void InputHandler::MouseEventDispatch(MouseEventArgs& args)
	{
		// Forward the event to the active mode's handler
		m_mode->HandleMouseEvent(args);
	}
	void InputHandler::WheelEventDispatch(MouseWheelArgs& args)
	{
		// Forward the event to the active mode's handler
		m_mode->HandleWheelEvent(args);
	}

	// Raw mouse input
	void InputHandler::OnMouseButton(gui::MouseEventArgs& args)
	{
		m_events.emplace_back(args);
	}
	void InputHandler::OnMouseClick(gui::MouseEventArgs& args)
	{
		m_events.emplace_back(args);
	}
	void InputHandler::OnMouseMove(gui::MouseEventArgs& args)
	{
		m_events.emplace_back(args);
	}
	void InputHandler::OnMouseWheel(gui::MouseWheelArgs& args)
	{
		m_events.emplace_back(args);
	}

	// Raw key input
	void InputHandler::OnKey(KeyEventArgs& args)
	{
		m_events.emplace_back(args);
	}
}