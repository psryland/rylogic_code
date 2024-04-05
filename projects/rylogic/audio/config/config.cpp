//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/forward.h"
#include "pr/audio/config/config.h"

#define _HIDE_GLOBAL_ASYNC_STATUS
#include <Windows.Devices.Enumeration.h>
#include <ppltasks.h>
#include <wrl.h>

namespace pr::audio
{
	SystemConfig::SystemConfig()
	{
		// This code came from the XAudio2Enumerate sample.
		// Search for 'XAudio2 Win32 Samples' on 'code.msdn.microsoft.com'

		// Enumerating with WinRT using WRL
		using namespace Microsoft::WRL;
		using namespace Microsoft::WRL::Wrappers;
		using namespace ABI::Windows::Foundation;
		using namespace ABI::Windows::Foundation::Collections;
		using namespace ABI::Windows::Devices::Enumeration;

		// Create the XAudio interface
		RefPtr<IXAudio2> xaudio;
		Check(XAudio2Create(&xaudio.m_ptr, 0));

		// Initialise WinRT
		RoInitializeWrapper initialize(RO_INIT_MULTITHREADED);
		Check(initialize);

		// Create the factory object for enumerating devices
		ComPtr<IDeviceInformationStatics> device_info_factory;
		Check(GetActivationFactory(HStringReference(RuntimeClass_Windows_Devices_Enumeration_DeviceInformation).Get(), &device_info_factory));

		// Create an event to signal the find is complete
		Event find_completed(CreateEventExW(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, WRITE_OWNER | EVENT_ALL_ACCESS));
		if (!find_completed.IsValid())
			Check(HRESULT_FROM_WIN32(GetLastError()));

		// Search for audio devices
		ComPtr<IAsyncOperation<DeviceInformationCollection*>> operation;
		Check(device_info_factory->FindAllAsyncDeviceClass(DeviceClass_AudioRender, operation.GetAddressOf()));
		operation->put_Completed(Callback<IAsyncOperationCompletedHandler<DeviceInformationCollection*>>(
			[&find_completed](IAsyncOperation<DeviceInformationCollection*>* aDevices, AsyncStatus status) -> HRESULT
			{
				SetEvent(find_completed.Get());
				return S_OK;
			}).Get());

		// Wait till complete
		WaitForSingleObject(find_completed.Get(), INFINITE);

		// Copy the results to 'devices'
		ComPtr<IVectorView<DeviceInformation*>> devices;
		operation->GetResults(devices.GetAddressOf());

		// Copy the device info to the 'm_devices' container
		unsigned int count = 0;
		Check(devices->get_Size(&count));
		for (unsigned int j = 0; j < count; ++j)
		{
			ComPtr<IDeviceInformation> deviceInfo;
			Check(devices->GetAt(j, deviceInfo.GetAddressOf()));

			HString id;
			deviceInfo->get_Id(id.GetAddressOf());

			HString name;
			deviceInfo->get_Name(name.GetAddressOf());

			AudioDevice device;
			device.m_device_id = id.GetRawBuffer(nullptr);
			device.m_description = name.GetRawBuffer(nullptr);
			m_devices.emplace_back(device);
		}
	}
}