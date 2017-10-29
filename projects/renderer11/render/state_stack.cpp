//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "renderer11/render/state_stack.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/steps/shadow_map.h"

namespace pr
{
	namespace rdr
	{
		StateStack::StateStack(ID3D11DeviceContext* dc, Scene& scene)
			:m_dc(dc)
			,m_scene(scene)
			,m_init_state()
			,m_pending()
			,m_current()
			,m_tex_default(scene.m_wnd->tex_mgr().FindTexture(EStockTexture::White))
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
					ID3D11Buffer* buffers[] = {pending.m_mb->m_vb.m_ptr};
					UINT          strides[] = {pending.m_mb->m_vb.m_stride};
					UINT          offsets[] = {0};
					m_dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);

					// Bind the index buffer to the IA
					m_dc->IASetIndexBuffer(pending.m_mb->m_ib.m_ptr, pending.m_mb->m_ib.m_format, 0);
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

		// Set up a shader
		void StateStack::SetupShdrs(DeviceState& current, DeviceState& pending, bool force)
		{
			if (current.m_shdrs != pending.m_shdrs || force)
			{
				for (auto& s : current.m_shdrs.Enumerate())
					if (s) s->Cleanup(m_dc);

				if (current.m_shdrs.VS() != pending.m_shdrs.VS()) m_dc->VSSetShader(pending.m_shdrs.VS().m_ptr, nullptr, 0);
				if (current.m_shdrs.GS() != pending.m_shdrs.GS()) m_dc->GSSetShader(pending.m_shdrs.GS().m_ptr, nullptr, 0);
				if (current.m_shdrs.PS() != pending.m_shdrs.PS()) m_dc->PSSetShader(pending.m_shdrs.PS().m_ptr, nullptr, 0);
			}

			// Always call set up on the pending shaders even if they
			// haven't changed. They may have per-nugget set up to do
			for (auto& s : pending.m_shdrs.Enumerate())
				if (s) s->Setup(m_dc, pending);
		}

		// Set up textures and samplers
		void StateStack::SetupTextures(DeviceState& current, DeviceState& pending, bool force)
		{
			// Bind the diffuse texture
			if (current.m_tex_diffuse != pending.m_tex_diffuse || force)
			{
				ID3D11ShaderResourceView* srv[1]  = {m_tex_default->m_srv.m_ptr};
				ID3D11SamplerState*       samp[1] = {m_tex_default->m_samp.m_ptr};

				if (pending.m_dle != nullptr && pending.m_dle->m_nugget->m_tex_diffuse != nullptr)
				{
					srv[0]  = pending.m_dle->m_nugget->m_tex_diffuse->m_srv.m_ptr;
					samp[0] = pending.m_dle->m_nugget->m_tex_diffuse->m_samp.m_ptr;
				}

				//todo, diffuse texture hardcored to slot 0 here
				m_dc->PSSetShaderResources(0, 1, srv);
				m_dc->PSSetSamplers(0, 1, samp);
			}

			// Set shadow map texture
			if (current.m_rstep_smap != pending.m_rstep_smap || force)
			{
				ID3D11ShaderResourceView* srv[1]  = {nullptr};
				ID3D11SamplerState*       samp[1] = {m_tex_default->m_samp.m_ptr};
				
				if (pending.m_rstep_smap != nullptr)
				{
					srv[0] = pending.m_rstep_smap->m_srv.m_ptr;
					samp[0] = pending.m_rstep_smap->m_samp.m_ptr;
				}

				//todo, shadow map texture hardcored to slot 1 here
				m_dc->PSSetShaderResources(1, 1, srv);
				m_dc->PSSetSamplers(1, 1, samp);
			}
		}

		// State stack frame for a render step
		StateStack::RSFrame::RSFrame(StateStack& ss, RenderStep const& rstep)
			:Frame(ss)
		{
			m_ss.m_pending.m_rstep = &rstep;
		}

		// State stack frame for a DLE
		StateStack::DleFrame::DleFrame(StateStack& ss, DrawListElement const& dle)
			:Frame(ss)
		{
			// Save the DLE
			m_ss.m_pending.m_dle = &dle;

			// Get the shaders involved
			m_ss.m_pending.m_shdrs = dle.m_nugget->m_smap[m_ss.m_pending.m_rstep->GetId()];

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
	}
}
