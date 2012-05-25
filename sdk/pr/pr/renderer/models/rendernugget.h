//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_RENDER_NUGGET_H
#define PR_RDR_RENDER_NUGGET_H

#include "pr/renderer/models/types.h"
#include "pr/renderer/materials/material.h"

namespace pr
{
	namespace rdr
	{
		struct RenderNugget :pr::chain::link<RenderNugget, model::RdrRenderNuggetChain>
		{
			ModelBufferPtr          m_model_buffer;        // The vertex and index buffers
			model::Range            m_Vrange;              // The byte offset into the vertex buffer and the number of vertices for this nugget
			model::Range            m_Irange;              // The byte offset into the index buffer and the number of indices for this nugget
			model::EPrimitive::Type m_primitive_type;      // The kind of primitives in this nugget
			std::size_t             m_primitive_count;     // The number of primitives in this nugget
			Material                m_material;            // The material to use for this nugget
			SortKey                 m_sort_key;            // A key for sorting this nugget
			Model*                  m_owner;               // The model that this nugget belongs to (for debugging mainly)
		};
		typedef chain::head<RenderNugget, model::RdrRenderNuggetChain> TNuggetChain;
	}
}

#endif
