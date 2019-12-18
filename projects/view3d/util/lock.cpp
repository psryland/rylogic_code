//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/util/lock.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/model_buffer.h"

namespace pr::rdr
{
	MLock::MLock(Model* model, EMap map_type, EMapFlags flags)
		:m_local_vlock()
		,m_local_ilock()
		,m_model(model)
		,m_vlock(m_local_vlock)
		,m_ilock(m_local_ilock)
	{
		m_model->MapVerts(m_vlock, map_type, flags);
		m_model->MapIndices(m_ilock, map_type, flags);
	}

	MLock::MLock(Model* model, Lock& vlock, Lock& ilock, EMap map_type, EMapFlags flags)
		:m_local_vlock()
		,m_local_ilock()
		,m_model(model)
		,m_vlock(vlock)
		,m_ilock(ilock)
	{
		if (!m_vlock.data()) m_model->MapVerts(m_vlock, map_type, flags);
		if (!m_ilock.data()) m_model->MapIndices(m_ilock, map_type, flags);
	}
}