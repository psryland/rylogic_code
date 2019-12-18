//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/util.h"

namespace pr::rdr
{
	ModelBuffer::ModelBuffer()
		:m_vb()
		,m_ib()
		,m_mdl_mgr()
	{}

	Renderer& ModelBuffer::rdr() const
	{
		return m_mdl_mgr->rdr();
	}
	ModelManager& ModelBuffer::mdl_mgr() const
	{
		return *m_mdl_mgr;
	}

	// Returns true if 'settings' describe a model format that is compatible with this model buffer
	bool ModelBuffer::IsCompatible(MdlSettings const& settings) const
	{
		if (m_vb == 0 || m_ib == 0)
			return false;

		for (int i = 0; i != 2; ++i)
		{
			D3D11_BUFFER_DESC lhs; i == 0 ? m_vb->GetDesc(&lhs) : m_ib->GetDesc(&lhs);
			D3D11_BUFFER_DESC const& rhs(i == 0 ? (D3D11_BUFFER_DESC const&)settings.m_vb : (D3D11_BUFFER_DESC const&)settings.m_ib);
			if (lhs.Usage != rhs.Usage) return false;
		}
		return true;
	}

	// Returns true if there is enough free space in this model for 'vcount' verts and 'icount' indices
	bool ModelBuffer::IsRoomFor(size_t vcount, size_t icount) const
	{
		return
			m_vb.m_used.size() + vcount <= m_vb.m_range.size() &&
			m_ib.m_used.size() + icount <= m_ib.m_range.size();
	}

	// Reserve vertices from this model buffer
	Range ModelBuffer::ReserveVerts(size_t vcount)
	{
		PR_ASSERT(PR_DBG_RDR, IsRoomFor(vcount, 0), "Insufficient vertex space in this model buffer");
		Range range(m_vb.m_used.size(), m_vb.m_used.size() + vcount);
		m_vb.m_used.m_end += vcount;
		return range;
	}

	// Reserve indices from this model buffer
	Range ModelBuffer::ReserveIndices(size_t icount)
	{
		PR_ASSERT(PR_DBG_RDR, IsRoomFor(0, icount), "Insufficient index space in this model buffer");
		Range range(m_ib.m_used.size(), m_ib.m_used.size() + icount);
		m_ib.m_used.m_end += icount;
		return range;
	}

	// Access to the vertex/index buffers
	// Only return false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' flag is set, all other fail cases throw
	bool ModelBuffer::MapVerts(Lock& lock, EMap map_type, EMapFlags flags, Range vrange)
	{
		PR_ASSERT(PR_DBG_RDR, m_vb, "This model buffer has not been created");
		PR_ASSERT(PR_DBG_RDR, lock.m_res == 0, "This lock has already been used, make a new one");
		Renderer::Lock rdrlock(m_mdl_mgr->m_rdr);

		// If no sub range is given, use the entire buffer
		if (vrange == RangeZero) vrange = m_vb.m_used;

		auto dc = rdrlock.ImmediateDC();
		return lock.Map(dc, m_vb.get(), 0, m_vb.m_stride, map_type, flags, vrange);
	}
	bool ModelBuffer::MapIndices(Lock& lock, EMap map_type, EMapFlags flags, Range irange)
	{
		PR_ASSERT(PR_DBG_RDR, m_ib, "This model buffer has not been created");
		PR_ASSERT(PR_DBG_RDR, lock.m_res == 0, "This lock has already been used, make a new one");
		Renderer::Lock rdrlock(m_mdl_mgr->m_rdr);

		// If no sub range is given, use the entire buffer
		if (irange == RangeZero) irange = m_ib.m_used;

		auto dc = rdrlock.ImmediateDC();
		return lock.Map(dc, m_ib.get(), 0, BytesPerPixel(m_ib.m_format), map_type, flags, irange);
	}

	// Ref counting clean up function
	void ModelBuffer::RefCountZero(pr::RefCount<ModelBuffer>* doomed)
	{
		ModelBuffer* mb = static_cast<ModelBuffer*>(doomed);
		mb->m_mdl_mgr->Delete(mb);
	}
}