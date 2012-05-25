//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

// A collection of global functions for configuring the renderer.

#pragma once
#ifndef PR_RDR_CONFIGURE_H
#define PR_RDR_CONFIGURE_H

#include "pr/renderer/types/forward.h"

namespace pr
{
	namespace rdr
	{
		// Config with which to initialise d3d
		struct DeviceConfig
		{
			uint                m_adapter_index;       // The ordinal of the adapter we want to create the device on
			D3DDEVTYPE          m_device_type;         // The type of device to create
			D3DCAPS9            m_caps;                // Capabilities of this device
			uint                m_behavior;            // Hardware / Software / Mixed vertex processing
			D3DDISPLAYMODE      m_display_mode;        // The screen size, format, and refresh rate
			bool                m_windowed;            // True if this is a config for windowed moded
		};
		
		// Display mode iteration data
		struct DisplayModeIter
		{
			D3DDEVTYPE     m_device;
			D3DFORMAT      m_format;
			bool           m_windowed;
			D3DDISPLAYMODE m_mode;
			uint           m_index;
			uint           m_count;
			DisplayModeIter(D3DDEVTYPE device_type = D3DDEVTYPE_HAL, D3DFORMAT format = D3DFMT_A8R8G8B8, bool windowed = true) :m_device(device_type) ,m_format(format) ,m_windowed(windowed) {}
		};

		// Adapters on the current system
		struct Adapter
		{
			D3DPtr<IDirect3D9>      m_d3d;              // The d3d interface
			uint                    m_adapter_index;    // The ordinal for this adapter
			D3DADAPTER_IDENTIFIER9  m_info;             // Info about the driver for this adapter
			
			Adapter(D3DPtr<IDirect3D9> d3d, uint adapter_index);
			D3DDISPLAYMODE          CurrentDisplayMode() const                { D3DDISPLAYMODE mode; m_d3d->GetAdapterDisplayMode(m_adapter_index, &mode); return mode; }
			D3DDISPLAYMODE const*   ModeFirst(DisplayModeIter& iter) const;
			D3DDISPLAYMODE const*   ModeNext(DisplayModeIter& iter) const;
			DeviceConfig            GetDeviceConfig(D3DDISPLAYMODE const& display_mode, D3DDEVTYPE device_type = D3DDEVTYPE_HAL, bool windowed = true, uint d3dcreate_flags = 0) const;
		};
	
		// An object representing the current system
		struct System
		{
			D3DPtr<IDirect3D9> m_d3d;

			System();
			uint    GetAdapterCount() const;
			Adapter GetAdapter(uint i) const;
		};
	
		// Config creation functions
		DeviceConfig AutoDetectDeviceConfig(D3DDISPLAYMODE wanted_display_mode, bool windowed, D3DDEVTYPE device_type = D3DDEVTYPE_HAL, uint d3dcreate_flags = 0);
		DeviceConfig GetDefaultDeviceConfigFullScreen(uint screen_width, uint screen_height, D3DDEVTYPE device_type = D3DDEVTYPE_HAL, uint d3dcreate_flags = 0);
		DeviceConfig GetBestDeviceConfigFullScreen(D3DDEVTYPE device_type = D3DDEVTYPE_HAL, uint d3dcreate_flags = 0);
		DeviceConfig GetDefaultDeviceConfigWindowed(D3DDEVTYPE device_type = D3DDEVTYPE_HAL, uint d3dcreate_flags = 0);
	}
}
	
// Display mode operators
inline bool operator == (D3DDISPLAYMODE const& lhs, D3DDISPLAYMODE const& rhs)
{
	return lhs.Width == rhs.Width && lhs.Height == rhs.Height && lhs.Format == rhs.Format && lhs.RefreshRate == rhs.RefreshRate;
}
inline bool operator != (D3DDISPLAYMODE const& lhs, D3DDISPLAYMODE const& rhs)
{
	return !(lhs == rhs);
}
inline bool operator <  (D3DDISPLAYMODE const& lhs, D3DDISPLAYMODE const& rhs)
{
	if (lhs.Format != rhs.Format) return lhs.Format < rhs.Format;
	if (lhs.Width  != rhs.Width ) return lhs.Width  < rhs.Width;
	if (lhs.Height != rhs.Height) return lhs.Height < rhs.Height;
	return lhs.RefreshRate < rhs.RefreshRate;
}

#endif
