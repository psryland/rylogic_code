//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
//	Root
//		TexturesPackage		// Texture should be first so that they are registered first
//			Texture[]
//				Texture id
//				Texture data (=file in memory)
//		ModelsPackage
//			Model[]
//				Model Id (= hash<xfilename>)		
//				VertexFormat
//				PrimitiveType
//				Num vertices
//				Num indices
//				Num material ranges
//				BoundingBox
//				Vertices[]
//				Indices[]
//				Material Ranges[]
//					V range
//					I range
//					Effect id
//					Texture id
//					future effect parameters
//
// Provides a collection of functions for loading/saving packages of renderer data.
// A 'model_package' is a nugget file containing models as the child nuggets
// A 'material_package' is a nugget file containing materials as the child nuggets
// A 'package' is a nugget file containing one or more of the packages above as
// child nuggets.

#pragma once
#ifndef PR_RDR_PACKAGE_H
#define PR_RDR_PACKAGE_H

#include <map>
#include "pr/common/ireport.h"
#include "pr/renderer/types/forward.h"
#include "pr/storage/nugget_file/nuggetfile.h"
#include "pr/geometry/geometry.h"
#include "pr/renderer/models/types.h"

namespace pr
{
	namespace rdr
	{
		enum EPackageType
		{
			EPackageType_RdrPackage,
			EPackageType_Models,
			EPackageType_Model,
			EPackageType_Textures,
			EPackageType_Texture,
			EPackageType_NumberOf
		};
		enum EPackageId
		{
			EPackageId_RdrPackage	= PR_MAKE_NUGGET_ID('R','d','r','P'),
			EPackageId_Models		= PR_MAKE_NUGGET_ID('M','d','l','s'),
			EPackageId_Model		= PR_MAKE_NUGGET_ID('M','d','l',' '),
			EPackageId_Textures		= PR_MAKE_NUGGET_ID('T','e','x','s'),
			EPackageId_Texture		= PR_MAKE_NUGGET_ID('T','e','x',' ')
		};
		enum EPackageVersion
		{
			EPackageVersion_RdrPackage		= 1000,
			EPackageVersion_Models			= 1000,
			EPackageVersion_Model			= 1000,
			EPackageVersion_Textures		= 1000,
			EPackageVersion_Texture			= 1000
		};
		static const char* PackageDescription[EPackageType_NumberOf] =
		{
			"Renderer Package",
			"Models Package",
			"Model",
			"Textures Package",
			"Texture"
		};

		namespace package
		{
			struct Texture
			{
				RdrId	m_texture_id;								// Texture
				uint32	m_size;										// Size of the texture data
				uint32	m_byte_offset;								// Byte offset from the material to the start of the texture data
				//uint8	m_texture_data[m_size];						// Textures that contribute to the material
			};
			struct Model
			{
				RdrId			m_model_id;								// Hash of the model filename
				uint32			m_vertex_type;							// vf::Type
				uint32			m_primitive_type;						// EPrimitive::Type
				uint32			m_vertex_count;							// The number of vertices in the following buffer
				uint32			m_vertex_size;							// Size of one vertex in bytes
				uint32			m_vertex_byte_offset;					// The byte offset to the first vertex (start of the buffer)
				uint32			m_index_count;							//
				uint32			m_index_size;							//
				uint32			m_index_byte_offset;					//
				uint32			m_material_range_count;					//
				uint32			m_material_range_size;					//
				uint32			m_material_range_byte_offset;			//
				BoundingBox		m_bbox;									// 
				//Vertex		m_vertex[m_vertex_count];				// Vertex type based on m_vertex_type
				//rdr::Index	m_index[m_index_count];					//
				//MatRange		m_mat_range[m_material_range_count];	// Sub models
			};
			struct MatRange
			{
				model::Range	m_v_range;
				model::Range	m_i_range;							//
				RdrId			m_effect_id;						// The effect to use for this sub model
				RdrId			m_diffuse_texture_id;				// The parameters to use with the effect
				// Future parameters
			};

			// An object for building a package
			class Builder : public IReport
			{
			public:
				Builder(const IReport* report = 0);
				RdrId	AddTexture		(const char* texture_filename);
				void	AddModel		(RdrId model_id, const pr::Mesh& mesh);
				void	Serialise		(nugget::Nugget& package);

				// IReport interface
				void Error  (const char* msg) const { msg; PR_ASSERT(PR_DBG_RDR, false, msg); }
				void Warn   (const char* msg) const { msg; PR_INFO(PR_DBG_RDR, msg); }
				void Message(const char* msg) const	{ msg; PR_INFO(PR_DBG_RDR, msg); }
				void Assert (const char* msg) const { msg; PR_ASSERT(PR_DBG_RDR, false, msg); }

			private:
				typedef std::map<RdrId, nugget::Nugget> TNuggetCont;
				const IReport* m_report;
				TNuggetCont m_textures;
				TNuggetCont m_models;
			};
		}//namespace package
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_PACKAGE_H
