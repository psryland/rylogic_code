//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/dijoystick.h"
#include "pr/input/directinput/directinput.h"

using namespace pr;
using namespace pr::dinput;

// Constructor
Joystick::Joystick(Context& di_context, const DeviceSettings& settings)
:Device(di_context, settings)
{
	// Construct the data format for this device
	impl::DataFormatHelper helper;
	if (Failed(m_device->EnumObjects(pr::dinput::impl::EnumDeviceObjectsCallback, &helper, DIDFT_ALL)))
		throw EResult::EnumerateDeviceObjectsFailed;
	
	DIDATAFORMAT format;
	format.dwSize		= sizeof(DIDATAFORMAT);
	format.dwObjSize	= sizeof(DIOBJECTDATAFORMAT);
	format.dwFlags		= DIDF_ABSAXIS;
	format.dwDataSize	= (DWORD)helper.m_data_format.size() * 4;
	format.dwNumObjs	= (DWORD)helper.m_data_format.size();
	format.rgodf		= &helper.m_data_format[0];
	m_state				.resize(helper.m_data_format.size());

	// Set the data format for the joystick
	if (Failed(m_device->SetDataFormat(&format)))
		throw EResult::SetDataFormatFailed;
}

// Sample the state of the joystick at this point in time
EResult::Type Joystick::Sample()
{
	std::size_t buffer_size = sizeof(uint) * m_state.size();
	ZeroMemory(&m_state[0], buffer_size);

	m_device->Poll();
	return ReadDeviceState(&m_state[0], buffer_size);
}

// Query for buffered data.
// Read up to 'max_to_read' into 'm_state'. After calling this the 'AxisValue'
// and 'ButtonDown' methods can be used.
// If 'buffer' is non-null, fill it with the joystick data
// Returns EResult::Success if there is more data to read
EResult::Type Joystick::ReadBuffer(uint max_to_read, TJoyBuffer* buffer)
{
	if (buffer) { buffer->reserve(buffer->size() + max_to_read); }

	uint read = 0;
	while (read < max_to_read)
	{
		// Read the buffer
		DWORD count = BufferedBlockReadSize < max_to_read ? BufferedBlockReadSize : max_to_read;
		DIDEVICEOBJECTDATA buffered_object_data[BufferedBlockReadSize];
		if( Failed(ReadDeviceData(buffered_object_data, count)) ) return EResult::Failed;

		// Copy the data into the state buffer
		DIDEVICEOBJECTDATA* in = buffered_object_data;
		for (uint i = 0; i < count; ++i, ++in)
		{
			// Copy into the state buffer
			PR_ASSERT(PR_DBG_DINPUT, in->dwOfs < m_state.size(), "");
			m_state[in->dwOfs] = in->dwData;

			if (buffer)
			{
				JoyData joy = {in->dwOfs, in->dwData};
				buffer->push_back(joy);
			}
		}
		read += count;

		// No more data left?
		if (count != max_to_read) return EResult::Success;
	}
	return EResult::MoreDataAvailable;
}
