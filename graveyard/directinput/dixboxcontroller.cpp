//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/dixboxcontroller.h"
#include "pr/input/directinput/directinput.h"

using namespace pr;
using namespace pr::dinput;

// Locate a xbox controller instance
DeviceInstance GetXBoxController(const Context& di_context)
{
	return GetDeviceInstance(di_context, "Microsoft Xbox Controller", EDeviceClass_Joystick, EFlag_AllDevices);
}

// Constructor
XBoxController::XBoxController(Context& di_context, const XBoxControllerSettings& settings)
:Joystick(di_context, DeviceSettings(GetXBoxController(di_context), settings.m_base))
{}
