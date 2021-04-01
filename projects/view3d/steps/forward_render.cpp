//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/sortkey.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/steps/forward_render.h"
#include "pr/view3d/steps/shadow_map.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/shaders/common.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	ForwardRender::ForwardRender(Scene& scene, bool clear_bb)
		:RenderStep(scene)
		,m_cbuf_frame (m_shdr_mgr->GetCBuf<hlsl::fwd::CBufFrame>("Fwd::CBufFrame"))
		,m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::fwd::CBufNugget>("Fwd::CBufNugget"))
		,m_clear_bb(clear_bb)
		,m_vs(m_shdr_mgr->FindShader(RdrId(EStockShader::FwdShaderVS)))
		,m_ps(m_shdr_mgr->FindShader(RdrId(EStockShader::FwdShaderPS)))
	{}

	// Add model nuggets to the draw list for this render step
	void ForwardRender::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
	{
		Lock lock(*this);
		auto& drawlist = lock.drawlist();

		// See if the instance has a sort key override
		auto sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
			nug.AddToDrawlist(drawlist, inst, sko, Id);

		m_sort_needed = true;
	}

	// Update the provided shader set appropriate for this render step
	void ForwardRender::ConfigShaders(ShaderSet1& ss, ETopo) const
	{
		if (ss.m_vs == nullptr) ss.m_vs = m_vs.get();
		if (ss.m_ps == nullptr) ss.m_ps = m_ps.get();
	}

	// Perform the render step
	void ForwardRender::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Sort the draw list if needed
		SortIfNeeded();

		// Clear the back buffer and depth/stencil
		if (m_clear_bb)
		{
			// Get the render target views
			// Note: if you've called GetDC() you need to call ReleaseDC() and Window.RestoreRT() or 'rtv' will be null
			D3DPtr<ID3D11RenderTargetView> rtv;
			D3DPtr<ID3D11DepthStencilView> dsv;
			dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
			if (rtv.m_ptr == nullptr) throw std::runtime_error("Render target is null. Ensure RestoreRT has been called");
			if (dsv.m_ptr == nullptr) throw std::runtime_error("Depth buffer is null. Ensure RestoreRT has been called");

			dc->ClearRenderTargetView(rtv.m_ptr, m_scene->m_bkgd_colour.arr);
			dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
		}

		// Set the viewport
		dc->RSSetViewports(1, &m_scene->m_viewport);

		// Check if shadows are enabled
		auto smap_rstep = m_scene->FindRStep<ShadowMap>();
		StateStack::SmapFrame smap_frame(ss, smap_rstep);

		// Set the frame constants
		hlsl::fwd::CBufFrame cb0 = {};
		SetViewConstants(m_scene->m_view, cb0.m_cam);
		SetLightingConstants(m_scene->m_global_light, m_scene->m_view, cb0.m_global_light);
		SetShadowMapConstants(smap_rstep, cb0.m_shadow);
		SetEnvMapConstants(m_scene->m_global_envmap.get(), cb0.m_env_map);
		WriteConstants(dc, m_cbuf_frame.get(), cb0, EShaderType::VS|EShaderType::PS);

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			// Something no rendering?
			//  - Check the tint for the nugget isn't 0x00000000.
			// Tips:
			//  - To uniquely identify an instance in a shader for debugging, set the Instance Id (cb1.m_flags.w)
			//    Then in the shader, use: if (m_flags.w == 1234) ...

			StateStack::DleFrame frame(ss, dle);
			auto const& nugget = *dle.m_nugget;

			// Set the per-nugget constants
			hlsl::fwd::CBufNugget cb1 = {};
			SetModelFlags(*dle.m_instance, nugget, *m_scene, cb1);
			SetTxfm(*dle.m_instance, m_scene->m_view, cb1);
			SetTint(*dle.m_instance, nugget, cb1);
			SetEnvMap(*dle.m_instance, nugget, cb1);
			SetTexDiffuse(nugget, cb1);
			WriteConstants(dc, m_cbuf_nugget.get(), cb1, EShaderType::VS|EShaderType::PS);

			// Draw the nugget
			DrawNugget(dc, nugget, ss);
		}
	}

	// Call draw for a nugget
	void ForwardRender::DrawNugget(ID3D11DeviceContext* dc, Nugget const& nugget, StateStack& ss)
	{
		// Render solid or wireframe nuggets
		if (nugget.m_fill_mode == EFillMode::Default ||
			nugget.m_fill_mode == EFillMode::Solid ||
			nugget.m_fill_mode == EFillMode::Wireframe ||
			nugget.m_fill_mode == EFillMode::SolidWire)
		{
			ss.Commit();
			if (nugget.m_irange.empty())
			{
				dc->Draw(
					UINT(nugget.m_vrange.size()),
					UINT(nugget.m_vrange.m_beg));
			}
			else
			{
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_beg),
					0);
			}
		}

		// Render wire frame over solid for 'SolidWire' mode
		if (!nugget.m_irange.empty() && 
			nugget.m_fill_mode == EFillMode::SolidWire && (
			nugget.m_topo == ETopo::TriList ||
			nugget.m_topo == ETopo::TriListAdj ||
			nugget.m_topo == ETopo::TriStrip ||
			nugget.m_topo == ETopo::TriStripAdj))
		{
			m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
			m_bsb.Set(EBS::BlendEnable, FALSE, 0);

			ss.Commit();
			dc->DrawIndexed(
				UINT(nugget.m_irange.size()),
				UINT(nugget.m_irange.m_beg),
				0);

			m_rsb.Clear(ERS::FillMode);
			m_bsb.Clear(EBS::BlendEnable, 0);
		}

		// Render points for 'Points' mode
		if (nugget.m_fill_mode == EFillMode::Points)
		{
			ss.m_pending.m_topo = ETopo::PointList;
			ss.m_pending.m_shdrs.m_gs = m_scene->m_diag.m_gs_fillmode_points.get();
			ss.Commit();
			dc->Draw(
				UINT(nugget.m_vrange.size()),
				UINT(nugget.m_vrange.m_beg));
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
//	auto pt_count = s_cast<uint>(proj_tex.size());
//	buf.m_proj_tex_count = pr::v4(static_cast<float>(pt_count),0.0f,0.0f,0.0f);
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
