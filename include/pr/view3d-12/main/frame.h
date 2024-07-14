//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/utility/pipe_state.h"
#include "pr/view3d-12/utility/diagnostics.h"

namespace pr::rdr12
{
	struct Frame
	{
		using GfxCmdLists = pr::vector<ID3D12GraphicsCommandList*, 4, false>;

		GfxCmdList m_prepare; // Commands before the first scene is rendered
		GfxCmdList m_resolve; // Commands used to resolve the msaa buffer into the swap chain buffer
		GfxCmdList m_present; // Commands after the last scene is rendered

		GfxCmdLists m_main; // Command lists to execute before the msaa buffer is resolved
		GfxCmdLists m_post; // Command lists to execute after the msaa buffer is resolved

		BackBuffer& m_bb_main; // The back buffer to render the scene to that will be anti-aliased.
		BackBuffer& m_bb_post; // The back buffer for post-processing effects (assume main has been rendered into post).
		
		GfxCmdAllocPool& m_cmd_alloc_pool; // The command allocator pool to create allocators from

		Frame(ID3D12Device4* device, BackBuffer& bb_main, BackBuffer& bb_post, GfxCmdAllocPool& cmd_alloc_pool)
			: m_prepare(device, cmd_alloc_pool.Get(), nullptr, "Prepare", EColours::Orange)
			, m_resolve(device, cmd_alloc_pool.Get(), nullptr, "Resolve", EColours::Orange)
			, m_present(device, cmd_alloc_pool.Get(), nullptr, "Present", EColours::Orange)
			, m_main()
			, m_post()
			, m_bb_main(bb_main)
			, m_bb_post(bb_post)
			, m_cmd_alloc_pool(cmd_alloc_pool)
		{}
		Frame(Frame&&) = default;
		Frame(Frame const&) = delete;
		Frame& operator =(Frame&&) = default;
		Frame& operator =(Frame const&) = delete;
	};
}