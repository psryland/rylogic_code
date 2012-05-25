//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/gbuffer.h"
#include "pr/renderer11/materials/textures/texture2d.h"

using namespace pr::rdr;

// Setup or release the GBuffer. Use Init(0,0,0) to release
void pr::rdr::GBuffer::Init(D3DPtr<ID3D11Device> device, UINT width, UINT height)
{
	m_device = device;
	
	// Create texture buffers that we will use as the render targets in the GBuffer
	pr::rdr::TextureDesc desc;
	desc.Width              = width;
	desc.Height             = height;
	desc.MipLevels          = 1;
	desc.ArraySize          = 1;
	desc.SampleDesc         = MultiSamp(1,0);
	desc.Usage              = D3D11_USAGE_DEFAULT;
	desc.BindFlags          = D3D11_BIND_RENDER_TARGET;
	desc.CPUAccessFlags     = 0;
	desc.MiscFlags          = 0;
	D3DPtr<ID3D11Texture2D> tex[RTCount];
	
	desc.Format = DXGI_FORMAT_R8G8B8A8_SNORM;
	pr::Throw(device->CreateTexture2D(&desc, 0, &tex[0].m_ptr));
	desc.Format = DXGI_FORMAT_R8G8B8A8_SNORM;
	pr::Throw(device->CreateTexture2D(&desc, 0, &tex[1].m_ptr));
	desc.Format = DXGI_FORMAT_R32_FLOAT;
	pr::Throw(device->CreateTexture2D(&desc, 0, &tex[2].m_ptr));
	
	// Get render target views of the texture buffers
	pr::Throw(device->CreateRenderTargetView(tex[0].m_ptr, 0, &m_colour.m_ptr));
	pr::Throw(device->CreateRenderTargetView(tex[1].m_ptr, 0, &m_normal.m_ptr));
	pr::Throw(device->CreateRenderTargetView(tex[2].m_ptr, 0, &m_depth.m_ptr));
}

// Notification of a resize event
void pr::rdr::GBuffer::OnEvent(pr::rdr::Evt_Resize const& evt)
{
	if (!evt.m_done)
	{
		m_colour = 0;
		m_normal = 0;
		m_depth = 0;
	}
	else if (m_device)
	{
		Init(m_device, evt.m_area.x, evt.m_area.y);
	}
}

void pr::rdr::GBuffer::Set()
{
	PR_ASSERT(PR_DBG_RDR, m_device != 0, "Can't set the GBuffer as the output when it hasn't been initialised");
	D3DPtr<ID3D11DeviceContext> immed;
	m_device->GetImmediateContext(&immed.m_ptr);
	
	//immed->OMGetRenderTargets(
	//immed->OMSetRenderTargets(RTCount, rtviews, 0);
}

