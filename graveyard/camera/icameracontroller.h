//***********************************************************************************
// Camera Controller Interface - All classes that control a camera must implement this interface
//	(c)opyright Rylogic Limited 2007
//***********************************************************************************
#ifndef CAMERA_CONTROLLER_H
#define CAMERA_CONTROLLER_H

#include "pr/maths/maths.h"
#include "pr/camera/Camera.h"

namespace pr
{
	struct CameraControllerSettings
	{
		CameraControllerSettings()
		{
			m_camera					= 0;
			m_window_handle				= 0;
			m_app_instance				= 0;
			m_scale						= 1.0f;
			m_max_linear_velocity		= 1.0f;
			m_linear_acceleration		= 1.0f;
			m_max_rotational_velocity	= 0.1f;
			m_rotational_acceleration	= 0.1f;
		}

		Camera*		m_camera;
		HWND		m_window_handle;
		HINSTANCE	m_app_instance;
		float		m_scale;
		float		m_max_linear_velocity;
		float		m_linear_acceleration;
		float		m_max_rotational_velocity;
		float		m_rotational_acceleration;
	};

	// An interface for controlling a camera
	struct ICameraController
	{
		virtual ~ICameraController() {}
		virtual void						SetScale(float scale) = 0;
		virtual void						Step(float elapsed_seconds) = 0;
		virtual Camera&						GetCamera() = 0;
		virtual CameraControllerSettings&	GetSettings() = 0;
	};

}//namespace pr

#endif//CAMERA_CONTROLLER_H
