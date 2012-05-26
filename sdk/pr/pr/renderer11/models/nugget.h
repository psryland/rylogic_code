//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_NUGGET_H
#define PR_RDR_MODELS_NUGGET_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/draw_method.h"

namespace pr
{
	namespace rdr
	{
		struct Nugget :pr::chain::link<Nugget, ChainGroupNugget>
		{
			typedef D3D11_PRIMITIVE_TOPOLOGY TopoType;
			
			ModelPtr   m_model;      // The vertex and index buffers
			Range      m_vrange;     // The byte offset into the vertex buffer and the number of vertices for this nugget (relative to model buffer, not model)
			Range      m_irange;     // The byte offset into the index buffer and the number of indices for this nugget (relative to model buffer, not model)
			TopoType   m_prim_topo;  // The primitive topology for this nugget
			size_t     m_prim_count; // The number of primitives in this nugget
			DrawMethod m_draw;       // The method to use to draw this nugget
			SortKey    m_sort_key;   // A key for sorting this nugget
			Model*     m_owner;      // The model that this nugget belongs to (for debugging mainly)
		};
		typedef pr::chain::head<Nugget, ChainGroupNugget> TNuggetChain;
	}
}

#endif

