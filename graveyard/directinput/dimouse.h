//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#ifndef PR_DINPUT_MOUSE_H
#define PR_DINPUT_MOUSE_H

#include <vector>
#include "pr/input/directinput/didevice.h"

namespace pr
{
	namespace dinput
	{
		struct MouseData
		{
			long m_x;
			long m_y;
			long m_z;
			uint8 m_button[8];
		};
		typedef std::vector<MouseData> TMouseBuffer;

		class Mouse :public Device
		{
			DIMOUSESTATE2 m_state;
			DIMOUSESTATE2 m_last_state;

		public:
			enum EAxis   { X = 0, Y = 1, Z = 2 };
			enum EButton { Left = 0, Right = 1, Middle = 2, LeftLeft = 3, RightRight = 4 };
			Mouse(Context& di_context, const DeviceSettings& settings);
			
			// Non buffered data
			EResult::Type Sample();
			long          Axis(EAxis axis) const     { switch( axis ) { case X: return m_state.lX; case Y: return m_state.lY; case Z: m_state.lZ; }; PR_ASSERT(PR_DBG_DINPUT, false, ""); return 0; }
			bool          Btn(int btn) const         { PR_ASSERT(PR_DBG_DINPUT, btn < 8, ""); return (m_state.rgbButtons[btn] & 0x80) == 0x80; }
			long          x() const                  { return m_state.lX; }
			long          y() const                  { return m_state.lY; }
			long          z() const                  { return m_state.lZ; }
			long          dx() const                 { return m_last_state.lX - m_state.lX; }
			long          dy() const                 { return m_last_state.lY - m_state.lY; }
			long          dz() const                 { return m_last_state.lZ - m_state.lZ; }
			void          xy(long &x, long &y) const { x = m_state.lX; y = m_state.lY; }
			
			// Buffered data
			EResult::Type ReadBuffer();
		};
	}
}

#endif
