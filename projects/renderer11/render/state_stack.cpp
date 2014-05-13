//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
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

namespace pr
{
	namespace rdr
	{
		// Setup the input assembler
		void SetupIA(DeviceState& current, DeviceState& pending, D3DPtr<ID3D11DeviceContext>& dc, bool force)
		{
			// Render nugget v/i ranges are relative to the model buffer, not the model
			// so when we set the v/i buffers we don't need any offsets, the offsets are
			// provided to the DrawIndexed() call

			// Set the input vertex format
			auto current_vs = current.m_shdrs.find(EShaderType::VS);
			auto pending_vs = pending.m_shdrs.find(EShaderType::VS);
			if (current_vs != pending_vs || force)
				dc->IASetInputLayout(pending_vs ? pending_vs->m_iplayout.m_ptr : nullptr);

			// Bind the v/i buffer to the IA
			if (current.m_mb != pending.m_mb || force)
			{
				if (pending.m_mb != nullptr)
				{
					ID3D11Buffer* buffers[] = {pending.m_mb->m_vb.m_ptr};
					UINT          strides[] = {pending.m_mb->m_vb.m_stride};
					UINT          offsets[] = {0};
					dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);

					// Bind the index buffer to the IA
					dc->IASetIndexBuffer(pending.m_mb->m_ib.m_ptr, pending.m_mb->m_ib.m_format, 0);
				}
				else
				{
					dc->IASetVertexBuffers(0, 0, nullptr, nullptr, nullptr);
					dc->IASetIndexBuffer(nullptr, DXGI_FORMAT_UNKNOWN, 0);
				}
			}

			// Tell the IA what sort of primitives to expect
			if (current.m_topo != pending.m_topo || force)
				dc->IASetPrimitiveTopology((D3D11_PRIMITIVE_TOPOLOGY)pending.m_topo.value);
		}

		// Setup render states
		void SetupRS(DeviceState& current, DeviceState& pending, D3DPtr<ID3D11DeviceContext>& dc, pr::Renderer& rdr, bool force)
		{
			// Set the depth buffering states
			if (current.m_dsb != pending.m_dsb || force)
			{
				auto ptr = rdr.m_ds_mgr.State(pending.m_dsb);
				dc->OMSetDepthStencilState(ptr.m_ptr, 0);
			}

			// Set the rasterizer states
			if (current.m_rsb != pending.m_rsb || force)
			{
				auto ptr = rdr.m_rs_mgr.State(pending.m_rsb);
				dc->RSSetState(ptr.m_ptr);
			}

			// Set the blend states
			if (current.m_bsb != pending.m_bsb || force)
			{
				auto ptr = rdr.m_bs_mgr.State(pending.m_bsb);
				dc->OMSetBlendState(ptr.m_ptr, 0, 0xFFFFFFFF); // todo, the BlendFactor and SampleMask should really be part of the BSBlock
			}
		}

		// Setup a shader
		void SetupShdrs(DeviceState& current, DeviceState& pending, D3DPtr<ID3D11DeviceContext>& dc, bool force)
		{
			if (current.m_shdrs != pending.m_shdrs || force)
			{
				for (auto& s : current.m_shdrs)
					s->Cleanup(dc);

				dc->VSSetShader(pending.m_shdrs.find_dx<EShaderType::VS>().m_ptr, nullptr, 0);
				dc->HSSetShader(pending.m_shdrs.find_dx<EShaderType::HS>().m_ptr, nullptr, 0);
				dc->DSSetShader(pending.m_shdrs.find_dx<EShaderType::DS>().m_ptr, nullptr, 0);
				dc->GSSetShader(pending.m_shdrs.find_dx<EShaderType::GS>().m_ptr, nullptr, 0);
				dc->PSSetShader(pending.m_shdrs.find_dx<EShaderType::PS>().m_ptr, nullptr, 0);
			}

			// Always call setup on the pending shaders even if they haven't changed
			// They may have per-nugget setup to do
			for (auto& s : pending.m_shdrs)
				s->Setup(dc, pending);
		}

		// Apply the current state to the device
		void ApplyState(DeviceState& current, DeviceState& pending, D3DPtr<ID3D11DeviceContext>& dc, pr::Renderer& rdr, bool force = false)
		{
			SetupIA(current, pending, dc, force);
			SetupRS(current, pending, dc, rdr, force);
			SetupShdrs(current, pending, dc, force);

			current = pending;
		}

		StateStack::StateStack(D3DPtr<ID3D11DeviceContext> dc, Scene& scene)
			:m_dc(dc)
			,m_scene(scene)
			,m_init_state()
			,m_pending()
			,m_current()
		{
			ApplyState(m_current, m_init_state, dc, *scene.m_rdr, true);
		}
		StateStack::~StateStack()
		{
			// Restore the initial device state
			ApplyState(m_current, m_init_state, m_dc, *m_scene.m_rdr);
		}

		// Apply the current state to the device
		void StateStack::Commit()
		{
			ApplyState(m_current, m_pending, m_dc, *m_scene.m_rdr);
		}

		// Default state
		DeviceState::DeviceState()
			:m_rstep()
			,m_dle()
			,m_mb()
			,m_topo(EPrim::PointList)
			,m_dsb()
			,m_rsb()
			,m_bsb()
			,m_shdrs()
		{}

		// State stack frame for a render step
		StateStack::RSFrame::RSFrame(StateStack& ss, RenderStep const& rstep)
			:Frame(ss)
		{
			m_ss.m_pending.m_rstep = &rstep;
		}

		// State stack frame for a dle
		StateStack::DleFrame::DleFrame(StateStack& ss, DrawListElement const& dle)
			:Frame(ss)
		{
			// Save the dle
			m_ss.m_pending.m_dle = &dle;

			// Get the shaders involved
			m_ss.m_pending.m_shdrs.clear();
			for (auto& shdr : dle.m_nugget->m_sset)
			{
				if (!shdr->IsUsedBy(m_ss.m_pending.m_rstep->GetId())) continue;
				m_ss.m_pending.m_shdrs.push_back(shdr);
			}
			
			// Sort them into execution order so the render states get merged in a fixed order
			std::sort(std::begin(m_ss.m_pending.m_shdrs), std::end(m_ss.m_pending.m_shdrs), [](ShaderPtr lhs,ShaderPtr rhs){ return lhs->m_shdr_type < rhs->m_shdr_type; });
			PR_ASSERT(PR_DBG_RDR, m_ss.m_pending.m_shdrs.find(EShaderType::VS) != nullptr, "Nugget has no vertex shader");
			PR_ASSERT(PR_DBG_RDR, m_ss.m_pending.m_shdrs.find(EShaderType::PS) != nullptr, "Nugget has no pixel shader");

			// IA states
			m_ss.m_pending.m_mb       = dle.m_nugget->m_model_buffer;
			m_ss.m_pending.m_topo     = dle.m_nugget->m_topo;

			auto inst_dsb = dle.m_instance->find<DSBlock>(EInstComp::DSBlock);
			auto inst_rsb = dle.m_instance->find<RSBlock>(EInstComp::RSBlock);
			auto inst_bsb = dle.m_instance->find<BSBlock>(EInstComp::BSBlock);

			// DS states
			DSBlock dsb;                          // Combine states in priority order
			for (auto& s : m_ss.m_pending.m_shdrs)
				dsb |= s->m_dsb;                  // default states from the shaders
			dsb |= dle.m_nugget->m_dsb;           // default states from the model nugget
			if (m_ss.m_pending.m_rstep)           // render step state overrides
				dsb |= m_ss.m_pending.m_rstep->m_dsb;
			if (inst_dsb)                         // instance specific overrides
				dsb |= *inst_dsb;
			m_ss.m_pending.m_dsb = dsb;

			// RS states
			RSBlock rsb;                          // Combine states in priority order
			for (auto& s : m_ss.m_pending.m_shdrs)
				rsb |= s->m_rsb;                  // default states from the shader
			rsb |= dle.m_nugget->m_rsb;           // default states from the model nugget
			if (m_ss.m_pending.m_rstep)           // render step state overrides
				rsb |= m_ss.m_pending.m_rstep->m_rsb;
			if (inst_rsb)                         // instance specific overrides
				rsb |= *inst_rsb;
			m_ss.m_pending.m_rsb = rsb;

			// BS states
			BSBlock bsb;                          // Combine states in priority order
			for (auto& s : m_ss.m_pending.m_shdrs)
				bsb |= s->m_bsb;                  // default states from the shader
			bsb |= dle.m_nugget->m_bsb;           // default states from the draw method
			if (m_ss.m_pending.m_rstep)           // render step state overrides
				bsb |= m_ss.m_pending.m_rstep->m_bsb;
			if (inst_bsb)                         // instance specific overrides
				bsb |= *inst_bsb;
			m_ss.m_pending.m_bsb = bsb;
		}
	}
}
