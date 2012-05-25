//*************************************
// Camera Controller Keyboard Free Camera
//	(c)opyright Rylogic Limited 2007
//*************************************
#ifndef CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA2_H
#define CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA2_H

#include "pr/camera/ICameraController.h"

namespace pr
{
	namespace camera
	{
		// A full 3d camera controlled by the keyboard (using 'GetKeyState()' instead of DInput)
		class KeyboardFreeCamera_2 : public ICameraController
		{
		public:
			KeyboardFreeCamera_2(const CameraControllerSettings& settings);
			void	Step(float elapsed_seconds);
			void	SetScale(float scale)			{ m_settings.m_scale = scale; }
			Camera&	GetCamera()						{ return *m_settings.m_camera; }
			CameraControllerSettings& GetSettings()	{ return m_settings; }

		private:
			CameraControllerSettings	m_settings;
		};
	}//namespace camera
}//namespace pr

#endif//CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA2_H
