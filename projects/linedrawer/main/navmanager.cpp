//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/navmanager.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"

namespace ldr
{
	NavManager::NavManager(pr::Camera& camera, pr::iv2 view_size, pr::v4 const& reset_up)
		:m_camera(camera)
		,m_ctrl_mode(ENavMode::Navigation)
		,m_view_size()
		,m_reset_up(pr::Length3Sq(reset_up) > pr::maths::tiny ? reset_up : pr::v4YAxis)
		,m_reset_forward(pr::Parallel(m_reset_up, pr::v4ZAxis) ? -pr::v4XAxis : -pr::v4ZAxis)
		,m_orbit_timer(GetTickCount())
		,m_views()
	{
		// Set an initial camera position
		SetViewSize(view_size);
		m_camera.View(pr::BBoxUnit, m_reset_forward, m_reset_up, true);
	}

	// Set the view area so we know how to convert screen space to normalised space
	void NavManager::SetViewSize(pr::iv2 view_size)
	{
		m_view_size = view_size;
		m_camera.Aspect(m_view_size.x / float(m_view_size.y));
	}

	// Set the direction the camera should look when reset
	void NavManager::SetResetOrientation(pr::v4 const& forward, pr::v4 const& up)
	{
		m_reset_forward = forward;
		m_reset_up = up;
	}

	// Set/Get the camera up align vector
	void NavManager::CameraAlign(pr::v4 const& up)
	{
		m_camera.SetAlign(up);
		if (m_camera.IsAligned()) m_reset_up = m_camera.m_align;
		m_reset_forward = pr::Parallel(m_reset_up, pr::v4ZAxis) ? -pr::v4XAxis : -pr::Normalise3(pr::Cross3(pr::v4XAxis, m_reset_up));
	}
	pr::v4 NavManager::CameraAlign() const
	{
		return m_camera.m_align;
	}

	// Set/Get perspective or orthographic projection
	void NavManager::Render2D(bool yes)
	{
		m_camera.m_orthographic = yes;
	}
	bool NavManager::Render2D() const
	{
		return m_camera.m_orthographic;
	}

	// Reset the camera to view a bbox
	void NavManager::ResetView(pr::BBox const& view_bbox)
	{
		m_camera.View(view_bbox, m_reset_forward, m_reset_up, true);
	}

	//// Position the camera prior to rendering a frame
	//void NavManager::PositionCamera()
	//{
	//}

	// Normalise a screen-space point
	inline pr::v2 NormalisedScreenSpace(pr::v2 pos, pr::iv2 view_size)
	{
		return pr::v2::make(2.0f * pos.x / view_size.x - 1.0f, 1.0f - 2.0f * pos.y / view_size.y);
	}

	// Mouse input. This should be raw input from the UI
	// 'pos' is the screen space position of the mouse
	// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
	// 'start_or_end' is true on mouse down/up
	// Returns true if the camera has moved or objects in the scene have moved
	bool NavManager::MouseInput(pr::v2 const& pos, int button_state, bool start_or_end)
	{
		// Ignore mouse movement unless a button is pressed
		if (button_state == 0 && !start_or_end)
			return false;

		// If we're in navigation mode, interpret the mouse movement into camera movement
		switch (m_ctrl_mode)
		{
		default:break;
		case ENavMode::Navigation:
			m_camera.MouseControl(NormalisedScreenSpace(pos, m_view_size), button_state, start_or_end);
			return true;
		case ENavMode::Manipulation:
			break;
		}
		return false;
	}
	bool NavManager::MouseWheel(pr::v2 const&, float delta)
	{
		// If we're in navigation mode, interpret the mouse movement into camera movement
		switch (m_ctrl_mode)
		{
		default:break;
		case ENavMode::Navigation:
			m_camera.Translate(0, 0, delta, true);
			return true;
		case ENavMode::Manipulation:
			break;
		}
		return false;
	}
	bool NavManager::MouseClick(pr::v2 const&, int button_state)
	{
		switch (m_ctrl_mode)
		{
		default:break;
		case ENavMode::Navigation:
			if (pr::AllSet(button_state, pr::camera::ENavBtn::Middle) ||
				pr::AllSet(button_state, pr::camera::ENavBtn::Left|pr::camera::ENavBtn::Right))
			{
				m_camera.ResetZoom();
				return true;
			}
			break;
		case ENavMode::Manipulation:
			break;
		}
		return false;
	}

	// Return a point in world space corresponding to a screen space point.
	// The x,y components of 'screen' should be in client area space
	// The z component should be the world space distance from the camera
	pr::v4 NavManager::WSPointFromSSPoint(pr::v4 const& screen) const
	{
		// Note: 'screen' can be outside of 'm_client_area' because we capture the mouse
		float x = -1.0f + 2.0f * screen.x / m_view_size.x;
		float y = +1.0f - 2.0f * screen.y / m_view_size.y;
		return m_camera.WSPointFromNormSSPoint(pr::v4::make(x, y, m_camera.FocusDist(), 0.0f));
	}

	// Orbit the camera about the current focus point
	void NavManager::OrbitCamera(float orbit_speed_rad_p_s)
	{
		// Determine the angle to rotate by
		pr::uint32 elapsed_time = GetTickCount() - m_orbit_timer;
		m_orbit_timer = GetTickCount();

		m_camera.Orbit(orbit_speed_rad_p_s * elapsed_time * 0.001f, true);
	}

	// Save the current view
	void NavManager::ClearSavedViews()
	{
		m_views.clear();
	}
	NavManager::SavedViewID NavManager::SaveView()
	{
		m_views.push_back(m_camera);
		return NavManager::SavedViewID(m_views.size() - 1);
	}
	void NavManager::RestoreView(SavedViewID id)
	{
		PR_ASSERT(PR_DBG_LDR, id < (SavedViewID)m_views.size(), "Invalid saved view id");
		m_camera = m_views[id];
	}
}