//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/sortkey.h"

namespace pr::rdr
{
	struct DrawListElement
	{
		SortKey             m_sort_key; // The key for this element (not necessarily the same as 'nugget->m_sortkey')
		Nugget const*       m_nugget;   // The geometry nugget to draw
		BaseInstance const* m_instance; // The instance of the model that 'm_nugget' belongs to

		friend bool operator < (DrawListElement const& lhs, DrawListElement const& rhs) { return lhs.m_sort_key < rhs.m_sort_key; }
		friend bool operator < (DrawListElement const& lhs, SortKey rhs) { return lhs.m_sort_key < rhs; }
		friend bool operator < (SortKey lhs, DrawListElement const& rhs) { return lhs < rhs.m_sort_key; }
	};
	static_assert(std::is_pod_v<DrawListElement>, "DLE must be POD so that the drawlist can be sorted efficiently");
}
