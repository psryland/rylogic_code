//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "view3d/render/state_stack.h"
#include "view3d/shaders/common.h"
#include "pr/view3d/models/model_buffer.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/shaders/shader.h"
#include "pr/view3d/render/drawlist_element.h"
#include "pr/view3d/renderer.h"
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/steps/shadow_map.h"

namespace pr::rdr
{
	StateStack::StateStack(ID3D11DeviceContext1* dc, Scene& scene)
		:m_dc(dc)
		,m_scene(scene)
		,m_init_state()
		,m_pending()
		,m_current()
		,m_tex_default(scene.m_wnd->tex_mgr().FindTexture<Texture2D>(RdrId(EStockTexture::White)))
		,m_dbg()
	{
		// Create the debugging interface
		PR_EXPAND(PR_DBG_RDR, pr::Throw(dc->QueryInterface(__uuidof(ID3DUserDefinedAnnotation), (void**)&m_dbg.m_ptr)));

		// Apply initial state
		ApplyState(m_current, m_init_state, true);
	}
	StateStack::~StateStack()
	{
		// Restore the initial device state
		ApplyState(m_current, m_init_state, true);
	}

	// Apply the current state to the device
	void StateStack::Commit()
	{
		ApplyState(m_current, m_pending, false);
	}

	// Apply the current state to the device
	void StateStack::ApplyState(DeviceState& current, DeviceState& pending, bool force)
	{
		SetupIA(current, pending, force);
		SetupRS(current, pending, force);
		SetupShdrs(current, pending, force);
		SetupTextures(current, pending, force);
		current = pending;
	}

	// Set up the input assembler
	void StateStack::SetupIA(DeviceState& current, DeviceState& pending, bool force)
	{
		// Render nugget v/i ranges are relative to the model buffer, not the model
		// so when we set the v/i buffers we don't need any offsets, the offsets are
		// provided to the DrawIndexed() call

		// Set the input vertex format
		auto current_vs = current.m_shdrs.m_vs;
		auto pending_vs = pending.m_shdrs.m_vs;
		if (current_vs != pending_vs || force)
			m_dc->IASetInputLayout(pending_vs ? pending_vs->IpLayout().m_ptr : nullptr);

		// Bind the v/i buffer to the IA
		if (current.m_mb != pending.m_mb || force)
		{
			if (pending.m_mb != nullptr)
			{
				ID3D11Buffer* buffers[] = {pending.m_mb->m_vb.get()};
				UINT          strides[] = {pending.m_mb->m_vb.m_stride};
				UINT          offsets[] = {0};
				m_dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);

				// Bind the index buffer to the IA
				m_dc->IASetIndexBuffer(pending.m_mb->m_ib.get(), pending.m_mb->m_ib.m_format, 0);
			}
			else
			{
				ID3D11Buffer* buffers[] = {nullptr};
				UINT          strides[] = {0};
				UINT          offsets[] = {0};
				m_dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);
				m_dc->IASetIndexBuffer(nullptr, DXGI_FORMAT_UNKNOWN, 0);
			}
		}

		// Tell the IA what sort of primitives to expect
		if (current.m_topo != pending.m_topo || force)
			m_dc->IASetPrimitiveTopology((D3D11_PRIMITIVE_TOPOLOGY)pending.m_topo);
	}

	// Set up render states
	void StateStack::SetupRS(DeviceState& current, DeviceState& pending, bool force)
	{
		// Combine states in priority order
		pending.m_dsb = m_scene.m_dsb;
		pending.m_rsb = m_scene.m_rsb;
		pending.m_bsb = m_scene.m_bsb;
			
		if (pending.m_dle != nullptr)
		{
			auto& dle = *pending.m_dle;
			pending.m_dsb |= dle.m_nugget->m_dsb;
			pending.m_rsb |= dle.m_nugget->m_rsb;
			pending.m_bsb |= dle.m_nugget->m_bsb;
			
			if (auto inst_dsb = pending.m_dle->m_instance->find<DSBlock>(EInstComp::DSBlock))
				pending.m_dsb |= *inst_dsb;
			if (auto inst_rsb = pending.m_dle->m_instance->find<RSBlock>(EInstComp::RSBlock))
				pending.m_rsb |= *inst_rsb;
			if (auto inst_bsb = pending.m_dle->m_instance->find<BSBlock>(EInstComp::BSBlock))
				pending.m_bsb |= *inst_bsb;
		}

		if (pending.m_rstep != nullptr)
		{
			pending.m_dsb |= pending.m_rstep->m_dsb;
			pending.m_rsb |= pending.m_rstep->m_rsb;
			pending.m_bsb |= pending.m_rstep->m_bsb;
		}

		for (auto& s : pending.m_shdrs.Enumerate())
		{
			if (s == nullptr) continue;
			pending.m_dsb |= s->m_dsb;
			pending.m_rsb |= s->m_rsb;
			pending.m_bsb |= s->m_bsb;
		}

		// Set the depth buffering states
		if (current.m_dsb != pending.m_dsb || force)
		{
			auto ptr = m_scene.m_wnd->ds_mgr().State(pending.m_dsb);
			m_dc->OMSetDepthStencilState(ptr.m_ptr, 0);
		}

		// Set the rasterizer states
		if (current.m_rsb != pending.m_rsb || force)
		{
			auto ptr = m_scene.m_wnd->rs_mgr().State(pending.m_rsb);
			m_dc->RSSetState(ptr.m_ptr);
		}

		// Set the blend states
		if (current.m_bsb != pending.m_bsb || force)
		{
			auto ptr = m_scene.m_wnd->bs_mgr().State(pending.m_bsb);
			m_dc->OMSetBlendState(ptr.m_ptr, 0, 0xFFFFFFFF); // todo, the BlendFactor and SampleMask should really be part of the BSBlock
		}
	}

	// Set up a set of shaders
	void StateStack::SetupShdrs(DeviceState& current, DeviceState& pending, bool force)
	{
		if (current.m_shdrs != pending.m_shdrs || force)
		{
			// Clean up the current shaders
			for (auto& s : current.m_shdrs.Enumerate())
				if (s) s->Cleanup(m_dc);

			if (current.m_shdrs.VS() != pending.m_shdrs.VS())
				m_dc->VSSetShader(pending.m_shdrs.VS(), nullptr, 0);
			if (current.m_shdrs.PS() != pending.m_shdrs.PS())
				m_dc->PSSetShader(pending.m_shdrs.PS(), nullptr, 0);
			if (current.m_shdrs.GS() != pending.m_shdrs.GS())
				m_dc->GSSetShader(pending.m_shdrs.GS(), nullptr, 0);
			if (current.m_shdrs.CS() != pending.m_shdrs.CS())
				m_dc->CSSetShader(pending.m_shdrs.CS(), nullptr, 0);
		}

		// Always call set up on the pending shaders even if they
		// haven't changed. They may have per-nugget set up to do.
		for (auto& s : pending.m_shdrs.Enumerate())
			if (s) s->Setup(m_dc, pending);
	}

	// Set up textures and samplers
	void StateStack::SetupTextures(DeviceState& current, DeviceState& pending, bool force)
	{
		// Bind the diffuse texture
		if (current.m_tex_diffuse != pending.m_tex_diffuse || force)
		{
			ID3D11ShaderResourceView* srv[1]  = {m_tex_default->m_srv.get()};
			ID3D11SamplerState*       samp[1] = {m_tex_default->m_samp.get()};

			if (pending.m_tex_diffuse != nullptr)
			{
				srv[0]  = pending.m_tex_diffuse->m_srv.get();
				samp[0] = pending.m_tex_diffuse->m_samp.get();
			}

			// Diffuse texture hard-coded to slot 0 here
			m_dc->PSSetShaderResources(UINT(hlsl::ERegister::t0), 1, srv);
			m_dc->PSSetSamplers(UINT(hlsl::ERegister::s0), 1, samp);
		}

		// Bind the environment map texture
		if (current.m_tex_envmap != pending.m_tex_envmap || force)
		{
			ID3D11ShaderResourceView* srv[1]  = {nullptr};
			ID3D11SamplerState*       samp[1] = {m_tex_default->m_samp.get()};

			if (pending.m_tex_envmap != nullptr)
			{
				srv[0]  = pending.m_tex_envmap->m_srv.get();
				samp[0] = pending.m_tex_envmap->m_samp.get();
			}
			//if (pending.m_dle != nullptr && pending.m_dle->m_nugget->m_tex_envmap != nullptr)
			//{
			//	srv[0]  = pending.m_dle->m_nugget->m_tex_envmap->m_srv.m_ptr;
			//	samp[0] = pending.m_dle->m_nugget->m_tex_envmap->m_samp.m_ptr;
			//}

			// Env-map texture hard-coded to slot 1 here
			m_dc->PSSetShaderResources(UINT(hlsl::ERegister::t1), 1, srv);
			m_dc->PSSetSamplers(UINT(hlsl::ERegister::s1), 1, samp);
		}

		// Set shadow map texture
		if (current.m_rstep_smap != pending.m_rstep_smap || force)
		{
			ID3D11ShaderResourceView* srv[1]  = {nullptr};
			ID3D11SamplerState*       samp[1] = {m_tex_default->m_samp.get()};
				
			if (pending.m_rstep_smap != nullptr)
			{
				srv[0] = pending.m_rstep_smap->m_srv.get();
				samp[0] = pending.m_rstep_smap->m_samp.get();
			}

			//todo, shadow map texture hard-coded to slot 2 here
			m_dc->PSSetShaderResources(UINT(hlsl::ERegister::t2), 1, srv);
			m_dc->PSSetSamplers(UINT(hlsl::ERegister::s2), 1, samp);
		}
	}

	// State stack frame for a render step
	StateStack::RSFrame::RSFrame(StateStack& ss, RenderStep const& rstep)
		:Frame(ss)
	{
		m_ss.m_pending.m_rstep = &rstep;
		m_ss.m_pending.m_tex_envmap = ss.m_scene.m_global_envmap.get();
	}

	// State stack frame for a DLE
	StateStack::DleFrame::DleFrame(StateStack& ss, DrawListElement const& dle)
		:Frame(ss)
	{
		auto& nugget = *dle.m_nugget;

		// Save the DLE
		m_ss.m_pending.m_dle = &dle;

		// Get the shaders to use for this nugget.
		// Pass them to the renderer to override or provide defaults
		m_ss.m_pending.m_shdrs = nugget.m_smap[m_ss.m_pending.m_rstep->GetId()];
		m_ss.m_pending.m_rstep->ConfigShaders(m_ss.m_pending.m_shdrs, nugget.m_topo);

		// IA states
		m_ss.m_pending.m_mb   = dle.m_nugget->m_model_buffer;
		m_ss.m_pending.m_topo = dle.m_nugget->m_topo;

		// Texture
		m_ss.m_pending.m_tex_diffuse = dle.m_nugget->m_tex_diffuse.get();
	}

	// State stack frame for shadow map texture
	StateStack::SmapFrame::SmapFrame(StateStack& ss, ShadowMap const* rstep)
		:Frame(ss)
	{
		ss.m_pending.m_rstep_smap = rstep;
	}

	// State stack frame for pushing a render target/depth buffer
	StateStack::RTFrame::RTFrame(StateStack& ss, ID3D11RenderTargetView* rtv, ID3D11DepthStencilView* dsv)
		:RTFrame(ss, 1U, &rtv, dsv)
	{}
	StateStack::RTFrame::RTFrame(StateStack& ss, UINT count, ID3D11RenderTargetView*const* rtv, ID3D11DepthStencilView* dsv)
		:Frame(ss)
		,m_count(count)
		,m_rtv()
		,m_dsv()
	{
		// Save the current RT, then set the given RT (only need to save 'rtv_count' because those are all we're changing)
		m_ss.m_dc->OMGetRenderTargets(m_count, &m_rtv[0], &m_dsv);
		m_ss.m_dc->OMSetRenderTargets(count, rtv, dsv);
	}
	StateStack::RTFrame::~RTFrame()
	{
		// Restore RT
		m_ss.m_dc->OMSetRenderTargets(m_count, &m_rtv[0], m_dsv);
	}

	// State stack frame for pushing unordered access views
	StateStack::UAVFrame::UAVFrame(StateStack& ss, UINT first_uav, ID3D11UnorderedAccessView* uav, UINT initial_count)
		:UAVFrame(ss, first_uav, 1, &uav, &initial_count)
	{}
	StateStack::UAVFrame::UAVFrame(StateStack& ss, UINT first, UINT count, ID3D11UnorderedAccessView*const* uav, UINT const* initial_counts)
		:Frame(ss)
		,m_first(first)
		,m_count(count)
		,m_uav()
		,m_initial_counts()
	{
		// Save the current UAVs, then set the given RT (only need to preserve the ones we're replacing)
		m_ss.m_dc->OMGetRenderTargetsAndUnorderedAccessViews(0U, nullptr, nullptr, m_first, m_count, &m_uav[0]);
		m_ss.m_dc->OMSetRenderTargetsAndUnorderedAccessViews(D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL, nullptr, nullptr, first, count, uav, initial_counts);
		std::fill(&m_initial_counts[0], &m_initial_counts[0] + _countof(m_initial_counts), UINT(-1));
	}
	StateStack::UAVFrame::~UAVFrame()
	{
		// Restore UAVs
		m_ss.m_dc->OMSetRenderTargetsAndUnorderedAccessViews(D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL, nullptr, nullptr, m_first, m_count, &m_uav[0], &m_initial_counts[0]);
	}

	// State stack frame for pushing Stream Out stage targets
	StateStack::SOFrame::SOFrame(StateStack& ss, ID3D11Buffer* target, UINT offset)
		:SOFrame(ss, 1U, &target, &offset)
	{}
	StateStack::SOFrame::SOFrame(StateStack& ss, UINT num_buffers, ID3D11Buffer*const* targets, UINT const* offsets)
		:Frame(ss)
	{
		m_ss.m_dc->SOSetTargets(num_buffers, targets, offsets);
	}
	StateStack::SOFrame::~SOFrame()
	{
		UINT offsets = 0U;
		ID3D11Buffer* targets = {nullptr};
		m_ss.m_dc->SOSetTargets(1U, &targets, &offsets);
	}
}
