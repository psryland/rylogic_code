//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/render_step.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/util/lock.h"
#include "renderer11/shaders/cbuffer.h"
//#include "pr/renderer11/render/draw_method.h"

namespace pr
{
	namespace rdr
	{
		RenderStep::RenderStep(Scene& scene)
			:m_scene(&scene)
			,m_drawlist(scene.m_rdr->Allocator<DrawListElement>())
			,m_sort_needed(true)
			,m_bsb()
			,m_rsb()
			,m_dsb()
		{}

		// Reset/Populate the drawlist
		void RenderStep::ClearDrawlist()
		{
			m_drawlist.resize(0);
		}
		void RenderStep::UpdateDrawlist()
		{
			pr::events::Send(Evt_SceneRender(*this));
		}

		// Sort the drawlist based on sortkey
		void RenderStep::Sort()
		{
			std::sort(std::begin(m_drawlist), std::end(m_drawlist));
			m_sort_needed = false;
		}
		void RenderStep::SortIfNeeded()
		{
			if (!m_sort_needed) return;
			Sort();
		}

		// Add an instance. The instance must be resident for the entire time that it is
		// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		void RenderStep::AddInstance(BaseInstance const& inst)
		{
			// Get the model associated with the isntance
			ModelPtr const& model = GetModel(inst);
			PR_ASSERT(PR_DBG_RDR, model != nullptr, "Null model pointer");

			// Get the nuggets for this render step
			auto& nuggets = model->m_nmap.at(Id());
			#if PR_DBG_RDR
			if (nuggets.empty() && !AllSet(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets))
			{
				PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') has no nuggets, you need to call SetMaterial() on the model first\n", model->m_name.c_str()));
				model->m_dbg_flags = SetBits(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets, true);
			}
			#endif

			// Check the instance transform is valid
			PR_ASSERT(PR_DBG_RDR, FEql(GetO2W(inst).w.w, 1.0f), "Invalid instance transform");

			// See if the instance has a sort key override
			SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

			// Add the drawlist elements for this instance that
			// correspond to the render nuggets of the renderable
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				DrawListElement element;
				element.m_instance = &inst;
				element.m_nugget   = &nug;
				element.m_sort_key = nug.m_sort_key;
				if (sko) element.m_sort_key = sko->Combine(element.m_sort_key);
				m_drawlist.push_back_fast(element);
			}

			m_sort_needed = true;
		}

		// Remove an instance from the scene
		void RenderStep::RemoveInstance(BaseInstance const& inst)
		{
			auto new_end = std::remove_if(std::begin(m_drawlist), std::end(m_drawlist), [&](DrawListElement const& dle){ return dle.m_instance == &inst; });
			m_drawlist.resize(new_end - std::begin(m_drawlist));
		}

		// Remove a batch of instances. Optimised by a single past through the drawlist
		void RenderStep::RemoveInstances(BaseInstance const** inst, std::size_t count)
		{
			// Make a sorted list from the batch to remove
			BaseInstance const** doomed = PR_ALLOCA_POD(BaseInstance const* , count);
			BaseInstance const** doomed_end = doomed + count;
			std::copy(inst, inst + count, doomed);
			std::sort(doomed, doomed_end);

			// Remove instances
			auto new_end = std::remove_if(std::begin(m_drawlist), std::end(m_drawlist), [&](DrawListElement const& dle)
			{
				auto iter = std::lower_bound(doomed, doomed_end, dle.m_instance);
				return iter != doomed_end && *iter == dle.m_instance;
			});
			m_drawlist.resize(new_end - std::begin(m_drawlist));
		}

		// Forward rendering ***********************************************************

		ForwardRender::ForwardRender(Scene& scene, bool clear_bb, pr::Colour const& bkgd_colour)
			:RenderStep(scene)
			,m_background_colour(bkgd_colour)
			,m_global_light()
			,m_cbuf_frame()
			,m_clear_bb(clear_bb)
		{
			CBufFrame scene_constants = {};
			scene_constants.m_c2w = m_scene->m_view.m_c2w;
			scene_constants.m_w2c = pr::GetInverseFast(scene_constants.m_c2w);

			// Create a constants buffer that changes per frame
			CBufferDesc cbdesc(sizeof(CBufFrame));
			SubResourceData init(scene_constants);
			pr::Throw(m_scene->m_rdr->Device()->CreateBuffer(&cbdesc, &init, &m_cbuf_frame.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "CBufFrame"));

			m_rsb = RSBlock::SolidCullBack();

			// Use line antialiasing if multisampling is enabled
			if (m_scene->m_rdr->Settings().m_multisamp.Count != 1)
				m_rsb.Set(ERS::MultisampleEnable, TRUE);
		}

		// Projected textures
		void SetProjectedTextures(D3DPtr<ID3D11DeviceContext>& dc, CBufFrame& buf, ForwardRender::ProjTextCont const& proj_tex)
		{
			PR_ASSERT(PR_DBG_RDR, proj_tex.size() <= PR_RDR_MAX_PROJECTED_TEXTURES, "Too many projected textures for shader");

			// Build a list of the projected texture pointers
			auto texs = PR_ALLOCA_POD(ID3D11ShaderResourceView*, proj_tex.size());
			auto samp = PR_ALLOCA_POD(ID3D11SamplerState*, proj_tex.size());

			// Set the number of projected textures
			auto pt_count = checked_cast<uint>(proj_tex.size());
			buf.m_proj_tex_count = pr::v4::make(static_cast<float>(pt_count),0.0f,0.0f,0.0f);

			// Set the PT transform and populate the textures/sampler arrays
			for (uint i = 0; i != pt_count; ++i)
			{
				buf.m_proj_tex[i] = proj_tex[i].m_o2w;
				texs[i] = proj_tex[i].m_tex->m_srv.m_ptr;
				samp[i] = proj_tex[i].m_tex->m_samp.m_ptr;
			}

			// Set the shader resource view of the texture and the texture sampler
			dc->PSSetShaderResources(0, pt_count, texs);
			dc->PSSetSamplers(0, pt_count, samp);
		}

		// Perform the render step
		void ForwardRender::Execute()
		{
			auto dc = m_scene->m_rdr->ImmediateDC();

			// Sort the draw list if needed
			SortIfNeeded();

			// Clear the back buffer and depth/stencil
			if (m_clear_bb)
			{
				// Get the render target views
				D3DPtr<ID3D11RenderTargetView> rtv;
				D3DPtr<ID3D11DepthStencilView> dsv;
				dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
				dc->ClearRenderTargetView(rtv.m_ptr, m_background_colour);
				dc->ClearDepthStencilView(dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);
			}

			// Set the viewport
			dc->RSSetViewports(1, &m_scene->m_viewport);

			// Set the frame constants
			CBufFrame buf = {};
			buf.m_c2w = m_scene->m_view.m_c2w;
			buf.m_w2c = pr::GetInverse(m_scene->m_view.m_c2w);
			buf.m_w2s = m_scene->m_view.m_c2s * pr::GetInverseFast(m_scene->m_view.m_c2w);

			// Lighting
			buf.m_global_lighting    = pr::v4::make(static_cast<float>(m_global_light.m_type),0.0f,0.0f,0.0f);
			buf.m_ws_light_direction = m_global_light.m_direction;
			buf.m_ws_light_position  = m_global_light.m_position;
			buf.m_light_ambient      = m_global_light.m_ambient;
			buf.m_light_colour       = m_global_light.m_diffuse;
			buf.m_light_specular     = pr::Colour::make(m_global_light.m_specular, m_global_light.m_specular_power);
			buf.m_spot               = pr::v4::make(m_global_light.m_inner_cos_angle, m_global_light.m_outer_cos_angle, m_global_light.m_range, m_global_light.m_falloff);

			SetProjectedTextures(dc, buf, m_proj_tex);

			{// Lock and write the constants
				LockT<CBufFrame> lock(dc, m_cbuf_frame, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = buf;
			}

			// Bind the frame constants to the shaders
			dc->VSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);
			dc->PSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);

			// Loop over the elements in the draw list
			for (auto& dle : m_drawlist)
			{
				Nugget const&       nugget = *dle.m_nugget;
				BaseInstance const& inst   = *dle.m_instance;
				ShaderPtr const&    shader = nugget.m_draw.m_shader;

				// Bind the shader to the device
				shader->Bind(dc, nugget, inst, *this);

				// Add the nugget to the device context
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}
	}
}
