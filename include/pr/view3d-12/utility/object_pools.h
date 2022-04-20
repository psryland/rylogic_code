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
		D3DPtr<ID3D12CommandAllocator> ptr; // The allocator pointer
		uint64_t issue; // The sync point after which 'alloc' can be reused.
	};

	// A "pair" of command list and nothing
	struct CmdListSyncPair
	{
		D3DPtr<ID3D12GraphicsCommandList> ptr; // The command list
	};

	// A pair of pipeline state object and hash
	struct PipeStatePair
	{
		D3DPtr<ID3D12PipelineState> ptr; // The state object pointer
		int64_t frame_number;            // The frame number when last used
		int hash;                        // Hash of the pipeline state data used to create 'm_pso'
	};

	// Pool types
	using CmdListPool = pr::vector<CmdListSyncPair, 4, false>;
	using CmdAllocPool = pr::vector<CmdAllocSyncPair, 4, false>;
	using PipeStatePool = pr::vector<PipeStatePair, 16, false>;

	// Cmd allocator wrapper that returns to the pool when out of scope.
	struct CmdAllocScope
	{
		CmdAllocPool& m_pool;
		CmdAllocSyncPair m_cmd_alloc;
		Window* m_wnd;

		CmdAllocScope(CmdAllocPool& pool, CmdAllocSyncPair cmd_alloc, Window* wnd);
		CmdAllocScope(CmdAllocScope&&) = default;
		CmdAllocScope(CmdAllocScope const&) = delete;
		CmdAllocScope& operator= (CmdAllocScope&&) = default;
		CmdAllocScope& operator= (CmdAllocScope const&) = delete;
		~CmdAllocScope();
		ID3D12CommandAllocator* operator->() { return m_cmd_alloc.ptr.get(); }
		operator ID3D12CommandAllocator* () { return m_cmd_alloc.ptr.get(); }
	};

	// Cmd list wrapper that returns to the pool when out of scope
	struct CmdListScope
	{
		CmdListPool& m_pool;
		CmdListSyncPair m_cmd_list;

		CmdListScope(CmdListPool& pool, CmdListSyncPair cmd_list);
		CmdListScope(CmdListScope&&) = default;
		CmdListScope(CmdListScope const&) = delete;
		CmdListScope& operator= (CmdListScope&&) = default;
		CmdListScope& operator= (CmdListScope const&) = delete;
		~CmdListScope();
		ID3D12GraphicsCommandList* operator->() { return m_cmd_list.ptr.get(); }
		operator ID3D12GraphicsCommandList* () { return m_cmd_list.ptr.get(); }
	};
}
