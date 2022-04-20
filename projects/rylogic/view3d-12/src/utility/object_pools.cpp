//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"

namespace pr::rdr12
{
	// Command allocator scope
	CmdAllocScope::CmdAllocScope(CmdAllocPool& pool, CmdAllocSyncPair cmd_alloc, Window* wnd)
		:m_pool(pool)
		,m_cmd_alloc(cmd_alloc)
		,m_wnd(wnd)
	{}
	CmdAllocScope::~CmdAllocScope()
	{
		// std::move creates 'dead' instances
		if (m_cmd_alloc.ptr == nullptr)
			return;

		// This allocator can't be used again while the GPU might still be rendering command lists it created.
		m_cmd_alloc.issue = m_wnd->LatestSyncPoint() + 1;
		m_pool.push_back(m_cmd_alloc);
	}

	// Command list scope
	CmdListScope::CmdListScope(CmdListPool& pool, CmdListSyncPair cmd_list)
		:m_pool(pool)
		,m_cmd_list(cmd_list)
	{}
	CmdListScope::~CmdListScope()
	{
		// std::move creates 'dead' instances
		if (m_cmd_list.ptr == nullptr)
			return;

		// This list can be used again immediately
		m_pool.push_back(m_cmd_list);
	}
}