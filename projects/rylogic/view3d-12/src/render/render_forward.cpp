//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/render/render_forward.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/shaders/shader_registers.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/pipe_state.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12
{
	RenderForward::RenderForward(Scene& scene)
		: RenderStep(ERenderStep::RenderForward, scene)
		, m_shader(scene.D3DDevice())
	{
		// Create the default PSO description
		m_default_pipe_state = D3D12_GRAPHICS_PIPELINE_STATE_DESC {
			.pRootSignature = m_shader.Signature.get(),
			.VS = m_shader.Code.VS,
			.PS = m_shader.Code.PS,
			.DS = m_shader.Code.DS,
			.HS = m_shader.Code.HS,
			.GS = m_shader.Code.GS,
			.StreamOutput = StreamOutputDesc{},
			.BlendState = BlendStateDesc{},
			.SampleMask = UINT_MAX,
			.RasterizerState = RasterStateDesc{},
			.DepthStencilState = DepthStateDesc{},
			.InputLayout = Vert::LayoutDesc(),
			.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
			.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
			.NumRenderTargets = 1U,
			.RTVFormats = {
				DXGI_FORMAT_B8G8R8A8_UNORM,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
				DXGI_FORMAT_UNKNOWN,
			},
			.DSVFormat = DXGI_FORMAT_D32_FLOAT,
			.SampleDesc = MultiSamp{},
			.NodeMask = 0U,
			.CachedPSO = {
				.pCachedBlob = nullptr,
				.CachedBlobSizeInBytes = 0U,
			},
			.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
		};
	}

	// Add model nuggets to the draw list for this render step.
	void RenderForward::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist)
	{
		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
		{
			// Ignore if flagged as not visible
			if (AllSet(nug.m_nflags, ENuggetFlag::Hidden))
				continue;

			// Don't add alpha back faces when using 'Points' fill mode
			if (nug.m_id == Nugget::AlphaNuggetId && nug.FillMode() == EFillMode::Points)
				continue;

			// If not visible for other reasons, don't render but add child nuggets.
			if (nug.Visible())
			{
				// Create the combined sort key for this nugget
				auto sk = nug.m_sort_key;
				auto sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);
				if (sko) sk = sko->Combine(sk);

				// Set the texture id part of the key if not set already
				if (!AnySet(sk, SortKey::TextureIdMask) && nug.m_tex_diffuse != nullptr)
					sk = SetBits(sk, SortKey::TextureIdMask, nug.m_tex_diffuse->SortId() << SortKey::TextureIdOfs);

				// Set the shader id part of the key if not set already
				if (!AnySet(sk, SortKey::ShaderIdMask))
				{
					#if 0 //todo
					auto shdr_id = 0;
					for (auto& shdr : m_smap[rstep].Enumerate())
					{
						if (shdr == nullptr) continue;
						shdr_id = shdr_id*13 ^ shdr->m_sort_id; // hash the sort ids together
					}
					sk |= (shdr_id << SortKey::ShaderIdOfs) & SortKey::ShaderIdMask;
					#endif
				}

				// Add an element to the drawlist
				DrawListElement dle = {
					.m_sort_key = sk,
					.m_nugget = &nug,
					.m_instance = &inst,
				};
				drawlist.push_back(dle);
			}

			// Recursively add dependent nuggets
			AddNuggets(inst, nug.m_nuggets, drawlist);
		}
	}

	// Perform the render step
	void RenderForward::ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list)
	{
		// Sort the draw list if needed
		SortIfNeeded();

		// Set the pipeline for this render step
		cmd_list->SetGraphicsRootSignature(m_shader.Signature.get());

		// Bind the descriptor heaps
		auto des_heaps = {wnd().m_heap_srv.get(), wnd().m_heap_samp.get()};
		cmd_list->SetDescriptorHeaps(s_cast<UINT>(des_heaps.size()), des_heaps.begin());

		// Get the back buffer view handle and set the back buffer as the render target.
		cmd_list->OMSetRenderTargets(1, &bb.m_rtv, FALSE, &bb.m_dsv);

		// Clear the render target to the background colour
		if (scn().m_bkgd_colour != ColourZero)
		{
			cmd_list->ClearRenderTargetView(bb.m_rtv, scn().m_bkgd_colour.arr, 0, nullptr);
			cmd_list->ClearDepthStencilView(bb.m_dsv, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0U, 0, nullptr);
		}

		// Set the viewport
		auto const& vp = scn().m_viewport;
		cmd_list->RSSetViewports(1, &vp);
		cmd_list->RSSetScissorRects(s_cast<UINT>(vp.m_clip.size()), vp.m_clip.data());

		#if 0 // todo
		// Check if shadows are enabled
		auto smap_rstep = scn().FindRStep<ShadowMap>();
		StateStack::SmapFrame smap_frame(ss, smap_rstep);
		#endif

		// Setup for the frame
		m_shader.Setup(cmd_list, m_cbuf_upload, scn(), nullptr);

		//if (scn().m_global_envmap != nullptr)
		//	cmd_list->SetGraphicsRootDescriptorTable(((UINT)ERootParam::EnvMap, );

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			// Something not rendering?
			//  - Check the tint for the nugget isn't 0x00000000.
			// Tips:
			//  - To uniquely identify an instance in a shader for debugging, set the Instance Id (cb1.m_flags.w)
			//    Then in the shader, use: if (m_flags.w == 1234) ...
			auto const& nugget = *dle.m_nugget;
			auto desc = m_default_pipe_state;

			// Set pipeline state
			desc.PrimitiveTopologyType = To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(nugget.m_topo);
			cmd_list->IASetPrimitiveTopology(To<D3D12_PRIMITIVE_TOPOLOGY>(nugget.m_topo));
			cmd_list->IASetVertexBuffers(0U, 1U, &nugget.m_model->m_vb_view);
			cmd_list->IASetIndexBuffer(&nugget.m_model->m_ib_view);

			// Setup for the nugget
			m_shader.Setup(cmd_list, m_cbuf_upload, scn(), &dle);

			// Bind textures to the pipeline
			auto tex = nugget.m_tex_diffuse != nullptr
				? nugget.m_tex_diffuse
				: rdr().res_mgr().FindTexture(EStockTexture::White);

			auto handle = wnd().m_heap_srv.Add(tex->m_srv);
			cmd_list->SetGraphicsRootDescriptorTable((UINT)shaders::fwd::ERootParam::DiffTexture, handle);

			// Apply scene pipe state overrides
			for (auto& ps : scn().m_pso)
				desc.Apply(ps);

			// Apply nugget pipe state overrides
			for (auto& ps : nugget.m_pso)
				desc.Apply(ps);

			// Apply instance pipe state overrides
			for (auto& ps : GetPipeStates(*dle.m_instance))
				desc.Apply(ps);

			// Apply nugget shader overrides
			for (auto& shdr : nugget.m_shaders)
			{
				// Ignore shader overrides for other render steps
				if (shdr.m_rdr_step != Id) continue;
				auto& shader = *shdr.m_shader.get();

				// Setup the shader parameters
				shader.Setup(cmd_list, m_cbuf_upload, scn(), &dle);

				// Update the pipe state with the shader byte code
				if (shader.Signature) desc.Apply(PSO<EPipeState::RootSignature>(shader.Signature.get()));
				if (shader.Code.VS) desc.Apply(PSO<EPipeState::VS>(shader.Code.VS));
				if (shader.Code.PS) desc.Apply(PSO<EPipeState::PS>(shader.Code.PS));
				if (shader.Code.DS) desc.Apply(PSO<EPipeState::DS>(shader.Code.DS));
				if (shader.Code.HS) desc.Apply(PSO<EPipeState::HS>(shader.Code.HS));
				if (shader.Code.GS) desc.Apply(PSO<EPipeState::GS>(shader.Code.GS));
			}

			// Draw the nugget **** 
			DrawNugget(nugget, desc, cmd_list);
		}
	}

	// Draw a single nugget
	void RenderForward::DrawNugget(Nugget const& nugget, PipeStateDesc& desc, ID3D12GraphicsCommandList* cmd_list)
	{
		// Render solid or wireframe nuggets
		auto fill_mode = nugget.FillMode();
		if (fill_mode == EFillMode::Default ||
			fill_mode == EFillMode::Solid ||
			fill_mode == EFillMode::Wireframe ||
			fill_mode == EFillMode::SolidWire)
		{
			cmd_list->SetPipelineState(m_pipe_state_pool.Get(desc));
			if (nugget.m_irange.empty())
			{
				cmd_list->DrawInstanced(
					s_cast<UINT>(nugget.m_vrange.size()), 1U,
					s_cast<UINT>(nugget.m_vrange.m_beg), 0U);
			}
			else
			{
				cmd_list->DrawIndexedInstanced(
					s_cast<UINT>(nugget.m_irange.size()), 1U,
					s_cast<UINT>(nugget.m_irange.m_beg), 0, 0U);
			}
		}

		// Render wire frame over solid for 'SolidWire' mode
		if (!nugget.m_irange.empty() && fill_mode == EFillMode::SolidWire && (
			nugget.m_topo == ETopo::TriList ||
			nugget.m_topo == ETopo::TriListAdj ||
			nugget.m_topo == ETopo::TriStrip ||
			nugget.m_topo == ETopo::TriStripAdj))
		{
			// Change the pipe state to wireframe
			auto prev_fill_mode = desc.RasterizerState.FillMode;
			desc.RasterizerState.FillMode = D3D12_FILL_MODE_WIREFRAME;
			desc.BlendState.RenderTarget[0].BlendEnable = FALSE;
			cmd_list->SetPipelineState(m_pipe_state_pool.Get(desc));

			cmd_list->DrawIndexedInstanced(
				s_cast<UINT>(nugget.m_irange.size()), 1U,
				s_cast<UINT>(nugget.m_irange.m_beg), 0, 0U);

			// Restore it
			desc.RasterizerState.FillMode = prev_fill_mode;
			//ps.Clear(EPipeState::BlendEnable0);
		}

		// Render points for 'Points' mode
		if (fill_mode == EFillMode::Points)
		{
			// Change the pipe state to point list
			desc.PrimitiveTopologyType = To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(ETopo::PointList);
			desc.GS = wnd().m_diag.m_gs_fillmode_points->Code.GS;
			//todo scn().m_diag.m_gs_fillmode_points->Setup();
			cmd_list->SetPipelineState(m_pipe_state_pool.Get(desc));

			cmd_list->DrawInstanced(
				s_cast<UINT>(nugget.m_vrange.size()), 1U,
				s_cast<UINT>(nugget.m_vrange.m_beg), 0U);
		}
	}
}
