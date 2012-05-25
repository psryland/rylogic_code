//*************************************
// Camera Controller Mouse Free Camera
//	(c)opyright Rylogic Limited 2007
//*************************************
#ifndef CAMERA_CONTROLLER_MOUSE_FREE_CAMERA_H
#define CAMERA_CONTROLLER_MOUSE_FREE_CAMERA_H

#include "pr/camera/ICameraController.h"

namespace pr
{
	namespace camera
	{
		// A full 3d mouse driven camera controller using 'GetCursorPos()'
		class MouseFreeCamera : public ICameraController
		{
		public:
			MouseFreeCamera(const CameraControllerSettings& settings);
			void	Step(float elapsed_seconds);
			void	SetScale(float scale)			{ m_settings.m_scale = scale; }
			Camera&	GetCamera()						{ return *m_settings.m_camera; }
			CameraControllerSettings& GetSettings()	{ return m_settings; }

		private:
			CameraControllerSettings	m_settings;
			POINT						m_last_pos;
		};

	}//namespace camera
}//namespace pr

#endif//CAMERA_CONTROLLER_MOUSE_FREE_CAMERA_H
