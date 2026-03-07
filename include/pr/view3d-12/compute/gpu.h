// HyperPose Tools
// Copyright (c) 2025
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/resource/gpu_transfer_buffer.h"

namespace pr::rdr12
{
	// A DirectX 12 instance for running compute shaders
	class Gpu
	{
		// Notes:
		//  - Use this type when you want to run compute shaders on the GPU but don't
		//    want to create a full 'Renderer' instance.

		// D3D Device and command queue
		D3DPtr<ID3D12Device4> m_device;
		D3DPtr<ID3D12CommandQueue> m_cmd_queue;

		// GPU fence helper
		GpuSync m_gsync;

		// Command lists/allocators
		GfxCmdAllocPool m_cmd_alloc_pool;
		GfxCmdListPool m_cmd_list_pool;

		// Upload memory buffer for initialising resources
		GpuUploadBuffer m_upload_buffer;
		
	public:

		Gpu(ID3D12Device4* existing_device = nullptr);

		// Allow use as a device
		ID3D12Device4 const* operator -> () const;
		ID3D12Device4* operator ->();
		operator ID3D12Device4 const* () const;
		operator ID3D12Device4* ();

		// Access the GPU upload buffer
		GpuUploadBuffer& UploadBuffer();

		// Allocate a DX resource
		D3DPtr<ID3D12Resource> CreateResource(ResDesc const& desc, GfxCmdList& cmd_list, std::string_view name);
	};
}
