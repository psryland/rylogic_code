//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/render_step.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"

namespace pr
{
	namespace rdr
	{
		BaseShader::BaseShader(ShaderManager* mgr)
			:m_iplayout()
			,m_vs()
			,m_ps()
			,m_gs()
			,m_hs()
			,m_ds()
			,m_cbuf()
			,m_id()
			,m_geom_mask()
			,m_mgr(mgr)
			,m_sort_id()
			,m_bsb()
			,m_rsb()
			,m_dsb()
			,m_last_modified()
			,m_name()
		{}

		// Ref counting cleanup function
		void BaseShader::RefCountZero(pr::RefCount<BaseShader>* doomed)
		{
			BaseShader* shdr = static_cast<BaseShader*>(doomed);
			shdr->m_mgr->Delete(shdr);
		}

		// Setup the input assembler for 'nugget'
		void SetupIA(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle)
		{
			ModelBuffer const& mb = *dle.m_nugget->m_model_buffer.m_ptr;

			// Render nuggets v/i ranges are relative to the model buffer, not the model
			// so when we set the v/i buffers we don't need any offsets, the offsets are
			// provided to the DrawIndexed() call

			// Bind the vertex buffer to the IA
			ID3D11Buffer* buffers[] = {mb.m_vb.m_ptr};
			UINT          strides[] = {mb.m_vb.m_stride};
			UINT          offsets[] = {0};
			dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);

			// Bind the index buffer to the IA
			dc->IASetIndexBuffer(mb.m_ib.m_ptr, mb.m_ib.m_format, 0);

			// Tell the IA what sort of primitives to expect
			dc->IASetPrimitiveTopology((D3D11_PRIMITIVE_TOPOLOGY)dle.m_nugget->m_topo.value);
		}

		// Set the depth buffering states
		void SetupDS(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
		{
			auto& ds_mgr = rstep.m_scene->m_rdr->m_ds_mgr;
			auto inst_dsb = dle.m_instance->find<DSBlock>(EInstComp::DSBlock);

			// Combine states in priority order
			DSBlock dsb;
			dsb  = dle.m_shader->m_dsb;     // default states from the shader
			dsb |= dle.m_nugget->m_dsb;     // default states from the model nugget
			dsb |= rstep.m_dsb;             // render step state overrides
			if (inst_dsb) dsb |= *inst_dsb; // instance specific overrides

			auto ptr = ds_mgr.State(dsb);
			dc->OMSetDepthStencilState(ptr.m_ptr, 0);
		}

		// Set the rasterizer states
		void SetupRS(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
		{
			auto& rs_mgr = rstep.m_scene->m_rdr->m_rs_mgr;
			auto inst_rsb = dle.m_instance->find<RSBlock>(EInstComp::RSBlock);

			// Combine states in priority order
			RSBlock rsb;
			rsb  = dle.m_shader->m_rsb;     // default states from the shader
			rsb |= dle.m_nugget->m_rsb;     // default states from the model nugget
			rsb |= rstep.m_rsb;             // render step state overrides
			if (inst_rsb) rsb |= *inst_rsb; // instance specific overrides

			auto ptr = rs_mgr.State(rsb);
			dc->RSSetState(ptr.m_ptr);
		}

		// Set the blend states
		void SetupBS(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
		{
			auto& bs_mgr = rstep.m_scene->m_rdr->m_bs_mgr;
			auto inst_bsb = dle.m_instance->find<BSBlock>(EInstComp::BSBlock);

			// Combine states in priority order
			BSBlock bsb;
			bsb  = dle.m_shader->m_bsb;     // default states from the shader
			bsb |= dle.m_nugget->m_bsb;     // default states from the draw method
			bsb |= rstep.m_bsb;             // render step state overrides
			if (inst_bsb) bsb |= *inst_bsb; // instance specific overrides

			auto ptr = bs_mgr.State(bsb);
			dc->OMSetBlendState(ptr.m_ptr, 0, 0xFFFFFFFF); // todo, the BlendFactor and SampleMask should really be part of the BSBlock
		}

		// Bind the shader to the device context in preparation for rendering
		void BindShader(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const* dle, RenderStep const& rstep)
		{
			if (!dle)
				return; // todo

			auto& shdr = *dle->m_shader;

			// Call the custom binding function provided when the shader was created
			shdr.m_setup_func(dc, *dle, rstep);

			// Setup the input assembler
			dc->IASetInputLayout(shdr.m_iplayout.m_ptr);
			SetupIA(dc, *dle);

			// Set the depth buffering states
			SetupDS(dc, *dle, rstep);

			// Set the raster states
			SetupRS(dc, *dle, rstep);

			// Set the blend states
			SetupBS(dc, *dle, rstep);

			// Bind the shaders (passing null disables the shader)
			dc->VSSetShader(shdr.m_vs.m_ptr, 0, 0);
			dc->PSSetShader(shdr.m_ps.m_ptr, 0, 0);
			dc->GSSetShader(shdr.m_gs.m_ptr, 0, 0);
			dc->HSSetShader(shdr.m_hs.m_ptr, 0, 0);
			dc->DSSetShader(shdr.m_ds.m_ptr, 0, 0);
		}
	}
}