//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_DRAW_LIST_ELEMENT_H
#define PR_RDR_RENDER_DRAW_LIST_ELEMENT_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/nugget.h"

namespace pr
{
	namespace rdr
	{
		struct DrawListElement
		{
			SortKey             m_sort_key; // The key for this element
			Nugget const*       m_nugget;   // The render nugget to draw
			BaseInstance const* m_instance; // Instance data for the model that this nugget belongs to
		};
		inline bool operator < (DrawListElement const& lhs, DrawListElement const& rhs) { return lhs.m_sort_key < rhs.m_sort_key; }

		static_assert(std::is_pod<DrawListElement>::value, "Drawlist elements must be POD types");
	}
}

#endif
