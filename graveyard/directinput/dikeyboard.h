//***********************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************

#ifndef PR_DINPUT_KEYBOARD_H
#define PR_DINPUT_KEYBOARD_H

#include "pr/input/directinput/didevice.h"

namespace pr
{
	namespace dinput
	{
		struct KeyData
		{
			uint  m_key;            // Which key
			uint8 m_state;          // The state of the key
			uint  m_timestamp;      // The time at which the key changed state in milliseconds
			bool  KeyDown() const   { return (m_state & 0x80) != 0; }
		};
		
		class Keyboard :public Device
		{
			enum { MaxkeyStates = 256 };
			uint8 m_key_state[MaxkeyStates];
			
		public:
			Keyboard(Context& di_context, const DeviceSettings& settings);
			
			// Non buffered data
			EResult::Type Sample();
			bool          KeyDown(int key) const { return (m_key_state[key] & 0x80) == 0x80; }
			
			// Buffered data - Reads from the directX buffered keyboard data into 'm_key_state'
			// This method can be used to read key event data into 'events'. 'events' must point
			// to a buffer of at least 'max_to_read' KeyData structs. Returns the number of
			// buffered events read.
			uint ReadBuffer(uint max_to_read)       { return ReadBuffer(max_to_read, 0); }
			uint ReadBuffer(uint max_to_read, KeyData* events);
		};
	}
}

#endif
