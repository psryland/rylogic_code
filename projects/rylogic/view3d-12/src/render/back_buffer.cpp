//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"

namespace pr::rdr12
{
	BackBuffer::BackBuffer()
		:m_wnd()
		,m_bb_index()
		,m_bb_sync()
		,m_render_target()
		,m_depth_stencil()
		,m_d2d_target()
		,m_alloc_pool_free()
		,m_alloc_pool_used()
	{}

	// Get an allocator that isn't currently in use. It will be reset and ready to go
	CmdAllocScope BackBuffer::CmdAlloc()
	{
		return std::move(m_wnd->CmdAlloc());
	}

	// Get a command list from the pool.
	CmdListScope BackBuffer::CmdList()
	{
		return std::move(m_wnd->CmdList());
	}

	// Wait until this frame is ready to be used again
	void BackBuffer::Sync()
	{
		m_wnd->m_gpu_sync.Wait(m_bb_sync);
	}
	
	// The value of the most recent sync point for the window (as opposed to this back buffer)
	uint64_t BackBuffer::LatestSyncPoint() const
	{
		return m_wnd->LatestSyncPoint();
	}
}