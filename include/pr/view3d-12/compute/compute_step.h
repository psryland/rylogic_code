//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct ComputeStep
	{
		D3DPtr<ID3D12RootSignature> m_sig;
		D3DPtr<ID3D12PipelineState> m_pso;
	};
}
