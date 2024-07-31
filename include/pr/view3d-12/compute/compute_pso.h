//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	struct ComputePSO : D3D12_COMPUTE_PIPELINE_STATE_DESC
	{
		// Notes:
		//  - This type helps build a compute pipeline state object
		
		ComputePSO(ID3D12RootSignature* sig, std::span<uint8_t const> bytecode)
			: D3D12_COMPUTE_PIPELINE_STATE_DESC{
				.pRootSignature = sig,
				.CS = {
					.pShaderBytecode = bytecode.data(),
					.BytecodeLength = bytecode.size(),
				},
				.NodeMask = 0,
				.CachedPSO = D3D12_CACHED_PIPELINE_STATE{},
				.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
			}
		{}

		// Create the compute pipeline state object
		D3DPtr<ID3D12PipelineState> Create(ID3D12Device* device, char const* name)
		{
			D3DPtr<ID3D12PipelineState> pso;
			Check(device->CreateComputePipelineState(this, __uuidof(ID3D12PipelineState), (void**)&pso.m_ptr));
			DebugName(pso, name);
			return pso;
		}
	};
}
