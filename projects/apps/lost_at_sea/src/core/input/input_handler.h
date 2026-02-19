//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"
#include "src/core/input/actions.h"
#include "src/core/input/modes.h"

namespace las
{
	struct InputHandler
	{
		// Notes:
		//  - The input handler's job is to translate raw input (mouse/keyboard/etc) into game actions.
		//    It isolates the rest of the game from handling specific input devices.
		//  - For deterministic behaviour, all input is buffered, then processed in the Step function.
		//  - The actual mapping from input to actions is done by 'Modes'. Only one mode can be active
		//    at a time.
		//
		// Plans:
		//  - Eventually this class will handle input devices being connected/disconnected (joysticks, etc)
		//  - This class will support custom key bindings to actions.

	private:

		using input_event_t = std::variant<KeyEventArgs, MouseEventArgs, MouseWheelArgs>;
		
		// The buffer of input events collected between Step calls
		vector<input_event_t, 1> m_events;

		// Current input mode that performs the mapping to actions
		std::shared_ptr<input::IMode> m_mode;

	public:

		InputHandler();

		// Get/Set input mode
		input::EMode Mode() const;
		void Mode(input::EMode mode);

		// Number of buffered input events (for diagnostics)
		size_t EventCount() const { return m_events.size(); }

		// Sim step: process buffered input events and raise game actions as needed
		void Step(float dt);

		// Raised when the input mode changes
		pr::EventHandler<InputHandler, input::EMode, true> ModeChanged;

		// Raised when a game action occurs (e.g. move forward, turn left, etc).
		pr::EventHandler<InputHandler, input::Action, true> Action;

	private:

		friend struct MainUI;
		friend struct input::IMode;

		void KeyEventDispatch(KeyEventArgs& args);
		void MouseEventDispatch(MouseEventArgs& args);
		void WheelEventDispatch(MouseWheelArgs& args);

		// Raw mouse input
		void OnMouseButton(gui::MouseEventArgs& args);
		void OnMouseClick(gui::MouseEventArgs& args);
		void OnMouseMove(gui::MouseEventArgs& args);
		void OnMouseWheel(gui::MouseWheelArgs& args);
		
		// Raw key input
		void OnKey(KeyEventArgs& args);
	};
}
