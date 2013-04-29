//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/wrappers.h"

using namespace pr::rdr;

pr::rdr::Model::Model()
	:m_model_buffer()
	,m_vrange()
	,m_irange()
	,m_nuggets()
	,m_bbox(pr::BBoxReset)
	,m_name()
	,m_dbg_flags(0)
{}
pr::rdr::Model::~Model()
{
	DeleteNuggets();
}

// Access to the vertex/index buffers
bool pr::rdr::Model::MapVerts(Lock& lock, D3D11_MAP map_type, uint flags, Range vrange)
{
	if (vrange == RangeZero) vrange  = m_vrange;
	else vrange.shift((int)m_vrange.m_begin);
	return m_model_buffer->MapVerts(lock, map_type, flags, vrange);
}
bool pr::rdr::Model::MapIndices(Lock& lock, D3D11_MAP map_type, uint flags, Range irange)
{
	if (irange == RangeZero) irange  = m_irange;
	else irange.shift((int)m_irange.m_begin);
	return m_model_buffer->MapIndices(lock, map_type, flags, irange);
}

// Call to create a render nugget from a range within this model that uses 'material'
// Ranges are model relative, i.e. the first vert in the model is range [0,1)
void pr::rdr::Model::CreateNugget(pr::rdr::DrawMethod const& meth, D3D11_PRIMITIVE_TOPOLOGY prim_type, Range const* vrange_, Range const* irange_)
{
	PR_ASSERT(PR_DBG_RDR, meth.m_shader != 0, "The draw method must contain a shader");

	Range vrange, irange;
	if (vrange_) { vrange = *vrange_; vrange.shift((int)m_vrange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_vrange, vrange), "This range exceeds the size of this model"); }
	else         { vrange = m_vrange; }
	if (irange_) { irange = *irange_; irange.shift((int)m_irange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_irange, irange), "This range exceeds the size of this model"); }
	else         { irange = m_irange; }

	// Verify the ranges do not overlap existing nuggets, that's probably an error
	#if PR_DBG_RDR
	PR_ASSERT(PR_DBG_RDR, irange.empty() == vrange.empty(), "Illogical combination of Irange and Vrange");
	for (TNuggetChain::const_iterator i = m_nuggets.begin(), iend = m_nuggets.end(); i != iend; ++i)
		PR_ASSERT(PR_DBG_RDR, !Intersect(irange, i->m_irange), "A render nugget covering this index range already exists. DeleteNuggets() call may be needed");
	#endif

	if (!irange.empty())
	{
		m_nuggets.push_back(*m_model_buffer->m_mdl_mgr->m_alex_nugget.New());
		Nugget& nugget        = m_nuggets.back();
		nugget.m_model_buffer = m_model_buffer;
		nugget.m_vrange       = vrange;
		nugget.m_irange       = irange;
		nugget.m_prim_topo    = prim_type;
		nugget.m_prim_count   = PrimCount(irange.size(), prim_type);
		nugget.m_draw         = meth;
		nugget.m_sort_key     = MakeSortKey(nugget);
		nugget.m_owner        = this;
	}
}

// Clear the render nuggets for this model.
void pr::rdr::Model::DeleteNuggets()
{
	while (!m_nuggets.empty())
		m_model_buffer->m_mdl_mgr->Delete(&m_nuggets.front());
}

// Refcounting cleanup function
void pr::rdr::Model::RefCountZero(pr::RefCount<Model>* doomed)
{
	pr::rdr::Model* mdl = static_cast<pr::rdr::Model*>(doomed);
	mdl->m_model_buffer->m_mdl_mgr->Delete(mdl);
}