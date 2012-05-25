//**********************************************************************
//
//	Configure.
//
//**********************************************************************
// A collection of global functions for configuring the renderer.
#include "Stdafx.h"
#include "PR/Renderer/Configure.h"
#include "PR/Renderer/Errors.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Return a default device config for this system
DeviceConfig pr::rdr::GetDefaultDeviceConfig(bool windowed)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);
	adapter.SetWindowed(windowed);

	if( windowed )
	{
		return adapter.GetDeviceConfig(adapter.GetCurrentDisplayMode());
	}
	else
	{
		// Find a 800 x 600 any refresh rate, X8R8G8B8 format display mode
        for( DisplayModeIter mode = adapter.ModeBegin(), mode_end = adapter.ModeEnd(); mode != mode_end; ++mode )
		{
			if( mode->Width == 800 && mode->Height == 600 && mode->Format == D3DFMT_X8R8G8B8 )
			{
				return adapter.GetDeviceConfig(*mode);
			}
		}
	}
	throw Exception(EResult_FailedToCreateDefaultConfig);
}

//*****
// Auto detect a configuration
DeviceConfig pr::rdr::AutoDetectDeviceConfig(D3DDISPLAYMODE wanted_display_mode, bool windowed, D3DDEVTYPE device_type, bool software_vertex_processing)
{
	System system;
	Adapter adapter = system.GetAdapter(D3DADAPTER_DEFAULT);
	adapter.SetDeviceType              (device_type);
	adapter.SetFormat                  (wanted_display_mode.Format);
	adapter.SetWindowed                (windowed);
	adapter.SetSoftwareVertexProcessing(software_vertex_processing);

	if( windowed )
	{
		D3DDISPLAYMODE mode = adapter.GetCurrentDisplayMode();
		mode.Width  = wanted_display_mode.Width;
		mode.Height = wanted_display_mode.Height;
		return adapter.GetDeviceConfig(mode);
	}
	else
	{
		DisplayModeIter best_mode = adapter.ModeEnd();
		for( DisplayModeIter mode = adapter.ModeBegin(), mode_end = adapter.ModeEnd(); mode != mode_end; ++mode )
		{
			if( (!(*mode <  wanted_display_mode) && !(wanted_display_mode <  *mode)) ||
				( (*best_mode           < *mode) &&  (*mode <  wanted_display_mode)) || 
				( (wanted_display_mode  < *mode) &&  (*mode < *best_mode          )) )
			{
				best_mode = mode;
			}
		}
		if( best_mode == adapter.ModeEnd() ) { throw Exception(EResult_AutoSelectDisplayModeFailed, "Failed to locate a suitable display mode on the selected graphics adapter"); }
		return adapter.GetDeviceConfig(*best_mode);
	}
}

//**********************************************************************
// System
//*****
// The main object from which the configuration can be determined
System::System()
:m_d3d(Direct3DCreate9(D3D_SDK_VERSION))
{
	if( !m_d3d )
	{
		throw Exception(EResult_CreateD3DInterfaceFailed);
	}
}

//**********************************************************************
// Adapter
Adapter::Adapter(D3DPtr<IDirect3D9> d3d, uint adapter_index)
:m_d3d(d3d)
,m_adapter_index(adapter_index)
,m_device_type(D3DDEVTYPE_HAL)
,m_format(D3DFMT_UNKNOWN)
,m_windowed(true)
,m_software_vp(false)
,m_max_modes(0)
{
	m_d3d->GetAdapterIdentifier(m_adapter_index, 0, &m_identifier);
}

//*****
// Return a device config based on the provided display mode
DeviceConfig Adapter::GetDeviceConfig(const D3DDISPLAYMODE& display_mode) const
{
	DeviceConfig config;
	config.m_adapter_index	= m_adapter_index;
	config.m_device_type	= m_device_type;
	config.m_display_mode	= display_mode;
	
	// Set the caps for the device
	Verify(m_d3d->GetDeviceCaps(m_adapter_index, m_device_type, &config.m_caps));

	// Choose a vertex processing behaviour based on whether there is hardware support
	config.m_behavior = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
	if( !m_software_vp && config.m_caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT )
	{
		config.m_behavior = D3DCREATE_HARDWARE_VERTEXPROCESSING;
		//if( config.m_caps.DevCaps & D3DDEVCAPS_PUREDEVICE )
		//	config.m_behavior |= D3DCREATE_PUREDEVICE;	// Steve says don't do this because the state won't be restored in effects
	}
	return config;
}

//**********************************************************************
// Display Mode iterator
DisplayModeIter::DisplayModeIter(const Adapter* adapter, uint mode_index, uint last_mode_index)
:m_adapter			(adapter)
,m_mode_index		(mode_index)
,m_last_mode_index	(last_mode_index)
,m_display_mode		(Zero<D3DDISPLAYMODE>())
{
	for( m_mode_index; m_mode_index != m_last_mode_index && !IsValid(); ++m_mode_index ) {}
}
DisplayModeIter& DisplayModeIter::operator ++()
{
	for( ++m_mode_index; m_mode_index != m_last_mode_index && !IsValid(); ++m_mode_index ) {}
	return *this;
}
DisplayModeIter& DisplayModeIter::operator --()
{
	for( --m_mode_index; m_mode_index != (uint)(-1) && !IsValid(); --m_mode_index ) {}
	return *this;
}

//*****
// Return true if the current mode index is valid for this adapter
bool DisplayModeIter::IsValid()
{
	return	Succeeded(m_adapter->m_d3d->EnumAdapterModes(
				m_adapter->m_adapter_index,
				m_adapter->m_format,
				m_mode_index,
				&m_display_mode)) &&
			Succeeded(m_adapter->m_d3d->CheckDeviceType (
				m_adapter->m_adapter_index,
				m_adapter->m_device_type,
				m_adapter->m_format,
				m_adapter->m_format,
				m_adapter->m_windowed));			
}
