//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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

		// Create a nugget from a range within this model
		// Ranges are model relative, i.e. the first vert in the model is range [0,1)
		// Remember you might need to delete render nuggets first
		void Model::CreateNugget(NuggetProps props)
		{
			PR_ASSERT(PR_DBG_RDR, props.m_irange.empty() == props.m_vrange.empty(), "Illogical combination of Irange and Vrange");

			// Empty ranges are assumed to mean the entire model
			if (props.m_vrange.empty()) props.m_vrange = m_vrange;
			else                        props.m_vrange.shift((int)m_vrange.m_begin);
			if (props.m_irange.empty()) props.m_irange = m_irange;
			else                        props.m_irange.shift((int)m_irange.m_begin);
			PR_ASSERT(PR_DBG_RDR, IsWithin(m_vrange, props.m_vrange), "This range exceeds the size of this model"); 
			PR_ASSERT(PR_DBG_RDR, IsWithin(m_irange, props.m_irange), "This range exceeds the size of this model");

			// Verify the ranges do not overlap with existing nuggets in this chain, unless explicitly allowed.
			#if PR_DBG_RDR
			if (!props.m_range_overlaps)
			{
				for (auto& nug : m_nuggets)
				{
					PR_ASSERT(PR_DBG_RDR, !Intersects(props.m_irange, nug.m_irange), "A render nugget covering this index range already exists. DeleteNuggets() call may be needed");
				}
			}
			#endif

			// Create the nugget and add it to the model
			if (!props.m_irange.empty())
			{
				m_nuggets.push_back(*m_model_buffer->m_mdl_mgr->m_alex_nugget.New(props));
				Nugget& nugget        = m_nuggets.back();
				nugget.m_model_buffer = m_model_buffer;
				nugget.m_prim_count   = PrimCount(props.m_irange.size(), props.m_topo);
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