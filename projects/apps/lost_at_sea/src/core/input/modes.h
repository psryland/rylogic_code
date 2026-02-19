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

		// Per-frame update for continuous actions (called after event processing)
		virtual void Update(float dt) = 0;
	};

	struct Mode_FreeCamera : IMode
	{
		// Mouse state
		v2 m_mouse_pos;        // Current mouse position (client pixels)
		v2 m_mouse_ref_lb;     // Mouse position at the start of a drag of Left button down
		v2 m_mouse_ref_rb;     // Mouse position at the start of a drag of Right button down
		bool m_rmb_down;       // Right mouse button held
		bool m_lmb_down;       // Left mouse button held
		bool m_mmb_down;       // Middle mouse button held

		// Key state tracking for continuous movement
		bool m_key_w;
		bool m_key_s;
		bool m_key_a;
		bool m_key_d;
		bool m_key_q;
		bool m_key_e;

		// Mouse look sensitivity (radians per pixel)
		float m_mouse_sensitivity;

		Mode_FreeCamera(InputHandler& ih);
		EMode Mode() const override;
		void HandleKeyEvent(KeyEventArgs& args) override;
		void HandleMouseEvent(MouseEventArgs& args) override;
		void HandleWheelEvent(MouseWheelArgs& args) override;
		void Update(float dt) override;
	};

	struct Mode_ShipControl : IMode
	{
		using IMode::IMode;
		EMode Mode() const override { return EMode::ShipControl; }
		void HandleKeyEvent(KeyEventArgs& args) override { (void)args; }
		void HandleMouseEvent(MouseEventArgs& args) override { (void)args; }
		void HandleWheelEvent(MouseWheelArgs& args) override { (void)args; }
		void Update(float dt) override { (void)dt; }
	};

	struct Mode_MenuNavigation : IMode
	{
		using IMode::IMode;
		EMode Mode() const override { return EMode::MenuNavigation; }
		void HandleKeyEvent(KeyEventArgs& args) override { (void)args; }
		void HandleMouseEvent(MouseEventArgs& args) override { (void)args; }
		void HandleWheelEvent(MouseWheelArgs& args) override { (void)args; }
		void Update(float dt) override { (void)dt; }
	};
}
