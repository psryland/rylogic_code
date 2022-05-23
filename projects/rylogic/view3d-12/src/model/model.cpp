//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/resource/resource_manager.h"

namespace pr::rdr12
{
	Model::Model(ResourceManager& mgr, size_t vcount, size_t icount, int vstride, int istride, ID3D12Resource* vb, ID3D12Resource* ib, BBox const& bbox, char const* name)
		:m_mgr(&mgr)
		, m_vcount(vcount)
		, m_icount(icount)
		, m_vstride(vstride)
		, m_istride(istride)
		, m_vb(vb, true)
		, m_ib(ib, true)
		, m_vb_view({
			.BufferLocation = m_vb->GetGPUVirtualAddress(),
			.SizeInBytes = s_cast<UINT>(m_vcount * m_vstride),
			.StrideInBytes = s_cast<UINT>(m_vstride),
		})
		, m_ib_view({
			.BufferLocation = m_ib->GetGPUVirtualAddress(),
			.SizeInBytes = s_cast<UINT>(m_icount * m_istride),
			.Format =
				m_istride == sizeof(uint32_t) ? DXGI_FORMAT_R32_UINT :
				m_istride == sizeof(uint16_t) ? DXGI_FORMAT_R16_UINT :
				throw std::runtime_error("Unsupported index buffer format"),
		})
		, m_nuggets()
		, m_bbox(bbox)
		, m_name(name)
		, m_dbg_flags(EDbgFlags::None)
	{
		Throw(m_vb->SetName(FmtS(L"%S:VB:%d", name, vcount)));
		Throw(m_ib->SetName(FmtS(L"%S:IB:%d", name, icount)));
	}
	Model::~Model()
	{
		DeleteNuggets();
	}

	// Renderer access
	Renderer& Model::rdr() const
	{
		return m_mgr->rdr();
	}
	ResourceManager& Model::res_mgr() const
	{
		return *m_mgr;
	}
	
	// Create a nugget from a range within this model
	// Ranges are model relative, i.e. the first vert in the model is range [0,1)
	// Remember you might need to delete render nuggets first
	void Model::CreateNugget(NuggetData const& nugget_data)
	{
		NuggetData ndata(nugget_data);

		#if PR_DBG_RDR
		if (ndata.m_vrange.empty() != ndata.m_irange.empty())
			throw std::runtime_error(FmtS("Illogical combination of I-Range and V-Range for nugget (%s)", m_name.c_str()));
		#endif

		// Empty ranges are assumed to mean the entire model
		if (ndata.m_vrange.empty())
			ndata.m_vrange = Range(0, m_vcount);
		if (ndata.m_irange.empty())
			ndata.m_irange = Range(0, m_icount);

		// Verify the ranges do not overlap with existing nuggets in this chain, unless explicitly allowed.
		#if PR_DBG_RDR
		if (!IsWithin(Range(0, m_vcount), ndata.m_vrange))
			throw std::runtime_error(FmtS("V-Range exceeds the size of this model  (%s)", m_name.c_str()));
		if (!IsWithin(Range(0, m_icount), ndata.m_irange))
			throw std::runtime_error(FmtS("I-Range exceeds the size of this model (%s)", m_name.c_str()));
		if (!AllSet(ndata.m_nflags, ENuggetFlag::RangesCanOverlap))
			for (auto& nug : m_nuggets)
				if (Intersects(ndata.m_irange, nug.m_irange))
					throw std::runtime_error(FmtS("A render nugget covering this index range already exists. DeleteNuggets() call may be needed (%s)", m_name.c_str()));
		#endif

		// Defend against crashes in release...
		if (!ndata.m_irange.empty())
		{
			auto nug = res_mgr().CreateNugget(ndata, this);
			m_nuggets.push_back(*nug);
		}
	}

	// Clear the render nuggets for this model.
	void Model::DeleteNuggets()
	{
		for (;!m_nuggets.empty();)
			res_mgr().Delete(&m_nuggets.front());
	}

	// Ref-counting clean up function
	void Model::RefCountZero(RefCounted<Model>* doomed)
	{
		auto mdl = static_cast<Model*>(doomed);
		mdl->res_mgr().Delete(mdl);
	}
}
