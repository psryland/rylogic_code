//***********************************************************************************
//
// Camera Controller Mouse
//
//***********************************************************************************

#include "PR/Camera/CameraController.h"

using namespace pr;

//*****
CameraControllerMouseFull3d::CameraControllerMouseFull3d(const CameraControllerSettings& settings)
:m_settings(settings)
{
	GetCursorPos(&m_last_pos);
}

//*****
void CameraControllerMouseFull3d::Step(float /*elapsed_seconds*/)
{
	struct KeyState
	{
		bool operator [] (int ch) const
		{
			short key_state = GetAsyncKeyState(ch);
			return (key_state & 0x8000) != 0;
		}
	} keys;

	POINT pos;
	if( !GetCursorPos(&pos) ) return;
	v2 delta_pos = v2::construct(float(pos.x - m_last_pos.x), float(pos.y - m_last_pos.y));
	m_last_pos = pos;

	if( keys[VK_ADD]		) m_settings.m_scale = Clamp<float>(m_settings.m_scale * 1.01f, 0.0001f, 1000.0f);
	if( keys[VK_SUBTRACT ]	) m_settings.m_scale = Clamp<float>(m_settings.m_scale * 0.99f, 0.0001f, 1000.0f);

	if( keys[VK_RBUTTON] )
	{
		v2 trans = 0.01f * delta_pos * m_settings.m_scale;
		m_settings.m_camera->DTranslateRel(-trans.x, trans.y, 0.0f);
	}
	if( keys[VK_MBUTTON] )
	{
		v2 rot = 0.001f * delta_pos * m_settings.m_scale;
		m_settings.m_camera->DRotateAbout(-rot.y, -rot.x, 0.0f, v4Origin);
	}
}

////*****
//// Move in/out
//void NavigationManager::MoveZ(float delta)
//{
//	if( m_translation_lock[Camera::Z] ) return;
//
//	// Move in a fraction of the focus distance
//	float movez = m_focus_dist * delta / 1200.0f;
//	m_camera.DTranslateRel(0.0f, 0.0f, movez);
//	m_focus_dist += movez;
//}
//
////*****
//// Zoom in/out
//void NavigationManager::Zoom(float delta)
//{
//	if( m_zoom_lock ) return;
//
//	// Do the zooming
//	float fov = m_camera.GetViewProperty(Camera::FOV);
//	fov *= 1.0f + delta / 100.0f;
//	fov = Clamp(fov, maths::tiny, maths::pi);
//	m_camera.SetViewProperty(Camera::FOV, fov);
//
//	// Record the percentage of zoom for the window text
//	m_zoom_fraction = m_view.m_fov / fov;
//	LineDrawer::Get().RefreshWindowText();
//}
