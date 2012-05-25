//***********************************************************************************
//
// Renderable - These objects have there own vertex and index streams
// 
//***********************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Models/Renderable.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Attribute.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Release the buffers for this renderable
void Renderable::Release()
{
	m_index_buffer = 0;
	m_vertex_buffer = 0;
	delete [] m_attribute_buffer; m_attribute_buffer = 0;
	m_render_nugget.clear();
	m_num_indices		= 0;
	m_num_vertices		= 0;
	m_num_attribs		= 0;
}

//*****
// Create a blank renderable
bool Renderable::Create(const RenderableParams& params)
{
	D3DPtr<IDirect3DDevice9> d3d_device	= params.m_renderer->GetD3DDevice();
	m_num_indices	= params.m_num_indices;
	m_num_vertices	= params.m_num_vertices;
	m_num_attribs	= params.m_num_primitives;
	m_vertex_type	= params.m_vertex_type;
	m_name			= params.m_name;
	m_material_map	= params.m_material_map;
	RenderableBase::SetPrimitiveType((D3DPRIMITIVETYPE)params.m_primitive_type);

	if( Failed(d3d_device->CreateIndexBuffer(
		m_num_indices * sizeof(Index),
		params.m_usage,
		D3DFMT_INDEX16,
		(D3DPOOL)params.m_pool,
		&m_index_buffer.m_ptr,
		0)) ) { PR_ERROR_STR(PR_DBG_RDR, "Failed to create an index buffer"); return false; }
	if( Failed(d3d_device->CreateVertexBuffer(
		m_num_vertices * vf::GetSize(m_vertex_type),
		params.m_usage,
		0,
		(D3DPOOL)params.m_pool,
		&m_vertex_buffer.m_ptr,
		0)) ) { PR_ERROR_STR(PR_DBG_RDR, "Failed to create a vertex buffer"); return false; }
	m_attribute_buffer = new Attribute[m_num_attribs];
	if( !m_attribute_buffer ) { PR_ERROR_STR(PR_DBG_RDR, "Failed to create an attrubute buffer"); return false; }
	return true;
}

//*****
// Create a renderable from a frame.
bool Renderable::Create(RenderableParams params, const Geometry& geometry, uint frame_number)
{
	PR_ASSERT(PR_DBG_RDR, m_render_nugget.empty());
	
	// Create the buffers
	const Frame& frame		= geometry.m_frame[frame_number];
	params.m_num_indices	= (uint)frame.m_mesh.m_face.size() * 3;
	params.m_num_vertices	= (uint)frame.m_mesh.m_vertex.size();
	params.m_num_primitives	= (uint)frame.m_mesh.m_face.size();
	params.m_vertex_type	= vf::GetTypeFromGeomType(frame.m_mesh.m_geometry_type);
	params.m_primitive_type	= EPrimitiveType_TriangleList;
	if( !Create(params) )
		return false;

	return LoadGeometry(*params.m_renderer, frame.m_mesh);
}
