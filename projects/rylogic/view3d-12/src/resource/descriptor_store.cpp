//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/resource/descriptor_store.h"

namespace pr::rdr12
{
	DescriptorStore::DescriptorStore(ID3D12Device* device)
		: m_device(device)
		, m_store_cpu()
	{}

	// Create a CBV descriptor
	Descriptor DescriptorStore::Create(D3D12_CONSTANT_BUFFER_VIEW_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateConstantBufferView(&desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Create a SRV descriptor
	Descriptor DescriptorStore::Create(ID3D12Resource* resource, D3D12_SHADER_RESOURCE_VIEW_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateShaderResourceView(resource, &desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Create a UAV descriptor
	Descriptor DescriptorStore::Create(ID3D12Resource* resource, D3D12_UNORDERED_ACCESS_VIEW_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateUnorderedAccessView(resource, nullptr, &desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Create a RTV descriptor
	Descriptor DescriptorStore::Create(ID3D12Resource* resource, D3D12_RENDER_TARGET_VIEW_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateRenderTargetView(resource, &desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Create a DSV descriptor
	Descriptor DescriptorStore::Create(ID3D12Resource* resource, D3D12_DEPTH_STENCIL_VIEW_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateDepthStencilView(resource, &desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Create a sampler descriptor
	Descriptor DescriptorStore::Create(D3D12_SAMPLER_DESC const& desc)
	{
		constexpr auto type = D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER;

		// Find the free slot in the block and create the descriptor there
		auto& block = GetBlock(type);
		auto des_index = LowBitIndex(block.free);

		// Create the descriptor at 'index'
		auto handle = block.heap->GetCPUDescriptorHandleForHeapStart();
		handle.ptr += des_index * m_device->GetDescriptorHandleIncrementSize(type);
		m_device->CreateSampler(&desc, handle);
		block.free = SetBits(block.free, 1 << des_index, false);

		// Add the block index to 'index' and return
		auto blk_index = s_cast<int>(&block - m_store_cpu[type].data());
		return Descriptor((blk_index << ShftBlk) | des_index, type, handle);
	}

	// Release a descriptor by index
	void DescriptorStore::Release(Descriptor const& descriptor)
	{
		auto blk_index = descriptor.m_index >> ShftBlk;
		auto des_index = descriptor.m_index & MaskIdx;
		auto& block = m_store_cpu[descriptor.m_type][blk_index];
		block.free = SetBits(block.free, 1 << des_index, true);
	}

	// Return a block with a free slot from 'store'
	DescriptorStore::Block& DescriptorStore::GetBlock(D3D12_DESCRIPTOR_HEAP_TYPE type)
	{
		auto& store = m_store_cpu[type];
		
		// Find a block with a free slot.
		int i = 0, iend = s_cast<int>(store.size());
		for (; i != iend && store[i].free != 0; ++i) {}
		if (i == iend)
		{
			// Add a new block to the store
			D3D12_DESCRIPTOR_HEAP_DESC heap_desc = {
				.Type = type,
				.NumDescriptors = 1U << ShftBlk,
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
				.NodeMask = 0U,
			};
			Block block = {.heap = nullptr, .free = ~0ULL};
			Throw(m_device->CreateDescriptorHeap(&heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&block.heap.m_ptr));
			store.push_back(block);
			i = iend;
		}
		return store[i];
	}
}