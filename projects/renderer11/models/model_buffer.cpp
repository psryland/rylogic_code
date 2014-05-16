//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/util.h"

namespace pr
{
	namespace rdr
	{
		ModelBuffer::ModelBuffer()
			:m_vb()
			,m_ib()
			,m_mdl_mgr()
		{}

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
			PR_ASSERT(PR_DBG_RDR, IsRoomFor(vcount, 0), "Insufficent vertex space in this model buffer");
			Range range = Range::make(m_vb.m_used.size(), m_vb.m_used.size() + vcount);
			m_vb.m_used.m_end += vcount;
			return range;
		}

		// Reserve indices from this model buffer
		Range ModelBuffer::ReserveIndices(size_t icount)
		{
			PR_ASSERT(PR_DBG_RDR, IsRoomFor(0, icount), "Insufficent index space in this model buffer");
			Range range = Range::make(m_ib.m_used.size(), m_ib.m_used.size() + icount);
			m_ib.m_used.m_end += icount;
			return range;
		}

		// Access to the vertex/index buffers
		// Only return false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' flag is set, all other fail cases throw
		bool ModelBuffer::MapVerts(Lock& lock, D3D11_MAP map_type, UINT flags, Range vrange)
		{
			PR_ASSERT(PR_DBG_RDR, m_vb, "This model buffer has not been created");
			PR_ASSERT(PR_DBG_RDR, lock.m_res == 0, "This lock has already been used, make a new one");

			// If no subrange is given, use the entire buffer
			if (vrange == RangeZero) vrange = m_vb.m_used;

			D3DPtr<ID3D11DeviceContext> dc = ImmediateDC(m_mdl_mgr->m_device);
			D3DPtr<ID3D11Resource> res = m_vb;
			return lock.Map(dc, res, 0, map_type, flags, m_vb.m_stride, vrange);
		}
		bool ModelBuffer::MapIndices(Lock& lock, D3D11_MAP map_type, UINT flags, Range irange)
		{
			PR_ASSERT(PR_DBG_RDR, m_ib, "This model buffer has not been created");
			PR_ASSERT(PR_DBG_RDR, lock.m_res == 0, "This lock has already been used, make a new one");

			// If no subrange is given, use the entire buffer
			if (irange == RangeZero) irange = m_ib.m_used;

			D3DPtr<ID3D11DeviceContext> dc = ImmediateDC(m_mdl_mgr->m_device);
			D3DPtr<ID3D11Resource> res = m_ib;
			return lock.Map(dc, res, 0, map_type, flags, BytesPerPixel(m_ib.m_format), irange);
		}

		// Refcounting cleanup function
		void ModelBuffer::RefCountZero(pr::RefCount<ModelBuffer>* doomed)
		{
			ModelBuffer* mb = static_cast<ModelBuffer*>(doomed);
			mb->m_mdl_mgr->Delete(mb);
		}
	}
}