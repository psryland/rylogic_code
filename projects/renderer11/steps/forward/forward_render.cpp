//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/forward/forward_render.h"
#include "renderer11/steps/common.h"
#include "renderer11/steps/forward/fwd_shader.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		ForwardRender::ForwardRender(Scene& scene, bool clear_bb)
			:RenderStep(scene)
			,m_cbuf_frame()
			,m_clear_bb(clear_bb)
		{
			// Create a constants buffer that changes per frame
			CBufferDesc cbdesc(sizeof(FwdShader::CBufFrame));
			pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_frame.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "ForwardRender::CBufFrame"));

			m_rsb = RSBlock::SolidCullBack();

			// Use line antialiasing if multisampling is enabled
			if (m_scene->m_rdr->Settings().m_multisamp.Count != 1)
				m_rsb.Set(ERS::MultisampleEnable, TRUE);
		}

		// Add model nuggets to the draw list for this render step
		void ForwardRender::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
		{
			// See if the instance has a sort key override
			SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

			// Add the drawlist elements for this instance that
			// correspond to the render nuggets of the renderable
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				DrawListElement dle;
				dle.m_shader   = m_scene->m_rdr->m_shdr_mgr.FindShaderFor(nug.m_geom).m_ptr;
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
			// Sort the draw list if needed
			SortIfNeeded();

			// Clear the back buffer and depth/stencil
			if (m_clear_bb)
			{
				// Get the render target views
				D3DPtr<ID3D11RenderTargetView> rtv;
				D3DPtr<ID3D11DepthStencilView> dsv;
				ss.m_dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
				ss.m_dc->ClearRenderTargetView(rtv.m_ptr, m_scene->m_bkgd_colour);
				ss.m_dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
			}

			// Set the viewport
			ss.m_dc->RSSetViewports(1, &m_scene->m_viewport);

			// Set the frame constants
			FwdShader::CBufFrame cb = {};
			SetViewConstants(m_scene->m_view, cb);
			SetLightingConstants(m_scene->m_global_light, cb);
			//SetProjectedTextures(dc, cb, m_proj_tex);
			WriteConstants(ss.m_dc, m_cbuf_frame, cb);

			for (auto& dle : m_drawlist)
			{
				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Add the nugget to the device context
				Nugget const& nugget = *dle.m_nugget;
				ss.m_dc->DrawIndexed(
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
