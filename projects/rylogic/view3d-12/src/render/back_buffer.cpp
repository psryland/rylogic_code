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
		,m_render_target()
		,m_alloc_pool_free()
		,m_alloc_pool_used()
		,m_issue()
	{}

	// Get an allocator that isn't currently in use. It will be reset and ready to go
	CmdAllocScope BackBuffer::CmdAlloc()
	{
		// Create a new allocator if there isn't one available
		if (m_alloc_pool_free.empty())
		{
			Renderer::Lock lock(m_wnd->rdr());
			auto device = lock.D3DDevice();

			// Create a command allocator
			D3DPtr<ID3D12CommandAllocator> cmd_alloc;
			Throw(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof(ID3D12CommandAllocator), (void**)&cmd_alloc.m_ptr));
			m_alloc_pool_free.push_back(AllocSync{cmd_alloc, m_issue});
		}

		// Hand out an allocator to the caller. It will come back automatically.
		return CmdAllocScope(
			[this]
			{
				auto cl = m_alloc_pool_free.back();
				m_alloc_pool_free.pop_back();
				Throw(cl.alloc->Reset());
				PR_ASSERT(PR_DBG_RDR, cl.issue <= m_issue, "This allocator is still in use");
				return cl;
			},
			[this](auto cl)
			{
				// This allocator can't be used again while the GPU might still be rendering command lists it created.
				cl.issue = m_issue + 1;
				m_alloc_pool_used.push_back(cl);
			});
	}

	// Wait until this frame is ready to be used again
	void BackBuffer::Sync()
	{
		// Wait until this back buffer can be rendered to again
		m_wnd->m_gpu_sync.Wait(m_issue);
		
		// Move all allocators, that are now available, back to the free pool.
		auto i = std::remove_if(std::begin(m_alloc_pool_used), std::end(m_alloc_pool_used), [=](auto cl) { return cl.issue <= m_issue; });
		m_alloc_pool_free.insert(std::end(m_alloc_pool_free), i, std::end(m_alloc_pool_used));
		m_alloc_pool_used.erase(i, std::end(m_alloc_pool_used));
	}
}