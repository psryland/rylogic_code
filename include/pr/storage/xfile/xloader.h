//************************************************************************
//
// A Machine for turning X File filenames into XFile Objects
//
//************************************************************************
#ifndef PR_XFILE_XLOADER_H
#define PR_XFILE_XLOADER_H

#include <d3d9.h>
#include <d3dx9xof.h>
#include <vector>
#include "pr/common/array.h"
#include "pr/common/d3dptr.h"
#include "pr/geometry/geometry.h"
#include "pr/storage/xfile/xfile.h"

namespace pr
{
	namespace xfile
	{
		namespace impl
		{
			struct XFace
			{
				XFace() { memset(this, 0, sizeof(*this)); }
				uint m_vert_index[3];
				uint m_norm_index[3];
				uint m_tex_index[3];
				uint m_mat_index;
			};
			struct XVertex : public pr::Vert
			{
				enum { INVALID = 0x7FFFFFFF };
				uint	m_index_position;
				uint	m_vertex_index;
				uint	m_normal_index;
				uint	m_colour_index;
				uint	m_tex_vertex_index;
			};
			inline bool operator == (const XVertex& lhs, const XVertex& rhs)
			{
				return	lhs.m_vertex_index		== rhs.m_vertex_index &&
						lhs.m_normal_index		== rhs.m_normal_index &&
						lhs.m_colour_index		== rhs.m_colour_index &&
						lhs.m_tex_vertex_index	== rhs.m_tex_vertex_index;
			}
			inline bool operator < (const XVertex& lhs, const XVertex& rhs)
			{
				if( lhs.m_vertex_index != rhs.m_vertex_index )	return lhs.m_vertex_index < rhs.m_vertex_index;
				if( lhs.m_normal_index != rhs.m_normal_index )	return lhs.m_normal_index < rhs.m_normal_index;
				if( lhs.m_colour_index != rhs.m_colour_index )	return lhs.m_colour_index < rhs.m_colour_index;
				return lhs.m_tex_vertex_index < rhs.m_tex_vertex_index;
			}
			struct XVertexPointersPred
			{
				bool operator () (XVertex const * const& lhs, XVertex const* const& rhs) const { return (*lhs) < (*rhs); }
			};
		}//namespace impl

		// Class that does the actual loading
		class XLoader
		{
			std::string                 m_xfilepath;
			D3DPtr<ID3DXFile>           m_d3d_xfile;
			TGUIDSet const*             m_partial_load_set;
			pr::vector<v4>               m_vertex;
			pr::vector<v4>               m_normal;
			pr::vector<Colour32>         m_colour;
			pr::vector<v2>               m_tex_coord;
			pr::vector<Material>         m_material;
			pr::vector<impl::XFace>      m_face;
			pr::vector<impl::XVertex>    m_x_vertex;
			pr::vector<impl::XVertex*>   m_px_vertex;

			bool	IsInLoadSet(GUID guid) const;
			void	LoadFrame				(D3DPtr<ID3DXFileData> data, Geometry& geometry);
			void	LoadFrameTransform		(D3DPtr<ID3DXFileData> data, m4x4& transform);
			void	LoadMesh				(D3DPtr<ID3DXFileData> data, Mesh& mesh);
			void	LoadMeshNormal			(D3DPtr<ID3DXFileData> data);
			void	LoadMeshMaterial		(D3DPtr<ID3DXFileData> data);
			void	LoadMaterial			(D3DPtr<ID3DXFileData> data, Material& material);
			void	LoadTextureFilename		(D3DPtr<ID3DXFileData> data, Texture& texture);
			void	LoadMeshVertexColours	(D3DPtr<ID3DXFileData> data);
			void	LoadMeshTexCoords		(D3DPtr<ID3DXFileData> data);
			void	CompleteMesh			(Mesh& mesh);

		public:
			XLoader(const void* custom_templates, unsigned int custom_templates_size);
			EResult Load(const char *xfilename, Geometry& geometry, const TGUIDSet* partial_load_set);
		};
	}//namespace xfile
}//namespace pr

#endif
