//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/Renderer/renderer.h"
#include "pr/renderer/Viewport/Viewport.h"

using namespace pr;
using namespace pr::rdr;

namespace pr
{
	namespace rdr
	{
		// Forward initial settings while creating the renderer
		RdrSettings const& CopySettings(RdrSettings const& settings)
		{
			return settings;
		}
		
		// Convert 'settings' into present parameters based on the capabilities  of the provided adapter and device.
		D3DPRESENT_PARAMETERS CompilePresentParameters(D3DPtr<IDirect3D9> d3d, RdrSettings& settings)
		{
			if (!d3d) throw RdrException(EResult::CreateInterfaceFailed, "Failed to create a d3d interface");
			
			// Modify things if we're debugging shaders
			//PR_EXPAND(PR_DBG_RDR_SHADERS, settings.m_device_config.m_device_type = D3DDEVTYPE_REF);
			//PR_EXPAND(PR_DBG_RDR_SHADERS, settings.m_device_config.m_behavior &= ~D3DCREATE_HARDWARE_VERTEXPROCESSING);
			//PR_EXPAND(PR_DBG_RDR_SHADERS, settings.m_device_config.m_behavior &= ~D3DCREATE_PUREDEVICE);
			//PR_EXPAND(PR_DBG_RDR_SHADERS, settings.m_device_config.m_behavior |= D3DCREATE_SOFTWARE_VERTEXPROCESSING);
			
			if (!settings.m_device_config.m_windowed)
				settings.m_client_area.set(0, 0, settings.m_device_config.m_display_mode.Width, settings.m_device_config.m_display_mode.Height);
				
			D3DPRESENT_PARAMETERS pp;
			pp.BackBufferWidth            = settings.m_client_area.SizeX();
			pp.BackBufferHeight           = settings.m_client_area.SizeY();
			pp.BackBufferFormat           = settings.m_device_config.m_display_mode.Format;
			pp.BackBufferCount            = settings.m_back_buffer_count;
			pp.SwapEffect                 = settings.m_swap_effect;
			pp.hDeviceWindow              = settings.m_window_handle;
			pp.Windowed                   = (settings.m_device_config.m_windowed) ? (TRUE) : (FALSE);
			pp.EnableAutoDepthStencil     = FALSE;
			pp.AutoDepthStencilFormat     = settings.m_zbuffer_format;
			pp.Flags                      = settings.m_present_flags;
			pp.FullScreen_RefreshRateInHz = (settings.m_device_config.m_windowed) ? (0) : (settings.m_device_config.m_display_mode.RefreshRate);
			pp.PresentationInterval       = (settings.m_device_config.m_windowed) ? (D3DPRESENT_INTERVAL_IMMEDIATE) : (D3DPRESENT_INTERVAL_DEFAULT);
			pp.MultiSampleQuality         = 0;
			
			// Some temporaries to make the following code more readable
			uint       a        = settings.m_device_config.m_adapter_index;
			D3DDEVTYPE dev_type = settings.m_device_config.m_device_type;
			D3DCAPS9&  caps     = settings.m_device_config.m_caps;
			
			// Check that the device is supported
			if (Failed(d3d->CheckDeviceType(a, dev_type, pp.BackBufferFormat, pp.BackBufferFormat, pp.Windowed)))
				throw RdrException(EResult::DeviceNotSupported, "The required device is not supported on this graphics adapter");
				
			// Check that the display format is supported
			if (Failed(d3d->CheckDeviceFormat(a, dev_type, pp.BackBufferFormat, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, pp.BackBufferFormat)))
				throw RdrException(EResult::DisplayFormatNotSupported, "The required display format is not supported on this graphics adapter");
				
			// Check the depth stencil format is supported
			if (Failed(d3d->CheckDeviceFormat(a, dev_type, pp.BackBufferFormat, D3DUSAGE_DEPTHSTENCIL, D3DRTYPE_SURFACE, pp.AutoDepthStencilFormat)))
				throw RdrException(EResult::DepthStencilFormatNotSupported, "The required depth stencil format is not supported on this graphics adapter");
				
			// Check that the depth stencil format is compatible with the display format
			if (Failed(d3d->CheckDepthStencilMatch(a, dev_type, pp.BackBufferFormat, pp.BackBufferFormat, pp.AutoDepthStencilFormat)))
				throw RdrException(EResult::DepthStencilFormatIncompatibleWithDisplayFormat, "The required depth stencil format is not compatible with the required display format on this graphics adapter");
				
			// Antialiasing
			pp.MultiSampleType = GetAntiAliasingLevel(d3d, settings.m_device_config, pp.BackBufferFormat, settings.m_geometry_quality);
			
			// Set the texture filter levels
			SetTextureFilter(settings.m_texture_filter, caps, settings.m_texture_quality);
			return pp;
		}
		
		// Create the d3d device
		D3DPtr<IDirect3DDevice9> CreateD3DDevice(D3DPtr<IDirect3D9> d3d, DeviceConfig const& config, D3DPRESENT_PARAMETERS& pp)
		{
			D3DPtr<IDirect3DDevice9> d3d_device;
			if (Failed(d3d->CreateDevice(config.m_adapter_index, config.m_device_type, pp.hDeviceWindow, config.m_behavior, &pp, &d3d_device.m_ptr)))
				throw RdrException(EResult::CreateD3DDeviceFailed, "Failed to create a d3d device");
			return d3d_device;
		}
		
		// Get the back buffer
		D3DPtr<IDirect3DSurface9> GetBackBuffer(D3DPtr<IDirect3DDevice9> d3d_device)
		{
			D3DPtr<IDirect3DSurface9> back_buffer;
			Verify(d3d_device->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &back_buffer.m_ptr));
			return back_buffer;
		}
		
		// Create a depth stencil surface
		D3DPtr<IDirect3DSurface9> CreateDepthBuffer(D3DPtr<IDirect3DDevice9> d3d_device, D3DPRESENT_PARAMETERS const& pp)
		{
			D3DPtr<IDirect3DSurface9> depth_buffer;
			if (Failed(d3d_device->CreateDepthStencilSurface(pp.BackBufferWidth, pp.BackBufferHeight, pp.AutoDepthStencilFormat, pp.MultiSampleType, pp.MultiSampleQuality, TRUE, &depth_buffer.m_ptr, 0)))
				throw RdrException(EResult::CreateDepthStencilFailed, "Failed to create a depth stencil surface on this graphics adapter");
				
			if (Failed(d3d_device->SetDepthStencilSurface(depth_buffer.m_ptr)))
				throw RdrException(EResult::SetDepthStencilFailed, "Failed to assign the depth stencil surface to the d3d device");
				
			return depth_buffer;
		}
	}
}

// Constructor
Renderer::Renderer(rdr::RdrSettings const& settings)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::Renderer)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::Renderer)
,m_settings(CopySettings(settings))
,m_d3d(Direct3DCreate9(D3D_SDK_VERSION))
,m_pp(CompilePresentParameters(m_d3d, m_settings))
,m_d3d_device(CreateD3DDevice(m_d3d, m_settings.m_device_config, m_pp))
,m_back_buffer(GetBackBuffer(m_d3d_device))
,m_depth_buffer(CreateDepthBuffer(m_d3d_device, m_pp))
,m_viewport()
,m_global_render_states()
,m_global_rsb_sf()
,m_rendering_phase(EState::Idle)
,m_device_lost(false)
,m_vert_mgr(m_d3d_device)
,m_rdrstate_mgr(m_d3d_device, m_vert_mgr, m_settings.m_client_area)
,m_light_mgr(m_d3d_device)
,m_mat_mgr(*settings.m_allocator, m_d3d_device, settings.m_texture_filter)
,m_mdl_mgr(*settings.m_allocator, m_d3d_device)
{
	// Check that required dlls to run the renderer are available
	CheckDependencies();
	
	// When moving from fullscreen to windowed mode, it is important to adjust the window size after recreating
	// the device rather than beforehand to ensure that you get the window size you want. For example, when
	// switching from 640x480 fullscreen to windowed with a 1000x600 window on a 1024x768 desktop, it is impossible
	// to set the window size to 1000x600 until after the display mode has changed to 1024x768, because windows
	// cannot be larger than the desktop.
	//if (m_pp.Windowed)
	//  SetWindowPos(m_pp.hDeviceWindow, HWND_NOTOPMOST, m_settings.m_window_bounds.m_min.x, m_settings.m_window_bounds.m_min.y, m_settings.m_window_bounds.SizeX(), m_settings.m_window_bounds.SizeY(), SWP_SHOWWINDOW);
	
	// Set the viewport to the area of the back buffer
	D3DVIEWPORT9 viewport = {0, 0, m_pp.BackBufferWidth, m_pp.BackBufferHeight, 0.0f, 1.0f};
	Verify(m_d3d_device->SetViewport(&viewport));
	
	// Clear the backbuffer
	Verify(m_d3d_device->Clear(0L, 0, D3DCLEAR_TARGET|D3DCLEAR_ZBUFFER|D3DCLEAR_STENCIL, d3dc(m_settings.m_background_colour), 1.0f, 0L));
	Verify(m_d3d_device->Present(0, 0, 0, 0));
}

// Resize the display that we are rendering to.
void Renderer::Resize(IRect const& client_area)
{
	if (m_settings.m_client_area == client_area)
		return;
	if (client_area.SizeX() <= 0 || client_area.SizeY() <= 0)
		return;
		
	// Set the new size
	m_settings.m_client_area = client_area;
	if (m_pp.Windowed)
	{
		m_pp.BackBufferWidth  = client_area.SizeX();
		m_pp.BackBufferHeight = client_area.SizeY();
	}
	if (Failed(ResetDevice()))
		throw RdrException(EResult::ResetDeviceFailed, "Resetting the device failed");
}

// Release everything that depends on the device
void Renderer::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	// Release the back and depth buffers
	m_depth_buffer = 0;
	m_back_buffer  = 0;
}

// Re-Create device dependent objects
void Renderer::OnEvent(pr::rdr::Evt_DeviceRestored const&)
{
	// Recreate the back buffer and depth buffer
	m_back_buffer  = GetBackBuffer(m_d3d_device);
	m_depth_buffer = CreateDepthBuffer(m_d3d_device, m_pp);
}

// Prepare for a frame. Returns EResult::Success if it is ok to continue building the scene,
// EResult::DeviceLost if the device was lost and the scene should not be build.
EResult::Type Renderer::RenderStart()
{
	if (m_rendering_phase != EState::Idle)
	{
		PR_INFO(PR_DBG_RDR, "Incorrect render call sequence");
		return EResult::Failed;
	}
	
	// Test whether we're allowed to draw
	EResult::Type hr = TestCooperativeLevel();
	if (Failed(hr)) return hr;
	
	// Begin the scene
	if (Failed(m_d3d_device->BeginScene())) return EResult::Failed;
	
	// Add the renderer's render states to the render state manager
	pr::imposter::construct(m_global_rsb_sf, m_rdrstate_mgr, m_global_render_states);
	
	m_rendering_phase = EState::BuildingScene;
	return EResult::Success;
}

// Present the scene
void Renderer::RenderEnd()
{
	PR_ASSERT(PR_DBG_RDR, m_rendering_phase == EState::BuildingScene, "Incorrect render call sequence");
	m_d3d_device->EndScene();
	m_rendering_phase = EState::PresentPending;
}

// Send the scene to the display
EResult::Type Renderer::Present()
{
	PR_ASSERT(PR_DBG_RDR, m_rendering_phase == EState::PresentPending, "Incorrect render call sequence");
	
	// Present the scene
	HRESULT hr = m_d3d_device->Present(0, 0, 0, 0);
	
	// Restore the states
	pr::imposter::destruct(m_global_rsb_sf);
	
	m_rendering_phase = EState::Idle;
	
	if (Failed(hr))
	{
		m_device_lost = hr == D3DERR_DEVICELOST;
		if (m_device_lost) return EResult::DeviceLost;
		return EResult::Failed;
	}
	return EResult::Success;
}

// Blt the back buffer to the primary surface again without re-rendering the scene
EResult::Type Renderer::BltBackBuffer()
{
	PR_ASSERT(PR_DBG_RDR, m_rendering_phase == EState::Idle, "Incorrect render call sequence");
	PR_ASSERT(PR_DBG_RDR, m_settings.m_swap_effect == D3DSWAPEFFECT_COPY, "This only works if the swap effect is copy");
	
	HRESULT hr = m_d3d_device->Present(0, 0, 0, 0);
	
	if (Failed(hr))
	{
		m_device_lost = hr == D3DERR_DEVICELOST;
		if (m_device_lost) return EResult::DeviceLost;
		return EResult::Failed;
	}
	return EResult::Success;
}

// Called by a viewport to clear the backbuffer after the viewport has been set
void Renderer::ClearBackBuffer()
{
	Verify(m_d3d_device->Clear(0L, 0, D3DCLEAR_TARGET|D3DCLEAR_ZBUFFER|D3DCLEAR_STENCIL, d3dc(m_settings.m_background_colour), 1.0f, 0L));
}

// Register a viewport
void Renderer::RegisterViewport(Viewport& viewport)
{
	#if PR_DBG_RDR
	for (TViewportChain::const_iterator v = m_viewport.begin(), v_end = m_viewport.end(); v != v_end; ++v)
	{
		PR_ASSERT(PR_DBG_RDR, &*v != &viewport, "Viewport already registered");
		PR_ASSERT(PR_DBG_RDR, v->GetViewportId() != viewport.GetViewportId(), "Viewport Identifier is not unique");
	}
	#endif
	
	m_viewport.push_back(viewport);
}

// Remove a viewport
void Renderer::UnregisterViewport(Viewport& viewport)
{
	#if PR_DBG_RDR
	TViewportChain::const_iterator v     = m_viewport.begin();
	TViewportChain::const_iterator v_end = m_viewport.end();
	for (; v != v_end; ++v) { if (&*v == &viewport) break; }
	PR_ASSERT(PR_DBG_RDR, v != v_end, "Viewport not registered");
	#endif
	
	m_viewport.erase(viewport);
}

// Test whether we are allowed to draw now (i.e. not device lost). If this method returns
// "EResult::DeviceLost" you need to release all models and model buffers not created using
// PoolManaged. This should only be resources within the renderer.
EResult::Type Renderer::TestCooperativeLevel()
{
	HRESULT hr;
	
	// Test the cooperative level to see if it's okay to render
	if (Succeeded(hr = m_d3d_device->TestCooperativeLevel())) { return EResult::Success; }
	
	// If the device was lost, do not render until we get it back
	if (hr == D3DERR_DEVICELOST)
	{
		PR_INFO(PR_DBG_RDR, "Device lost\n");
		return EResult::DeviceLost;
	}
	// Check if the device needs to be restored
	else if (hr == D3DERR_DEVICENOTRESET)
	{
		// If we are windowed, read the desktop mode and use the same format for the back buffer
		if (m_pp.Windowed)
		{
			m_d3d->GetAdapterDisplayMode(m_settings.m_device_config.m_adapter_index, &m_settings.m_device_config.m_display_mode);
			m_pp.BackBufferFormat = m_settings.m_device_config.m_display_mode.Format;
		}
		return ResetDevice();
	}
	return EResult::Failed;
}

// Recover from a lost device
EResult::Type Renderer::ResetDevice()
{
	// Notify that the device has been lost.
	// Release everything that depends on the device.
	pr::events::Send(pr::rdr::Evt_DeviceLost(), false);

	PR_ASSERT(PR_DBG_RDR, m_d3d_device, "The device should not have been released");
	m_pp.Flags = m_settings.m_present_flags; // DirectX changes this for some reason!?
	
	// Reset the device
	// NOTE: Reset will fail unless the application releases all resources that are allocated in
	// D3DPOOL_DEFAULT, including those created by the IDirect3DDevice9::CreateRenderTarget and
	// IDirect3DDevice9::CreateDepthStencilSurface methods
	HRESULT hr = m_d3d_device->Reset(&m_pp);
	if (hr == D3DERR_DEVICELOST) return EResult::DeviceLost;
	if (Failed(hr))              return EResult::ResetDeviceFailed; // Some other error occurred
	
	// Notify that the device has been restored
	pr::events::Send(pr::rdr::Evt_DeviceRestored(m_d3d_device, ClientArea()));
	return EResult::Success;
}
