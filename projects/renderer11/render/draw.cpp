//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "renderer11/render/draw.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/nugget.h"

using namespace pr::rdr;

// Clear the back buffer to 'colour'
void pr::rdr::Draw::ClearBB(pr::Colour const& colour)
{
	D3DPtr<ID3D11RenderTargetView> rtv;
	m_dc->OMGetRenderTargets(1, &rtv.m_ptr, 0);
	m_dc->ClearRenderTargetView(rtv.m_ptr, colour);
}

// Clear the depth buffer to 'depth'
void pr::rdr::Draw::ClearDB(UINT flags, float depth, UINT8 stencil)
{
	D3DPtr<ID3D11DepthStencilView> dsv;
	m_dc->OMGetRenderTargets(1, 0, &dsv.m_ptr);
	m_dc->ClearDepthStencilView(dsv.m_ptr, flags, depth, stencil);
}

// Setup the input assembler for the given nugget
void pr::rdr::Draw::Setup(DrawListElement const& dle, SceneView const& view)
{
	Nugget const& nugget  = *dle.m_nugget;
	ModelBuffer const& mb = *nugget.m_model->m_model_buffer.m_ptr;
	Material const&   mat = nugget.m_material;
	
	// Bind the vertex buffer to the IA
	UINT          strides[] = {mb.m_vb.m_stride};
	UINT          offsets[] = {(UINT)nugget.m_vrange.m_begin};
	ID3D11Buffer* buffers[] = {mb.m_vb.m_ptr};
	m_dc->IASetVertexBuffers(0, 1, buffers, strides, offsets);
	
	// Set the input layout for this vertex buffer
	m_dc->IASetInputLayout(mat.m_shader->m_iplayout.m_ptr);

	// Bind the index buffer to the IA
	m_dc->IASetIndexBuffer(mb.m_ib.m_ptr, mb.m_ib.m_format, (UINT)nugget.m_irange.m_begin);
 
	// Tell the IA would sort of primitives to expect
	m_dc->IASetPrimitiveTopology(nugget.m_prim_topo);
	
	// Bind the shader to the device
	mat.m_shader->Setup(m_dc, dle, view);
}

// Add the nugget to the device context
void pr::rdr::Draw::Render(Nugget const& nugget)
{
	m_dc->DrawIndexed(
		UINT(nugget.m_irange.size()),
		UINT(nugget.m_irange.m_begin),
		UINT(nugget.m_vrange.m_begin));
}
