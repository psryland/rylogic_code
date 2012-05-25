//***********************************************************************************
//
// RenderableElement - Small geometry that will be modified by client code
//	This objects are copied to a larger buffer in the renderer
// 
//***********************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/RenderableElement.h"
#include "PR/Geometry/PRGeometry.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Attribute.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Free resources
void RenderableElement::Release()
{
	delete [] m_element_ibuffer;
	delete [] m_element_vbuffer;
	delete [] m_attribute_buffer;
	m_element_ibuffer  = 0;
	m_element_vbuffer  = 0;
	m_attribute_buffer = 0;
}

//*****
// Create a blank renderable
bool RenderableElement::Create(const RenderableParams& params)
{
	m_num_indices		= params.m_num_indices;
	m_num_vertices		= params.m_num_vertices;
	m_num_attribs		= params.m_num_primitives;
	m_vertex_type		= params.m_vertex_type;
	m_name				= params.m_name;
	m_material_map		= params.m_material_map;
	RenderableBase::SetPrimitiveType((D3DPRIMITIVETYPE)params.m_primitive_type);
	
	m_element_ibuffer	= new Index[m_num_indices];
	m_element_vbuffer	= new char[m_num_vertices * vf::GetSize(m_vertex_type)];
	m_attribute_buffer	= new Attribute[m_num_attribs];
	PR_ASSERT_STR(PR_DBG_RDR, m_element_ibuffer != 0 && m_element_vbuffer != 0 && m_attribute_buffer != 0, "Failed to allocate a index, vertex, or attribute buffer");
	return m_element_ibuffer != 0 && m_element_vbuffer != 0 && m_attribute_buffer != 0;
}

//*****
// Create a renderable from a frame.
bool RenderableElement::Create(RenderableParams params, const Geometry& geometry, uint frame_number)
{
	PR_ASSERT(PR_DBG_RDR, m_render_nugget.empty());
	
	// Create the buffers
	const Frame& frame	= geometry.m_frame[frame_number];
	params.m_num_indices	= (uint)frame.m_mesh.m_face.size() * 3;
	params.m_num_vertices	= (uint)frame.m_mesh.m_vertex.size();
	params.m_num_primitives	= (uint)frame.m_mesh.m_face.size();
	params.m_vertex_type	= vf::GetTypeFromGeomType(frame.m_mesh.m_geometry_type);
	params.m_primitive_type	= EPrimitiveType_TriangleList;
	if( !Create(params) )
		return false;

	return LoadGeometry(*params.m_renderer,frame.m_mesh);
}
