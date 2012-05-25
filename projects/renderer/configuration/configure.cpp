//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

// A collection of global functions for configuring the renderer.
#include "renderer/utility/stdafx.h"
#include "pr/renderer/utility/errors.h"
#include "pr/renderer/configuration/configure.h"

using namespace pr;
using namespace pr::rdr;

// System *************************************************************
// The main object from which the configuration can be determined
pr::rdr::System::System()
:m_d3d(Direct3DCreate9(D3D_SDK_VERSION))
{
	if (!m_d3d)
		throw RdrException(EResult::CreateInterfaceFailed);
}
	
// Return the number of adapters available on the system
uint pr::rdr::System::GetAdapterCount() const
{
	return m_d3d->GetAdapterCount();

}
	
// Return info about a particular adapter
Adapter pr::rdr::System::GetAdapter(uint i) const
{
	return Adapter(m_d3d, i);
}
	
// Adapter ********************************************
pr::rdr::Adapter::Adapter(D3DPtr<IDirect3D9> d3d, uint adapter_index)
:m_d3d(d3d)
,m_adapter_index(adapter_index)
{
	m_d3d->GetAdapterIdentifier(m_adapter_index, 0, &m_info);
}
	
// Return the first display mode for this adaptor
D3DDISPLAYMODE const* pr::rdr::Adapter::ModeFirst(DisplayModeIter& iter) const
{
	iter.m_index = (uint)-1;
	iter.m_count = m_d3d->GetAdapterModeCount(m_adapter_index, iter.m_format);
	iter.m_mode = D3DDISPLAYMODE();
	return ModeNext(iter);
}
	
// Return the next display mode for this adaptor
D3DDISPLAYMODE const* pr::rdr::Adapter::ModeNext(DisplayModeIter& iter) const
{
	for (++iter.m_index; iter.m_index != iter.m_count; ++iter.m_index)
	{
		bool valid = 
			Succeeded(m_d3d->EnumAdapterModes(m_adapter_index, iter.m_format, iter.m_index, &iter.m_mode)) &&
			Succeeded(m_d3d->CheckDeviceType (m_adapter_index, iter.m_device, iter.m_format, iter.m_format, iter.m_windowed));
		if (valid) return &iter.m_mode;
	}
	return 0;
}
	
// Return a device config based on the provided display mode
DeviceConfig pr::rdr::Adapter::GetDeviceConfig(D3DDISPLAYMODE const& display_mode, D3DDEVTYPE device_type, bool windowed, uint d3dcreate_flags) const
{
	DeviceConfig config;
	config.m_adapter_index = m_adapter_index;
	config.m_device_type   = device_type;
	config.m_display_mode  = display_mode;
	config.m_windowed      = windowed;
	config.m_behavior      = d3dcreate_flags;
	Verify(m_d3d->GetDeviceCaps(m_adapter_index, device_type, &config.m_caps));
	
	// Note:
	// D3DCREATE_MULTITHREADED
	//  Indicates that the application requests Direct3D to be multithread safe.
	//  This makes a Direct3D thread take ownership of its global critical section more frequently,
	//  which can degrade performance. If an application processes window messages in one thread while
	//  making Direct3D API calls in another, the application must use this flag when creating the device.
	//  This window must also be destroyed before unloading d3d9.dll.
	
	// Choose a vertex processing behaviour based on whether there is hardware support
	if ((d3dcreate_flags & D3DCREATE_SOFTWARE_VERTEXPROCESSING) == 0 && config.m_caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT)
	{
		config.m_behavior |= D3DCREATE_HARDWARE_VERTEXPROCESSING;
		//if( config.m_caps.DevCaps & D3DDEVCAPS_PUREDEVICE )
		//	config.m_behavior |= D3DCREATE_PUREDEVICE;	// Steve says don't do this because the state won't be restored in effects
	}
	else
	{
		config.m_behavior |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;
	}
	return config;
}
	
// Config creation functions ************************************************************
// Auto detect a configuration
DeviceConfig pr::rdr::AutoDetectDeviceConfig(D3DDISPLAYMODE wanted_display_mode, bool windowed, D3DDEVTYPE device_type, uint d3dcreate_flags)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);
	if (windowed)
	{
		D3DDISPLAYMODE mode = adapter.CurrentDisplayMode();
		mode.Width  = wanted_display_mode.Width;
		mode.Height = wanted_display_mode.Height;
		return adapter.GetDeviceConfig(mode, device_type, windowed, d3dcreate_flags);
	}
	else
	{
		// Find the best matching display mode
		DisplayModeIter iter(device_type, wanted_display_mode.Format, windowed);
		D3DDISPLAYMODE const* best = adapter.ModeFirst(iter);
		for (D3DDISPLAYMODE const* mode = best; mode; mode = adapter.ModeNext(iter))
		{
			if ((!(*mode <  wanted_display_mode) && !(wanted_display_mode <  *mode)) || // display modes equal
				( (*best                < *mode) &&  (*mode <  wanted_display_mode)) || // mode is between 'best' and 'wanted'
				( (wanted_display_mode  < *mode) &&  (*mode < *best               )))   // mode is between 'wanted' and 'best'
				best = mode;
		}
		if (best == 0) throw RdrException(EResult::AutoSelectDisplayModeFailed, "Failed to find a suitable display mode on the selected graphics adapter");
		return adapter.GetDeviceConfig(*best);
	}
}
	
// Return a default full screen device config for this system
DeviceConfig pr::rdr::GetDefaultDeviceConfigFullScreen(uint screen_width, uint screen_height, D3DDEVTYPE device_type, uint d3dcreate_flags)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);

	for (uint attempt = 0; ; ++attempt)
	{
		D3DFORMAT format;
		switch (attempt)
		{
		default: throw RdrException(EResult::FailedToCreateDefaultConfig);
		case 0: format = D3DFMT_A8R8G8B8   ; break;
		case 1: format = D3DFMT_X8R8G8B8   ; break;
		case 2: format = D3DFMT_A1R5G5B5   ; break;
		case 3: format = D3DFMT_X1R5G5B5   ; break;
		case 4: format = D3DFMT_R5G6B5     ; break;
		case 5: format = D3DFMT_A2R10G10B10; break;
		}

		// Find a display mode with the width and height wanted, best refresh rate
		DisplayModeIter iter(device_type, format, false);
		D3DDISPLAYMODE const* best = 0;
		for (D3DDISPLAYMODE const* mode = adapter.ModeFirst(iter); mode; mode = adapter.ModeNext(iter))
		{
			if (mode->Width != screen_width || mode->Height != screen_height) continue;
			if (best != 0 && best->RefreshRate > mode->RefreshRate) continue;
			best = mode;
		}
		if (best != 0) 
			return adapter.GetDeviceConfig(*best, device_type, false, d3dcreate_flags);
	}
}
	
// Return a full screen device config using the best supported resolution
DeviceConfig pr::rdr::GetBestDeviceConfigFullScreen(D3DDEVTYPE device_type, uint d3dcreate_flags)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);

	for (uint attempt = 0; ; ++attempt)
	{
		D3DFORMAT format;
		switch (attempt)
		{
		default: throw RdrException(EResult::FailedToCreateDefaultConfig);
		case 0: format = D3DFMT_A8R8G8B8   ; break;
		case 1: format = D3DFMT_X8R8G8B8   ; break;
		case 2: format = D3DFMT_A1R5G5B5   ; break;
		case 3: format = D3DFMT_X1R5G5B5   ; break;
		case 4: format = D3DFMT_R5G6B5     ; break;
		case 5: format = D3DFMT_A2R10G10B10; break;
		}

		// Find a display mode with the best width, height, and refresh rate
		DisplayModeIter iter(device_type, format, false);
		D3DDISPLAYMODE const* best = 0;
		for (D3DDISPLAYMODE const* mode = adapter.ModeFirst(iter); mode; mode = adapter.ModeNext(iter))
		{
			if (best != 0 && (mode->RefreshRate < best->RefreshRate)) continue;
			if (best != 0 && (mode->Width < best->Width || mode->Height < best->Height)) continue;
			best = mode;
		}
		if (best != 0) 
			return adapter.GetDeviceConfig(*best, device_type, false, d3dcreate_flags);
	}
}
	
// Return a default device config for windowed mode on this system
DeviceConfig pr::rdr::GetDefaultDeviceConfigWindowed(D3DDEVTYPE device_type, uint d3dcreate_flags)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);
	return adapter.GetDeviceConfig(adapter.CurrentDisplayMode(), device_type, true, d3dcreate_flags);
}
	
