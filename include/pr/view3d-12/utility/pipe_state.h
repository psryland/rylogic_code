//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	// A pipe state object instance
	struct PipeStateObject
	{
		D3DPtr<ID3D12PipelineState> m_pso; // The pipeline state object
		int64_t m_frame_number;            // The frame number when last used
		int m_hash;                        // Hash of the pipeline state data used to create 'm_pso'

		PipeStateObject(D3DPtr<ID3D12PipelineState> pso, int64_t frame_number, int hash)
			: m_pso(pso)
			, m_frame_number(frame_number)
			, m_hash(hash)
		{}

		// Access the allocator
		ID3D12PipelineState* operator ->() const
		{
			return m_pso.get();
		}

		// Convert to the allocator pointer
		operator ID3D12PipelineState* () const
		{
			return m_pso.get();
		}
	};

	// A pool of pipe states
	struct PipeStatePool
	{
		using pool_t = pr::vector<PipeStateObject, 16, false>;
		Window* m_wnd;
		pool_t m_pool;

		explicit PipeStatePool(Window& wnd);
		PipeStatePool(PipeStatePool&&) = default;
		PipeStatePool(PipeStatePool const&) = delete;
		PipeStatePool& operator=(PipeStatePool&&) = default;
		PipeStatePool& operator=(PipeStatePool const&) = delete;

		// Return a pipeline state instance for the given description
		ID3D12PipelineState* Get(PipeStateDesc const& desc);
	};
}