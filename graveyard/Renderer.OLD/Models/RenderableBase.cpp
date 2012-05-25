//***********************************************************************************
//
// Rendererable = Object that the Renderer can draw
//
//***********************************************************************************

#include "Stdafx.h"
#include "PR/Renderer/Models/RenderableBase.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/Materials/Material.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Constructor
RenderableBase::RenderableBase()
:m_model_to_root		(0)
,m_camera_to_screen		(0)
,m_primitive_type		(D3DPT_TRIANGLELIST)
,m_indices_per_primitive(3)
,m_num_indices			(0)
,m_num_vertices			(0)
,m_num_attribs			(0)
,m_vertex_type			(vf::EType_NumberOf)
,m_index_buffer			(0)
,m_vertex_buffer		(0)
,m_attribute_buffer		(0)
,m_render_bin			(0)
{}

//*****
// Set the kind of primitives this renderable contains
void RenderableBase::SetPrimitiveType(D3DPRIMITIVETYPE type)
{
	m_primitive_type = type;
	switch( m_primitive_type )
	{
	case D3DPT_TRIANGLELIST: m_indices_per_primitive = 3; break;
	case D3DPT_LINELIST:	 m_indices_per_primitive = 2; break;
	case D3DPT_POINTLIST:	 m_indices_per_primitive = 1; break;
	default: PR_ERROR_STR(PR_DBG_RDR, "Primitive type not supported");
	};
}

//*****
// Create the list of render nuggets from our vertex/index data. We iterate through the
// attribute buffer and make a nugget for each section that can be rendered without
// changing anything.
void RenderableBase::GenerateRenderNuggets()
{
	const Index*		 ib = LockIBuffer();
	const Attribute* attrib = LockABuffer();
	if( !ib || !attrib ) { UnlockIBuffer(); UnlockABuffer(); return; }

	// In case they've already been generated
	PR_WARN_EXP(PR_DBG_RDR, m_render_nugget.empty(), "Nuggets have already been created for this model");
	m_render_nugget.clear();
	
	uint index = 0;
	const Attribute* end = &attrib[m_num_attribs];
	while( attrib < end )
	{
		uint first_index				= index;
		uint first_vertex				= ib[index];
		uint last_vertex				= ib[index];
		const Attribute* first_attrib	= attrib;
		while( *attrib == *first_attrib )
		{
			// Iterate over the indices of this face to find the range of vertices
			for( int i = 0; i < m_indices_per_primitive; ++i )
			{
					 if( ib[index] < first_vertex ) first_vertex = ib[index];
				else if( ib[index] > last_vertex  ) last_vertex  = ib[index];
				++index;
			}

			// On to the next face/attribute
			if( ++attrib == end ) break;
		}

		// Create a render nugget for this section
		RenderNugget nugget;
		nugget.m_owner					= this;												// The renderable that this nugget is part of
		nugget.m_number_of_primitives	= (index - first_index) / m_indices_per_primitive;	// The number of primitives in this nugget
		nugget.m_index_byte_offset		= first_index * sizeof(Index);						// A byte offset into the index buffer for this nugget
		nugget.m_vertex_byte_offset		= first_vertex * vf::GetSize(m_vertex_type);		// A byte offset into the vertex buffer for this nugget
		nugget.m_index_length			= index - first_index;								// The number of indices in this nugget
		nugget.m_vertex_length			= last_vertex - first_vertex + 1;					// The number of vertices in this nugget
		nugget.m_attribute				= first_attrib;										// The material/texture information for this nugget

		Material material				= m_material_map[nugget.m_attribute->m_mat_index];
		if( material.m_texture->Alpha() )
		{
			nugget.m_render_state.SetRenderState(D3DRS_CULLMODE,			D3DCULL_NONE);
			nugget.m_render_state.SetRenderState(D3DRS_ZWRITEENABLE,		FALSE);
			nugget.m_render_state.SetRenderState(D3DRS_ALPHABLENDENABLE,	TRUE);
			nugget.m_render_state.SetRenderState(D3DRS_BLENDOP,				D3DBLENDOP_ADD);
			nugget.m_render_state.SetRenderState(D3DRS_SRCBLEND,			D3DBLEND_SRCALPHA);
			nugget.m_render_state.SetRenderState(D3DRS_DESTBLEND,			D3DBLEND_DESTALPHA);
		}
		nugget.m_sort_key = MakeSortKey(m_render_bin, material);
		m_render_nugget.push_back(nugget);
	}
	UnlockIBuffer();
	UnlockABuffer();
}

//*****
// Generate normals for a mesh
void RenderableBase::GenerateNormals()
{
	PR_ASSERT_STR(PR_DBG_RDR, vf::GetFormat(m_vertex_type) & vf::EFormat_Norm, "Vertices must have normals");

	vf::Iter	vertex_buffer	= LockVBuffer();
	Index*		index_buffer	= LockIBuffer();
	if( !vertex_buffer || !index_buffer ) { UnlockVBuffer(); UnlockIBuffer(); return; }
	
	// Initialise all of the normals to zero
	vf::Iter vb = vertex_buffer;
	for( uint v = 0; v < m_num_vertices; ++v, ++vb )
	{
		vb.Normal().Zero();
	}

    for( uint f = 0; f < m_num_attribs; ++f )
	{
		// Calculate a face normal
		Index i0 = index_buffer[3*f + 0];
		Index i1 = index_buffer[3*f + 1];
		Index i2 = index_buffer[3*f + 2];

		v3 norm = Cross3(vertex_buffer[i1].Vertex() - vertex_buffer[i0].Vertex(), vertex_buffer[i2].Vertex() - vertex_buffer[i0].Vertex());
		norm.Normalise3();

		// Add the normal to each vertex that references the face
		vertex_buffer[i0].Normal() += norm;
		vertex_buffer[i1].Normal() += norm;
		vertex_buffer[i2].Normal() += norm;
	}

	// Normalise all of the normals
	vb = vertex_buffer;
	for( uint v = 0; v < m_num_vertices; ++v, ++vb )
	{
		vb.Normal().Normalise3();
	}
	UnlockVBuffer();
	UnlockIBuffer();
}

//*****
// Load a frame into the vertex, index, and attrib buffers
bool RenderableBase::LoadGeometry(Renderer& renderer, const Mesh& mesh)
{
	// Read the index buffer
	Index* ib = LockIBuffer();	if( !ib ) return false;
	for( TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f )
	{
        *ib = f->m_vert_index[0];	++ib;
        *ib = f->m_vert_index[1];	++ib;
        *ib = f->m_vert_index[2];	++ib;
	}
	UnlockIBuffer();

	// Read the vertex buffer
	vf::Iter vb = LockVBuffer();	if( !vb ) return false;
	for( TVertexCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v )
	{
		vb.Set(*v);
		++vb;
	}
	UnlockVBuffer();
	
	// Read the attribute buffer
	Attribute* ab = LockABuffer(); if( !ab ) return false;
	for( TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f )
	{
		ab->m_mat_index = f->m_mat_index;
		++ab;
	}
	UnlockABuffer();

	// Load the materials
	struct Resolver : IMaterialResolver
	{
		Resolver(MaterialManager* matmgr, rdr::Material* mat) : m_matmgr(matmgr), m_mat(mat) {}
		bool AddMaterial(uint, rdr::Material material)	{ *m_mat = material; return false; }
		bool LoadEffect (const char*, effect::Base*&)	{ return false; }
		bool LoadTexture(const char* texture_filename, rdr::Texture*& texture_out)
		{
			Verify(m_matmgr->LoadTexture(texture_filename, texture_out));
			return false;
		}
		rdr::MaterialManager* m_matmgr;
		rdr::Material* m_mat;
	};
	Resolver mat_resolver(&renderer.GetMaterialManager(), 0);
	TMaterialCont::const_iterator m_iter = mesh.m_material.begin();
	uint num_materials = (uint)mesh.m_material.size();
	for( uint m = 0; m < num_materials; ++m, ++m_iter )
	{
		mat_resolver.m_mat = &m_material_map[m];
		renderer.GetMaterialManager().LoadMaterials(*m_iter, mesh.m_geometry_type, mat_resolver);
	}

	SetPrimitiveType(D3DPT_TRIANGLELIST);
	GenerateRenderNuggets();
	return true;
}
