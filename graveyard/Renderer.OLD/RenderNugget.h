//**********************************************************************************
//
// RenderNugget
//
//**********************************************************************************
// A render nugget contains the data that is constant for all instances
// of the renderable it belongs to. Each render nugget along with some
// instance data is enough information for one DrawIndexedPrimitive call
//
#ifndef RENDERNUGGET_H
#define RENDERNUGGET_H

#include "PR/Common/StdList.h"
#include "PR/Renderer/Sortkey.h"
#include "PR/Renderer/RenderState.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		struct RenderNugget
		{
			// Rendering members
			SortKey				m_sort_key;
			RenderableBase*		m_owner;				// The renderable that this nugget is part of
			uint				m_number_of_primitives;	// The number of primitives in this nugget
			uint				m_index_byte_offset;	// A byte offset into the index buffer for this nugget
			uint				m_vertex_byte_offset;	// A byte offset into the vertex buffer for this nugget
			uint				m_index_length;			// The number of indices in this nugget
			uint				m_vertex_length;		// The number of vertices in this nugget
			const Attribute*	m_attribute;			// The material/texture information for this nugget
			RenderStateBlock	m_render_state;			// Render states specific to this render nugget
		};
		typedef std::list<RenderNugget>  TNuggetList;

	}//namespace rdr
}//namespace pr

#endif//RENDERNUGGET_H
