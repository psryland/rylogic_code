//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2015
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "linedrawer/utility/misc.h"

namespace ldr
{
	// This object implements manipulation of selected objects
	struct Manipulator
		:IInputHandler
		,pr::events::IRecv<pr::ldr::Evt_LdrObjectSelectionChanged>
		,pr::events::IRecv<pr::rdr::Evt_UpdateScene>
	{
		pr::Camera&          m_cam; 
		pr::Renderer&        m_rdr;
		pr::ldr::LdrGizmoPtr m_gizmo;
		IInputHandler*       m_fwd_input; // The input handler to forward unused input to

		Manipulator(pr::Camera& cam, pr::Renderer& rdr);

	private:

		// Called when input focus is given or removed. Implementors should use
		// LostInputFocus() to abort any control operations in progress.
		void IInputHandler::GainInputFocus(IInputHandler* gained_from) override;
		void IInputHandler::LostInputFocus(IInputHandler* lost_to) override;

		// Keyboard input.
		// Return true if the key was handled and should not be
		// passed to anything else that might want the key event.
		bool KeyInput(UINT vk_key, bool down, UINT flags, UINT repeats) override;

		// Mouse input.
		// 'pos_ns' is the normalised screen space position of the mouse
		//   i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
		// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
		// 'start_or_end' is true on mouse down/up
		// Returns true if the camera has moved or objects in the scene have moved
		bool IInputHandler::MouseInput(pr::v2 const& pos_ns, int button_state, bool start_or_end) override;
		bool IInputHandler::MouseClick(pr::v2 const& pos_ns, int button_state) override;
		bool IInputHandler::MouseWheel(pr::v2 const& pos_ns, float delta) override;

		// Event handlers
		void OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&) override;
		void OnEvent(pr::rdr::Evt_UpdateScene const&) override;
	};
}
