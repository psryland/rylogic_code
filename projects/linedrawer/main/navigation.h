//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "linedrawer/input/keybindings.h"
#include "linedrawer/input/input_handler.h"

namespace ldr
{
	// Manages navigation around the scene
	struct Navigation :IInputHandler
	{
		typedef pr::vector<pr::Camera, 4> ViewCont;

		pr::Camera&       m_camera;        // Camera we're controlling
		pr::iv2           m_view_size;     // The size of the screen space area
		pr::v4            m_reset_up;      // The up direction of the camera after a view reset
		pr::v4            m_reset_forward; // The forward direction of the camera after a view reset
		pr::uint32        m_orbit_timer;   // A timer to ensuring constant orbit speed
		ViewCont          m_views;         // Saved views

		Navigation() = delete;
		Navigation(pr::Camera& camera, pr::iv2 view_size, pr::v4 const& reset_up);
		Navigation(Navigation const&) = delete;

		// Get/Set the current camera position
		pr::m4x4 CameraToWorld() const
		{
			return m_camera.CameraToWorld();
		}
		pr::v4 CameraPosition() const
		{
			return m_camera.CameraToWorld().pos;
		}
		void LookAt(pr::v4 const& position, pr::v4 const& lookat, pr::v4 const& up)
		{
			m_camera.LookAt(position, lookat, up, true);
		}

		// Get/Set the view size so we know how to convert screen space to normalised space
		pr::iv2 ViewSize() const;
		void ViewSize(pr::iv2 size);

		// Set the direction the camera should look when reset
		void SetResetOrientation(pr::v4 const& forward, pr::v4 const& up);

		// Set/Get the camera up align vector
		void CameraAlign(pr::v4 const& up);
		pr::v4 CameraAlign() const;

		// Set/Get perspective or orthographic projection
		void Render2D(bool yes);
		bool Render2D() const;

		// Reset the camera to view a bbox from the prefer orientation
		void ResetView(pr::BBox const& view_bbox);

		// Position the camera prior to rendering a frame
		void PositionCamera();

		// Called when input focus is given or removed. Implementers should use
		// LostInputFocus() to abort any control operations in progress.
		void GainInputFocus(IInputHandler* gained_from) override;
		void LostInputFocus(IInputHandler* lost_to) override;

		// Keyboard input.
		// Return true if the key was handled and should not be
		// passed to anything else that might want the key event.
		bool KeyInput(UINT vk_key, bool down, UINT flags, UINT repeats) override;

		// Mouse input. 
		// 'pos_ns' is the normalised screen space position of the mouse
		//   i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// 'nav_op' is a navigation/manipulation verb
		// 'start_or_end' is true on mouse down/up
		// Returns true if the scene needs refreshing
		bool MouseInput(pr::v2 const& pos_ns, ENavOp nav_op, bool start_or_end) override;
		bool MouseClick(pr::v2 const& pos_ns, ENavOp nav_op) override;
		bool MouseWheel(pr::v2 const& pos_ns, float delta) override;

		// Return the distance from the camera to the focus point
		float FocusDistance() const { return m_camera.FocusDist(); }

		// Return the zoom scaling factor
		float Zoom() const { return m_camera.Zoom(); }

		// Return the world space position of the focus point
		pr::v4 FocusPoint() const
		{
			return m_camera.FocusPoint();
		}
		void FocusPoint(pr::v4 const& pos)
		{
			auto c2w = CameraToWorld();
			m_camera.LookAt(c2w.pos, pos, c2w.y, true);
		}

		// Return a point in world space corresponding to a screen space point.
		// The x,y components of 'screen' should be in client area space
		// The z component should be the world space distance from the camera
		pr::v4 SSPointToWSPoint(pr::v4 const& screen) const;

		// Orbit the camera about the current focus point
		void OrbitCamera(float orbit_speed_rad_p_s);

		// Saved views. We maintain a history of saved views
		using SavedViewID = int;
		void ClearSavedViews();
		SavedViewID SaveView();
		void RestoreView(SavedViewID id);
	};
}
