//***********************************************************************************
//
// Camera Controller Inertial - An space ship-like camera controller
//
//***********************************************************************************

#include "PR/Camera/CameraController.h"
#include "PR/Common/KeyState.h"

using namespace pr;
using namespace pr::dinput;

//*****
CameraControllerFull3d_2::CameraControllerFull3d_2(const CameraControllerSettings& settings)
:m_settings(settings)
{
	PR_ASSERT_STR(PR_DBG_CAMERA, m_settings.m_camera != 0, "You must provide a camera to control");
}

//*****
// Camera Update
void CameraControllerFull3d_2::Step(float elapsed_seconds)
{
	struct KeyState
	{
		bool operator [] (int ch) const		{ return KeyDown(ch); }
	} keys;

	v4 accel = v4Zero;
	v4 rot   = v4Zero;

	// Scale acceleration/velocity/max velocity
	if( keys[VK_ADD]		) m_settings.m_scale = Clamp<float>(m_settings.m_scale * 1.01f, 0.0001f, 1000.0f);
	if( keys[VK_SUBTRACT ]	) m_settings.m_scale = Clamp<float>(m_settings.m_scale * 0.99f, 0.0001f, 1000.0f);

	if( keys['S'] )	m_settings.m_camera->Stop();
	if( keys['L'] )	m_settings.m_camera->LookAt(v4Origin, v4YAxis);
	if( keys['Z'] )	accel[Camera::Y]	+= -m_settings.m_linear_acceleration;
	if( keys['A'] )	accel[Camera::Y]	+=  m_settings.m_linear_acceleration;
	if( keys['X'] )	rot[Camera::Roll]	+= -m_settings.m_rotational_acceleration;
	if( keys['C'] )	rot[Camera::Roll]	+=  m_settings.m_rotational_acceleration;

	if( keys[VK_SHIFT] )	// Linear
	{
		if( keys[VK_LEFT] )		accel[Camera::X] += -m_settings.m_linear_acceleration;
		if( keys[VK_RIGHT] )	accel[Camera::X] +=  m_settings.m_linear_acceleration;
		if( keys[VK_UP] )		accel[Camera::Z] += -m_settings.m_linear_acceleration;
		if( keys[VK_DOWN] )		accel[Camera::Z] +=  m_settings.m_linear_acceleration;
	}
	else					// Rotational
	{
		if( keys[VK_LEFT] )		rot[Camera::Yaw]	+=  m_settings.m_rotational_acceleration;
		if( keys[VK_RIGHT] )	rot[Camera::Yaw]	+= -m_settings.m_rotational_acceleration;
		if( keys[VK_UP] )		rot[Camera::Pitch]	+=  m_settings.m_rotational_acceleration;
		if( keys[VK_DOWN] )		rot[Camera::Pitch]	+= -m_settings.m_rotational_acceleration;
	}
	
	accel *= m_settings.m_scale;
	rot   *= m_settings.m_scale;

	if( keys[VK_CONTROL] )		{ accel *= 5.0f; rot *= 2.0f; }
	
	m_settings.m_camera->ATranslateRel (accel);
	m_settings.m_camera->ARotateRel    (rot);
	m_settings.m_camera->Update        (elapsed_seconds);
	m_settings.m_camera->Drag          (0.95f);
	m_settings.m_camera->RotDrag       (0.95f);
}
