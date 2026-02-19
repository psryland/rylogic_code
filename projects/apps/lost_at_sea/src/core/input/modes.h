//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#pragma once
#include "src/forward.h"

namespace las::input
{
	// Input modes determine how raw input maps to game actions
	enum class EMode
	{
		FreeCamera,     // Free-look camera for development
		ShipControl,    // Player controls the ship (stub)
		MenuNavigation, // Menu/UI navigation (stub)
	};

	// Base class for input modes
	struct IMode
	{
		// Notes:
		//  - An input mode represents a specific mapping from raw input to actions.
		//  - There can be many different modes, but only one is active at a time.
		InputHandler& m_ih;
		IMode(InputHandler& ih) :m_ih(ih) {}
		virtual ~IMode() = default;
		virtual EMode Mode() const = 0;
		virtual void HandleKeyEvent(KeyEventArgs& args) = 0;
		virtual void HandleMouseEvent(MouseEventArgs& args) = 0;
		virtual void HandleWheelEvent(MouseWheelArgs& args) = 0;
	};

	struct Mode_FreeCamera : IMode
	{
		// Mouse state
		v2 m_mouse_pos;        // Current normalised mouse position [-1,1]
		v2 m_mouse_ref;        // Mouse position at the start of a drag
		bool m_rmb_down;       // Right mouse button held
		bool m_lmb_down;       // Left mouse button held
		bool m_mmb_down;       // Middle mouse button held

		Mode_FreeCamera(InputHandler& ih);
		EMode Mode() const;
		void HandleKeyEvent(KeyEventArgs& args) override;
		void HandleMouseEvent(MouseEventArgs& args) override;
		void HandleWheelEvent(MouseWheelArgs& args) override;
	};

	struct Mode_ShipControl : IMode
	{
		void HandleKeyEvent(KeyEventArgs& args) override {}
		void HandleMouseEvent(MouseEventArgs& args) override {}
		void HandleWheelEvent(MouseWheelArgs& args) override {}
	};

	struct Mode_MenuNavigation : IMode
	{
		void HandleKeyEvent(KeyEventArgs& args) override {}
		void HandleMouseEvent(MouseEventArgs& args) override {}
		void HandleWheelEvent(MouseWheelArgs& args) override {}
	};
}
