//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#ifndef PR_DINPUT_JOYSTICK_H
#define PR_DINPUT_JOYSTICK_H

#include <vector>
#include "pr/input/directinput/didevice.h"

namespace pr
{
	namespace dinput
	{
		struct JoyData
		{
			uint m_index;
			uint m_state;
		};
		typedef std::vector<JoyData> TJoyBuffer;

		class Joystick :public Device
		{
			std::vector<uint> m_state;
		public:
			Joystick(Context& di_context, const DeviceSettings& settings);

			// Non buffered data
			EResult::Type Sample();
			uint          AxisValue(uint index) const		{ PR_ASSERT(PR_DBG_DINPUT, index < m_state.size(), ""); return m_state[index]; }
			bool          ButtonDown(uint index) const	{ PR_ASSERT(PR_DBG_DINPUT, index < m_state.size(), ""); return (m_state[index] & 0x80) == 0x80; }

			// Buffered data
			EResult::Type ReadBuffer(uint max_to_read)	{ return ReadBuffer(max_to_read, 0); }
			EResult::Type ReadBuffer(uint max_to_read, TJoyBuffer* buffer);
		};
	}
}

#endif
