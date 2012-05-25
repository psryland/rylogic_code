//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/models/model.h"
#include "pr/renderer/models/modelmanager.h"
#include "pr/renderer/models/modelbuffer.h"
#include "pr/renderer/viewport/sortkey.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::model;

namespace pr
{
	namespace rdr
	{
		// Return the number of primitives implied by an index count and a primitive type
		inline size_t PrimitiveCount(model::EPrimitive::Type prim_type, size_t icount)
		{
			if (icount == 0) return 0;
			switch (prim_type)
			{
			default:
			case model::EPrimitive::Invalid:       PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
			case model::EPrimitive::PointList:     return icount;
			case model::EPrimitive::LineStrip:     PR_ASSERT(PR_DBG_RDR,  icount    >= 2, "Incomplete primitive implied by icount"); return icount - 1;
			case model::EPrimitive::LineList:      PR_ASSERT(PR_DBG_RDR, (icount%2) == 0, "Incomplete primitive implied by icount"); return icount / 2;
			case model::EPrimitive::TriangleStrip: PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by icount"); return icount - 2;
			case model::EPrimitive::TriangleFan:   PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by icount"); return icount - 2;
			case model::EPrimitive::TriangleList:  PR_ASSERT(PR_DBG_RDR, (icount%3) == 0, "Incomplete primitive implied by icount"); return icount / 3;
			}
		}
		
		// Return the number of indices required for 'prim_count' primitives of type 'prim_type'
		inline size_t IndexCount(model::EPrimitive::Type prim_type, size_t prim_count)
		{
			if (prim_count == 0) return 0;
			switch (prim_type)
			{
			default:
			case model::EPrimitive::Invalid:       PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
			case model::EPrimitive::PointList:     return prim_count;
			case model::EPrimitive::LineStrip:     return prim_count + 1;
			case model::EPrimitive::LineList:      return prim_count * 2;
			case model::EPrimitive::TriangleStrip: return prim_count + 2;
			case model::EPrimitive::TriangleFan:   return prim_count + 2;
			case model::EPrimitive::TriangleList:  return prim_count * 3;
			}
		}
	}
}

// Constructor
pr::rdr::Model::Model()
:m_model_buffer(0)
,m_Vrange(RangeZero)
,m_Irange(RangeZero)
,m_render_nugget()
,m_bbox(BBoxReset)
,m_name()
,m_dbg_flags(0)
{}

// Access the vertex buffer. 'offset' is in verts not bytes
pr::rdr::vf::iterator pr::rdr::Model::LockVBuffer(VLock& lock, model::Range v_range, uint flags)
{
	if (v_range == RangeZero) v_range  = m_Vrange;
	else                      v_range.shift((int)m_Vrange.m_begin);
	return m_model_buffer->LockVBuffer(lock, v_range, flags);
}
	
// Access the index buffer. 'offset' is in indices not bytes
pr::rdr::Index* pr::rdr::Model::LockIBuffer(ILock& lock, model::Range i_range, uint flags)
{
	if (i_range == RangeZero) i_range  = m_Irange;
	else                      i_range.shift((int)m_Irange.m_begin);
	return m_model_buffer->LockIBuffer(lock, i_range, flags);
}
	
// Clear the render nuggets for this model.
void pr::rdr::Model::DeleteRenderNuggets()
{
	while (!m_render_nugget.empty())
		m_model_buffer->m_mdl_mgr->Delete(&m_render_nugget.front());
}
	
// Set the material (i.e. create a single render nugget) for a range of vertices and indices
// Ranges are model relative, i.e. the first vert in the model is range [0,1)
void pr::rdr::Model::SetMaterial(pr::rdr::Material const& material, model::EPrimitive::Type prim_type, bool delete_existing_nuggets, model::Range const* v_range, model::Range const* i_range)
{
	model::Range vrange, irange;
	if (v_range) { vrange = *v_range; vrange.shift((int)m_Vrange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_Vrange, vrange), "This range exceeds the size of this model"); }
	else         { vrange = m_Vrange; }
	if (i_range) { irange = *i_range; irange.shift((int)m_Irange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_Irange, irange), "This range exceeds the size of this model"); }
	else         { irange = m_Irange; }
	
	if (delete_existing_nuggets)
		DeleteRenderNuggets();
	
	// Verify the ranges do not overlap existing nuggets, that's probably an error
	#if PR_DBG_RDR
	PR_ASSERT(PR_DBG_RDR, irange.empty() == vrange.empty(), "Illogical combination of Irange and Vrange");
	for (TNuggetChain::const_iterator i = m_render_nugget.begin(), iend = m_render_nugget.end(); i != iend; ++i)
		PR_ASSERT(PR_DBG_RDR, !Intersect(irange, i->m_Irange), "A render nugget covering this index range already exists. DeleteRenderNuggets() call may be needed");
	#endif
	
	if (!irange.empty())
	{
		m_render_nugget.push_back(*m_model_buffer->m_mdl_mgr->NewRenderNugget());
		RenderNugget& nugget     = m_render_nugget.back();
		nugget.m_model_buffer    = m_model_buffer;
		nugget.m_Vrange          = vrange;
		nugget.m_Irange          = irange;
		nugget.m_primitive_type  = prim_type;
		nugget.m_primitive_count = PrimitiveCount(prim_type, irange.size());
		nugget.m_material        = material;
		nugget.m_sort_key        = sort_key::make(nugget);
		nugget.m_owner           = this;
	}
}
	
// Return the vertex format for the model
pr::rdr::vf::Type Model::GetVertexType() const
{
	PR_ASSERT(PR_DBG_RDR, m_model_buffer, "");
	return m_model_buffer->GetVertexType();
}
	
// Refcounting cleanup function
void pr::rdr::Model::RefCountZero(pr::RefCount<Model>* doomed)
{
	pr::rdr::Model* mdl = static_cast<pr::rdr::Model*>(doomed);
	mdl->m_model_buffer->m_mdl_mgr->Delete(mdl);
}

