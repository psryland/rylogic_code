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

namespace pr
{
	namespace rdr
	{
		Model::Model(MdlSettings const& settings, ModelBufferPtr& model_buffer)
			:m_model_buffer(model_buffer)
			,m_vrange(model_buffer->ReserveVerts  (settings.m_vb.ElemCount))
			,m_irange(model_buffer->ReserveIndices(settings.m_ib.ElemCount))
			,m_nuggets()
			,m_bbox(settings.m_bbox)
			,m_name(settings.m_name)
			,m_dbg_flags(0)
		{}
		Model::~Model()
		{
			DeleteNuggets();
		}

		// Access to the vertex/index buffers
		bool Model::MapVerts(Lock& lock, D3D11_MAP map_type, uint flags, Range vrange)
		{
			if (vrange == RangeZero) vrange  = m_vrange;
			else vrange.shift((int)m_vrange.m_begin);
			return m_model_buffer->MapVerts(lock, map_type, flags, vrange);
		}
		bool Model::MapIndices(Lock& lock, D3D11_MAP map_type, uint flags, Range irange)
		{
			if (irange == RangeZero) irange  = m_irange;
			else irange.shift((int)m_irange.m_begin);
			return m_model_buffer->MapIndices(lock, map_type, flags, irange);
		}

		// Call to create a render nugget for render step 'rstep' from a range within this model that uses 'ddata'.
		// Ranges are model relative, i.e. the first vert in the model is range [0,1)
		// Remember you might need to delete render nuggets first
		void Model::CreateNugget(NuggetProps const& props, Range const* vrange_, Range const* irange_)
		{
			// If ranges aren't provided, use the entire model
			Range vrange, irange;
			if (vrange_) { vrange = *vrange_; vrange.shift((int)m_vrange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_vrange, vrange), "This range exceeds the size of this model"); }
			else         { vrange = m_vrange; }
			if (irange_) { irange = *irange_; irange.shift((int)m_irange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_irange, irange), "This range exceeds the size of this model"); }
			else         { irange = m_irange; }

			// Verify the ranges do not overlap with existing nuggets in this chain, that's probably an error
			#if PR_DBG_RDR
			PR_ASSERT(PR_DBG_RDR, irange.empty() == vrange.empty(), "Illogical combination of Irange and Vrange");
			for (auto& nug : m_nuggets)
			{
				PR_ASSERT(PR_DBG_RDR, !Intersects(irange, nug.m_irange), "A render nugget covering this index range already exists. DeleteNuggets() call may be needed");
			}
			#endif

			// Create the nugget and add it to the model
			if (!irange.empty())
			{
				m_nuggets.push_back(*m_model_buffer->m_mdl_mgr->m_alex_nugget.New(props));
				Nugget& nugget        = m_nuggets.back();
				nugget.m_model_buffer = m_model_buffer;
				nugget.m_vrange       = vrange;
				nugget.m_irange       = irange;
				nugget.m_prim_count   = PrimCount(irange.size(), props.m_topo);
				nugget.m_sort_key     = MakeSortKey(nugget);
				nugget.m_owner        = this;
			}
		}

		// Clear the render nuggets for this model.
		void Model::DeleteNuggets()
		{
			while (!m_nuggets.empty())
				m_model_buffer->m_mdl_mgr->Delete(&m_nuggets.front());
		}

		// Ref-counting cleanup function
		void Model::RefCountZero(pr::RefCount<Model>* doomed)
		{
			Model* mdl = static_cast<Model*>(doomed);
			mdl->m_model_buffer->m_mdl_mgr->Delete(mdl);
		}
	}
}