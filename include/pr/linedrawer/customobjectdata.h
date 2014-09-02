//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#ifndef LDR_CUSTOM_OBJECT_DATA_H
#define LDR_CUSTOM_OBJECT_DATA_H

#include "pr/maths/maths.h"
#include "pr/geometry/geometry.h"
#include "pr/renderer/renderer.h"

namespace pr
{
	namespace ldr
	{
		typedef void (*EditObjectFunc)(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager2& mat_mgr);

		struct CustomObjectData
		{
			CustomObjectData()
			:m_name("custom")
			,m_colour(pr::Colour32White)
			,m_num_verts(1)
			,m_num_indices(1)
			,m_i2w(pr::m4x4Identity)
			,m_geom_type(pr::geom::Vertex)
			,m_create_func(0)
			,m_user_data(0)
			{}
			std::string		m_name;
			pr::Colour32	m_colour;
			unsigned int	m_num_verts;
			unsigned int	m_num_indices;
			pr::m4x4		m_i2w;
			pr::GeomType	m_geom_type;
			EditObjectFunc	m_create_func;
			void*			m_user_data;
		};
	}//namespace ldr
}//namespace pr

#endif//LDR_CUSTOM_OBJECT_DATA_H