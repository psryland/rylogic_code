//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/packages/package.h"
#include "pr/renderer/materials/material.h"
#include "pr/renderer/utility/globalfunctions.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::package;

//*****
Builder::Builder(const IReport* report)
:m_report(report)
{
	if( !m_report ) { m_report = this; }
}

//*****
RdrId Builder::AddTexture(const char* texture_filename)
{
	package::Texture pkg_texture;
	pkg_texture.m_texture_id			= GetId(texture_filename);
	if( m_textures.find(pkg_texture.m_texture_id) == m_textures.end() )
	{
		pkg_texture.m_byte_offset		= sizeof(pkg_texture);
		pkg_texture.m_size				= static_cast<uint32>(pr::filesys::FileLength<std::string>(texture_filename));

		ByteCont tex_data;
		if( !FileToBuffer(texture_filename, tex_data) )
		{
			m_report->Error(Fmt("Texture '%s' not found", texture_filename).c_str());
			return 0;
		}

		nugget::Nugget& nug = m_textures[pkg_texture.m_texture_id];
		nug.Initialise(EPackageId_Texture, EPackageVersion_Texture, 0, PackageDescription[EPackageType_Texture]);
		nug.Reserve(sizeof(pkg_texture) + tex_data.size());
		nug.AppendData(&pkg_texture, sizeof(pkg_texture), nugget::ECopyFlag_CopyToBuffer);
		nug.AppendData(&tex_data[0], tex_data.size(), nugget::ECopyFlag_CopyToBuffer);
	}
	return pkg_texture.m_texture_id;
}

//*****
void Builder::AddModel(RdrId model_id, pr::Mesh const& mesh)
{
	// See if the model's been added before
	if( m_models.find(model_id) != m_models.end() )
	{	m_report->Error(Fmt("Model Id '%d' already exists in the package", model_id).c_str()); }

	package::Model pkg_model;
	pkg_model.m_model_id       = model_id;
	pkg_model.m_vertex_type    = vf::GetTypeFromGeomType(mesh.m_geom_type);
	pkg_model.m_primitive_type = model::EPrimitive::TriangleList;
	pkg_model.m_bbox           .reset();

	// Vertex buffer
	pkg_model.m_vertex_count				= (uint32)mesh.m_vertex.size();
	pkg_model.m_vertex_size					= static_cast<uint32>(vf::GetSize(pkg_model.m_vertex_type));
	pkg_model.m_vertex_byte_offset			= sizeof(pkg_model);
	ByteCont vertex(pkg_model.m_vertex_size * pkg_model.m_vertex_count);
	if( pkg_model.m_vertex_count )
	{
		vf::iterator vb(&vertex[0], pkg_model.m_vertex_type);
		for (TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
		{
			Encompase(pkg_model.m_bbox, v->m_vertex);
			vb->set(*v);
			++vb;
		}
	}
	
	// Index buffer
	pkg_model.m_index_count					= (uint32)mesh.m_face.size() * 3;
	pkg_model.m_index_size					= sizeof(rdr::Index);
	pkg_model.m_index_byte_offset			= static_cast<uint32>(sizeof(pkg_model) + vertex.size());
	ByteCont index(pkg_model.m_index_size * pkg_model.m_index_count);
	rdr::Index* ibuffer = reinterpret_cast<rdr::Index*>(&index[0]);
	if( pkg_model.m_index_count )
	{
		rdr::Index* ib = ibuffer;
		for (TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f)
		{
			*ib = f->m_vert_index[0];	++ib;
			*ib = f->m_vert_index[1];	++ib;
			*ib = f->m_vert_index[2];	++ib;
		}
	}
	
	// Material ranges
	pkg_model.m_material_range_count		= 0; // Fill out later
	pkg_model.m_material_range_size			= sizeof(package::MatRange);
	pkg_model.m_material_range_byte_offset	= static_cast<uint32>(sizeof(pkg_model) + vertex.size() + index.size());
	ByteCont material_range;
	TFaceCont::const_iterator face     = mesh.m_face.begin();
	TFaceCont::const_iterator face_end = mesh.m_face.end();
	if( face != face_end )
	{
		// If the model doesn't contain any materials use a default material for the whole model
		if( mesh.m_material.empty() )
		{
			package::MatRange range;
			range.m_v_range            = model::Range::make(0, pkg_model.m_vertex_count);
			range.m_i_range            = model::Range::make(0, pkg_model.m_index_count);
			range.m_effect_id          = 0;PR_ASSERT(1, false, "Code needs updating here"); //GetDefaultEffectId(mesh.m_geom_type);
			range.m_diffuse_texture_id = 0;
			pr::AppendData(material_range, range);
			++pkg_model.m_material_range_count;
		}

		// Otherwise, add each material to the package and determine the ranges of faces that use each material
		else
		{
			model::Range i_range = {0, 0};
			while( face != face_end )
			{
				std::size_t first_mat_index = Clamp<std::size_t>(face->m_mat_index, 0, mesh.m_material.size() - 1);
				const pr::Material& material = mesh.m_material[first_mat_index];
				RdrId diffuse_texture_id = material.m_texture.empty() ? 0 : AddTexture(material.m_texture[0].m_filename.c_str());

				// Determine the range of verts using this material
				for (; face != face_end; ++face)
				{
					if (face->m_mat_index != first_mat_index) break;
					i_range.m_end += 3;
				}

				if (!i_range.empty())
				{
					package::MatRange range;
					range.m_v_range            = GetVRange(i_range, ibuffer);
					range.m_i_range            = i_range;
					range.m_effect_id          = 0;PR_ASSERT(1, false, "Code needs updating here"); //GetDefaultEffectId(mesh.m_geom_type);
					range.m_diffuse_texture_id = diffuse_texture_id;
					pr::AppendData(material_range, range);
					++pkg_model.m_material_range_count;
					
					// Start the next range
					i_range.set(i_range.size(), i_range.size());
				}
			}
		}
	}	

	// Create a nugget for the mesh
	nugget::Nugget& nug = m_models[model_id];
	nug.Initialise(EPackageId_Model, EPackageVersion_Model, 0, PackageDescription[EPackageType_Model]);
	nug.Reserve(sizeof(pkg_model) + vertex.size() + index.size() + material_range.size());
	nug.AppendData(&pkg_model, sizeof(pkg_model), nugget::ECopyFlag_CopyToBuffer);
	if( !vertex.empty() )			{ nug.AppendData(&vertex[0]			,vertex.size()			,nugget::ECopyFlag_CopyToBuffer); }
	if( !index .empty() )			{ nug.AppendData(&index[0]			,index.size()			,nugget::ECopyFlag_CopyToBuffer); }
	if( !material_range.empty() )	{ nug.AppendData(&material_range[0]	,material_range.size()	,nugget::ECopyFlag_CopyToBuffer); }
}

//*****
void Builder::Serialise(nugget::Nugget& package)
{
	// Build the textures package
	nugget::Nugget textures_nug(EPackageId_Textures, EPackageVersion_Textures, 0, PackageDescription[EPackageType_Textures]);
	{
		std::size_t total_size = 0;
		for( TNuggetCont::const_iterator i = m_textures.begin(), i_end = m_textures.end(); i != i_end; ++i )
		{
			total_size += i->second.GetNuggetSizeInBytes();
		}
		textures_nug.Reserve(total_size);
		for( TNuggetCont::const_iterator i = m_textures.begin(), i_end = m_textures.end(); i != i_end; ++i )
		{
		    textures_nug.AppendData(i->second, nugget::ECopyFlag_CopyToBuffer);
		}
	}
	// Build the models package
	nugget::Nugget models_nug(EPackageId_Models, EPackageVersion_Models, 0, PackageDescription[EPackageType_Models]);
	{
		std::size_t total_size = 0;
		for( TNuggetCont::const_iterator i = m_models.begin(), i_end = m_models.end(); i != i_end; ++i )
		{
			total_size += i->second.GetNuggetSizeInBytes();
		}
		models_nug.Reserve(total_size);
		for( TNuggetCont::const_iterator i = m_models.begin(), i_end = m_models.end(); i != i_end; ++i )
		{
			models_nug.AppendData(i->second, nugget::ECopyFlag_CopyToBuffer);
		}
	}


	package.Initialise(EPackageId_RdrPackage, EPackageVersion_RdrPackage, 0, PackageDescription[EPackageType_RdrPackage]);
	package.Reserve(
		textures_nug.GetNuggetSizeInBytes() +
		models_nug.GetNuggetSizeInBytes()
		);
	package.AppendData(textures_nug, nugget::ECopyFlag_CopyToBuffer);
	package.AppendData(models_nug, nugget::ECopyFlag_CopyToBuffer);
}

