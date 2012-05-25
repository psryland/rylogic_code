//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/model_buffer.h"

using namespace pr::rdr;

pr::rdr::MLock::MLock(ModelPtr& model, D3D11_MAP map_type, UINT flags)
:m_local_vlock()
,m_local_ilock()
,m_model(model)
,m_vlock(m_local_vlock)
,m_ilock(m_local_ilock)
,m_vrange()
,m_irange()
{
	m_model->MapVerts  (m_vlock, map_type, flags);
	m_model->MapIndices(m_ilock, map_type, flags);
	m_vrange = m_vlock.m_range;
	m_irange = m_ilock.m_range;
}

pr::rdr::MLock::MLock(ModelPtr& model, Lock& vlock, Lock& ilock, D3D11_MAP map_type, UINT flags)
:m_local_vlock()
,m_local_ilock()
,m_model(model)
,m_vlock(vlock)
,m_ilock(ilock)
,m_vrange()
,m_irange()
{
	if (!m_vlock.ptr<void>()) m_model->MapVerts  (m_vlock, map_type, flags);
	if (!m_ilock.ptr<void>()) m_model->MapIndices(m_ilock, map_type, flags);
	m_vrange = m_vlock.m_range;
	m_irange = m_ilock.m_range;
}

pr::rdr::MLock::MLock(ModelPtr& model, Range const& vrange, Range const& irange, D3D11_MAP map_type, UINT flags)
:m_local_vlock()
,m_local_ilock()
,m_model(model)
,m_vlock(m_local_vlock)
,m_ilock(m_local_ilock)
,m_vrange(vrange)
,m_irange(irange)
{
	m_model->MapVerts  (m_vlock, map_type, flags, m_vrange);
	m_model->MapIndices(m_ilock, map_type, flags, m_irange);
	m_vrange = m_vlock.m_range;
	m_irange = m_ilock.m_range;
}

pr::rdr::MLock::MLock(ModelPtr& model, Lock& vlock, Lock& ilock, Range const& vrange, Range const& irange, D3D11_MAP map_type, UINT flags)
:m_local_vlock()
,m_local_ilock()
,m_model(model)
,m_vlock(vlock)
,m_ilock(ilock)
,m_vrange(vrange)
,m_irange(irange)
{
	if (!m_vlock.ptr<void>()) m_model->MapVerts  (m_vlock, map_type, flags, m_vrange);
	if (!m_ilock.ptr<void>()) m_model->MapIndices(m_ilock, map_type, flags, m_irange);
}

