//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/compute/forward.h"
#include "pr/compute/resource/gpu_transfer_allocation.h"

namespace pr::compute
{
	// A GPU address, expressed as a byte offset within a resource.
	// Command list methods that bind GPU addresses take this type, so the command list knows which resource its commands use.
	struct GpuAddress
	{
		ID3D12Resource* m_res; // The resource that contains the address. Not null.
		int64_t m_ofs;         // The byte offset from the start of 'm_res'

		GpuAddress(ID3D12Resource* res, int64_t ofs = 0)
			: m_res(res)
			, m_ofs(ofs)
		{
		}
		GpuAddress(D3DPtr<ID3D12Resource> const& res, int64_t ofs = 0)
			: GpuAddress(const_cast<ID3D12Resource*>(res.get()), ofs)
		{
		}
		GpuAddress(GpuTransferAllocation const& alloc)
			: GpuAddress(alloc.m_res, alloc.m_ofs)
		{
		}

		// The GPU virtual address
		D3D12_GPU_VIRTUAL_ADDRESS VirtualAddress() const
		{
			return m_res->GetGPUVirtualAddress() + s_cast<uint64_t>(m_ofs);
		}
	};
}
