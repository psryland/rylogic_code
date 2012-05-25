//************************************************************************
//
// A class for loading ASE files
//
//************************************************************************

#ifndef ASELOADER_H
#define ASELOADER_H

#include "pr/Maths/Maths.h"
#include "pr/common/StdVector.h"
#include "pr/common/StdList.h"
#include "pr/common/PRString.h"
#include "pr/common/HResult.h"
#include "pr/Geometry/PRGeometry.h"

#ifndef PR_DBG_ASELOADER
#define PR_DBG_ASELOADER  PR_DBG_GEOM
#endif//PR_DBG_ASELOADER

namespace pr
{
	struct AseLoaderSettings
	{
		AseLoaderSettings()
		:m_generate_normals(true)
		{}
		bool m_generate_normals;
	};

	class AseLoader
	{
	public:
		AseLoader();
		~AseLoader();

		HRESULT Load(const char *asefilename, Geometry& geometry) { return Load(asefilename, geometry, 0); }
		HRESULT Load(const char *asefilename, Geometry& geometry, const AseLoaderSettings* settings);

	private:
		void	LoadMaterialList			();
		void	LoadMaterialCount			();
		void	LoadMaterial				();
		void	LoadMaterialSubMaterial		(Material& material);
		void	LoadMaterialMapDiffuse		(Texture& texture);
		void	LoadGeomObject				(Geometry& geometry);
		void	LoadTMRow					(Frame& frame, int row);
		void	LoadMesh					(Frame& frame);
		void	LoadVertex					();
		void	LoadFace					();
		void	LoadTVertex					();
		void	LoadTFace					();
		void	LoadFaceNormal				();
		void	CompleteMesh				(Mesh& mesh);
		void	GenerateNormals				();
		
		void	SkipWhiteSpace();
		bool	FindSectionStart();
		bool	FindSectionEnd();
		bool	GetKeyWord();
		bool	PeekKeyWord();
		bool	ExtractString(std::string& word);
		bool	ExtractWord(std::string& word);
		bool	ExtractSizeT(std::size_t& _sizet, int radix = 10);
		bool	ExtractFloat(float& real);
		bool	SkipSection();

		void	Error(HRESULT err_code)		{ m_load_result = err_code; }

	private:
		struct AseNormal
		{
			AseNormal(){}
			AseNormal(std::size_t smoothing_group, const v4& normal) : m_smoothing_group(smoothing_group), m_normal(normal) {}
			std::size_t	m_smoothing_group;
			v4			m_normal;
		};
		struct AseVertex : public Vertex
		{
			enum { INVALID = 0x7FFFFFFF };
			bool operator ==(const AseVertex& other) const
			{
				return	m_vertex	 == other.m_vertex &&
						m_normal	 == other.m_normal &&
						m_tex_vertex == other.m_tex_vertex;
			}
			void AddNormal(std::size_t smoothing_group, const v4& normal)
			{
				for( std::list<AseNormal>::iterator n = m_sg_normal.begin(), n_end = m_sg_normal.end(); n != n_end; ++n )
				{
					if( n->m_smoothing_group == smoothing_group )
					{
						n->m_normal += normal;
						return;
					}
				}
				m_sg_normal.push_back(AseNormal(smoothing_group, normal));
			}
			const v4& GetNormal(std::size_t smoothing_group)
			{
				for( std::list<AseNormal>::iterator n = m_sg_normal.begin(), n_end = m_sg_normal.end(); n != n_end; ++n )
				{
					if( n->m_smoothing_group == smoothing_group )
					{
						return n->m_normal.Normalise3();
					}
				}
				PR_ERROR_STR(PR_DBG_ASELOADER, "Smoothing group not found");
				return v4Zero;
			}
			std::size_t				m_index_position;
			std::list<AseNormal>	m_sg_normal;
		};
		struct AseFace
		{
			AseFace()
			{
				for( int i = 0; i < 3; ++i ) { m_vert_index[i] = m_tex_index[i]	= 0; }
				m_face_normal		.Zero();
				m_smoothing_group	= 0;
				m_mat_index			= 0;
			}
			std::size_t	m_vert_index[3];		// Indices into the m_vertex array in AseMesh
			std::size_t	m_tex_index[3];			// Indices into the m_tex_coord array in AseMesh
			v4			m_face_normal;			// The face normal
			std::size_t	m_smoothing_group;		// The smoothing group of this face
			std::size_t	m_mat_index;			// The index of the material for this face
		};
		struct IndexMap
		{
			std::size_t	m_src_index;
			std::size_t	m_dst_index;
		};

	private:
		std::size_t				m_pos;
		std::size_t				m_count;
		std::vector<char>		m_source;
		std::string				m_keyword;
		HRESULT					m_load_result;
		AseLoaderSettings		m_settings;

		std::vector<AseVertex>	m_vertex;
		std::vector<v2>			m_tex_coord;
		std::vector<Material>	m_material;
		std::vector<AseFace>	m_face;
		std::vector<AseVertex>	m_expanded;
		std::list<IndexMap>		m_material_map;
	};

}//namespace pr

#endif//ASELOADER_H
