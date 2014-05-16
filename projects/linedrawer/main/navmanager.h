//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_NAV_MANAGER_H
#define LDR_NAV_MANAGER_H

#include "linedrawer/main/forward.h"
#include "linedrawer/input/keybindings.h"

namespace ldr
{
	// This object controls all input navigation and manipulation
	struct NavManager
	{
		typedef pr::Array<pr::Camera, 4> ViewCont;

		pr::Camera& m_camera;           // Camera we're controlling
		ENavMode    m_ctrl_mode;        // The mode of control, either navigating or manipulating
		pr::iv2     m_view_size;        // The size of the screen space area
		pr::v4      m_reset_up;         // The up direction of the camera after a view reset
		pr::v4      m_reset_forward;    // The forward direction of the camera after a view reset
		pr::uint32  m_orbit_timer;      // A timer to ensuring constant orbit speed
		ViewCont    m_views;            // Saved views

		NavManager(pr::Camera& camera, pr::iv2 view_size, pr::v4 const& reset_up);

		// Get/Set the current camera position
		pr::m4x4 CameraToWorld() const { return m_camera.CameraToWorld(); }
		pr::v4 CameraPosition() const  { return m_camera.CameraToWorld().pos; }
		void LookAt(pr::v4 const& position, pr::v4 const& lookat, pr::v4 const& up) { m_camera.LookAt(position, lookat, up, true); }

		// Set the view size so we know how to convert screen space to normalised space
		void SetViewSize(pr::iv2 size);

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

		//// Position the camera prior to rendering a frame
		//void PositionCamera();

		// Mouse input. This should be raw input from the UI
		// 'pos' is the screen space position of the mouse
		// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
		// 'start_or_end' is true on mouse down/up
		// Returns true if the camera has moved or objects in the scene have moved
		bool MouseInput(pr::v2 const& pos, int button_state, bool start_or_end);
		bool MouseWheel(pr::v2 const& pos, float delta);
		bool MouseClick(pr::v2 const& pos, int button_state);

		// Return the distance from the camera to the focus point
		float FocusDistance() const { return m_camera.FocusDist(); }

		// Return the zoom scaling factor
		float Zoom() const { return m_camera.Zoom(); }

		// Return the world space position of the focus point
		pr::v4 FocusPoint() const { return m_camera.FocusPoint(); }
		void FocusPoint(pr::v4 const& pos) { pr::m4x4 c2w = CameraToWorld(); m_camera.LookAt(c2w.pos, pos, c2w.y, true); }

		// Return a point in world space corresponding to a screen space point.
		// The x,y components of 'screen' should be in client area space
		// The z component should be the world space distance from the camera
		pr::v4 WSPointFromSSPoint(pr::v4 const& screen) const;

		// Orbit the camera about the current focus point
		void OrbitCamera(float orbit_speed_rad_p_s);

		// Saved views. The NavManager maintains a history of saved views
		typedef int SavedViewID;
		void ClearSavedViews();
		SavedViewID SaveView();
		void RestoreView(SavedViewID id);
	};
}
#endif
