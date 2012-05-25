//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/dimouse.h"
#include "pr/input/directinput/directinput.h"

using namespace pr;
using namespace pr::dinput;

// Constructor
Mouse::Mouse(Context& di_context, const DeviceSettings& settings)
:Device(di_context, settings)
{
	// Set data format for the mouse
	if (Failed(m_device->SetDataFormat(&c_dfDIMouse2)))
		throw EResult::SetDataFormatFailed;
}

// Sample the state of the mouse at this point in time
EResult::Type Mouse::Sample()
{
	m_last_state = m_state;
	ZeroMemory(&m_state, sizeof(m_state));
	return ReadDeviceState(&m_state, sizeof(m_state));
}

// Query for buffered data. After calling this the mouse accessor methods can be used.
EResult::Type Mouse::ReadBuffer()
{
	DWORD count = 1;
	DIDEVICEOBJECTDATA buffered_object_data;
	if (Failed(ReadDeviceData(&buffered_object_data, count))) return EResult::Failed;

	// No data retrieved?
	if (count == 0) return EResult::Success;

	// Copy the data
	m_last_state = m_state;
	switch (buffered_object_data.dwOfs)
	{
	case DIMOFS_X:			m_state.lX = buffered_object_data.dwData;					break;
	case DIMOFS_Y:			m_state.lY = buffered_object_data.dwData;					break;
	case DIMOFS_Z:			m_state.lZ = buffered_object_data.dwData;					break;
	case DIMOFS_BUTTON0:	m_state.rgbButtons[0] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON1:	m_state.rgbButtons[1] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON2:	m_state.rgbButtons[2] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON3:	m_state.rgbButtons[3] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON4:	m_state.rgbButtons[4] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON5:	m_state.rgbButtons[5] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON6:	m_state.rgbButtons[6] = (BYTE)buffered_object_data.dwData;	break;
	case DIMOFS_BUTTON7:	m_state.rgbButtons[7] = (BYTE)buffered_object_data.dwData;	break;
	default: PR_ASSERT(PR_DBG_DINPUT, false, "");
	}
	return EResult::Success;
}
