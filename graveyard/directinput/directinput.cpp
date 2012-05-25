//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#include "pr/input/directinput/directinput.h"
#include "pr/common/hresult.h"
#include "pr/common/chain.h"
#include "pr/str/wstring.h"

using namespace pr;
using namespace pr::dinput;

enum { DEVICE_TYPE_MASK = 0xFF };
inline uint DeviceType(uint type)	{ return type & DEVICE_TYPE_MASK; }

// Call back function for enumerating devices. 'pvRef' points to an output iterator interface
BOOL CALLBACK EnumDevicesCallback(LPCDIDEVICEINSTANCE lpddi, LPVOID pvRef)
{
	IEnumOutput<DeviceInstance>& out = *static_cast<IEnumOutput<DeviceInstance>*>(pvRef);
	
	DeviceInstance dev_inst;
	dev_inst.m_device_type   = (uint)lpddi->dwDevType;
	dev_inst.m_instance_guid = lpddi->guidInstance;
	dev_inst.m_product_guid  = lpddi->guidProduct;
	dev_inst.m_instance_name = pr::str::ToAString<std::string>(lpddi->tszInstanceName);
	dev_inst.m_product_name  = pr::str::ToAString<std::string>(lpddi->tszProductName);
	if (out.Add(dev_inst) ) return DIENUM_CONTINUE;
	return DIENUM_STOP;
}

// Enumerate the devices on the system
void pr::dinput::EnumerateDevices(const Context& di_context, EDeviceClass::Type device_class, uint device_flags, IEnumOutput<DeviceInstance>& out)
{
	di_context.DIInterface()->EnumDevices(device_class, EnumDevicesCallback, &out, device_flags);
}

// Call back function for enumerating the data objects of a device. e.g. the buttons on a joystick etc
BOOL CALLBACK pr::dinput::impl::EnumDeviceObjectsCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef)
{
	DataFormatHelper& dfh = *static_cast<DataFormatHelper*>(pvRef);

	DIOBJECTDATAFORMAT object;
		 if (IsEqualGUID(lpddoi->guidType, GUID_XAxis ))	{ PR_INFO(PR_DBG_DINPUT, "GUID_XAxis "); object.pguid = &GUID_XAxis ; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_YAxis ))	{ PR_INFO(PR_DBG_DINPUT, "GUID_YAxis "); object.pguid = &GUID_YAxis ; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_ZAxis ))	{ PR_INFO(PR_DBG_DINPUT, "GUID_ZAxis "); object.pguid = &GUID_ZAxis ; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_RxAxis))	{ PR_INFO(PR_DBG_DINPUT, "GUID_RxAxis"); object.pguid = &GUID_RxAxis; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_RyAxis))	{ PR_INFO(PR_DBG_DINPUT, "GUID_RyAxis"); object.pguid = &GUID_RyAxis; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_RzAxis))	{ PR_INFO(PR_DBG_DINPUT, "GUID_RzAxis"); object.pguid = &GUID_RzAxis; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_Slider))	{ PR_INFO(PR_DBG_DINPUT, "GUID_Slider"); object.pguid = &GUID_Slider; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_Button))	{ PR_INFO(PR_DBG_DINPUT, "GUID_Button"); object.pguid = &GUID_Button; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_Key   ))	{ PR_INFO(PR_DBG_DINPUT, "GUID_Key   "); object.pguid = &GUID_Key   ; }
	else if (IsEqualGUID(lpddoi->guidType, GUID_POV   ))	{ PR_INFO(PR_DBG_DINPUT, "GUID_POV   "); object.pguid = &GUID_POV   ; }
	else                                                    { PR_INFO(PR_DBG_DINPUT, "Unknown device object type"); return TRUE; }

	object.dwOfs     = (DWORD)(dfh.m_data_format.size() * 4);
	object.dwType    = lpddoi->dwType;
	object.dwFlags   = lpddoi->dwFlags;
	dfh.m_data_format.push_back(object);
	dfh.m_device_object_instance.push_back(*lpddoi);
	return TRUE;
}

// Return the direct input interface
D3DPtr<IDirectInput8> pr::dinput::Context::DIInterface() const
{
	// Obtain the Direct Input interface
	if (m_interface) return m_interface;
	if (Failed(DirectInput8Create(m_app_instance, DIRECTINPUT_VERSION, IID_IDirectInput8, (void**)&m_interface.m_ptr, 0))) throw pr::dinput::EResult::CreateInterfaceFailed;
	return m_interface;
}

// Acquire / UnAcquire all devices
EResult::Type pr::dinput::Context::AcquireAll()
{
	EResult::Type result = EResult::Success;
	for (TDevices::iterator i = m_device_list.begin(), i_end = m_device_list.end(); i != i_end; ++i)
		if (Failed(i->Acquire())) result = EResult::AcquireDeviceFailed;
	return result;
}
void Context::UnAcquireAll()
{
	for (TDevices::iterator i = m_device_list.begin(), i_end = m_device_list.end(); i != i_end; ++i)
		i->UnAcquire();
}

// A selector for selecting devices
struct DeviceSelector :IEnumOutput<DeviceInstance>
{
	DeviceInstance	m_instance;
	std::string		m_product_name;
	GUID			m_product_guid;
	bool			m_found;

	DeviceSelector(std::string product_name, GUID product_guid)
	:m_product_name(product_name)
	,m_product_guid(product_guid)
	,m_found(false)
	{}
	bool Add(const DeviceInstance& instance)
	{
		if	(!m_product_name.empty()      && m_product_name != instance.m_product_name) return true;
		if	( m_product_guid != GUID_NULL && m_product_guid != instance.m_product_guid) return true;
		m_instance = instance;
		m_found = true;
		return false;
	}
};

// Find an instance of a device
bool pr::dinput::FindDeviceInstance(const Context& di_context, std::string product_name, GUID product_guid, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance)
{
	DeviceSelector selector(product_name, product_guid);
	EnumerateDevices(di_context, device_class, device_flags, selector);
	if( !selector.m_found ) return false;
	instance = selector.m_instance;
	return true;
}
bool pr::dinput::FindDeviceInstance(const Context& di_context, std::string product_name, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance)
{
	return FindDeviceInstance(di_context, product_name, GUID_NULL, device_class, device_flags, instance);
}
bool pr::dinput::FindDeviceInstance(const Context& di_context, GUID product_guid, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance)
{
	return FindDeviceInstance(di_context, "", product_guid, device_class, device_flags, instance);
}
bool pr::dinput::FindDeviceInstance(const Context& di_context, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance)
{
	return FindDeviceInstance(di_context, "", GUID_NULL, device_class, device_flags, instance);
}

// Get an instance of a device
DeviceInstance pr::dinput::GetDeviceInstance(const Context& di_context, std::string product_name, GUID product_guid, EDeviceClass::Type device_class, uint device_flags)
{
	DeviceInstance instance;
	if( !FindDeviceInstance(di_context, product_name, product_guid, device_class, device_flags, instance) )
		throw pr::dinput::EResult::DeviceNotFound;
	return instance;
}
DeviceInstance pr::dinput::GetDeviceInstance(const Context& di_context, std::string product_name, EDeviceClass::Type device_class, uint device_flags)
{
	return GetDeviceInstance(di_context, product_name, GUID_NULL, device_class, device_flags);
}
DeviceInstance pr::dinput::GetDeviceInstance(const Context& di_context, GUID product_guid, EDeviceClass::Type device_class, uint device_flags)
{
	return GetDeviceInstance(di_context, "", product_guid, device_class, device_flags);
}
DeviceInstance pr::dinput::GetDeviceInstance(const Context& di_context, EDeviceClass::Type device_class, uint device_flags)
{
	return GetDeviceInstance(di_context, "", GUID_NULL, device_class, device_flags);
}


////*****
//// Start direct input
//EResult::Type DirectInput::Initialise(const DirectInputSettings& settings)
//{
//	m_settings = settings;
//
//	// Obtain the Direct Input interface
//	if( Failed(DirectInput8Create(m_settings.m_app_instance, DIRECTINPUT_VERSION, IID_IDirectInput8, &m_interface.m_ptr, 0)) )
//		return EResult::CreateInterfaceFailed;
//
//	m_created_devices = 0;
//	return EResult::Success;
//}
//
////*****
//// Stop direct input
//void DirectInput::UnInitialise()
//{
//	UnAcquireAll();
//	m_created_devices = 0;
//	m_interface = 0;
//}
//
////*****
//// Create a keyboard device
//EResult::Type DirectInput::CreateKeyboard(const DIDeviceSettings& settings, DIKeyboard& keyboard)
//{
//	// Save the keyboard settings in the keyboard
//	keyboard.m_settings = settings;
//
//	// Enumerate the keyboard devices
//	TDeviceInstanceContainer device_instance;
//	if( Failed(m_interface->EnumDevices(DI8DEVCLASS_KEYBOARD, DIEnumDevicesCallback, &device_instance, DIEDFL_ALLDEVICES)) )
//		return EResult::EnumerateDevicesFailed;
//
//    // Find the first keyboard device
//	if( device_instance.empty() ) return EResult::NoSuitableDeviceFound;
//	DIDEVICEINSTANCE& keyboard_instance = device_instance[0];
//
//	// Create the keyboard device
//	if( Failed(m_interface->CreateDevice(keyboard_instance.guidInstance, &keyboard.m_device, 0)) )
//		return EResult::CreateDeviceFailed;
//
//	// Set the data format for the keyboard
//	if( Failed(keyboard.m_device->SetDataFormat(&c_dfDIKeyboard)) )
//		return EResult::SetDataFormatFailed;
//	
//	// Co-operate with windows
//	if( Failed(keyboard.m_device->SetCooperativeLevel(m_settings.m_window_handle, DISCL_FOREGROUND | DISCL_NONEXCLUSIVE)) )
//		return EResult::SetCooperativeLevelFailed;
//
//	// Buffered keyboard data
//	if( keyboard.m_settings.m_buffered )
//	{
//		DIPROPDWORD prop_data;
//		prop_data.diph.dwSize		= sizeof(DIPROPDWORD);
//		prop_data.diph.dwHeaderSize	= sizeof(DIPROPHEADER);
//		prop_data.diph.dwObj		= 0;
//		prop_data.diph.dwHow		= DIPH_DEVICE;
//		prop_data.dwData			= keyboard.m_settings.m_buffer_size;
//		if( Failed(keyboard.m_device->SetProperty(DIPROP_BUFFERSIZE, &prop_data.diph)) )
//			return EResult::SetBufferSizeFailed;
//	}
//
//	// Use event notification
//	if( keyboard.m_settings.m_events )
//	{
//		keyboard.m_event = CreateEvent(0, FALSE, FALSE, 0);
//		if( keyboard.m_event == 0 )
//			return EResult::CreateEventFailed;
//		
//		if( Failed(keyboard.m_device->SetEventNotification(keyboard.m_event)) )
//			return EResult::SetEventFailed;
//	}
//
//	// Add the keyboard to our list of created devices
//	keyboard.m_direct_input = this;
//	m_created_device.push_back(&keyboard);
//	return EResult::Success;
//}
//
////*****
//// Create a mouse device
//EResult::Type DirectInput::CreateMouse(const DIDeviceSettings& settings, DIMouse& mouse)
//{
//	// Save the mouse settings in the mouse
//	mouse.m_settings = settings;
//
//	// Enumerate the mouse devices
//	TDeviceInstanceContainer device_instance;
//	if( Failed(m_interface->EnumDevices(DI8DEVCLASS_POINTER, DIEnumDevicesCallback, &device_instance, DIEDFL_ALLDEVICES)) )
//		return EResult::EnumerateDevicesFailed;
//
//    // Find the first mouse device
//	if( device_instance.empty() ) return EResult::NoSuitableDeviceFound;
//	DIDEVICEINSTANCE& mouse_instance = device_instance[0];
//
//	// Create the mouse device
//	if( Failed(m_interface->CreateDevice(mouse_instance.guidInstance, &mouse.m_device, 0)) )
//		return EResult::CreateDeviceFailed;
//	
//	// Set data format for the mouse
//	if( Failed(mouse.m_device->SetDataFormat(&c_dfDIMouse2)) )
//		return EResult::SetDataFormatFailed;
//
//	// Co-operate with windows
//	if( Failed(mouse.m_device->SetCooperativeLevel(m_settings.m_window_handle, DISCL_FOREGROUND | DISCL_NONEXCLUSIVE)) )
//		return EResult::SetCooperativeLevelFailed;
//	
//	// Buffered mouse data
//	if( mouse.m_settings.m_buffered )
//	{
//		DIPROPDWORD prop_data;
//		prop_data.diph.dwSize		= sizeof(DIPROPDWORD);
//		prop_data.diph.dwHeaderSize	= sizeof(DIPROPHEADER);
//		prop_data.diph.dwObj		= 0;
//		prop_data.diph.dwHow		= DIPH_DEVICE;
//		prop_data.dwData			= mouse.m_settings.m_buffer_size;
//		if( Failed(mouse.m_device->SetProperty(DIPROP_BUFFERSIZE, &prop_data.diph)) )
//			return EResult::SetBufferSizeFailed;
//	}
//
//	// Use event notification
//	if( mouse.m_settings.m_events )
//	{
//		mouse.m_event = CreateEvent(0, FALSE, FALSE, 0);
//		if( mouse.m_event == 0 )
//			return EResult::CreateEventFailed;
//		
//		if( Failed(mouse.m_device->SetEventNotification(mouse.m_event)) )
//			return EResult::SetEventFailed;
//	}
//
//	// Add the mouse to our list of created devices
//	mouse.m_direct_input = this;
//	m_created_device.push_back(&mouse);
//	return EResult::Success;
//}
//
////*****
//// Create a joystick device
//EResult::Type DirectInput::CreateJoystick(const JoystickSettings& settings, Joystick& joystick)
//{
//	// Save the joystick settings in the joystick
//	joystick.m_settings = settings;
//
//	// Enumerate the joystick devices
//	TDeviceInstanceContainer device_instance;
//	if( Failed(m_interface->EnumDevices(DI8DEVCLASS_GAMECTRL, DIEnumDevicesCallback, &device_instance, DIEDFL_ALLDEVICES)) )
//		return EResult::EnumerateDevicesFailed;
//
//    // Find the device with a matching product name
//	std::size_t d, num_devices = device_instance.size();
//	if( !joystick.m_settings.m_product_name.empty() )
//	{
//        for( d = 0; d < num_devices; ++d )
//			if( stricmp(joystick.m_settings.m_product_name.c_str(), device_instance[d].tszProductName) == 0 ) break;
//	}
//	// Otherwise look for the matching product GUID
//	else
//	{
//        for( d = 0; d < num_devices; ++d )
//			if( IsEqualGUID(joystick.m_settings.m_product_guid, device_instance[d].guidProduct) ) break;
//	}
//	if( d == num_devices ) return EResult::NoSuitableDeviceFound;
//	DIDEVICEINSTANCE& joystick_instance = device_instance[d];
//
//	// Create the joystick device
//	if( Failed(m_interface->CreateDevice(joystick_instance.guidInstance, &joystick.m_device, 0)) )
//		return EResult::CreateDeviceFailed;
//
//	// Construct the data format for this device
//	m_data_format.clear();
//	device_instance.clear();
//	DIDataFormatHelper helper = {&m_data_format, &m_device_object_instance };
//	if( Failed(joystick.m_device->EnumObjects(DIEnumDeviceObjectsCallback, &helper, DIDFT_ALL)) )
//		return EResult::EnumerateDeviceObjectsFailed;
//	
//	DIDATAFORMAT format;
//	format.dwSize		= sizeof(DIDATAFORMAT);
//	format.dwObjSize	= sizeof(DIOBJECTDATAFORMAT);
//	format.dwFlags		= DIDF_ABSAXIS;
//	format.dwDataSize	= (DWORD)m_data_format.size() * 4;
//	format.dwNumObjs	= (DWORD)m_data_format.size();
//	format.rgodf		= &m_data_format[0];
//	joystick			.m_state.resize(m_data_format.size());
//
//	// Set the data format for the joystick
//	if( Failed(joystick.m_device->SetDataFormat(&format)) )
//		return EResult::SetDataFormatFailed;
//
//	// Co-operate with windows
//	if( Failed(joystick.m_device->SetCooperativeLevel(m_settings.m_window_handle, DISCL_FOREGROUND | DISCL_NONEXCLUSIVE)) )
//		return EResult::SetCooperativeLevelFailed;
//
//	// Buffered data
//	if( joystick.m_settings.m_buffered )
//	{
//		DIPROPDWORD prop_data;
//		prop_data.diph.dwSize		= sizeof(DIPROPDWORD);
//		prop_data.diph.dwHeaderSize	= sizeof(DIPROPHEADER);
//		prop_data.diph.dwObj		= 0;
//		prop_data.diph.dwHow		= DIPH_DEVICE;
//		prop_data.dwData			= joystick.m_settings.m_buffer_size;
//		if( Failed(joystick.m_device->SetProperty(DIPROP_BUFFERSIZE, &prop_data.diph)) )
//			return EResult::SetBufferSizeFailed;
//	}
//
//	// Use event notification
//	if( joystick.m_settings.m_events )
//	{
//		joystick.m_event = CreateEvent(0, FALSE, FALSE, 0);
//		if( joystick.m_event == 0 )
//			return EResult::CreateEventFailed;
//		
//		if( Failed(joystick.m_device->SetEventNotification(joystick.m_event)) )
//			return EResult::SetEventFailed;
//	}
//
//	// Add the joystick to our list of created devices
//	joystick.m_direct_input = this;
//	m_created_device.push_back(&joystick);
//	return EResult::Success;
//}
//
