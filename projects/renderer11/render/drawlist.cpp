//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/drawlist.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/model.h"

using namespace pr::rdr;

pr::rdr::Drawlist::Drawlist(Renderer& rdr)
:m_dle(rdr.Allocator<DrawListElement>())
,m_sort_needed(true)
,m_rdr(&rdr)
{
	Clear();
}

// Add an instance to the draw list. Instances persist in the
// drawlist until they are removed or 'Clear()' is called.
void pr::rdr::Drawlist::Add(BaseInstance const& inst)
{
	ModelPtr const& model = GetModel(inst);
	PR_ASSERT(PR_DBG_RDR, model != 0, "Null model pointer");
	
	#if PR_DBG_RDR
	if (model->m_nuggets.empty() && (model->m_dbg_flags & EDbgRdrFlags::WarnedNoRenderNuggets) == 0)
	{
		PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') has no nuggets, you need to call SetMaterial() on the model first\n", model->m_name.c_str()));
		model->m_dbg_flags |= EDbgRdrFlags::WarnedNoRenderNuggets;
	}
	PR_ASSERT(PR_DBG_RDR, FEql(GetO2W(inst).w.w, 1.0f), "Invalid instance transform");
	#endif

	// See if the instance has a sort key override
	SKOverride const* sko = inst.find<SKOverride>(EInstComp::SortkeyOverride);

	// Add the drawlist elements for this instance that
	// correspond to the render nuggets of the renderable
	m_dle.reserve(m_dle.size() + model->m_nuggets.size());
	for (TNuggetChain::const_iterator n = model->m_nuggets.begin(), n_end = model->m_nuggets.end(); n != n_end; ++n)
	{
		DrawListElement element;
		element.m_instance = &inst;
		element.m_nugget   = &*n;
		element.m_sort_key = n->m_sort_key;
		if (sko) { element.m_sort_key = sko->Combine(element.m_sort_key); }
		m_dle.push_back_fast(element);
	}

	m_sort_needed = true;
}

// Remove an instance from the drawlist
void pr::rdr::Drawlist::Remove(BaseInstance const& inst)
{
	auto new_end = std::remove_if(std::begin(m_dle), std::end(m_dle), [&](DrawListElement const& dle){ return dle.m_instance == &inst; });
	//TDrawList::iterator new_end = std::remove_if(m_draw_list.begin(), m_draw_list.end(), MatchInst(instance));
	m_dle.resize(new_end - m_dle.begin());
}

// Remove a batch of instances from the draw list. Optimise by a single past through the drawlist
void pr::rdr::Drawlist::Remove(BaseInstance const** inst, std::size_t count)
{
	// Make a sorted list from the batch to remove
	BaseInstance const** doomed = PR_ALLOCA_POD(BaseInstance const* , count);
	BaseInstance const** doomed_end = doomed + count;
	std::copy(inst, inst + count, doomed);
	std::sort(doomed, doomed_end);
	
	// Remove instances from m_dle
	auto new_end = std::remove_if(std::begin(m_dle), std::end(m_dle), [&](DrawListElement const& dle)->bool
	{
		auto iter = std::lower_bound(doomed, doomed_end, dle.m_instance);
		return iter != doomed_end && *iter == dle.m_instance;
	});
	m_dle.resize(m_dle.end() - new_end);
}


