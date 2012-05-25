//***********************************************************************************
// Direct Input
//  Copyright © Rylogic Ltd 2009
//***********************************************************************************

#ifndef PR_DINPUT_DEVICE_H
#define PR_DINPUT_DEVICE_H

#include "pr/common/d3dptr.h"
#include "pr/common/chain.h"
#include "pr/input/directinput/forward.h"

namespace pr
{
	namespace dinput
	{
		struct DeviceSettingsBase
		{
			HWND            m_window_handle;    // The window that the device is associated with
			bool            m_buffered;         // Whether to use buffered data
			unsigned int    m_buffer_size;      // Number of events to buffer
			bool            m_events;           // Whether to use events
			
			DeviceSettingsBase()
			:m_window_handle(0)
			,m_buffered(false)
			,m_buffer_size(0)
			,m_events(false)
			{}
			DeviceSettingsBase(HWND window_handle, bool buffered, unsigned int  buffer_size, bool events)
			:m_window_handle(window_handle)
			,m_buffered(buffered)
			,m_buffer_size(buffer_size)
			,m_events(events)
			{}
		};
		
		struct DeviceSettings
		{
			DeviceSettingsBase  m_base;
			DeviceInstance      m_instance;         // The device instance to use
			
			DeviceSettings()
			:m_base()
			,m_instance()
			{}
			DeviceSettings(const DeviceInstance& instance, const DeviceSettingsBase& base)
			:m_base(base)
			,m_instance(instance)
			{}
			DeviceSettings(const DeviceInstance& instance, HWND window_handle, bool buffered, unsigned int  buffer_size, bool events)
			:m_base(window_handle, buffered, buffer_size, events)
			,m_instance(instance)
			{}
		};
		
		// Base class for a dinput device
		class Device :public chain::link<Device>
		{
		protected:
			friend D3DPtr<IDirectInput8> GetDirectInputInterface();
			DeviceSettings              m_settings;
			D3DPtr<IDirectInput8>       m_interface;        // The direct input interface
			D3DPtr<IDirectInputDevice8> m_device;
			HANDLE                      m_event;
			
			EResult::Type ReadDeviceState(void* buffer, std::size_t buffer_size);
			EResult::Type ReadDeviceData(DIDEVICEOBJECTDATA* buffer, DWORD& count, unsigned int flags);
			EResult::Type ReadDeviceData(DIDEVICEOBJECTDATA* buffer, DWORD& count) { return ReadDeviceData(buffer, count, 0); }
			
		public:
			Device(Context& di_context, const DeviceSettings& settings);
			virtual ~Device();
			Device(const Device& copy);
			
			EResult::Type Acquire();
			void UnAcquire();
			
			void FlushBuffer();
			
			// Returns one of EWaitResult. 'how_long' may equal INFINITE
			unsigned int WaitForEvent(unsigned int how_long) const      { return WaitForSingleObjectEx(m_event, how_long, TRUE); }
		};
	}
}

#endif
