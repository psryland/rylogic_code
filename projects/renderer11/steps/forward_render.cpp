//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		ForwardRender::ForwardRender(Scene& scene, bool clear_bb)
			:RenderStep(scene)
			,m_cbuf_frame(m_shdr_mgr->GetCBuf<fwd::CBufFrame>("Fwd::CBufFrame"))
			,m_cbuf_nugget(m_shdr_mgr->GetCBuf<fwd::CBufModel>("Fwd::CBufModel"))
			,m_clear_bb(clear_bb)
		{
			m_rsb = RSBlock::SolidCullBack();

			// Use line antialiasing if multisampling is enabled
			if (m_scene->m_rdr->Settings().m_multisamp.Count != 1)
				m_rsb.Set(ERS::MultisampleEnable, TRUE);
		}

		// Add model nuggets to the draw list for this render step
		void ForwardRender::AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets)
		{
			// See if the instance has a sort key override
			SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

			// Add a drawlist element for each nugget in the instance's model
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				// Ensure the nugget contains forward shaders vs/ps
				// Note, the nugget may contain other shaders that are used by this render step as well
				nug.m_sset.get(EStockShader::FwdShaderVS, m_shdr_mgr)->UsedBy(Id);
				nug.m_sset.get(EStockShader::FwdShaderPS, m_shdr_mgr)->UsedBy(Id);

				// Add a dle for this nugget
				DrawListElement dle;
				dle.m_instance = &inst;
				dle.m_nugget   = &nug;
				dle.m_sort_key = sko ? sko->Combine(nug.m_sort_key) : nug.m_sort_key;
				m_drawlist.push_back_fast(dle);
			}

			m_sort_needed = true;
		}

		// Perform the render step
		void ForwardRender::ExecuteInternal(StateStack& ss)
		{
			auto& dc = ss.m_dc;

			// Sort the draw list if needed
			SortIfNeeded();

			// Clear the back buffer and depth/stencil
			if (m_clear_bb)
			{
				// Get the render target views
				D3DPtr<ID3D11RenderTargetView> rtv;
				D3DPtr<ID3D11DepthStencilView> dsv;
				dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
				dc->ClearRenderTargetView(rtv.m_ptr, m_scene->m_bkgd_colour);
				dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
			}

			// Set the viewport
			dc->RSSetViewports(1, &m_scene->m_viewport);

			// Set the frame constants
			fwd::CBufFrame cb = {};
			SetViewConstants(m_scene->m_view, cb);
			SetLightingConstants(m_scene->m_global_light, cb);
			WriteConstants(dc, m_cbuf_frame, cb, EShaderType::VS|EShaderType::PS);

			for (auto& dle : m_drawlist)
			{
				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Set the per-nugget constants
				fwd::CBufModel cb = {};
				SetGeomType(*dle.m_nugget, cb);
				SetTxfm(*dle.m_instance, m_scene->m_view, cb);
				SetTint(*dle.m_instance, cb);
				SetTexDiffuse(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_nugget, cb, EShaderType::VS|EShaderType::PS);

				// Bind a texture
				BindTextureAndSampler(dc, 0, dle.m_nugget->m_tex_diffuse);

				// Draw the nugget
				Nugget const& nugget = *dle.m_nugget;
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}
	}
}

		//// Projected textures
		//void SetProjectedTextures(D3DPtr<ID3D11DeviceContext>& dc, CBufFrame_Forward& buf, ForwardRender::ProjTextCont const& proj_tex)
		//{
		//	PR_ASSERT(PR_DBG_RDR, proj_tex.size() <= PR_RDR_MAX_PROJECTED_TEXTURES, "Too many projected textures for shader");

		//	// Build a list of the projected texture pointers
		//	auto texs = PR_ALLOCA_POD(ID3D11ShaderResourceView*, proj_tex.size());
		//	auto samp = PR_ALLOCA_POD(ID3D11SamplerState*, proj_tex.size());

		//	// Set the number of projected textures
		//	auto pt_count = checked_cast<uint>(proj_tex.size());
		//	buf.m_proj_tex_count = pr::v4::make(static_cast<float>(pt_count),0.0f,0.0f,0.0f);

		//	// Set the PT transform and populate the textures/sampler arrays
		//	for (uint i = 0; i != pt_count; ++i)
		//	{
		//		buf.m_proj_tex[i] = proj_tex[i].m_o2w;
		//		texs[i] = proj_tex[i].m_tex->m_srv.m_ptr;
		//		samp[i] = proj_tex[i].m_tex->m_samp.m_ptr;
		//	}

		//	// Set the shader resource view of the texture and the texture sampler
		//	dc->PSSetShaderResources(0, pt_count, texs);
		//	dc->PSSetSamplers(0, pt_count, samp);
		//}
