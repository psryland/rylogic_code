//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
#include "renderer/utility/stdafx.h"
#include "pr/renderer/viewport/drawlist.h"
#include "pr/renderer/viewport/sortkey.h"
#include "pr/renderer/renderer/renderer.h"
#include "pr/renderer/instances/instance.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::instance;

// Constructor
Drawlist::Drawlist(Renderer& rdr, ViewportId viewport_id)
:m_viewport_id(viewport_id)
,m_draw_list()
,m_draw_list_sort_needed(true)
,m_rdr(&rdr)
{
	Clear();
}
Drawlist::~Drawlist()
{
	Clear();
}
	
// Reset the drawlist
void Drawlist::Clear()
{
	m_draw_list.resize(0);
}
	
// Add an instance to the draw list. Instances persist in the
// drawlist until they are removed or 'Clear()' is called.
void Drawlist::AddInstance(Base const& instance)
{
	ModelPtr const& model = instance::GetModel(instance);
	#if PR_DBG_RDR
	if (model->m_render_nugget.empty() && (model->m_dbg_flags & EDbgRdrFlags_WarnedNoRenderNuggets) == 0)
	{
		PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') has no nuggets, you need to call SetMaterial() on the model first\n", model->m_name.c_str()));
		model->m_dbg_flags |= EDbgRdrFlags_WarnedNoRenderNuggets;
	}
	PR_ASSERT(PR_DBG_RDR, FEql(instance::GetI2W(instance).w.w, 1.0f), "Invalid instance transform");
	#endif

	// See if the instance has a sort key override
	sort_key::Override const* sko = instance::FindCpt<sort_key::Override>(instance, ECpt_SortkeyOverride);

	// Add the drawlist elements for this instance that
	// correspond to the render nuggets of the renderable
	m_draw_list.reserve(m_draw_list.size() + model->m_render_nugget.size());
	for (TNuggetChain::const_iterator n = model->m_render_nugget.begin(), n_end = model->m_render_nugget.end(); n != n_end; ++n)
	{
		DrawListElement element;
		element.m_instance = &instance;
		element.m_nugget   = &*n;
		element.m_sort_key = n->m_sort_key;
		if (sko) { element.m_sort_key = sko->Combine(element.m_sort_key); }
		m_draw_list.push_back_fast(element);
	}

	m_draw_list_sort_needed = true;
}
	
// Remove an instance from the drawlist
struct MatchInst
{
	instance::Base const* m_inst;
	MatchInst(instance::Base const& inst) :m_inst(&inst) {}
	bool operator() (DrawListElement const& dle) const   { return dle.m_instance == m_inst; }
};
void Drawlist::RemoveInstance(instance::Base const& instance)
{
	TDrawList::iterator new_end = std::remove_if(m_draw_list.begin(), m_draw_list.end(), MatchInst(instance));
	m_draw_list.resize(new_end - m_draw_list.begin());
}
	
// Add a batch of instances to the draw list. Optimise by only resizing the drawlist once
void Drawlist::AddInstanceBatch(instance::Base const** instance, std::size_t count)
{
	instance;
	count;
	PR_STUB_FUNC();
}
	
// Remove a batch of instances from the draw list. Optimise by a single past through the drawlist
struct IsInstanceInBatch
{
	typedef pr::Array<instance::Base const*, 64, true> TInstBatch;
	TInstBatch m_batch;
	IsInstanceInBatch(instance::Base const** instance, std::size_t count) :m_batch(instance, instance + count) { std::sort(m_batch.begin(), m_batch.end()); }
	bool operator () (DrawListElement const& dle) const
	{
		TInstBatch::const_iterator iter = std::lower_bound(m_batch.begin(), m_batch.end(), dle.m_instance);
		return iter != m_batch.end() && *iter == dle.m_instance;
	}
};
void Drawlist::RemoveInstanceBatch(instance::Base const** instance, std::size_t count)
{
	PR_ASSERT(PR_DBG_RDR, count <= 64, "Max of 64 instances per batch");
	TDrawList::iterator new_end = std::remove_if(m_draw_list.begin(), m_draw_list.end(), IsInstanceInBatch(instance, count));
	m_draw_list.resize(m_draw_list.end() - new_end);
}





