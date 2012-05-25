//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************
// Design:
//  The client only knows about devices. Their code contains instances of devices
//  that are constructed using data returned from the enumerate function
//
//  The dinput lib contains a reference counted singleton object
//  Usuage:
//      Create a DirectInput object. Persistence of this object isn't required
//      Use it to enumerate/create devices
//
//      Call a "DirectInput::CreateXXX()" method to create a device
//      "DirectInput::ReleaseDevice()" can be called to release a device but is optional
//      All created devices are released when UnInitialiseDirectInput() is called
//
#ifndef PR_DINPUT_DIRECTINPUT_H
#define PR_DINPUT_DIRECTINPUT_H

#include "pr/input/directinput/forward.h"
#include "pr/input/directinput/didevice.h"
#include "pr/common/assert.h"
#include "pr/common/d3dptr.h"
#include "pr/common/chain.h"
#include "pr/common/ienumoutput.h"

// Required Libs: dinput8.lib dxguid.lib

namespace pr
{
	namespace dinput
	{
		// A context for creating direct input devices
		class Context
		{
			typedef chain::head<Device> TDevices;
			TDevices                        m_device_list;
			mutable D3DPtr<IDirectInput8>   m_interface;
			HINSTANCE                       m_app_instance;
			
		public:
			Context(HINSTANCE app_instance) :m_app_instance(app_instance), m_interface(0) {}
			D3DPtr<IDirectInput8>   DIInterface() const;
			void AddDevice(Device& device) { m_device_list.push_back(device); }
			
			// Acquire/UnAcquire all devices
			EResult::Type AcquireAll();
			void          UnAcquireAll();
		};
		
		namespace impl
		{
			// Enumerate device objects
			typedef std::vector<DIOBJECTDATAFORMAT>     TObjectDataFormatContainer;
			typedef std::vector<DIDEVICEOBJECTINSTANCE> TDeviceObjectInstanceContainer;
			struct DataFormatHelper
			{
				TObjectDataFormatContainer      m_data_format;
				TDeviceObjectInstanceContainer  m_device_object_instance;
			};
			BOOL CALLBACK EnumDeviceObjectsCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef);
		}
		
		// Enumerate the devices on the system
		void EnumerateDevices(Context const& di_context, EDeviceClass::Type device_class, uint device_flags, IEnumOutput<DeviceInstance>& out);
		
		// Find an instance of a device. Returns true if found
		bool FindDeviceInstance(Context const& di_context, std::string product_name, GUID product_guid, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance);
		bool FindDeviceInstance(Context const& di_context, std::string product_name,                    EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance);
		bool FindDeviceInstance(Context const& di_context,                           GUID product_guid, EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance);
		bool FindDeviceInstance(Context const& di_context,                                              EDeviceClass::Type device_class, uint device_flags, DeviceInstance& instance);
		
		// Get an instance of a device. di::Exception(EResult_DeviceNotFound} thrown if not found
		DeviceInstance GetDeviceInstance(Context const& di_context, std::string product_name, GUID product_guid, EDeviceClass::Type device_class, uint device_flags);
		DeviceInstance GetDeviceInstance(Context const& di_context, std::string product_name,                    EDeviceClass::Type device_class, uint device_flags);
		DeviceInstance GetDeviceInstance(Context const& di_context,                           GUID product_guid, EDeviceClass::Type device_class, uint device_flags);
		DeviceInstance GetDeviceInstance(Context const& di_context,                                              EDeviceClass::Type device_class, uint device_flags);
		
	}
}

#endif
