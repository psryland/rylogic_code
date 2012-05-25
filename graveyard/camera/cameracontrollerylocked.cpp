//***********************************************************************************
//
// Camera Controller Inertial - An space ship-like camera controller
//
//***********************************************************************************

#include "PR/Camera/CameraController.h"

using namespace pr;
using namespace pr::dinput;

//*****
// Make default settings for a keyboard device
DeviceSettings MakeYLockedKeyboardDeviceSettings(dinput::Context context, HWND window_handle)
{
	DeviceSettings settings;
	settings.m_instance = GetDeviceInstance(context, EDeviceClass_Keyboard, EFlag_AllDevices);
	settings.m_base.m_window_handle = window_handle;
	settings.m_base.m_buffered = false;
	settings.m_base.m_buffer_size = 0;
	settings.m_base.m_events = false;
	return settings;
}

//*****
CameraControllerYLocked::CameraControllerYLocked(const CameraControllerSettings& settings)
:m_settings(settings)
,m_context(m_settings.m_app_instance)
,m_keyboard(m_context, MakeYLockedKeyboardDeviceSettings(m_context, m_settings.m_window_handle))
{
	PR_ASSERT_STR(PR_DBG_CAMERA, m_settings.m_camera != 0, "You must provide a camera to control");
}

//*****
// Camera Update
void CameraControllerYLocked::Step(float elapsed_seconds)
{
	if( Failed(m_keyboard.Sample()) ) return;

	v4 accel = {0.0f, 0.0f, 0.0f, 0.0f};
	v4 rot   = {0.0f, 0.0f, 0.0f, 0.0f};
	float l_scale = 1.0f;
	float a_scale = 1.0f;

	if( m_keyboard.KeyDown(DIK_S) )	m_settings.m_camera->Stop();
	if( m_keyboard.KeyDown(DIK_L) )	m_settings.m_camera->LookAt(v4Origin, v4YAxis);
	if( m_keyboard.KeyDown(DIK_Z) )	accel[Camera::Y]	+= -m_settings.m_linear_acceleration;
	if( m_keyboard.KeyDown(DIK_A) )	accel[Camera::Y]	+=  m_settings.m_linear_acceleration;
	if( m_keyboard.KeyDown(DIK_X) )	rot[Camera::Roll]	+= -m_settings.m_rotational_acceleration;
	if( m_keyboard.KeyDown(DIK_C) )	rot[Camera::Roll]	+=  m_settings.m_rotational_acceleration;

	if( m_keyboard.KeyDown(DIK_LSHIFT) || m_keyboard.KeyDown(DIK_RSHIFT) )	// Linear
	{
		if( m_keyboard.KeyDown(DIK_LEFT) )		accel[Camera::X] += -m_settings.m_linear_acceleration;
		if( m_keyboard.KeyDown(DIK_RIGHT) )	accel[Camera::X] +=  m_settings.m_linear_acceleration;
		if( m_keyboard.KeyDown(DIK_UP) )		accel[Camera::Z] += -m_settings.m_linear_acceleration;
		if( m_keyboard.KeyDown(DIK_DOWN) )		accel[Camera::Z] +=  m_settings.m_linear_acceleration;
	}
	else								// Rotational
	{
		if( m_keyboard.KeyDown(DIK_LEFT) )		rot[Camera::Yaw]	+=  m_settings.m_rotational_acceleration;
		if( m_keyboard.KeyDown(DIK_RIGHT) )	rot[Camera::Yaw]	+= -m_settings.m_rotational_acceleration;
		if( m_keyboard.KeyDown(DIK_UP) )		rot[Camera::Pitch]	+=  m_settings.m_rotational_acceleration;
		if( m_keyboard.KeyDown(DIK_DOWN) )		rot[Camera::Pitch]	+= -m_settings.m_rotational_acceleration;
	}

	if( m_keyboard.KeyDown(DIK_LCONTROL) || m_keyboard.KeyDown(DIK_RCONTROL) )	{ l_scale = 5.0f; a_scale = 2.0f;	}
	if( m_keyboard.KeyDown(DIK_CAPSLOCK) )											{ l_scale *= 15.0f;					}
	
	m_settings.m_camera->ATranslateRel(accel * l_scale);
	m_settings.m_camera->ARotateRel(rot * a_scale);
	m_settings.m_camera->Update(elapsed_seconds);
	m_settings.m_camera->Drag(0.95f);
	m_settings.m_camera->RotDrag(0.95f);
}
