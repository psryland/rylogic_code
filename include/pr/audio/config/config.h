//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/audio/forward.h"

namespace pr
{
	namespace audio
	{
		// Create an instance of this object to enumerate the audio
		// devices on the current system
		struct SystemConfig
		{
			struct AudioDevice
			{
				std::wstring m_device_id;
				std::wstring m_description;
			};
			using DeviceCont = std::vector<AudioDevice>;
			DeviceCont m_devices;

			SystemConfig();
		};
	}
}