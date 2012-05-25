//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/dikeyboard.h"
#include "pr/input/directinput/directinput.h"
#include "pr/maths/scalar.h"

using namespace pr;
using namespace pr::dinput;

// Constructor
Keyboard::Keyboard(Context& di_context, const DeviceSettings& settings)
:Device(di_context, settings)
{
	// Set the data format for the keyboard
	if (Failed(m_device->SetDataFormat(&c_dfDIKeyboard)))
		throw EResult::SetDataFormatFailed;
}

// Sample the state of the keyboard at this point in time
EResult::Type Keyboard::Sample()
{
	ZeroMemory(m_key_state, sizeof(m_key_state));
	return ReadDeviceState(&m_key_state, sizeof(m_key_state));
}

// Query for buffered data.
uint Keyboard::ReadBuffer(uint max_to_read, KeyData* events)
{
	uint read = 0;
	while (read < max_to_read)
	{
		// Read the buffer
		DWORD count = pr::Min<DWORD>(BufferedBlockReadSize, max_to_read);
		DIDEVICEOBJECTDATA buffered_object_data[BufferedBlockReadSize];
		if (Failed(ReadDeviceData(buffered_object_data, count))) return 0;

		// Copy the data into the key buffer
		DIDEVICEOBJECTDATA* in = buffered_object_data;
		for (uint i = 0; i < count; ++i, ++in)
		{
			// Copy into the key state buffer
			PR_ASSERT(PR_DBG_DINPUT, in->dwOfs < 256, "");
			m_key_state[in->dwOfs] = static_cast<uint8>(in->dwData);

			if (events)
			{
				events->m_key		= in->dwOfs;
				events->m_state		= static_cast<uint8>(in->dwData);
				events->m_timestamp	= static_cast<uint> (in->dwTimeStamp);
				++events;
			}
		}
		read += count;

		// No more data left?
		if (count != max_to_read) break;
	}
	return read;
}
