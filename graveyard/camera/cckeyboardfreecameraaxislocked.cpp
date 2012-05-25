//*************************************
// Camera Controller Keyboard Free Camera Axis Locked
//	(c)opyright Rylogic Limited 2007
//*************************************
#include "pr/camera/CCKeyboardFreeCameraAxisLocked.h"

using namespace pr;
using namespace pr::dinput;
using namespace pr::camera;

// Constructor
KeyboardFreeCameraAxisLocked::KeyboardFreeCameraAxisLocked(const CameraControllerSettings& settings, const v4& axis)
:KeyboardFreeCamera(settings)
,m_axis(axis)
{}

// Camera Update
void KeyboardFreeCameraAxisLocked::Step(float elapsed_seconds)
{
	KeyboardFreeCamera::Step(elapsed_seconds);
	m_settings.m_camera->SetUp(m_axis);
}
