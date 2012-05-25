//*************************************
// Camera Controller Keyboard Free Camera
//	(c)opyright Rylogic Limited 2007
//*************************************
#ifndef CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_H
#define CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_H

#include "pr/input/directinput/directinput.h"
#include "pr/input/directinput/dikeyboard.h"
#include "pr/camera/icameracontroller.h"

namespace pr
{
	namespace camera
	{
		// A full 3d camera controlled by the keyboard
		class KeyboardFreeCamera : public ICameraController
		{
		public:
			KeyboardFreeCamera(const CameraControllerSettings& settings);
			void	Step(float elapsed_seconds);
			void	SetScale(float scale)			{ m_settings.m_scale = scale; }
			Camera&	GetCamera()						{ return *m_settings.m_camera; }
			CameraControllerSettings& GetSettings()	{ return m_settings; }

		protected:
			CameraControllerSettings	m_settings;
			dinput::Context				m_context;
			dinput::Keyboard			m_keyboard;
		};
	}//namespace camera
}//namespace pr

#endif//CAMERA_CONTROLLER_KEYBOARD_FREE_CAMERA_H
