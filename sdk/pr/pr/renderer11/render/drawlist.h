//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_DRAWLIST_H
#define PR_RDR_RENDER_DRAWLIST_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/util/allocator.h"

namespace pr
{
	namespace rdr
	{
		struct Drawlist
		{
			typedef pr::Array<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement> > DLECont;
			
			DLECont     m_dle;            // An array of DrawListElements
			bool        m_sort_needed;    // Lazy sorting of the draw list
			Renderer*   m_rdr;
			
			Drawlist(Renderer& rdr);
			
			// Remove all elements from the drawlist
			void Clear() { m_dle.resize(0); }
			
			// Add/Remove instances from the drawlist
			void Add(instance::Base const& inst);
			void Remove(instance::Base const& inst);
			void Remove(instance::Base const** inst, std::size_t count);
			
			// Sort the drawlist based on sortkey
			void Sort()         { std::sort(m_dle.begin(), m_dle.end()); m_sort_needed = false; }
			void SortIfNeeded() { if (m_sort_needed) Sort(); }
			
			// Iterators for accessing the drawlist
			DLECont::const_iterator begin() const { return m_dle.begin(); }
			DLECont::const_iterator end() const   { return m_dle.end(); }
		};
	}
}

#endif
