//**************************************************************************
//
//	Draw list element
//
//**************************************************************************

#ifndef PR_RDR_DRAW_LIST_ELEMENT_H
#define PR_RDR_DRAW_LIST_ELEMENT_H

#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/Materials/Material.h"
#include "PR/Renderer/Instance.h"
#include "PR/Renderer/RenderNugget.h"

namespace pr
{
	namespace rdr
	{
		struct DrawListElement
		{
			struct pr_is_pod { enum { value = true }; };

			Material GetMaterial() const	{ return m_instance->GetMaterial(m_nugget->m_attribute->m_mat_index); }

			const RenderNugget*	m_nugget;
			const InstanceBase*	m_instance;

			// A list of draw list elements corresponding to a particular instance
			DrawListElement* m_instance_next;

			// The position of this draw list element in the draw list
			DrawListElement* m_drawlist_next;
			DrawListElement* m_drawlist_prev;
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_DRAW_LIST_ELEMENT_H
