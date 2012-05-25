//*************************************
// Camera Controller Keyboard Free Camera Axis Locked
//	(c)opyright Rylogic Limited 2007
//*************************************
#ifndef CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_YLOCKED_H
#define CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_YLOCKED_H

#include "pr/camera/CCKeyboardFreeCamera.h"

namespace pr
{
	namespace camera
	{
		// A full 3d camera controlled by the keyboard
		class KeyboardFreeCameraAxisLocked : public KeyboardFreeCamera
		{
		public:
			KeyboardFreeCameraAxisLocked(const CameraControllerSettings& settings, const v4& axis);
			void	Step(float elapsed_seconds);
			
			v4	m_axis;
		};
	}//namespace camera
}//namespace pr

#endif//CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_YLOCKED_H
