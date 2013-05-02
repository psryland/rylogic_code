//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/textures/texture2d.h"

using namespace pr::rdr;

// Useful reading:
//   http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx
//

// Initialise the renderer state variables and creates the d3d device and swap chain.
pr::rdr::RdrState::RdrState(pr::rdr::RdrSettings const& settings)
:m_settings(settings)
,m_device()
,m_swap_chain()
,m_immediate()
,m_main_rtv()
,m_main_dsv()
,m_feature_level()
,m_bbdesc()
,m_idle(false)
{
	// Check dlls,dx features,etc required to run the renderer are available
	//CheckDependencies();
	
	// Create the d3d device and swap chain
	// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
	//  render using GDI on a swap chain or a surface. This will allow the application
	//  to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
	DXGI_SWAP_CHAIN_DESC sd = {0};
	sd.BufferCount  = settings.m_buffer_count;
	sd.BufferDesc   = settings.m_mode;
	sd.SampleDesc   = settings.m_multisamp;
	sd.BufferUsage  = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	sd.OutputWindow = settings.m_hwnd;
	sd.Windowed     = settings.m_windowed;
	sd.SwapEffect   = settings.m_swap_effect;
	sd.Flags        = settings.m_swap_chain_flags;
	pr::Throw(D3D11CreateDeviceAndSwapChain(
		settings.m_adapter.m_ptr,
		settings.m_driver_type,
		0,
		settings.m_device_layers,
		settings.m_feature_levels.empty() ? 0 : &settings.m_feature_levels[0],
		static_cast<UINT>(settings.m_feature_levels.size()),
		D3D11_SDK_VERSION,
		&sd,
		&m_swap_chain.m_ptr,
		&m_device.m_ptr,
		&m_feature_level,
		&m_immediate.m_ptr
		));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(m_device, pr::FmtS("d3d device")));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(m_swap_chain, pr::FmtS("swap chain")));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(m_immediate, pr::FmtS("immed dc")));

	// Make DXGI monitor for Alt-Enter and switch between windowed and full screen
	D3DPtr<IDXGIFactory> factory;
	pr::Throw(CreateDXGIFactory(__uuidof(IDXGIFactory) ,(void**)&factory.m_ptr));
	pr::Throw(factory->MakeWindowAssociation(m_settings.m_hwnd, 0));
	
	// Setup the main render target
	InitMainRT();
}

// Change the renderer to use a specific render method
void pr::rdr::RdrState::InitMainRT()
{
	// Get the back buffer so we can copy its properties
	D3DPtr<ID3D11Texture2D> back_buffer;
	pr::Throw(m_swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&back_buffer.m_ptr));
	back_buffer->GetDesc(&m_bbdesc);

	// Create a render-target view of the back buffer
	pr::Throw(m_device->CreateRenderTargetView(back_buffer.m_ptr, 0, &m_main_rtv.m_ptr));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(m_main_rtv, pr::FmtS("main RT <%dx%d>", m_bbdesc.Width, m_bbdesc.Height)));

	// Create a texture buffer that we will use as the depth buffer
	pr::rdr::TextureDesc desc;
	desc.Width              = m_bbdesc.Width;
	desc.Height             = m_bbdesc.Height;
	desc.MipLevels          = 1;
	desc.ArraySize          = 1;
	desc.Format             = DXGI_FORMAT_D24_UNORM_S8_UINT;
	desc.SampleDesc         = MultiSamp(1,0);
	desc.Usage              = D3D11_USAGE_DEFAULT;
	desc.BindFlags          = D3D11_BIND_DEPTH_STENCIL;
	desc.CPUAccessFlags     = 0;
	desc.MiscFlags          = 0;
	D3DPtr<ID3D11Texture2D> depth_stencil;
	pr::Throw(m_device->CreateTexture2D(&desc, 0, &depth_stencil.m_ptr));

	// Create a depth/stencil view of the texture buffer we just created
	D3D11_DEPTH_STENCIL_VIEW_DESC dsv_desc = {};
	dsv_desc.Format             = desc.Format;
	dsv_desc.ViewDimension      = D3D11_DSV_DIMENSION_TEXTURE2D;
	dsv_desc.Texture2D.MipSlice = 0;
	pr::Throw(m_device->CreateDepthStencilView(depth_stencil.m_ptr, &dsv_desc, &m_main_dsv.m_ptr));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(m_main_dsv, pr::FmtS("depth buffer <%dx%d>", desc.Width, desc.Height)));

	// Bind the render target and depth stencil to the immediate device context
	m_immediate->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);
	
	// Set the default viewport to all of the render target
	Viewport vp(m_bbdesc.Width, m_bbdesc.Height);
	m_immediate->RSSetViewports(1, &vp);
}

// Construct the renderer
pr::Renderer::Renderer(pr::rdr::RdrSettings const& settings)
:RdrState(settings)
,m_mdl_mgr(m_settings.m_mem, m_device)
,m_shdr_mgr(m_settings.m_mem, m_device)
,m_tex_mgr(m_settings.m_mem, m_device)
,m_bs_mgr(m_settings.m_mem, m_device)
,m_ds_mgr(m_settings.m_mem, m_device)
,m_rs_mgr(m_settings.m_mem, m_device)
{}
pr::Renderer::~Renderer()
{
	PR_EXPAND(PR_DBG_RDR, int rcnt);
	PR_ASSERT(PR_DBG_RDR, (rcnt = m_immediate.RefCount()) == 1, "Outstanding references to the immediate device context");
	m_immediate->OMSetRenderTargets(0, 0, 0);
	m_immediate = 0;
	m_main_rtv = 0;
	m_main_dsv = 0;
	
	// Destroying a Swap Chain:
	// You may not release a swap chain in full-screen mode because doing so may create thread contention
	// (which will cause DXGI to raise a non-continuable exception). Before releasing a swap chain, first
	// switch to windowed mode (using IDXGISwapChain::SetFullscreenState( FALSE, NULL )) and then call IUnknown::Release.
	PR_ASSERT(PR_DBG_RDR, (rcnt = m_swap_chain.RefCount()) == 1, "Outstanding references to the swap chain");
	m_swap_chain->SetFullscreenState(FALSE, 0);
	m_swap_chain = 0;
	
	// Can't assert this as the managers still contain references to the device (and possibly the client)
	//PR_ASSERT(PR_DBG_RDR, (rcnt = m_device.RefCount()) == 1, "Outstanding references to the d3d device");
	m_device = 0;
}

// Returns the size of the displayable area as known by the renderer
pr::iv2 pr::Renderer::DisplayArea() const
{
	DXGI_SWAP_CHAIN_DESC desc;
	pr::Throw(m_swap_chain->GetDesc(&desc));
	return pr::iv2::make(desc.BufferDesc.Width, desc.BufferDesc.Height);
}

// The display mode of the main render target
DXGI_FORMAT pr::Renderer::DisplayFormat() const
{
	DXGI_SWAP_CHAIN_DESC desc;
	pr::Throw(m_swap_chain->GetDesc(&desc));
	return desc.BufferDesc.Format;
}

// Called when the window size changes (e.g. from a WM_SIZE message)
void pr::Renderer::Resize(pr::iv2 const& size)
{
	// Applications can make some changes to make the transition from windowed to full screen more efficient.
	// For example, on a WM_SIZE message, the application should release any outstanding swap-chain back buffers,
	// call IDXGISwapChain::ResizeBuffers, then re-acquire the back buffers from the swap chain(s). This gives the
	// swap chain(s) an opportunity to resize the back buffers, and/or recreate them to enable full-screen flipping
	// operation. If the application does not perform this sequence, DXGI will still make the full-screen/windowed
	// transition, but may be forced to use a stretch operation (since the back buffers may not be the correct size),
	// which may be less efficient. Even if a stretch is not required, presentation may not be optimal because the back
	// buffers might not be directly interchangeable with the front buffer. Thus, a call to ResizeBuffers on WM_SIZE is
	// always recommended, since WM_SIZE is always sent during a fullscreen transition.
	
	// While you don't have to write any more code than has been described, a few simple steps can make your application
	// more responsive. The most important consideration is the resizing of the swap chain's buffers in response to the
	// resizing of the output window. Naturally, the application's best route is to respond to WM_SIZE, and call
	// IDXGISwapChain::ResizeBuffers, passing the size contained in the message's parameters. This behavior obviously makes
	// your application respond well to the user when he or she drags the window's borders, but it is also exactly what
	// enables a smooth full-screen transition. Your window will receive a WM_SIZE message whenever such a transition happens,
	// and calling IDXGISwapChain::ResizeBuffers is the swap chain's chance to re-allocate the buffers' storage for optimal
	// presentation. This is why the application is required to release any references it has on the existing buffers before
	// it calls IDXGISwapChain::ResizeBuffers.
	// Failure to call IDXGISwapChain::ResizeBuffers in response to switching to full-screen mode (most naturally, in response
	// to WM_SIZE), can preclude the optimization of flipping, wherein DXGI can simply swap which buffer is being displayed,
	// rather than copying a full screen's worth of data around.
	
	// Ignore resizes that aren't changes in size
	auto area = DisplayArea();
	if (size == area)
		return;
	
	// Ignore resizes to <= 0. Could report an error here, but what's the point? We handle it.
	if (size.x <= 0 || size.y <= 0)
		return;
	
	// Notify that a resize of the swap chain is about to happen.
	// Receivers need to ensure they don't have any outstanding references to the swap chain resources
	pr::events::Send(Evt_Resize(false, area));

	// Drop the render targets from the immediate context
	m_immediate->OMSetRenderTargets(0, 0, 0);
	m_main_rtv = 0;
	m_main_dsv = 0;
	
	// Get the swap chain to resize itself
	pr::Throw(m_swap_chain->ResizeBuffers(0, size.x, size.y, DXGI_FORMAT_UNKNOWN, m_settings.m_swap_chain_flags));
	
	// Setup the render targets again
	InitMainRT();
	
	// Notify that the resize is done
	area = DisplayArea();
	pr::events::Send(Evt_Resize(true, area));
}

// Flip the scene to the display
void pr::Renderer::Present()
{
	// Be careful that you never have the message-pump thread wait on the render thread.
	// For instance, calling IDXGISwapChain1::Present1 (from the render thread) may cause
	// the render thread to wait on the message-pump thread. When a mode change occurs,
	// this scenario is possible if Present1 calls ::SetWindowPos() or ::SetWindowStyle()
	// and either of these methods call ::SendMessage(). In this scenario, if the message-pump
	// thread has a critical section guarding it or if the render thread is blocked, then the
	// two threads will deadlock.
	
	// IDXGISwapChain1::Present1 will inform you if your output window is entirely occluded via DXGI_STATUS_OCCLUDED.
	// When this occurs, we recommended that your application go into standby mode (by calling IDXGISwapChain1::Present1
	// with DXGI_PRESENT_TEST) since resources used to render the frame are wasted. Using DXGI_PRESENT_TEST will prevent
	// any data from being presented while still performing the occlusion check. Once IDXGISwapChain1::Present1 returns
	// S_OK, you should exit standby mode; do not use the return code to switch to standby mode as doing so can leave
	// the swap chain unable to relinquish full-screen mode.
	// ^^ This means: Don't use calls to Present(?, DXGI_PRESENT_TEST) to test if the window is occluded,
	// only use it after Present() has returned DXGI_STATUS_OCCLUDED.
	
	HRESULT res = m_swap_chain->Present(m_settings.m_vsync, m_idle ? DXGI_PRESENT_TEST : 0);
	switch (res)
	{
	case S_OK:
		m_idle = false;
		break;
	
	// This happens when the window is not visible onscreen, the app should go into idle mode
	case DXGI_STATUS_OCCLUDED:
		m_idle = true;
		break;
	
	// The device failed due to a badly formed command. This is a run-time issue;
	// The application should destroy and recreate the device.
	case DXGI_ERROR_DEVICE_RESET:
		throw pr::Exception<HRESULT>(DXGI_ERROR_DEVICE_RESET, "Graphics adapter reset");
	
	// This happens in situations like, laptop undocked, or remote desktop connect etc.
	// We'll just through so the app can shutdown/reset/whatever
	case DXGI_ERROR_DEVICE_REMOVED:
		throw pr::Exception<HRESULT>(m_device->GetDeviceRemovedReason(), "Graphics adapter no longer available");
	}
}
