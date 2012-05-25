//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/didevice.h"
#include "pr/input/directinput/directinput.h"

using namespace pr;
using namespace pr::dinput;

// Constructor
Device::Device(Context& di_context, const DeviceSettings& settings)
:m_settings(settings)
,m_interface(di_context.DIInterface())
,m_device(0)
,m_event(0)
{
	// Create the device
	if (Failed(m_interface->CreateDevice(m_settings.m_instance.m_instance_guid, &m_device.m_ptr, 0)))
		throw EResult::CreateDeviceFailed;
		
	// Co-operate with windows
	if (Failed(m_device->SetCooperativeLevel(m_settings.m_base.m_window_handle, DISCL_FOREGROUND | DISCL_NONEXCLUSIVE)))
		throw EResult::SetCooperativeLevelFailed;
		
	// Support buffered data
	if (m_settings.m_base.m_buffered)
	{
		DIPROPDWORD prop_data;
		prop_data.diph.dwSize       = sizeof(DIPROPDWORD);
		prop_data.diph.dwHeaderSize = sizeof(DIPROPHEADER);
		prop_data.diph.dwObj        = 0;
		prop_data.diph.dwHow        = DIPH_DEVICE;
		prop_data.dwData            = m_settings.m_base.m_buffer_size;
		if (Failed(m_device->SetProperty(DIPROP_BUFFERSIZE, &prop_data.diph)))
			throw EResult::SetBufferSizeFailed;
	}
	
	// Support event notification
	if (m_settings.m_base.m_events)
	{
		m_event = CreateEvent(0, FALSE, FALSE, 0);
		if (!m_event) throw EResult::CreateEventFailed;
		
		if (Failed(m_device->SetEventNotification(m_event)))
			throw EResult::SetEventFailed;
	}
	
	// Add this device to the chain list of devices
	di_context.AddDevice(*this);
}

// Destructor
Device::~Device()
{
	UnAcquire();
}

// Copy constructor
Device::Device(const Device& copy)
:m_device(copy.m_device)
{}

// Acquire the device.
// Returns "Success" if the device was acquired, "InputLost" if not. All other cases are coding errors
EResult::Type Device::Acquire()
{
	switch (m_device->Acquire())
	{
	case DI_OK:
	case S_FALSE:               return EResult::Success;
	case DIERR_OTHERAPPHASPRIO: return EResult::InputLost;
	default: PR_ASSERT(PR_DBG_DINPUT, false, "Trying to acquire an uninitialised device"); return EResult::Failed;
	}
}

// UnAcquire
void Device::UnAcquire()
{
	m_device->Unacquire();
}

// Flush the data from the buffer
void Device::FlushBuffer()
{
	DWORD count = INFINITE;
	Verify(m_device->GetDeviceData(sizeof(DIDEVICEOBJECTDATA), NULL, &count, 0));
}

// Read the state of the device
// Returns "Success" if the status was read successfully, "InputLost" if another app has the device
// or "DataPending" if the data isn't ready yet.
EResult::Type Device::ReadDeviceState(void* buffer, std::size_t buffer_size)
{
	EResult::Type result;
	do
	{
		if (Succeeded(m_device->GetDeviceState((DWORD)buffer_size, buffer))) return EResult::Success;
	}
	while ((result = Acquire()) == EResult::Success);
	return result;
}

// Read up to 'count' data items from the device. On return 'count' contains the number actually read
// 'flags' is one of 0 or DIGDD_PEEK
// Returns "Success" if the status was read successfully, "InputLost" if another app has the device
// or "DataPending" if the data isn't ready yet.
EResult::Type Device::ReadDeviceData(DIDEVICEOBJECTDATA* buffer, DWORD& count, uint flags)
{
	EResult::Type result;
	do
	{
		switch (m_device->GetDeviceData(sizeof(DIDEVICEOBJECTDATA), buffer, &count, flags))
		{
		case DI_OK:             return EResult::Success;
		case DI_BUFFEROVERFLOW: return EResult::BufferOverflow; // This indicates some data was lost
		}
	}
	while ((result = Acquire()) == EResult::Success);
	return result;
}
