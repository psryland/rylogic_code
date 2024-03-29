//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/models/model_buffer.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/render/sortkey.h"
#include "pr/view3d/util/lock.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	Model::Model(MdlSettings const& settings, ModelBufferPtr& model_buffer)
		:m_model_buffer(model_buffer)
		,m_vrange(model_buffer->ReserveVerts  (settings.m_vb.ElemCount))
		,m_irange(model_buffer->ReserveIndices(settings.m_ib.ElemCount))
		,m_nuggets()
		,m_bbox(settings.m_bbox)
		,m_name(settings.m_name)
		,m_dbg_flags(EDbgFlags::None)
	{}
	Model::~Model()
	{
		DeleteNuggets();
	}

	// Access the model manager
	Renderer& Model::rdr() const
	{
		return m_model_buffer->rdr();
	}
	ModelManager& Model::mdl_mgr() const
	{
		return m_model_buffer->mdl_mgr();
	}

	// Access to the vertex/index buffers
	bool Model::MapVerts(Lock& lock, EMap map_type, EMapFlags flags, Range vrange)
	{
		if (vrange == RangeZero) vrange  = m_vrange;
		else vrange = Shift(vrange, (int)m_vrange.m_beg);
		return m_model_buffer->MapVerts(lock, map_type, flags, vrange);
	}
	bool Model::MapIndices(Lock& lock, EMap map_type, EMapFlags flags, Range irange)
	{
		if (irange == RangeZero) irange  = m_irange;
		else irange = Shift(irange, (int)m_irange.m_beg);
		return m_model_buffer->MapIndices(lock, map_type, flags, irange);
	}

	// Create a nugget from a range within this model
	// Ranges are model relative, i.e. the first vert in the model is range [0,1)
	// Remember you might need to delete render nuggets first
	void Model::CreateNugget(NuggetProps const& props)
	{
		NuggetData ndata(props);
		PR_ASSERT(PR_DBG_RDR, ndata.m_irange.empty() == ndata.m_vrange.empty(), "Illogical combination of I-Range and V-Range");

		// Empty ranges are assumed to mean the entire model
		if (ndata.m_vrange.empty()) ndata.m_vrange = m_vrange;
		else                        ndata.m_vrange = Shift(ndata.m_vrange, (int)m_vrange.m_beg);
		if (ndata.m_irange.empty()) ndata.m_irange = m_irange;
		else                        ndata.m_irange = Shift(ndata.m_irange, (int)m_irange.m_beg);
		PR_ASSERT(PR_DBG_RDR, IsWithin(m_vrange, ndata.m_vrange), "This range exceeds the size of this model"); 
		PR_ASSERT(PR_DBG_RDR, IsWithin(m_irange, ndata.m_irange), "This range exceeds the size of this model");

		// Verify the ranges do not overlap with existing nuggets in this chain, unless explicitly allowed.
		#if PR_DBG_RDR
		if (!props.m_range_overlaps)
		{
			for (auto& nug : m_nuggets)
			{
				PR_ASSERT(PR_DBG_RDR, !Intersects(ndata.m_irange, nug.m_irange), "A render nugget covering this index range already exists. DeleteNuggets() call may be needed");
			}
		}
		#endif

		// Create the nugget and add it to the model
		if (!ndata.m_irange.empty())
		{
			auto nug = mdl_mgr().CreateNugget(ndata, m_model_buffer.get(), this);
			m_nuggets.push_back(*nug);
		}
	}

	// Clear the render nuggets for this model.
	void Model::DeleteNuggets()
	{
		while (!m_nuggets.empty())
			mdl_mgr().Delete(&m_nuggets.front());
	}

	// Ref-counting clean up function
	void Model::RefCountZero(pr::RefCount<Model>* doomed)
	{
		Model* mdl = static_cast<Model*>(doomed);
		mdl->mdl_mgr().Delete(mdl);
	}
}