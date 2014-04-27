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
			,m_nmap(model_buffer->m_mdl_mgr->m_alex_model)
			,m_bbox(pr::BBoxReset)
			,m_name(settings.m_name)
			,m_dbg_flags(0)
		{}
		Model::~Model()
		{
			DeleteAllNuggets();
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

		// Call to create a render nugget for render step 'rstep' from a range within this model that uses 'meth'.
		// Ranges are model relative, i.e. the first vert in the model is range [0,1)
		// Remember you might need to delete render nuggets first
		void Model::CreateNugget(ERenderStep::Enum_ rstep, DrawMethod const& meth, EPrim prim_type, Range const* vrange_, Range const* irange_)
		{
			PR_ASSERT(PR_DBG_RDR, meth.m_shader != 0, "The draw method must contain a shader");

			// If ranges aren't provided, use the entire model
			Range vrange, irange;
			if (vrange_) { vrange = *vrange_; vrange.shift((int)m_vrange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_vrange, vrange), "This range exceeds the size of this model"); }
			else         { vrange = m_vrange; }
			if (irange_) { irange = *irange_; irange.shift((int)m_irange.m_begin); PR_ASSERT(PR_DBG_RDR, IsWithin(m_irange, irange), "This range exceeds the size of this model"); }
			else         { irange = m_irange; }

			// Find the chain in the nugget map for the given render step
			auto& nugget_chain = m_nmap[rstep];

			// Verify the ranges do not overlap with existing nuggets in this chain, that's probably an error
			#if PR_DBG_RDR
			PR_ASSERT(PR_DBG_RDR, irange.empty() == vrange.empty(), "Illogical combination of Irange and Vrange");
			for (auto& nug : nugget_chain)
			{
				PR_ASSERT(PR_DBG_RDR, !Intersects(irange, nug.m_irange), "A render nugget covering this index range already exists. DeleteNuggets() call may be needed");
			}
			#endif

			if (!irange.empty())
			{
				nugget_chain.push_back(*m_model_buffer->m_mdl_mgr->m_alex_nugget.New());
				Nugget& nugget        = nugget_chain.back();
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
		void Model::DeleteNuggets(ERenderStep::Enum_ rstep)
		{
			auto& nuggets = m_nmap[rstep];
			while (!nuggets.empty())
				m_model_buffer->m_mdl_mgr->Delete(&nuggets.front());
		}
		void Model::DeleteAllNuggets()
		{
			for (auto& map : m_nmap)
				DeleteNuggets(map.first);
		}

		// Ref-counting cleanup function
		void Model::RefCountZero(pr::RefCount<Model>* doomed)
		{
			Model* mdl = static_cast<Model*>(doomed);
			mdl->m_model_buffer->m_mdl_mgr->Delete(mdl);
		}
	}
}