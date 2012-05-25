//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#ifndef PR_DINPUT_XBOX_CONTROLLER_H
#define PR_DINPUT_XBOX_CONTROLLER_H

#include "pr/input/directinput/dijoystick.h"

namespace pr
{
	namespace dinput
	{
		// Settings for creating an xbox controller
		struct XBoxControllerSettings
		{
			DeviceSettingsBase m_base;

			XBoxControllerSettings() {}
			XBoxControllerSettings(HWND window_handle, bool buffered, unsigned int	buffer_size, bool events)
			:m_base(window_handle, buffered, buffer_size, events)
			{}
		};

		// The xbox controller
		class XBoxController : public Joystick
		{
		public:
			enum Axis { LeftX, LeftY, RightX, RightY };
			enum Btn  { A, B, X, Y, White, Black, StickBtnLeft, StickBtnRight, TrigLeft, TrigRight, Start, Back };

			XBoxController(Context& di_context, const XBoxControllerSettings& settings);

			// Non buffered data
			uint AxisValue(Axis axis) const			{ return Joystick::AxisValue(axis); }
			bool ButtonDown(Btn btn) const			{ return Joystick::ButtonDown(btn); }
			void LeftStick(uint& X, uint& Y) const	{ X = AxisValue(LeftX);  Y = AxisValue(LeftY); }
			void RightStick(uint& X, uint& Y) const	{ X = AxisValue(RightX); Y = AxisValue(RightY); }
			uint DPad() const						{ return 0; }
		};
	}//namespace dinput
}//namespace pr

#endif//PR_DINPUT_XBOX_CONTROLLER_H
