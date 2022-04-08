//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// A pair of allocator and sync issue number
	struct AllocSync
	{
		D3DPtr<ID3D12CommandAllocator> alloc; // The allocator
		uint64_t issue;                       // The sync point after which 'alloc' can be reused.
	};

	// Cmd allocator wrapper that returns to the pool when out of scope.
	struct CmdAllocScope :Scope<AllocSync>
	{
		using base_t = Scope<AllocSync>;
		CmdAllocScope(doit_t doit, undo_t undo) :base_t(doit, undo) {}
		ID3D12CommandAllocator* operator->() { return m_state.alloc.get(); }
		operator ID3D12CommandAllocator*() { return m_state.alloc.get(); }
	};

	// Data associated with a back buffer
	struct BackBuffer
	{
		// Notes:
		//  - The swap chain can contain multiple back buffers. There will be one of these per swap chain back buffer.
		//  - When rendering to an off-screen target, create one of these to represent the render target.

		using CmdAllocPool = pr::vector<AllocSync, 4, false>;

		Window* m_wnd;             // The owning window
		int                         m_bb_index;        // The index of the back buffer this object is for
		D3DPtr<ID3D12Resource>      m_render_target;   // The back buffer render target
		D3D12_CPU_DESCRIPTOR_HANDLE m_rtv;             // The descriptor of the back buffer as a RTV
		D3D12_CPU_DESCRIPTOR_HANDLE m_dsv;             // The descriptor of the back buffer as a DSV
		CmdAllocPool                m_alloc_pool_free; // The pool of command allocators available for use this frame
		CmdAllocPool                m_alloc_pool_used; // The pool of command allocators that have been used this frame
		uint64_t                    m_issue;           // The issue number if the last render to this back buffer

		BackBuffer();
		CmdAllocScope CmdAlloc();
		void Sync();
	};
}