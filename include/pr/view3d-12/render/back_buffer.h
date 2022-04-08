//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// A pair of allocator and sync issue number
	struct CmdAllocSyncPair
	{
		D3DPtr<ID3D12CommandAllocator> alloc; // The allocator
		uint64_t issue; // The sync point after which 'alloc' can be reused.
	};
	struct CmdListSyncPair
	{
		D3DPtr<ID3D12GraphicsCommandList> list; // The command list
	};

	using CmdListPool = pr::vector<CmdListSyncPair, 4, false>;
	using CmdAllocPool = pr::vector<CmdAllocSyncPair, 4, false>;

	// Cmd allocator wrapper that returns to the pool when out of scope.
	struct CmdAllocScope
	{
		CmdAllocPool& m_pool;
		CmdAllocSyncPair m_ca;
		Window* m_wnd;

		CmdAllocScope(CmdAllocPool& pool, CmdAllocSyncPair cmd_alloc, Window* wnd);
		CmdAllocScope(CmdAllocScope&&) = default;
		CmdAllocScope(CmdAllocScope const&) = delete;
		CmdAllocScope& operator= (CmdAllocScope&&) = default;
		CmdAllocScope& operator= (CmdAllocScope const&) = delete;
		~CmdAllocScope();
		ID3D12CommandAllocator* operator->() { return m_ca.alloc.get(); }
		operator ID3D12CommandAllocator*() { return m_ca.alloc.get(); }
	};

	// Cmd list wrapper that returns to the pool when out of scope
	struct CmdListScope
	{
		CmdListPool& m_pool;
		CmdListSyncPair m_cl;

		CmdListScope(CmdListPool& pool, CmdListSyncPair cmd_list);
		CmdListScope(CmdListScope&&) = default;
		CmdListScope(CmdListScope const&) = delete;
		CmdListScope& operator= (CmdListScope&&) = default;
		CmdListScope& operator= (CmdListScope const&) = delete;
		~CmdListScope();
		ID3D12GraphicsCommandList* operator->() { return m_cl.list.get(); }
		operator ID3D12GraphicsCommandList*() { return m_cl.list.get(); }
	};

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