//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
// Notes:
//  All buffers exposed to the client use D3DPOOL_MANAGED so that the client can
//  use the renderer blissfully unaware of device lost and device resets.
	
#include "renderer/utility/stdafx.h"
#include "pr/renderer/models/modelbuffer.h"
#include "pr/renderer/models/modelmanager.h"
	
using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::model;
	
// Constructor
pr::rdr::ModelBuffer::ModelBuffer()
:m_vertex_type(pr::rdr::vf::EVertType::Invalid)
,m_Vbuffer(0)
,m_Ibuffer(0)
,m_mdl_mgr(0)
,m_Vrange(RangeZero)
,m_Irange(RangeZero)
,m_Vused(RangeZero)
,m_Iused(RangeZero)
{}
	
// Lock the vertex buffer so that vertices can be added
pr::rdr::vf::iterator pr::rdr::ModelBuffer::LockVBuffer(VLock& lock, model::Range v_range, std::size_t flags)
{
	PR_ASSERT(PR_DBG_RDR, m_Vbuffer, "This model buffer has not been created");
	PR_ASSERT(PR_DBG_RDR, !lock.m_buffer, "This lock has already been used, make a new one");
	PR_ASSERT(PR_DBG_RDR, IsWithin(m_Vused, v_range), "Lock range exceeds the used size of this model buffer");
	
	if (v_range == RangeZero) lock.m_range = m_Vused;
	else                      lock.m_range = v_range;
	
	void* vbuffer = 0;
	std::size_t vertex_size = vf::GetSize(m_vertex_type);
	pr::Throw(m_Vbuffer->Lock((UINT)(lock.m_range.m_begin * vertex_size), (UINT)(lock.m_range.size() * vertex_size), &vbuffer, (DWORD)flags));
	lock.m_buffer = m_Vbuffer;
	lock.m_ptr    = vf::iterator(vbuffer, m_vertex_type);
	return lock.m_ptr;
}
	
// Lock the index buffer so that indices can be added. Note: pointer returned points to the beginning of the locked range
pr::rdr::Index* pr::rdr::ModelBuffer::LockIBuffer(ILock& lock, model::Range i_range, std::size_t flags)
{
	PR_ASSERT(PR_DBG_RDR, m_Ibuffer, "This model buffer has not been created");
	PR_ASSERT(PR_DBG_RDR, !lock.m_buffer, "This lock has already been used, make a new one");
	PR_ASSERT(PR_DBG_RDR, IsWithin(m_Iused, i_range), "Lock range exceeds the used size of this model buffer");

	if (i_range == RangeZero) lock.m_range = m_Iused;
	else                      lock.m_range = i_range;

	Index* ibuffer = 0;
	pr::Throw(m_Ibuffer->Lock((UINT)lock.m_range.m_begin * sizeof(Index), (UINT)lock.m_range.size() * sizeof(Index), (void**)&ibuffer, (DWORD)flags));
	lock.m_buffer = m_Ibuffer;
	lock.m_ptr    = ibuffer;
	return lock.m_ptr;
}
	
// Return true if 'settings' is the same as the settings used to create this model buffer
bool pr::rdr::ModelBuffer::IsCompatible(model::Settings const& settings) const
{
	// The vertex buffer should be the same
	D3DINDEXBUFFER_DESC Idesc;
	pr::Throw(m_Ibuffer->GetDesc(&Idesc));
	return m_vertex_type == settings.m_vertex_type && settings.m_usage == Idesc.Usage;
}
	
// Return true if there is room for 'Vcount' more vertices and 'Icount' more indices in this model buffer
bool pr::rdr::ModelBuffer::IsRoomFor(std::size_t Vcount, std::size_t Icount) const
{
	return m_Vused.size() + Vcount <= m_Vrange.size() &&
	       m_Iused.size() + Icount <= m_Irange.size();
}
	
// Reserve vertices from this model buffer
pr::rdr::model::Range pr::rdr::ModelBuffer::AllocateVertices(std::size_t Vcount)
{
	PR_ASSERT(PR_DBG_RDR, IsRoomFor(Vcount, 0), "Insufficent vertex space in this model buffer");
	pr::rdr::model::Range range = pr::rdr::model::Range::make(m_Vused.size(), m_Vused.size() + Vcount);
	m_Vused.m_end += Vcount;
	return range;
}
	
// Reserve indices from this model buffer
pr::rdr::model::Range pr::rdr::ModelBuffer::AllocateIndices(std::size_t Icount)
{
	PR_ASSERT(PR_DBG_RDR, IsRoomFor(0, Icount), "Insufficent index space in this model buffer");
	pr::rdr::model::Range range = pr::rdr::model::Range::make(m_Iused.size(), m_Iused.size() + Icount);
	m_Iused.m_end += Icount;
	return range;
}
	
// Refcounting cleanup function
void pr::rdr::ModelBuffer::RefCountZero(pr::RefCount<ModelBuffer>* doomed)
{
	pr::rdr::ModelBuffer* mb = static_cast<pr::rdr::ModelBuffer*>(doomed);
	mb->m_mdl_mgr->Delete(mb);
}
