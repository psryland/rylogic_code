//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/steps/gbuffer.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/shader.h"
#include "pr/view3d/util/stock_resources.h"
#include "pr/view3d/util/event_args.h"
#include "view3d/shaders/common.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	GBuffer::GBuffer(Scene& scene)
		:RenderStep(scene)
		,m_tex()
		,m_rtv()
		,m_srv()
		,m_dsv()
		,m_main_rtv()
		,m_main_dsv()
		,m_cbuf_camera(m_shdr_mgr->GetCBuf<hlsl::ds::CBufCamera>("ds::CBufCamera"))
		,m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::ds::CBufNugget>("ds::CBufNugget"))
		,m_vs(m_shdr_mgr->FindShader(RdrId(EStockShader::GBufferVS)))
		,m_ps(m_shdr_mgr->FindShader(RdrId(EStockShader::GBufferPS)))
	{
		InitRT(true);

		// Watch for renderer target size changes
		m_eh_resize = m_scene->m_wnd->m_rdr->BackBufferSizeChanged += [this](Window& wnd, BackBufferSizeChangedEventArgs const& args)
		{
			// Recreate the g-buffer on resize
			if (&wnd == m_scene->m_wnd)
				InitRT(args.m_done);
		};

		m_rsb = RSBlock::SolidCullBack();
	}

	// Create render targets for the GBuffer based on the current render target size
	void GBuffer::InitRT(bool create_buffers)
	{
		// Release any existing RTs
		m_dsv = nullptr;
		for (int i = 0; i != GBuffer::RTCount; ++i)
		{
			m_tex[i] = nullptr;
			m_rtv[i] = nullptr;
			m_srv[i] = nullptr;
		}

		if (!create_buffers)
			return;

		Renderer::Lock lock(*m_scene->m_wnd->m_rdr);
		auto device = lock.D3DDevice();
		auto size = m_scene->m_wnd->BackBufferSize();

		// Create texture buffers that we will use as the render targets in the GBuffer
		Texture2DDesc tdesc;
		tdesc.Width              = size.x;
		tdesc.Height             = size.y;
		tdesc.MipLevels          = 1;
		tdesc.ArraySize          = 1;
		tdesc.SampleDesc         = MultiSamp(1,0);
		tdesc.Usage              = D3D11_USAGE_DEFAULT;
		tdesc.BindFlags          = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
		tdesc.CPUAccessFlags     = 0;
		tdesc.MiscFlags          = 0;

		// Create a texture for each layer in the GBuffer
		// and get the render target view of each texture buffer
		DXGI_FORMAT fmt[GBuffer::RTCount] =
		{
			DXGI_FORMAT_R10G10B10A2_UNORM, // diffuse , normal Z sign  //DXGI_FORMAT_R8G8B8A8_UNORM,
			DXGI_FORMAT_R16G16_UNORM,      // normal x,y //DXGI_FORMAT_R11G11B10_FLOAT,
			DXGI_FORMAT_R32_FLOAT,         // depth layer
		};
		for (int i = 0; i != GBuffer::RTCount; ++i)
		{
			// Create the resource
			tdesc.Format = fmt[i];
			pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_tex[i].m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_tex[i].get(), FmtS("GBuffer %s tex", ToString((GBuffer::RTEnum_)i))));

			// Get the render target view
			RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
			rtvdesc.Texture2D.MipSlice = 0;
			pr::Throw(device->CreateRenderTargetView(m_tex[i].m_ptr, &rtvdesc, &m_rtv[i].m_ptr));

			// Get the shader res view
			ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
			srvdesc.Texture2D.MostDetailedMip = 0;
			srvdesc.Texture2D.MipLevels = 1;
			pr::Throw(device->CreateShaderResourceView(m_tex[i].m_ptr, &srvdesc, &m_srv[i].m_ptr));
		}

		// We need to create our own depth buffer to ensure it has the same dimensions
		// and multi-sampling properties as the g-buffer RTs.
		D3DPtr<ID3D11Texture2D> dtex;
		tdesc.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
		tdesc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
		pr::Throw(device->CreateTexture2D(&tdesc, 0, &dtex.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(dtex.get(), "GBuffer DSV"));

		DepthStencilViewDesc dsvdesc(tdesc.Format);
		dsvdesc.Texture2D.MipSlice = 0;
		pr::Throw(device->CreateDepthStencilView(dtex.m_ptr, &dsvdesc, &m_dsv.m_ptr));
	}

	// Bind the GBuffer RTs to the output merger
	void GBuffer::BindRT(bool bind)
	{
		Renderer::Lock lock(*m_scene->m_wnd->m_rdr);
		auto dc = lock.ImmediateDC();
		if (bind)
		{
			// Save a reference to the main render target/depth buffer
			dc->OMGetRenderTargets(1, &m_main_rtv.m_ptr, &m_main_dsv.m_ptr);

			// Bind the g-buffer RTs to the OM
			dc->OMSetRenderTargets(GBuffer::RTCount, (ID3D11RenderTargetView*const*)&m_rtv[0], m_dsv.m_ptr);
		}
		else
		{
			// Restore the main RT and depth buffer
			dc->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);

			// Release our reference to the main RTV/DSV
			m_main_rtv = nullptr;
			m_main_dsv = nullptr;
		}
	}

	// Add model nuggets to the draw list for this render step
	void GBuffer::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
	{
		// See if the instance has a sort key override
		SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

		Lock lock(*this);
		auto& drawlist = lock.drawlist();

		// Add the drawlist elements for this instance that
		// correspond to the render nuggets of the renderable
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
			nug.AddToDrawlist(drawlist, inst, sko, Id);

		m_sort_needed = true;
	}

	// Update the provided shader set appropriate for this render step
	void GBuffer::ConfigShaders(ShaderSet1& ss, ETopo) const
	{
		assert(ss.m_vs == nullptr);
		assert(ss.m_ps == nullptr);
		ss.m_vs = m_vs.get();
		ss.m_ps = m_ps.get();
	}

	// Perform the render step
	void GBuffer::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Sort the draw list
		SortIfNeeded();

		// Bind the g-buffer to the OM
		auto bind_gbuffer = Scope<void>(
			[this]{ BindRT(true); },
			[this]{ BindRT(false); });

		// Clear the g-buffer and depth buffer
		float diff_reset[4] = {m_scene->m_bkgd_colour.r, m_scene->m_bkgd_colour.g, m_scene->m_bkgd_colour.b, 0.5f};
		dc->ClearRenderTargetView(m_rtv[0].m_ptr, diff_reset);
		dc->ClearRenderTargetView(m_rtv[1].m_ptr, v4Half.arr);
		dc->ClearRenderTargetView(m_rtv[2].m_ptr, v4Max.arr);
		dc->ClearDepthStencilView(m_dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);

		// Set the viewport
		dc->RSSetViewports(1, &m_scene->m_viewport);

		// Set the frame constants and bind them to the shaders
		hlsl::ds::CBufCamera cb0 = {};
		SetViewConstants(m_scene->m_view, cb0.m_cam);
		WriteConstants(dc, m_cbuf_camera.get(), cb0, EShaderType::VS|EShaderType::PS);

		// Loop over the elements in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			StateStack::DleFrame frame(ss, dle);
			ss.Commit();

			auto const& nugget = *dle.m_nugget;

			// Set the per-nugget constants
			hlsl::ds::CBufNugget cb1 = {};
			SetModelFlags(*dle.m_instance, *dle.m_nugget, *m_scene, cb1);
			SetTxfm(*dle.m_instance, m_scene->m_view, cb1);
			SetTint(*dle.m_instance, nugget, cb1);
			SetTexDiffuse(*dle.m_nugget, cb1);
			WriteConstants(dc, m_cbuf_nugget.get(), cb1, EShaderType::VS|EShaderType::PS);

			// Add the nugget to the device context
			dc->DrawIndexed(
				UINT(nugget.m_irange.size()),
				UINT(nugget.m_irange.m_beg),
				0);
		}
	}
}
