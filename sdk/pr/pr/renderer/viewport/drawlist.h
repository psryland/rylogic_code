//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_DRAWLIST_H
#define PR_RDR_DRAWLIST_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/projectconfiguration.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/viewport/drawlistelement.h"

namespace pr
{
	namespace rdr
	{
		typedef pr::Array<DrawListElement, 1024, false> TDrawList;

		class Drawlist
		{
			ViewportId  m_viewport_id;              // The viewport that this draw list belongs to
			TDrawList   m_draw_list;                // An array of DrawListElements
			bool        m_draw_list_sort_needed;    // Lazy sorting of the draw list
			Renderer*   m_rdr;

		public:
			Drawlist(Renderer& rdr, ViewportId viewport_id);
			~Drawlist();

			void Clear();
			void AddInstance   (instance::Base const& instance);
			void RemoveInstance(instance::Base const& instance);
			void AddInstanceBatch   (instance::Base const** instance, std::size_t count);
			void RemoveInstanceBatch(instance::Base const** instance, std::size_t count);

			void SortIfNecessary()                  { if( m_draw_list_sort_needed ) {std::sort(m_draw_list.begin(), m_draw_list.end()); m_draw_list_sort_needed = false;} }
			TDrawList::const_iterator Begin() const { return m_draw_list.begin(); }
			TDrawList::const_iterator End() const   { return m_draw_list.end(); }
		};
	}
}

#endif
