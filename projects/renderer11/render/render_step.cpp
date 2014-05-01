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
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/shaders/cbuffer.h"

namespace pr
{
	namespace rdr
	{
		// Helper for setting scene view constants
		template <typename TCBuf> void SetViewConstants(SceneView const& view, TCBuf& cb)
		{
			cb.m_c2w = view.m_c2w;
			cb.m_w2c = pr::GetInverse(view.m_c2w);
			cb.m_w2s = view.m_c2s * pr::GetInverseFast(view.m_c2w);
		}

		// RenderStep *********************************************************

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

		// Add an instance. The instance, model, and nuggets must be resident for the entire time
		// that the instance is in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		void RenderStep::AddInstance(BaseInstance const& inst)
		{
			// Get the model associated with the isntance
			ModelPtr const& model = GetModel(inst);
			PR_ASSERT(PR_DBG_RDR, model != nullptr, "Null model pointer");

			// Get the nuggets for this render step
			auto& nuggets = model->m_nuggets;
			#if PR_DBG_RDR
			if (nuggets.empty() && !AllSet(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets))
			{
				PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') has no nuggets, you need to call CreateNugget() on the model first\n", model->m_name.c_str()));
				model->m_dbg_flags = SetBits(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets, true);
			}
			#endif

			// Check the instance transform is valid
			PR_ASSERT(PR_DBG_RDR, FEql(GetO2W(inst).w.w, 1.0f), "Invalid instance transform");

			// Add to the derived objects drawlist
			AddNuggets(inst, nuggets);
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

		// Perform the render step
		void RenderStep::Execute()
		{
			// Notify that this render step is about to execute
			pr::events::Send(Evt_RenderStepExecute(*this, false));

			ExecuteInternal();

			// Notify that the render step has finished
			pr::events::Send(Evt_RenderStepExecute(*this, false));
		}

		// GBufferCreate ******************************************************

		GBufferCreate::GBufferCreate(Scene& scene)
			:RenderStep(scene)
			,m_tex()
			,m_rtv()
			,m_srv()
			,m_dsv()
			,m_main_rtv()
			,m_main_dsv()
			,m_cbuf_frame()
			,m_shader(scene.m_rdr->m_shdr_mgr.FindShader(EStockShader::GBuffer))
		{
			// Create a constants buffer for constants that only change once per frame
			CBufferDesc cbdesc(sizeof(CBufFrame_GBuffer));
			pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_frame.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "gbuffer CBufFrame"));

			InitGBuffer(true);
		}

		// Create render targets for the gbuffer based on the current render target size
		void GBufferCreate::InitGBuffer(bool create_buffers)
		{
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

			// Release any existing RTs
			m_dsv = nullptr;
			for (int i = 0; i != RTCount; ++i)
			{
				m_tex[i] = nullptr;
				m_rtv[i] = nullptr;
				m_srv[i] = nullptr;
			}

			if (create_buffers)
			{
				// Create a texture for each layer in the gbuffer
				// and get the render target view of each texture buffer
				DXGI_FORMAT fmt[RTCount] =
				{
					DXGI_FORMAT_R8G8B8A8_UNORM, // diffuse layer
					DXGI_FORMAT_R16G16_SNORM,   // normal layer  (x,y) component
					DXGI_FORMAT_R32_FLOAT,      // depth layer
				};
				for (int i = 0; i != RTCount; ++i)
				{
					// Create the resource
					tdesc.Format = fmt[i];
					pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_tex[i].m_ptr));
					PR_EXPAND(PR_DBG_RDR, NameResource(m_tex[i], FmtS("gbuffer %s tex", ToString((RTEnum_)i))));

					// Get the render target view
					RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
					rtvdesc.Texture2D.MipSlice = 0;
					pr::Throw(device->CreateRenderTargetView(m_tex[i].m_ptr, &rtvdesc, &m_rtv[i].m_ptr));
					PR_EXPAND(PR_DBG_RDR, NameResource(m_tex[i], FmtS("gbuffer %s rtv", ToString((RTEnum_)i))));

					// Get the shader res view
					ShaderResViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
					srvdesc.Texture2D.MostDetailedMip = 0;
					srvdesc.Texture2D.MipLevels = 1;
					pr::Throw(device->CreateShaderResourceView(m_tex[i].m_ptr, &srvdesc, &m_srv[i].m_ptr));
					PR_EXPAND(PR_DBG_RDR, NameResource(m_tex[i], FmtS("gbuffer %s srv", ToString((RTEnum_)i))));
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
		}

		// Bind the gbuffer RTs to the output merger
		void GBufferCreate::BindGBuffer(bool bind)
		{
			auto dc = m_scene->m_rdr->ImmediateDC();
			if (bind)
			{
				// Save a reference to the main render target/depth buffer
				dc->OMGetRenderTargets(1, &m_main_rtv.m_ptr, &m_main_dsv.m_ptr);

				// Bind the g-buffer RTs to the OM
				dc->OMSetRenderTargets(RTCount, (ID3D11RenderTargetView*const*)&m_rtv[0], m_dsv.m_ptr);
			}
			else
			{
				// Restore the main RT and depth buffer
				dc->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);
			}
		}

		// Add model nuggets to the draw list for this render step
		void GBufferCreate::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
		{
			// See if the instance has a sort key override
			SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

			// Add the drawlist elements for this instance that
			// correspond to the render nuggets of the renderable
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				DrawListElement dle;
				dle.m_shader   = m_shader.m_ptr;
				dle.m_instance = &inst;
				dle.m_nugget   = &nug;
				dle.m_sort_key = sko ? sko->Combine(nug.m_sort_key) : nug.m_sort_key;
				m_drawlist.push_back_fast(dle);
			}

			m_sort_needed = true;
		}

		// Perform the render step
		void GBufferCreate::ExecuteInternal()
		{
			auto dc = m_scene->m_rdr->ImmediateDC();

			// Sort the draw list
			SortIfNeeded();

			// Bind the g-buffer to the OM
			BindGBuffer(true);

			// Clear the g-buffer and depth buffer
			pr::Colour reset_colour[RTCount] = { pr::ColourZero, pr::ColourZero, pr::ColourWhite };
			for (int i = 0; i != RTCount; ++i)
				dc->ClearRenderTargetView(m_rtv[i].m_ptr, reset_colour[i]);
			dc->ClearDepthStencilView(m_dsv.m_ptr, D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0U);

			// Set the viewport
			dc->RSSetViewports(1, &m_scene->m_viewport);

			// Set the frame constants and bind them to the shaders
			CBufFrame_GBuffer cb = {};
			SetViewConstants(m_scene->m_view, cb);
			{
				LockT<CBufFrame_GBuffer> lock(dc, m_cbuf_frame, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}
			dc->VSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);
			dc->PSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);

			// Loop over the elements in the draw list
			for (auto& dle : m_drawlist)
			{
				// Bind the shader to the device
				BindShader(dc, &dle, *this);

				// Add the nugget to the device context
				Nugget const& nugget = *dle.m_nugget;
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}

		// Handle main window resize events
		void GBufferCreate::OnEvent(Evt_Resize const& evt)
		{
			// Recreate the g-buffer on resize
			InitGBuffer(evt.m_done);
		}

		// DSLightingPass *****************************************************

		DSLightingPass::DSLightingPass(Scene& scene, bool clear_bb, pr::Colour const& bkgd_colour)
			:RenderStep(scene)
			,m_gbuffer(scene.RStep<GBufferCreate>())
			,m_cbuf_frame()
			,m_unit_quad()
			,m_background_colour(bkgd_colour)
			,m_clear_bb(clear_bb)
			,m_shader(scene.m_rdr->m_shdr_mgr.FindShader(EStockShader::DSLighting))
		{
			m_unit_quad.m_model = scene.m_rdr->m_mdl_mgr.m_unit_quad;

			// Create a constants buffer for constants that only change once per frame
			CBufferDesc cbdesc(sizeof(CBufFrame_DSLighting));
			pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_frame.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "dslighting CBufFrame"));
		}

		// Perform the render step
		void DSLightingPass::ExecuteInternal()
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
			// We need the camera transform to calculate ws_pos from the depth
			CBufFrame_DSLighting cb = {};
			SetViewConstants(m_scene->m_view, cb);
			{
				// Lock and write the constants
				LockT<CBufFrame_DSLighting> lock(dc, m_cbuf_frame, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}

			// Bind the frame constants to the shaders
			dc->VSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);
			dc->PSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);

			// Draw the full screen quad
			{
				Nugget const& nugget = m_unit_quad.m_model->m_nuggets.front();

				// Bind the shader to the device
				DrawListElement dle;
				dle.m_shader   = m_shader.m_ptr;
				dle.m_nugget   = &nugget;
				dle.m_instance = &m_unit_quad.m_base;
				dle.m_sort_key = 0;
				BindShader(dc, &dle, *this);

				// Add the nugget to the device context
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}

		// ForwardRender ******************************************************

		ForwardRender::ForwardRender(Scene& scene, bool clear_bb, pr::Colour const& bkgd_colour)
			:RenderStep(scene)
			,m_cbuf_frame()
			,m_background_colour(bkgd_colour)
			,m_global_light()
			,m_clear_bb(clear_bb)
		{
			// Create a constants buffer that changes per frame
			CBufferDesc cbdesc(sizeof(CBufFrame_Forward));
			pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_frame.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_frame, "CBufFrame_Forward"));

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

		// Perform the render step
		void ForwardRender::ExecuteInternal()
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
			CBufFrame_Forward cb = {};
			SetViewConstants(m_scene->m_view, cb);

			// Lighting
			cb.m_global_lighting    = pr::v4::make(static_cast<float>(m_global_light.m_type),0.0f,0.0f,0.0f);
			cb.m_ws_light_direction = m_global_light.m_direction;
			cb.m_ws_light_position  = m_global_light.m_position;
			cb.m_light_ambient      = m_global_light.m_ambient;
			cb.m_light_colour       = m_global_light.m_diffuse;
			cb.m_light_specular     = pr::Colour::make(m_global_light.m_specular, m_global_light.m_specular_power);
			cb.m_spot               = pr::v4::make(m_global_light.m_inner_cos_angle, m_global_light.m_outer_cos_angle, m_global_light.m_range, m_global_light.m_falloff);

			//SetProjectedTextures(dc, cb, m_proj_tex);

			{// Lock and write the constants
				LockT<CBufFrame_Forward> lock(dc, m_cbuf_frame, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}

			// Bind the frame constants to the shaders
			dc->VSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);
			dc->PSSetConstantBuffers(EConstBuf::FrameConstants, 1, &m_cbuf_frame.m_ptr);

			// Loop over the elements in the draw list
			for (auto& dle : m_drawlist)
			{
				// Bind the shader to the device
				BindShader(dc, &dle, *this);

				// Add the nugget to the device context
				Nugget const& nugget = *dle.m_nugget;
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}
	}
}
