//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_DRAW_LIST_ELEMENT_H
#define PR_RDR_DRAW_LIST_ELEMENT_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/models/rendernugget.h"

namespace pr
{
	namespace rdr
	{
		struct DrawListElement
		{
			SortKey               m_sort_key; // The key for this element
			RenderNugget const*   m_nugget;   // The render nugget to draw
			instance::Base const* m_instance; // Instance data for the model that this nugget belongs to
			
			struct pr_is_pod { enum { value = true }; };
			Material const& GetMaterial() const { return m_nugget->m_material; }
		};
		inline bool operator < (DrawListElement const& lhs, DrawListElement const& rhs) { return lhs.m_sort_key < rhs.m_sort_key; }
	}
}

#endif
