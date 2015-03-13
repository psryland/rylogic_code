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
		m_gizmo = pr::ldr::LdrGizmoPtr(new pr::ldr::LdrGizmo(m_rdr, pr::ldr::LdrGizmo::EMode::Scale, pr::m4x4Identity));
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
	bool Manipulator::MouseInput(pr::v2 const& pos_ns, int btn_state, bool start_or_end)
	{
		auto refresh = m_gizmo->MouseControl(m_cam, pos_ns, btn_state, start_or_end);
		if (!m_gizmo->m_manipulating)
			refresh |= m_fwd_input->MouseInput(pos_ns, btn_state, start_or_end);
		
		return refresh;
	}
	bool Manipulator::MouseClick(pr::v2 const& pos_ns, int btn_state)
	{
		return m_fwd_input->MouseClick(pos_ns, btn_state);
	}
	bool Manipulator::MouseWheel(pr::v2 const& pos_ns, float delta)
	{
		return m_fwd_input->MouseWheel(pos_ns, delta);
	}

	// Event handlers
	void Manipulator::OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&)
	{
	}
	void Manipulator::OnEvent(pr::rdr::Evt_UpdateScene const& e)
	{
		if (m_gizmo)
			m_gizmo->AddToScene(e.m_scene);
	}
}
