//************************************************************************
// XFile Saver
//  Copyright © Rylogic Ltd 2009
//************************************************************************

#ifndef PR_XFILE_XSAVER_H
#define PR_XFILE_XSAVER_H

#include <string>
#include <vector>
#include "pr/common/d3dptr.h"
#include "pr/geometry/geometry.h"
#include "pr/storage/xfile/xfile.h"

namespace pr
{
	namespace xfile
	{
		// Class that does the actual saving
		class XSaver
		{
			const TGUIDSet*		m_partial_save_set;
			std::string			m_output_filename;
			std::vector<uint>	m_buffer;

			bool	IsInSaveSet(GUID guid) const;
			void	SaveFrame			(D3DPtr<ID3DXFileSaveObject> save_object, const Frame& frame);
			void	SaveFrameTransform	(D3DPtr<ID3DXFileSaveData> parent,        const m4x4& transform);
			void	SaveMesh			(D3DPtr<ID3DXFileSaveData> parent,        const Mesh& mesh);
			void	SaveMeshNormals		(D3DPtr<ID3DXFileSaveData> parent,        const Mesh& mesh);
			void	SaveMeshMaterials	(D3DPtr<ID3DXFileSaveData> parent,        const Mesh& mesh);
			void	SaveMeshColours		(D3DPtr<ID3DXFileSaveData> parent,        const Mesh& mesh);
			void	SaveMeshTexCoords	(D3DPtr<ID3DXFileSaveData> parent,        const Mesh& mesh);
			void	SaveMaterial		(D3DPtr<ID3DXFileSaveData> parent,        const Material& material);
			void	SaveSubmaterial		(D3DPtr<ID3DXFileSaveData> parent,        const Texture& texture);
			uint	FtoUint(float f)	{ return reinterpret_cast<uint&>(f); }

		public:
			EResult Save(const Geometry& geometry, const char* xfilename, const TGUIDSet* partial_save_set, const void* custom_templates, unsigned int custom_templates_size);
		};
	}
}

#endif
