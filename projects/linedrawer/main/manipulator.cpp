//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2015
//*****************************************************************************************

#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/manipulator.h"
#include "linedrawer/main/ldrevent.h"

namespace ldr
{
	// This object implements manipulation of selected objects
	Manipulator::Manipulator(pr::Camera& cam, pr::Renderer& rdr)
		:m_cam(cam)
		,m_rdr(rdr)
		,m_gizmo()
		,m_fwd_input()
	{}

	// Called when input focus is given or removed. Implementors should use
	// LostInputFocus() to abort any control operations in progress.
	void Manipulator::GainInputFocus(IInputHandler* gained_from)
	{
		m_gizmo = pr::New<pr::ldr::Gizmo>(m_cam, m_rdr, LdrContext, pr::ldr::Gizmo::EMode::Translate);
		m_fwd_input = gained_from;
		pr::events::Send(Event_Refresh());
	}
	void Manipulator::LostInputFocus(IInputHandler*)
	{
		m_gizmo = nullptr;
		m_fwd_input = nullptr;
		pr::events::Send(Event_Refresh());
	}

	// Keyboard input.
	// Return true if the key was handled and should not be
	// passed to anything else that might want the key event.
	bool Manipulator::KeyInput(UINT vk_key, bool down, UINT flags, UINT repeats)
	{
		return m_fwd_input->KeyInput(vk_key, down, flags, repeats);
	}

	// Mouse input.
	// 'pos_ns' is the normalised screen space position of the mouse
	//   i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
	// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
	// 'start_or_end' is true on mouse down/up
	// Returns true if the camera has moved or objects in the scene have moved
	void Manipulator::MouseInput(pr::v2 const& pos_ns, int btn_state, bool start_or_end)
	{
		// behaviour:
		//  On mouse over an axis, the axis colour changes to yellow
		//  If mouse down while over an axis, manipulation begins:
		//     Record reference transform
		//     Start callbacks with manipulation transforms
		//  On mouse up send commit
		//  On escape, send revert
		//  If not manipulating, forward calls to another input handler

		m_gizmo->MouseControl(pos_ns, btn_state, start_or_end);
		if (m_gizmo->m_moved)
			pr::events::Send(Event_Refresh());
		if (!m_gizmo->m_manipulating)
			m_fwd_input->MouseInput(pos_ns, btn_state, start_or_end);
	}
	void Manipulator::MouseClick(pr::v2 const& pos_ns, int btn_state)
	{
		return m_fwd_input->MouseClick(pos_ns, btn_state);
	}
	void Manipulator::MouseWheel(pr::v2 const& pos_ns, float delta)
	{
		return m_fwd_input->MouseWheel(pos_ns, delta);
	}

	// Event handlers
	void Manipulator::OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&)
	{
	}
	void Manipulator::OnEvent(pr::rdr::Evt_UpdateScene const& e)
	{
		if (m_gizmo && m_gizmo->m_gfx)
			m_gizmo->m_gfx->AddToScene(e.m_scene);
	}
}
