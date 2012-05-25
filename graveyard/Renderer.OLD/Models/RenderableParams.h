//**************************************************************************
//
//	Create Renderable
//
//**************************************************************************

#ifndef RENDERABLE_PARAMS_H
#define RENDERABLE_PARAMS_H

#include "PR/Common/StdString.h"
#include "PR/Maths/Maths.h"
#include "PR/Renderer/Materials/MaterialMap.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Models/RenderableFlags.h"

namespace pr
{
	namespace rdr
	{
		struct RenderableParams
		{
			RenderableParams()
			:m_renderer			(0)
			,m_num_indices		(0)
			,m_num_vertices		(0)
			,m_num_primitives	(0)
			,m_vertex_type		(vf::EType_PosNormDiffTex)	// Use vf::GetTypeFromGeomType(pr::GeomType | pr::GeomType);
			,m_material_map		()
			,m_primitive_type	(EPrimitiveType_TriangleList)
			,m_name				("")
			,m_usage			(EUsage_WriteOnly)
			,m_pool				(EMemPool_Default)
			{}

			Renderer*			m_renderer;
			uint				m_num_indices;
			uint				m_num_vertices;
			uint				m_num_primitives;
			vf::EType			m_vertex_type;
			MaterialMap			m_material_map;
			EPrimitiveType		m_primitive_type;
			std::string			m_name;
			uint				m_usage;	// EUsage_WriteOnly | EUsage_Dynamic
			EMemPool			m_pool;		// EMemPool_Managed, EMemPool_Default
		};

	}//namespace rdr
}//namespace pr

#endif//RENDERABLE_PARAMS_H
