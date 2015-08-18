//**************************************************************
// FindBTDevices
//  (c)opyright Rylogic Limited 2015
//**************************************************************
// Scoped wrapper around the BluetoothFindFirstRadio/BluetoothFindNextRadio  API
// Usage:
//	for (pr::FindBTDevices f; !f.done(); f.next())
//	{
//		OutputDebugStringW(f.szName);
//	}

#pragma once

#include <string>
#include <windows.h>
#include <ws2bth.h>
#include <bthsdpdef.h>
#include <bluetoothapis.h>

namespace pr
{
	struct FindBTDevices :BLUETOOTH_DEVICE_INFO_STRUCT
	{
		struct Params :BLUETOOTH_DEVICE_SEARCH_PARAMS
		{
			Params() :BLUETOOTH_DEVICE_SEARCH_PARAMS()
			{
				dwSize = sizeof(BLUETOOTH_DEVICE_SEARCH_PARAMS);
				fReturnAuthenticated = TRUE;
				fReturnRemembered = TRUE;
				fReturnUnknown = TRUE;
				fReturnConnected = TRUE;

				// Use this to start a query for devices
				fIssueInquiry = FALSE;
				cTimeoutMultiplier = 0;

				// The specific radio to search, use nullptr for all
				hRadio = nullptr;
			}
		};

		Params m_search_params;
		HBLUETOOTH_RADIO_FIND m_find_radio;
		HBLUETOOTH_DEVICE_FIND m_find_device;
		HANDLE m_radio;
		bool m_more_radios;
		bool m_more_devices;

		FindBTDevices(Params const& p = Params())
			:BLUETOOTH_DEVICE_INFO_STRUCT()
			,m_search_params(p)
			,m_find_radio()
			,m_find_device()
			,m_more_radios(true)
			,m_more_devices(true)
		{
			dwSize = sizeof(BLUETOOTH_DEVICE_INFO_STRUCT);

			// Find the first radio
			auto find_radio_params = BLUETOOTH_FIND_RADIO_PARAMS{ sizeof(BLUETOOTH_FIND_RADIO_PARAMS) };
			m_find_radio = ::BluetoothFindFirstRadio(&find_radio_params, &m_radio);
			if (m_find_radio == nullptr)
			{
				auto err = ::GetLastError();
				switch (err) {
				default: throw std::exception("Error while enumerating bluetooth radio devices");
				case ERROR_NO_MORE_ITEMS:
					m_more_radios = false;
					m_more_devices = false;
					break;
				}
			}

			// Find the first device on this radio
			if (m_more_devices)
			{
				m_find_device = ::BluetoothFindFirstDevice(&m_search_params, this);
				if (m_find_device == nullptr)
				{
					auto err = ::GetLastError();
					switch (err) {
					default: throw std::exception("Failed to enumerate devices on bluetooth radio");
					case ERROR_NO_MORE_ITEMS:
						m_more_devices = false;
						break;
					}
				}
			}
		}
		~FindBTDevices()
		{
			if (m_find_device != nullptr)
				::BluetoothFindDeviceClose(m_find_device);
			if (m_find_radio != nullptr)
				::BluetoothFindRadioClose(m_find_radio);
		}

		// True once enumeration is complete
		bool done() const
		{
			return !(m_more_devices || m_more_radios);
		}

		// The handle of the current radio device being enumerated
		HANDLE radio() const
		{
			return m_radio;
		}

		// Move to the next device
		void next()
		{
			for (;!done();)
			{
				// If there are no more devices on the current radio, look for the next radio
				if (!m_more_devices)
				{
					if (!::BluetoothFindNextRadio(m_find_radio, &m_radio))
					{
						auto err = ::GetLastError();
						switch (err) {
						default: throw std::exception("Error while enumerating bluetooth radio devices");
						case ERROR_NO_MORE_ITEMS:
							m_more_radios = false;
							break;
						}
					}
					else
					{
						m_more_devices = true;
					}
				}
				
				// Look for the next device
				if (m_more_devices)
				{
					if (!::BluetoothFindNextDevice(m_find_device, this))
					{
						auto err = ::GetLastError();
						switch (err) {
						default: throw std::exception("Failed to enumerate devices on bluetooth radio");
						case ERROR_NO_MORE_ITEMS:
							m_more_devices = false;
							break;
						}
					}
				}
			}
		}

	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_hardware_find_bt_devices)
		{
			for (pr::FindBTDevices f; !f.done(); f.next())
			{
				OutputDebugStringW(f.szName);
			}
		}
	}
}
#endif
