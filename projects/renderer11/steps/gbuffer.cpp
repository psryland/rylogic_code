//*********************************************
// Renderer
//  Copyright � Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/shaders/shader.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		GBuffer::GBuffer(Scene& scene)
			:RenderStep(scene)
			,m_tex()
			,m_rtv()
			,m_srv()
			,m_dsv()
			,m_main_rtv()
			,m_main_dsv()
			,m_cbuf_camera(m_shdr_mgr->GetCBuf<ds::CBufCamera>("ds::CBufCamera"))
			,m_cbuf_nugget(m_shdr_mgr->GetCBuf<ds::CBufModel>("ds::CBufModel"))
		{
			InitRT(true);

			m_rsb = RSBlock::SolidCullBack();
		}

		// Create render targets for the gbuffer based on the current render target size
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

			auto dc = m_scene->m_rdr->ImmediateDC();
			auto size = m_scene->m_rdr->RenderTargetSize();
			auto device = m_scene->m_rdr->Device();

			// Create texture buffers that we will use as the render targets in the GBuffer
			TextureDesc tdesc;
			tdesc.Width              = size.x;
			tdesc.Height             = size.y;
			tdesc.MipLevels          = 1;
			tdesc.ArraySize          = 1;
			tdesc.SampleDesc         = MultiSamp(1,0);
			tdesc.Usage              = D3D11_USAGE_DEFAULT;
			tdesc.BindFlags          = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
			tdesc.CPUAccessFlags     = 0;
			tdesc.MiscFlags          = 0;

			// Create a texture for each layer in the gbuffer
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
				PR_EXPAND(PR_DBG_RDR, NameResource(m_tex[i], FmtS("gbuffer %s tex", ToString((GBuffer::RTEnum_)i))));

				// Get the render target view
				RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
				rtvdesc.Texture2D.MipSlice = 0;
				pr::Throw(device->CreateRenderTargetView(m_tex[i].m_ptr, &rtvdesc, &m_rtv[i].m_ptr));

				// Get the shader res view
				ShaderResViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
				srvdesc.Texture2D.MostDetailedMip = 0;
				srvdesc.Texture2D.MipLevels = 1;
				pr::Throw(device->CreateShaderResourceView(m_tex[i].m_ptr, &srvdesc, &m_srv[i].m_ptr));
			}

			// We need to create our own depth buffer to ensure it has the same dimensions
			// and multisampling properties as the g-buffer RTs.
			D3DPtr<ID3D11Texture2D> dtex;
			tdesc.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
			tdesc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
			pr::Throw(device->CreateTexture2D(&tdesc, 0, &dtex.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(dtex, "gbuffer dsv"));

			DepthStencilViewDesc dsvdesc(tdesc.Format);
			dsvdesc.Texture2D.MipSlice = 0;
			pr::Throw(device->CreateDepthStencilView(dtex.m_ptr, &dsvdesc, &m_dsv.m_ptr));
		}

		// Bind the gbuffer RTs to the output merger
		void GBuffer::BindRT(bool bind)
		{
			auto dc = m_scene->m_rdr->ImmediateDC();
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

				// Release our reference to the main rtv/dsv
				m_main_rtv = nullptr;
				m_main_dsv = nullptr;
			}
		}

		// Add model nuggets to the draw list for this render step
		void GBuffer::AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets)
		{
			// See if the instance has a sort key override
			SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

			// Add the drawlist elements for this instance that
			// correspond to the render nuggets of the renderable
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				// Ensure the nugget contains gbuffer shaders vs/ps
				// Note, the nugget may contain other shaders that are used by this render step as well
				nug.m_sset.get(EStockShader::GBufferVS, m_shdr_mgr)->UsedBy(Id);
				nug.m_sset.get(EStockShader::GBufferPS, m_shdr_mgr)->UsedBy(Id);

				DrawListElement dle;
				dle.m_instance = &inst;
				dle.m_nugget   = &nug;
				dle.m_sort_key = sko ? sko->Combine(nug.m_sort_key) : nug.m_sort_key;
				m_drawlist.push_back_fast(dle);
			}

			m_sort_needed = true;
		}

		// Perform the render step
		void GBuffer::ExecuteInternal(StateStack& ss)
		{
			auto& dc = ss.m_dc;

			// Sort the draw list
			SortIfNeeded();

			// Bind the g-buffer to the OM
			auto bind_gbuffer = pr::CreateScope(
				[this]{ BindRT(true); },
				[this]{ BindRT(false); });

			// Clear the g-buffer and depth buffer
			float diff_reset[4] = {m_scene->m_bkgd_colour.r, m_scene->m_bkgd_colour.g, m_scene->m_bkgd_colour.b, 0.5f};
			dc->ClearRenderTargetView(m_rtv[0].m_ptr, diff_reset);
			dc->ClearRenderTargetView(m_rtv[1].m_ptr, pr::v4Half.ToArray());
			dc->ClearRenderTargetView(m_rtv[2].m_ptr, pr::v4Max.ToArray());
			dc->ClearDepthStencilView(m_dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);

			// Set the viewport
			dc->RSSetViewports(1, &m_scene->m_viewport);

			// Set the frame constants and bind them to the shaders
			ds::CBufCamera cb = {};
			SetViewConstants(m_scene->m_view, cb);
			WriteConstants(dc, m_cbuf_camera, cb, EShaderType::VS|EShaderType::PS);

			// Loop over the elements in the draw list
			for (auto& dle : m_drawlist)
			{
				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Set the per-nugget constants
				ds::CBufModel cb = {};
				SetGeomType(*dle.m_nugget, cb);
				SetTxfm(*dle.m_instance, m_scene->m_view, cb);
				SetTint(*dle.m_instance, cb);
				SetTexDiffuse(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_nugget, cb, EShaderType::VS|EShaderType::PS);

				// Bind a texture
				BindTextureAndSampler(dc, 0, dle.m_nugget->m_tex_diffuse);

				// Add the nugget to the device context
				Nugget const& nugget = *dle.m_nugget;
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}

		// Handle main window resize events
		void GBuffer::OnEvent(Evt_Resize const& evt)
		{
			// Recreate the g-buffer on resize
			InitRT(evt.m_done);
		}
	}
}