//************************************
// Lost at Sea
//  Copyright © Rylogic Ltd 2011
//************************************
#ifndef LAS_CAMERA_H
#define LAS_CAMERA_H
#pragma once

#include "forward.h"
#include "event.h"
#include "pr/camera/camctrl_dinput_wasd.h"

namespace las
{
	// Common base class for camera controllers
	struct ICam :pr::RefCount<ICam>
	{
		virtual ~ICam() {}
	};
	typedef pr::RefPtr<ICam> ICamPtr;
	
	// Dev camera for flying anywhere
	struct DevCam :ICam ,pr::events::IRecv<las::Evt_Step>
	{
		pr::camera::WASDCtrller m_ctrl;
		DevCam(pr::Camera& cam, HINSTANCE app_inst, HWND hwnd, pr::IRect const& area) :m_ctrl(cam, app_inst, hwnd, area) {}
		void OnEvent(las::Evt_Step const& e) { m_ctrl.Step(e.m_elapsed_s); }
	};
}

#endif
