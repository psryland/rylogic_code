//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/render/scene.h"

using namespace pr::rdr;

pr::rdr::BaseShader::BaseShader(ShaderManager* mgr)
:m_iplayout()
,m_cbuf()
,m_vs()
,m_ps()
,m_gs()
,m_hs()
,m_ds()
,m_rs()
,m_id()
,m_geom_mask()
,m_mgr(mgr)
,m_name()
,m_sort_id()
,m_last_modified()
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
	dc->IASetPrimitiveTopology(nugget.m_prim_topo);
}

// Set the rasterizer states
void SetupRS(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, Scene const& scene) 
{
	auto& draw = nugget.m_draw;
	if (draw.m_rstates != 0)
		dc->RSSetState(draw.m_rstates.m_ptr);
	else if (draw.m_shader->m_rs != 0)
		dc->RSSetState(draw.m_shader->m_rs.m_ptr);
	else if (scene.m_rs != 0)
		dc->RSSetState(scene.m_rs.m_ptr);
	else
		dc->RSSetState(0);
}

// Bind the shader to the device context in preparation for rendering
void pr::rdr::BaseShader::Bind(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
{
	// Call the custom binding function provided when the shader was created
	m_setup_func(dc, nugget, inst, scene);

	// Setup the input assembler
	SetupIA(dc, nugget);

	// Set the raster state
	SetupRS(dc, nugget, scene);

	// Bind input format
	dc->IASetInputLayout(m_iplayout.m_ptr);

	// Bind the shaders (passing null disables the shader)
	dc->VSSetShader(m_vs.m_ptr, 0, 0);
	dc->PSSetShader(m_ps.m_ptr, 0, 0);
	dc->GSSetShader(m_gs.m_ptr, 0, 0);
	dc->HSSetShader(m_hs.m_ptr, 0, 0);
	dc->DSSetShader(m_ds.m_ptr, 0, 0);

	// Don't set the rasterizer state here as we need to check
	// if the draw method has an override for the state
}

// Ref counting cleanup function
void pr::rdr::BaseShader::RefCountZero(pr::RefCount<BaseShader>* doomed)
{
	pr::rdr::BaseShader* shdr = static_cast<pr::rdr::BaseShader*>(doomed);
	shdr->m_mgr->Delete(shdr);
}

