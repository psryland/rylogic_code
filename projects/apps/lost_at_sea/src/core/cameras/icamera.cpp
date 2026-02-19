//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
#include "src/core/cameras/icamera.h"
#include "src/core/input/input_handler.h"

namespace las::camera
{
	ICamera::ICamera(Camera& cam, InputHandler& input)
		:m_cam(cam)
		,m_sub_input(input.Action += std::bind(&ICamera::OnAction, this, _2))
	{}
	
	void ICamera::OnAction(input::Action)
	{}
}
