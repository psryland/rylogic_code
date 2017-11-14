//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/model_buffer.h"

namespace pr
{
	namespace rdr
	{
		MLock::MLock(Model* model, D3D11_MAP map_type, UINT flags)
			:m_local_vlock()
			,m_local_ilock()
			,m_model(model)
			,m_vlock(m_local_vlock)
			,m_ilock(m_local_ilock)
		{
			m_model->MapVerts(m_vlock, map_type, flags);
			m_model->MapIndices(m_ilock, map_type, flags);
		}

		MLock::MLock(Model* model, Lock& vlock, Lock& ilock, D3D11_MAP map_type, UINT flags)
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
}