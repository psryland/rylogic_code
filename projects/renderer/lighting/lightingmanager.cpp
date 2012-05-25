//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/lighting/lightingmanager.h"
#include "pr/renderer/materials/effects/fragments.h"

pr::rdr::LightingManager::LightingManager(D3DPtr<IDirect3DDevice9> d3d_device)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::LightingManager)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::LightingManager)
,m_d3d_device(d3d_device)
,m_light()
,m_smap()
,m_smap_depth()
{}

// Release the device objects.
void pr::rdr::LightingManager::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	ReleaseSmaps(0);
	m_d3d_device = 0;
}

// Recreate the device objects
void pr::rdr::LightingManager::OnEvent(pr::rdr::Evt_DeviceRestored const& e)
{
	// Don't recreate the smaps, that will be done automatically during a call to render
	m_d3d_device = e.m_d3d_device;
}

// Create the shadow maps for caster index 'idx'
void pr::rdr::LightingManager::CreateSmap(int idx)
{
	// do nothing if it already exists
	if (m_smap[idx] != 0) return;

	const int tex_size = pr::rdr::effect::frag::SMap::TexSize;
	D3DFORMAT smap_format = D3DFMT_A8R8G8B8;//D3DFMT_G16R16F;//
	D3DFORMAT smap_depth_format = D3DFMT_D24S8;

	// Get the d3d interface
	D3DPtr<IDirect3D9> d3d; Verify(m_d3d_device->GetDirect3D(&d3d.m_ptr));

	// Check that the smap format we want to use is supported on this hardware
	D3DDEVICE_CREATION_PARAMETERS cp; Verify(m_d3d_device->GetCreationParameters(&cp));
	D3DDISPLAYMODE                dm; Verify(m_d3d_device->GetDisplayMode(0, &dm));
	if (Failed(d3d->CheckDeviceFormat(cp.AdapterOrdinal, cp.DeviceType, dm.Format, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, smap_format)))
		throw RdrException(EResult::TextureFormatNotSupported, "Smap render target format unsupported on this hardware");

	// Create the smap texture
	Verify(m_d3d_device->CreateTexture(tex_size, tex_size, 1, D3DUSAGE_RENDERTARGET, smap_format, D3DPOOL_DEFAULT, &m_smap[idx].m_ptr, 0));
	
	// Ensure the depth buffer exists
	if (m_smap_depth != 0) return;
	
	// Check the depth buffer and smap texture are compatible
	if (Failed(d3d->CheckDeviceFormat(cp.AdapterOrdinal, cp.DeviceType, dm.Format, D3DUSAGE_DEPTHSTENCIL, D3DRTYPE_SURFACE, smap_depth_format)))
		throw RdrException(EResult::TextureFormatNotSupported, "Smap depth buffer format unsupported on this hardware");
	if (Failed(d3d->CheckDepthStencilMatch(cp.AdapterOrdinal, cp.DeviceType, dm.Format, smap_format, smap_depth_format)))
		throw RdrException(EResult::DepthStencilFormatIncompatibleWithDisplayFormat, "Depth buffer format incompatible with Smap format on this hardware");
	
	// Create the depth buffer
	Verify(m_d3d_device->CreateDepthStencilSurface(tex_size, tex_size, smap_depth_format, D3DMULTISAMPLE_NONE, 0, TRUE, &m_smap_depth.m_ptr, 0));
}

// Release the smaps leaving 'leave_remaining'
void pr::rdr::LightingManager::ReleaseSmaps(int leave_remaining)
{
	for (int i = leave_remaining; i < MaxShadowCasters; ++i)
		m_smap[i] = 0;

	if (leave_remaining == 0)
		m_smap_depth = 0;
}
