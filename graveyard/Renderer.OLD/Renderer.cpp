//**********************************************************************************
//
// A DirectX Renderer
//
//**********************************************************************************
#include "Stdafx.h"
#include "PR/FileSys/FileSys.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Viewport.h"

using namespace pr;
using namespace pr::rdr;

//**********************************************************************************
namespace pr
{
	namespace rdr
	{
		inline bool operator == (const Viewport& lhs, const Viewport& rhs) { return &lhs == &rhs; }

		//*****
		// Convert 'settings' into present parameters based on the capabilities  of the provided adapter and device.
		D3DPRESENT_PARAMETERS CompilePresentParameters(D3DPtr<IDirect3D9> d3d, RdrSettings& settings)
		{
			if( !d3d ) { throw Exception(EResult_CreateD3DInterfaceFailed, "Failed to create a d3d interface"); }

			// Modify things if we're debugging shaders
			PR_DEBUG_SHADERS_ONLY(settings.m_device_config.m_device_type = D3DDEVTYPE_REF;)
			PR_DEBUG_SHADERS_ONLY(settings.m_device_config.m_behavior &= ~D3DCREATE_HARDWARE_VERTEXPROCESSING;)
			PR_DEBUG_SHADERS_ONLY(settings.m_device_config.m_behavior &= ~D3DCREATE_PUREDEVICE;)
			PR_DEBUG_SHADERS_ONLY(settings.m_device_config.m_behavior |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;)

			D3DPRESENT_PARAMETERS pp;
			pp.BackBufferWidth				= settings.m_screen_width;
			pp.BackBufferHeight				= settings.m_screen_height;
			pp.BackBufferFormat				= settings.m_device_config.m_display_mode.Format;
			pp.BackBufferCount				= settings.m_back_buffer_count;
			pp.SwapEffect					= settings.m_swap_effect;
			pp.hDeviceWindow				= settings.m_window_handle;
			pp.Windowed						= (settings.m_device_config.m_windowed) ? (TRUE) : (FALSE);
			pp.EnableAutoDepthStencil		= FALSE;
			pp.AutoDepthStencilFormat		= settings.m_zbuffer_format;
			pp.Flags						= 0;
			pp.FullScreen_RefreshRateInHz	= (settings.m_device_config.m_windowed) ? (0) : (settings.m_device_config.m_display_mode.RefreshRate);
			pp.PresentationInterval			= (settings.m_device_config.m_windowed) ? (D3DPRESENT_INTERVAL_IMMEDIATE) : (D3DPRESENT_INTERVAL_DEFAULT);
			pp.MultiSampleQuality			= 0;

			// Some temporaries to make the following code more readable
			uint					a			= settings.m_device_config.m_adapter_index;
			D3DDEVTYPE				dev_type	= settings.m_device_config.m_device_type;
			D3DCAPS9&				caps		= settings.m_device_config.m_caps;
			D3DTEXTUREFILTERTYPE*	tfilter		= settings.m_texture_filter.m_filter;

			// Check that the device is supported
			if( Failed(d3d->CheckDeviceType(a, dev_type, pp.BackBufferFormat, pp.BackBufferFormat, pp.Windowed)) )
			{	throw Exception(EResult_DeviceNotSupported, "The required device is not supported on this graphics adapter"); }

			// Check that the display format is supported
			if( Failed(d3d->CheckDeviceFormat(a, dev_type, pp.BackBufferFormat, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, pp.BackBufferFormat)) )
			{	throw Exception(EResult_DisplayFormatNotSupported, "The required display format is not supported on this graphics adapter"); }

			// Check the depth stencil format is supported
			if( Failed(d3d->CheckDeviceFormat(a, dev_type, pp.BackBufferFormat, D3DUSAGE_DEPTHSTENCIL, D3DRTYPE_SURFACE, pp.AutoDepthStencilFormat)) )
			{	throw Exception(EResult_DepthStencilFormatNotSupported, "The required depth stencil format is not supported on this graphics adapter"); }

			// Check that the depth stencil format is compatible with the display format
			if( Failed(d3d->CheckDepthStencilMatch(a, dev_type, pp.BackBufferFormat, pp.BackBufferFormat, pp.AutoDepthStencilFormat)) )
			{	throw Exception(EResult_DepthStencilFormatIncompatibleWithDisplayFormat, "The required depth stencil format is not compatible with the required display format on this graphics adapter"); }

			// Antialiasing	
			switch( settings.m_geometry_quality )
			{
			case RdrSettings::High:		pp.MultiSampleType = D3DMULTISAMPLE_16_SAMPLES; if( Succeeded(d3d->CheckDeviceMultiSampleType(a, dev_type, pp.BackBufferFormat, pp.Windowed, pp.MultiSampleType, 0)) ) break;
										pp.MultiSampleType = D3DMULTISAMPLE_9_SAMPLES;  if( Succeeded(d3d->CheckDeviceMultiSampleType(a, dev_type, pp.BackBufferFormat, pp.Windowed, pp.MultiSampleType, 0)) ) break;
			case RdrSettings::Medium:	pp.MultiSampleType = D3DMULTISAMPLE_4_SAMPLES;  if( Succeeded(d3d->CheckDeviceMultiSampleType(a, dev_type, pp.BackBufferFormat, pp.Windowed, pp.MultiSampleType, 0)) ) break;
										pp.MultiSampleType = D3DMULTISAMPLE_2_SAMPLES;  if( Succeeded(d3d->CheckDeviceMultiSampleType(a, dev_type, pp.BackBufferFormat, pp.Windowed, pp.MultiSampleType, 0)) ) break;
			case RdrSettings::Low:		pp.MultiSampleType = D3DMULTISAMPLE_NONE;       if( Succeeded(d3d->CheckDeviceMultiSampleType(a, dev_type, pp.BackBufferFormat, pp.Windowed, pp.MultiSampleType, 0)) ) break;
				throw Exception(EResult_NoMultiSamplingTypeSupported, "No multi sample type (including none) is supported on this graphics adapter");
			}

			// Set the texture filter levels
			switch( settings.m_texture_quality )
			{
			case RdrSettings::High:		if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFGAUSSIANQUAD )		{ tfilter[TextureFilter::Mag] = D3DTEXF_GAUSSIANQUAD; break; }
										if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFPYRAMIDALQUAD )	{ tfilter[TextureFilter::Mag] = D3DTEXF_PYRAMIDALQUAD; break; }
										if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFANISOTROPIC )		{ tfilter[TextureFilter::Mag] = D3DTEXF_ANISOTROPIC; break; }
			case RdrSettings::Medium:	if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFLINEAR )			{ tfilter[TextureFilter::Mag] = D3DTEXF_LINEAR; break; }
			case RdrSettings::Low:																			{ tfilter[TextureFilter::Mag] = D3DTEXF_POINT; break; }
			}
			switch( settings.m_texture_quality )
			{
			case RdrSettings::High:
			case RdrSettings::Medium: if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MIPFLINEAR )				{ tfilter[TextureFilter::Mip] = D3DTEXF_LINEAR; break; }
			case RdrSettings::Low:																			{ tfilter[TextureFilter::Mip] = D3DTEXF_POINT; break; }
			}
			switch( settings.m_texture_quality )
			{
			case RdrSettings::High:		if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFGAUSSIANQUAD )		{ tfilter[TextureFilter::Min] = D3DTEXF_GAUSSIANQUAD; break; }
										if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFPYRAMIDALQUAD )	{ tfilter[TextureFilter::Min] = D3DTEXF_PYRAMIDALQUAD; break; }
										if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFANISOTROPIC )		{ tfilter[TextureFilter::Min] = D3DTEXF_ANISOTROPIC; break; }
			case RdrSettings::Medium:	if( caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFLINEAR )			{ tfilter[TextureFilter::Min] = D3DTEXF_LINEAR; break; }
			case RdrSettings::Low:																			{ tfilter[TextureFilter::Min] = D3DTEXF_POINT; break; }
			}
			return pp;
		}

		//*****
		// Create the d3d device
		D3DPtr<IDirect3DDevice9> CreateD3DDevice(D3DPtr<IDirect3D9> d3d, const DeviceConfig& config, D3DPRESENT_PARAMETERS& pp)
		{
			D3DPtr<IDirect3DDevice9> d3d_device;
			if( Failed(d3d->CreateDevice(config.m_adapter_index, config.m_device_type, pp.hDeviceWindow, config.m_behavior, &pp, &d3d_device.m_ptr)) )
			{	throw Exception(EResult_CreateD3DDeviceFailed, "Failed to create a d3d device"); }
			return d3d_device;
		}

		//*****
		// Get the back buffer
		D3DPtr<IDirect3DSurface9> GetBackBuffer(D3DPtr<IDirect3DDevice9> d3d_device)
		{
			D3DPtr<IDirect3DSurface9> back_buffer;
			Verify(d3d_device->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &back_buffer.m_ptr));
			return back_buffer;
		}

		//*****
		// Create a depth stencil surface
		D3DPtr<IDirect3DSurface9> CreateDepthBuffer(D3DPtr<IDirect3DDevice9> d3d_device, const D3DPRESENT_PARAMETERS& pp)
		{
			D3DPtr<IDirect3DSurface9> depth_buffer;
			if( Failed(d3d_device->CreateDepthStencilSurface(pp.BackBufferWidth, pp.BackBufferHeight, pp.AutoDepthStencilFormat, pp.MultiSampleType, pp.MultiSampleQuality, TRUE, &depth_buffer.m_ptr, 0)) )
			{	throw Exception(EResult_CreateDepthStencilFailed, "Failed to create a depth stencil surface on this graphics adapter"); }
			
			if( Failed(d3d_device->SetDepthStencilSurface(depth_buffer.m_ptr)) )
			{	throw Exception(EResult_SetDepthStencilFailed, "Failed to assign the depth stencil surface to the d3d device"); }

			return depth_buffer;
		}
	}//namespace rdr
}//namespace pr

//**********************************************************************************

//*****
// Constructor
Renderer::Renderer(const rdr::RdrSettings& settings)
:m_settings				(settings)
,m_d3d					(Direct3DCreate9(D3D_SDK_VERSION))
,m_pp					(CompilePresentParameters(m_d3d, m_settings))
,m_d3d_device			(CreateD3DDevice(m_d3d, m_settings.m_device_config, m_pp))
,m_back_buffer			(GetBackBuffer(m_d3d_device))
,m_depth_buffer			(CreateDepthBuffer(m_d3d_device, m_pp))
,m_viewport				()
,m_clear_flags			(D3DCLEAR_TARGET | D3DCLEAR_ZBUFFER | D3DCLEAR_STENCIL)
,m_renderer_state		(EState_Idle)
,m_device_lost			(false)
,m_global_render_states	()
,m_vertex_manager		(m_d3d_device)
,m_render_state_manager	(m_d3d_device, &m_vertex_manager, m_settings.m_client_area)
,m_lighting_manager		()
,m_material_manager		(m_d3d_device, m_settings.m_shader_paths)
{
	// When moving from fullscreen to windowed mode, it is important to adjust the window size after recreating
	// the device rather than beforehand to ensure that you get the window size you want. For example, when
	// switching from 640x480 fullscreen to windowed with a 1000x600 window on a 1024x768 desktop, it is impossible
	// to set the window size to 1000x600 until after the display mode has changed to 1024x768, because windows
	// cannot be larger than the desktop.
    if( m_pp.Windowed )
    {
        SetWindowPos(m_pp.hDeviceWindow, HWND_NOTOPMOST, m_settings.m_window_bounds.m_left, m_settings.m_window_bounds.m_top,
			m_settings.m_window_bounds.Width(), m_settings.m_window_bounds.Height(), SWP_SHOWWINDOW);
    }

	// Set the viewport to the area of the back buffer
	D3DVIEWPORT9 viewport = {0, 0, m_pp.BackBufferWidth, m_pp.BackBufferHeight, 0.0f, 1.0f};
	Verify(m_d3d_device->SetViewport(&viewport));

	// Clear the backbuffer
	m_d3d_device->Clear(0L, 0, m_clear_flags, d3dc(m_settings.m_background_colour), 1.0f, 0L);
	m_d3d_device->Present(0, 0, 0, 0);

	// Set the texture sampling filters based on the texture quality in settings
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MAGFILTER, m_settings.m_texture_filter.m_filter[TextureFilter::Mag]));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MIPFILTER, m_settings.m_texture_filter.m_filter[TextureFilter::Mip]));
	Verify(m_d3d_device->SetSamplerState(0, D3DSAMP_MINFILTER, m_settings.m_texture_filter.m_filter[TextureFilter::Min]));
}

//*****
// Resize the display that we are rendering to. Returns false if resizing failed
// Note: If you change this, consider the ResetDevice method
// Clients have to recreate anything that is not pool managed and depends on m_d3d_device.
void Renderer::Resize(const IRect& client_area, const IRect& window_bounds)
{
	PR_ASSERT_STR(PR_DBG_RDR, client_area.m_right > client_area.m_left, "Width resized to zero or less");
	PR_ASSERT_STR(PR_DBG_RDR, client_area.m_bottom > client_area.m_top, "Height resized to zero or less");

	// Release everything that depends on the device
	ReleaseDeviceDependentObjects();

	// Set the new size
	m_pp.BackBufferWidth		= client_area.Width();
	m_pp.BackBufferHeight		= client_area.Height();
	m_settings.m_client_area	= client_area;
	m_settings.m_window_bounds	= window_bounds;
	m_settings.m_device_config.m_display_mode.Width = client_area.Width();
	m_settings.m_device_config.m_display_mode.Height = client_area.Height();
	
	// Recreate the device
	m_d3d_device = CreateD3DDevice(m_d3d, m_settings.m_device_config, m_pp);

	// Re-Create the device and the device dependent objects
	CreateDeviceDependentObjects();

	// When moving from fullscreen to windowed mode, it is important to adjust the window size after recreating
	// the device rather than beforehand to ensure that you get the window size you want. For example, when
	// switching from 640x480 fullscreen to windowed with a 1000x600 window on a 1024x768 desktop, it is impossible
	// to set the window size to 1000x600 until after the display mode has changed to 1024x768, because windows
	// cannot be larger than the desktop.
    if( m_pp.Windowed )
    {
        SetWindowPos(m_pp.hDeviceWindow, HWND_NOTOPMOST, m_settings.m_window_bounds.m_left, m_settings.m_window_bounds.m_top,
			m_settings.m_window_bounds.Width(), m_settings.m_window_bounds.Height(), SWP_SHOWWINDOW);
    }

	// Set the viewport to the area of the back buffer
	D3DVIEWPORT9 viewport = {0, 0, m_pp.BackBufferWidth, m_pp.BackBufferHeight, 0.0f, 1.0f};
	Verify(m_d3d_device->SetViewport(&viewport));

	// Tell the render state manager that we've resized
	m_render_state_manager.Resize(client_area);
}

//*****
// Re-Create device dependent objects
void Renderer::CreateDeviceDependentObjects()
{
	// Recreate the back buffer and depth buffer
	m_back_buffer  = GetBackBuffer(m_d3d_device);
	m_depth_buffer = CreateDepthBuffer(m_d3d_device, m_pp);

	m_vertex_manager      .CreateDeviceDependentObjects(m_d3d_device);
	m_render_state_manager.CreateDeviceDependentObjects(m_d3d_device);
	m_material_manager    .CreateDeviceDependentObjects(m_d3d_device);
	for( TViewportChain::iterator vp = m_viewport.begin(), vp_end = m_viewport.end(); vp != vp_end; ++vp ) 
	{
		vp->CreateDeviceDependentObjects();
	}
}

//*****
// Release everything that depends on the device
void Renderer::ReleaseDeviceDependentObjects()
{
	for( TViewportChain::iterator vp = m_viewport.begin(), vp_end = m_viewport.end(); vp != vp_end; ++vp ) 
	{
		vp->ReleaseDeviceDependentObjects();
	}
	m_material_manager    .ReleaseDeviceDependentObjects();
	m_render_state_manager.ReleaseDeviceDependentObjects();
	m_vertex_manager      .ReleaseDeviceDependentObjects();

	// Release the back and depth buffers
	m_back_buffer  = 0;
	m_depth_buffer = 0;
	PR_EXPAND  (PR_DBG_RDR, ULONG count = m_d3d_device.m_ptr->AddRef() - 1; m_d3d_device.m_ptr->Release();)
	PR_WARN_EXP(PR_DBG_RDR, count == 1, Fmt("%d references to the d3d device still exist", count - 1).c_str());
	m_d3d_device   = 0;
}

//*****
// Prepare for a frame. Returns EResult_Success if it is ok to continue building the scene,
// EResult_DeviceLost if the device was lost and the scene should not be build.
EResult Renderer::RenderStart()
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer_state == EState_Idle, "Incorrect render call sequence");
	EResult hr;
	
	// Test whether we are allowed to draw now. 
	if( Failed(hr = TestCooperativeLevel()) ) return hr;

	// Add the renderer's render states to the render state manager
	m_render_state_manager.PushRenderStateBlock(m_global_render_states);

	// Begin the scene
	if( Failed(m_d3d_device->BeginScene()) ) return EResult_Failed;

	m_renderer_state = EState_BuildingScene;
	ClearBackBuffer();
	return hr;
}

//*****
// Get the render to draw a viewport
void Renderer::RenderViewport(Viewport& viewport)
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer_state == EState_BuildingScene, "Incorrect render call sequence");
	PR_ASSERT_STR(PR_DBG_RDR, std::find(m_viewport.begin(), m_viewport.end(), viewport) != m_viewport.end(), "Viewport not registered");
	viewport.Render();
}

//*****
// Present the scene
void Renderer::RenderEnd()
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer_state == EState_BuildingScene, "Incorrect render call sequence");
	m_d3d_device->EndScene();

	m_renderer_state = EState_PresentPending;
}

//*****
// Send the scene to the display
EResult	Renderer::Render()
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer_state == EState_PresentPending, "Incorrect render call sequence");
	
	// Present the scene
	HRESULT hr = m_d3d_device->Present(0, 0, 0, 0);

	// Pop the renderer's render states from the render state manager
	m_render_state_manager.PopRenderStateBlock(m_global_render_states);

	m_renderer_state = EState_Idle;

	if( Failed(hr) )
	{
        m_device_lost = hr == D3DERR_DEVICELOST;
		if( m_device_lost ) return EResult_DeviceLost;
		return EResult_Failed;
	}
	return EResult_Success;
}

//*****
// Blt the back buffer to the primary surface again without re-rendering the scene
EResult Renderer::BltBackBuffer()
{
	PR_ASSERT_STR(PR_DBG_RDR, m_renderer_state == EState_Idle, "Incorrect render call sequence");
	PR_ASSERT_STR(PR_DBG_RDR, m_settings.m_swap_effect == D3DSWAPEFFECT_COPY, "This only works if the swap effect is copy");

	HRESULT hr = m_d3d_device->Present(0, 0, 0, 0);

	if( Failed(hr) )
	{
        m_device_lost = hr == D3DERR_DEVICELOST;
		if( m_device_lost ) return EResult_DeviceLost;
		return EResult_Failed;
	}
	return EResult_Success;
}

//*****
// Called by a viewport to clear the backbuffer after the viewport has been set
void Renderer::ClearBackBuffer()
{
	Verify(m_d3d_device->Clear(0L, 0, m_clear_flags, d3dc(m_settings.m_background_colour), 1.0f, 0L));
}

//*****
// Register a viewport
void Renderer::RegisterViewport(Viewport& viewport)
{
	PR_ASSERT_STR(PR_DBG_RDR, std::find(m_viewport.begin(), m_viewport.end(), viewport) == m_viewport.end(), "Viewport already registered");
	m_viewport.push_back(viewport);
}

//*****
// Remove a viewport
void Renderer::UnregisterViewport(Viewport& viewport)
{
	PR_ASSERT_STR(PR_DBG_RDR, std::find(m_viewport.begin(), m_viewport.end(), viewport) != m_viewport.end(), "Viewport not registered");
	m_viewport.erase(viewport);
}

//*****
// Test for device lost and reacquire the device if so.
EResult Renderer::TestCooperativeLevel()
{
    HRESULT hr;

	// Test the cooperative level to see if it's okay to render
	if( Succeeded(hr = m_d3d_device->TestCooperativeLevel()) ) { return EResult_Success; }

    // If the device was lost, do not render until we get it back
    if( hr == D3DERR_DEVICELOST )
	{
		return EResult_DeviceLost;
	}
    // Check if the device needs to be restored
    else if( hr == D3DERR_DEVICENOTRESET )
    {
        // If we are windowed, read the desktop mode and use the same format for the back buffer
        if( m_pp.Windowed )
        {
			m_d3d->GetAdapterDisplayMode(m_settings.m_device_config.m_adapter_index, &m_settings.m_device_config.m_display_mode);
            m_pp.BackBufferFormat = m_settings.m_device_config.m_display_mode.Format;
        }
		return ResetDevice();
	}
	return EResult_Failed;
}

//*****
// Recover from a lost device
EResult Renderer::ResetDevice()
{
	// Release everything that depends on the device. (only once)
	if( m_d3d_device ) ReleaseDeviceDependentObjects();

	// Reset the device
	// NOTE: Reset will fail unless the application releases all resources that are allocated in
	// D3DPOOL_DEFAULT, including those created by the IDirect3DDevice9::CreateRenderTarget and
	// IDirect3DDevice9::CreateDepthStencilSurface methods
    HRESULT hr = m_d3d_device->Reset(&m_pp);
	if( hr == D3DERR_DEVICELOST ) { return EResult_DeviceLost; }
	if( Failed(hr) ) { return EResult_Failed; } // Some other error occurred

	CreateDeviceDependentObjects();
	return EResult_Success;
}
