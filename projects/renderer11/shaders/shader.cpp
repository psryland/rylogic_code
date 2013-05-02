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
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"

using namespace pr::rdr;

pr::rdr::BaseShader::BaseShader(ShaderManager* mgr)
:m_iplayout()
,m_cbuf()
,m_vs()
,m_ps()
,m_gs()
,m_hs()
,m_ds()
,m_id()
,m_geom_mask()
,m_mgr(mgr)
,m_sort_id()
,m_bsb()
,m_rsb()
,m_last_modified()
,m_name()
{}

// Setup the input assembler for 'nugget'
void SetupIA(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget)
{
	ModelBuffer const& mb = *nugget.m_model_buffer.m_ptr;

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
	dc->IASetPrimitiveTopology((D3D11_PRIMITIVE_TOPOLOGY)nugget.m_prim_topo.value);
}

// Set the rasterizer states
void SetupRS(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene) 
{
	auto& rs_mgr = scene.m_rdr->m_rs_mgr;
	auto& draw = nugget.m_draw;
	RSBlock const* inst_rsb = inst.find<RSBlock>(EInstComp::RSBlock);

	// Combine the rasterizer state
	RSBlock rsb = scene.m_rsb;      // scene is the lowest priority
	rsb |= draw.m_shader->m_rsb;    // shader next
	rsb |= draw.m_rsb;              // draw method overridden states
	if (inst_rsb) rsb |= *inst_rsb; // instance states

	auto ptr = rs_mgr.State(rsb);
	dc->RSSetState(ptr.m_ptr);
}

// Set the output merger state
void SetupOM(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
{
	auto& bs_mgr = scene.m_rdr->m_bs_mgr;
	auto& draw = nugget.m_draw;
	BSBlock const* inst_bsb = inst.find<BSBlock>(EInstComp::BSBlock);

	BSBlock bsb = scene.m_bsb;      // scene is the lowest priority
	bsb |= draw.m_shader->m_bsb;    // shader next
	bsb |= draw.m_bsb;              // draw method overridden states
	if (inst_bsb) bsb |= *inst_bsb; // instance states

	auto ptr = bs_mgr.State(bsb);
	dc->OMSetBlendState(ptr.m_ptr, 0, 0xFFFFFFFF); // todo, the BlendFactor and SampleMask should really be part of the BSBlock
}

// Bind the shader to the device context in preparation for rendering
void pr::rdr::BaseShader::Bind(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
{
	// Call the custom binding function provided when the shader was created
	m_setup_func(dc, nugget, inst, scene);

	// Setup the input assembler
	dc->IASetInputLayout(m_iplayout.m_ptr);
	SetupIA(dc, nugget);

	// Set the raster state
	SetupRS(dc, nugget, inst, scene);

	// Set the output merger
	SetupOM(dc, nugget, inst, scene);

	// Bind the shaders (passing null disables the shader)
	dc->VSSetShader(m_vs.m_ptr, 0, 0);
	dc->PSSetShader(m_ps.m_ptr, 0, 0);
	dc->GSSetShader(m_gs.m_ptr, 0, 0);
	dc->HSSetShader(m_hs.m_ptr, 0, 0);
	dc->DSSetShader(m_ds.m_ptr, 0, 0);
}

// Ref counting cleanup function
void pr::rdr::BaseShader::RefCountZero(pr::RefCount<BaseShader>* doomed)
{
	pr::rdr::BaseShader* shdr = static_cast<pr::rdr::BaseShader*>(doomed);
	shdr->m_mgr->Delete(shdr);
}

