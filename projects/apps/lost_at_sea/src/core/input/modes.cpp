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
		, m_mouse_ref_lb()
		, m_mouse_ref_rb()
		, m_rmb_down(false)
		, m_lmb_down(false)
		, m_mmb_down(false)
		, m_key_w(false)
		, m_key_s(false)
		, m_key_a(false)
		, m_key_d(false)
		, m_key_q(false)
		, m_key_e(false)
		, m_mouse_sensitivity(0.003f)
	{}

	EMode Mode_FreeCamera::Mode() const
	{
		return EMode::FreeCamera;
	}

	void Mode_FreeCamera::HandleKeyEvent(KeyEventArgs& args)
	{
		// Track movement key states for continuous polling in Update()
		switch (args.m_vk_key)
		{
			case 'W': m_key_w = args.m_down; args.m_handled = true; break;
			case 'S': m_key_s = args.m_down; args.m_handled = true; break;
			case 'A': m_key_a = args.m_down; args.m_handled = true; break;
			case 'D': m_key_d = args.m_down; args.m_handled = true; break;
			case 'Q': m_key_q = args.m_down; args.m_handled = true; break;
			case 'E': m_key_e = args.m_down; args.m_handled = true; break;
		}
	}

	void Mode_FreeCamera::HandleMouseEvent(MouseEventArgs& args)
	{
		auto pt = v2{static_cast<float>(args.m_point.x), static_cast<float>(args.m_point.y)};

		// Track button state from key_state flags. For WM_MOUSEMOVE, m_button contains all
		// held buttons and m_down is always false, so we can't use m_button/m_down to detect
		// press/release transitions. Use m_key_state which reliably reflects the current state.
		auto lmb_held = AllSet(args.m_key_state, EMouseKey::Left);
		auto rmb_held = AllSet(args.m_key_state, EMouseKey::Right);
		auto mmb_held = AllSet(args.m_key_state, EMouseKey::Middle);

		if (lmb_held && !m_lmb_down)
			m_mouse_ref_lb = pt;
		if (rmb_held && !m_rmb_down)
			m_mouse_ref_rb = pt;

		m_lmb_down = lmb_held;
		m_rmb_down = rmb_held;
		m_mmb_down = mmb_held;

		// Mouse move with RMB held → yaw/pitch rotation
		if (m_rmb_down)
		{
			auto delta = pt - m_mouse_ref_rb;

			if (delta.x != 0)
				m_ih.Action(m_ih, {EAction::FreeCamera_Yaw, -delta.x * m_mouse_sensitivity, 0});
			if (delta.y != 0)
				m_ih.Action(m_ih, {EAction::FreeCamera_Pitch, -delta.y * m_mouse_sensitivity, 0});

			m_mouse_ref_rb = pt;
		}

		m_mouse_pos = pt;
	}

	void Mode_FreeCamera::HandleWheelEvent(MouseWheelArgs& args)
	{
		if (args.m_delta > 0)
			m_ih.Action(m_ih, {EAction::FreeCamera_SpeedUp, 1.0f, 0});
		else if (args.m_delta < 0)
			m_ih.Action(m_ih, {EAction::FreeCamera_SlowDown, 1.0f, 0});
	}

	void Mode_FreeCamera::Update(float dt)
	{
		// Emit movement actions for held keys (continuous movement)
		if (m_key_w) m_ih.Action(m_ih, {EAction::FreeCamera_MoveForward, 1.0f, dt});
		if (m_key_s) m_ih.Action(m_ih, {EAction::FreeCamera_MoveBack,    1.0f, dt});
		if (m_key_a) m_ih.Action(m_ih, {EAction::FreeCamera_MoveLeft,    1.0f, dt});
		if (m_key_d) m_ih.Action(m_ih, {EAction::FreeCamera_MoveRight,   1.0f, dt});
		if (m_key_q) m_ih.Action(m_ih, {EAction::FreeCamera_MoveDown,    1.0f, dt});
		if (m_key_e) m_ih.Action(m_ih, {EAction::FreeCamera_MoveUp,      1.0f, dt});
	}

	// --- Mode_ShipControl ---

	Mode_ShipControl::Mode_ShipControl(InputHandler& ih)
		: IMode(ih)
		, m_mouse_pos()
		, m_mouse_ref_rb()
		, m_rmb_down(false)
		, m_mouse_sensitivity(0.003f)
	{}

	void Mode_ShipControl::HandleKeyEvent(KeyEventArgs& args)
	{
		// Ship control keys (stubs for now)
		(void)args;
	}

	void Mode_ShipControl::HandleMouseEvent(MouseEventArgs& args)
	{
		auto pt = v2{static_cast<float>(args.m_point.x), static_cast<float>(args.m_point.y)};

		auto rmb_held = AllSet(args.m_key_state, EMouseKey::Right);

		if (rmb_held && !m_rmb_down)
			m_mouse_ref_rb = pt;

		m_rmb_down = rmb_held;

		// RMB drag → orbit around ship
		if (m_rmb_down)
		{
			auto delta = pt - m_mouse_ref_rb;

			if (delta.x != 0)
				m_ih.Action(m_ih, {EAction::ShipCamera_Yaw, -delta.x * m_mouse_sensitivity, 0});
			if (delta.y != 0)
				m_ih.Action(m_ih, {EAction::ShipCamera_Pitch, -delta.y * m_mouse_sensitivity, 0});

			m_mouse_ref_rb = pt;
		}

		m_mouse_pos = pt;
	}

	void Mode_ShipControl::HandleWheelEvent(MouseWheelArgs& args)
	{
		if (args.m_delta > 0)
			m_ih.Action(m_ih, {EAction::ShipCamera_ZoomIn, 1.0f, 0});
		else if (args.m_delta < 0)
			m_ih.Action(m_ih, {EAction::ShipCamera_ZoomOut, 1.0f, 0});
	}
}
