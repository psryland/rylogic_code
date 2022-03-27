//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "view3d/render/stereo.h"
#include "pr/view3d/render/renderer.h"

namespace pr::rdr
{
	Stereo::Stereo(ID3D11Device* device, Viewport const& viewport, DXGI_FORMAT target_format, bool swap_eyes, float eye_separation)
		:m_nv_magic(NvStereoImageHeader::make(static_cast<size_t>(viewport.Width), static_cast<size_t>(viewport.Height), BitsPerPixel(target_format), swap_eyes))
		,m_mark()
		,m_rt_tex()
		,m_rtv()
		,m_ds_tex()
		,m_dsv()
		,m_eye_separation(eye_separation)
	{
		// NVidia 3D works like this:
		// - Create a render target with dimensions 2*width,height+1
		// - Render the left eye to [0,width), right to [width,2*width)
		// - Write the NV_STEREO_IMAGE_SIGNITURE in row 'height'
		// - CopySubResourceRegion to the back buffer

		// Create a staging texture to contain the NVidia magic data
		SubResourceData tex_data(&m_nv_magic, sizeof(m_nv_magic), 0);
		Texture2DDesc nvdesc(m_nv_magic.pixel_width(), m_nv_magic.pixel_height(), 1, target_format);
		nvdesc.BindFlags      = 0;
		nvdesc.Usage          = D3D11_USAGE_STAGING;
		nvdesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
		pr::Throw(device->CreateTexture2D(&nvdesc, &tex_data, &m_mark.m_ptr));

		// Create a render target with dimensions width*2, height+1
		Texture2DDesc rtdesc(UINT(viewport.Width * 2), UINT(viewport.Height + 1), 1, target_format);
		rtdesc.BindFlags = D3D11_BIND_RENDER_TARGET;
		pr::Throw(device->CreateTexture2D(&rtdesc, nullptr, &m_rt_tex.m_ptr));

		// Create a render target view of the render target texture
		RenderTargetViewDesc rtvdesc(target_format);
		pr::Throw(device->CreateRenderTargetView(m_rt_tex.m_ptr, &rtvdesc, &m_rtv.m_ptr));

		// Create a depth stencil buffer to fit this rt
		Texture2DDesc dsdesc(UINT(viewport.Width * 2), UINT(viewport.Height + 1), 1, DXGI_FORMAT_D24_UNORM_S8_UINT);
		dsdesc.SampleDesc = MultiSamp(1,0);
		dsdesc.BindFlags  = D3D11_BIND_DEPTH_STENCIL;
		pr::Throw(device->CreateTexture2D(&dsdesc, nullptr, &m_ds_tex.m_ptr));

		// Create a depth/stencil view of the texture buffer we just created
		DepthStencilViewDesc dsvdesc(dsdesc.Format);
		pr::Throw(device->CreateDepthStencilView(m_ds_tex.m_ptr, &dsvdesc, &m_dsv.m_ptr));
	}

	// Add the NVidia magic data to the bottom row of the current render target
	void Stereo::BlitNvMagic(ID3D11DeviceContext* dc) const
	{
		// Add the NVidia magic data to the current render target
		CD3D11_BOX nvdata_box(0, 0, 0, m_nv_magic.pixel_width(), m_nv_magic.pixel_height(), 1);
		dc->CopySubresourceRegion(m_rt_tex.m_ptr, 0, 0, m_nv_magic.offscreen_height() - 1, 0, m_mark.m_ptr, 0, &nvdata_box);
	}

	// Copy the off screen render target to the current render target
	void Stereo::BlitRTV(ID3D11DeviceContext* dc) const
	{
		D3DPtr<ID3D11RenderTargetView> rtv;
		dc->OMGetRenderTargets(1, &rtv.m_ptr, nullptr);

		D3DPtr<ID3D11Resource> rtv_res;
		rtv->GetResource(&rtv_res.m_ptr);

		CD3D11_BOX src_box(0, 0, 0, m_nv_magic.target_width(), m_nv_magic.target_height(), 1);
		dc->CopySubresourceRegion(rtv_res.m_ptr, 0, 0, 0, 0, m_rt_tex.m_ptr, 0, &src_box);
	}
}