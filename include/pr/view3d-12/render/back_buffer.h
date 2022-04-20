//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/object_pools.h"

namespace pr::rdr12
{
	// Data associated with a back buffer
	struct BackBuffer
	{
		// Notes:
		//  - The swap chain can contain multiple back buffers. There will be one of these per swap chain back buffer.
		//  - When rendering to an off-screen target, create one of these to represent the render target.

		Window*                     m_wnd;             // The owning window
		int                         m_bb_index;        // The index of the back buffer this object is for
		uint64_t                    m_bb_sync;         // The sync point of the last render to this back buffer
		D3DPtr<ID3D12Resource>      m_render_target;   // The back buffer render target
		D3DPtr<ID3D12Resource>      m_depth_stencil;   // The back buffer depth stencil
		D3DPtr<ID2D1Bitmap1>        m_d2d_target;      // D2D render target
		D3D12_CPU_DESCRIPTOR_HANDLE m_rtv;             // The descriptor of the back buffer as a RTV
		D3D12_CPU_DESCRIPTOR_HANDLE m_dsv;             // The descriptor of the back buffer as a DSV
		CmdAllocPool                m_alloc_pool_free; // The pool of command allocators available for use this frame
		CmdAllocPool                m_alloc_pool_used; // The pool of command allocators that have been used this frame

		BackBuffer();

		// Get an allocator that isn't currently in use. It will be reset and ready to go
		CmdAllocScope CmdAlloc();

		// Get a command list from the pool.
		CmdListScope CmdList();

		// Wait until this frame is ready to be used again
		void Sync();

		// The value of the most recent sync point for the window (as opposed to this back buffer)
		uint64_t LatestSyncPoint() const;
	};
}