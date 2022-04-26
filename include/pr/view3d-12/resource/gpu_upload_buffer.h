//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/gpu_sync.h"

namespace pr::rdr12
{
	struct GpuUploadBuffer
	{
		// Notes:
		//  - In Dx11, setting the shader constants would copy to a new area of memory, behind the scenes, for each Map/Unmap.
		//    In Dx12, we have to do this ourselves, you can't use the same bit of memory in 'SetGraphicsRootConstantBufferView'
		//    calls (for e.g.). It's not making any copies.
		//  - This class is a deque of ID3D12Resource buffers (blocks) used to store data until the GPU has finished with it.
		//    It's a  bit like the GpuDescriptorHeap, except that it is a container of Upload resource memory.
		//  - This type is used for uploading constant buffers for shaders, initialising textures, initialising V/I buffers, etc.

		// Todo: this supersedes the UploadBufferPool...

		// A 'page' in the upload buffer
		struct Block
		{
			D3DPtr<ID3D12Resource> m_res; // The upload buffer resource
			uint8_t* m_mem;               // The mapped CPU memory
			int64_t m_capacity;           // The sizeof the resource buffer
			int64_t m_size;               // The consumed space in this block
			uint64_t m_sync_point;        // The highest sync point recorded while this was the head block

			Block()
				: m_res()
				, m_mem()
				, m_capacity()
				, m_size()
				, m_sync_point()
			{}
			Block(ID3D12Device* device, int64_t size, int alignment, int64_t sync_point)
				: m_res()
				, m_mem()
				, m_capacity(size)
				, m_size(0)
				, m_sync_point(sync_point)
			{
				auto desc = BufferDesc::Buffer(size, nullptr, alignment);
				Throw(device->CreateCommittedResource(
					&HeapProps::Upload(),
					D3D12_HEAP_FLAG_NONE,
					&desc,
					D3D12_RESOURCE_STATE_GENERIC_READ,
					nullptr,
					__uuidof(ID3D12Resource),
					(void**)&m_res.m_ptr));
				Throw(m_res->SetName(L"GpuUploadBuffer:Block"));

				// Upload buffers can live mapped
				Throw(m_res->Map(0U, nullptr, (void**)&m_mem));
			}
			Block(Block&&) = default;
			Block(Block const&) = delete;
			Block& operator=(Block&&) = default;
			Block& operator=(Block const&) = delete;
			~Block()
			{
				if (m_res != nullptr)
					m_res->Unmap(0U, nullptr);
			}
			int64_t free() const
			{
				return m_capacity - m_size;
			}
		};
		struct Allocation
		{
			ID3D12Resource* m_buf; // The upload resource.
			uint8_t* m_mem;        // The system memory address, mapped to m_buf->GetGPUAddress().
			int64_t m_ofs;         // The offset from 'm_buf->GetGPUAddress()' and 'm_mem' to the start of the allocation.
			int64_t m_size;        // The size of the allocation (in bytes).
		};

		using Lookup = Lookup<int, D3D12_GPU_VIRTUAL_ADDRESS>;
		using SyncPoint = struct { Block const* m_block; int64_t m_offset; };
		using SyncPoints = pr::deque<SyncPoint>;

		pr::deque<Block>  m_used;      // The set of blocks in use by the GPU (or currently being added to).
		pr::vector<Block> m_free;      // Blocks that the GPU has finished with and can be recycled.
		int64_t           m_blk_size;  // The size of each block.
		int32_t           m_blk_align; // The alignment to create blocks with.
		GpuSync*          m_gsync;     // The GPU fence marking GPU progress.
		Lookup            m_lookup;    // A lookup for buffer reuse (since the last sync point).
		AutoSub           m_eh0;       // Event subscription

		GpuUploadBuffer(GpuSync& gsync, int64_t block_size, int block_alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT)
			:m_used()
			,m_free()
			,m_blk_size(block_size)
			,m_blk_align(block_alignment)
			,m_gsync(&gsync)
			,m_lookup()
			,m_eh0()
		{
			// Sign up for notifications when a sync point is added
			m_eh0 = m_gsync->SyncPointAdded += [this](GpuSync&, EmptyArgs const&)
			{
				// This begins a new "frame" of allocations
				if (m_used.empty()) return;
				m_used.back().m_sync_point = m_gsync->LastAddedSyncPoint();
				m_lookup.clear();
				PurgeCompleted();
			};
		}

		// Allocate some upload buffer space
		Allocation Alloc(int64_t size, int alignment)
		{
			if (alignment > m_blk_align)
				throw std::runtime_error("Cannot use alignment larger than the block alignment");

			// Make sure there's space
			EnsureCapacity(size, alignment);
			auto& block = m_used.back();

			// Allocate space
			Allocation alex = {
				.m_buf = block.m_res.get(),
				.m_mem = block.m_mem,
				.m_ofs = PadTo(block.m_size, alignment),
				.m_size = size,
			};

			// Consume from the block
			block.m_size = PadTo(block.m_size, alignment) + size;

			// Return the allocation
			return alex;
		}

		// Copy an object into upload buffer memory, and return the GPU pointer to it's location
		template <typename Item>
		D3D12_GPU_VIRTUAL_ADDRESS Add(Item const& item, int alignment, bool might_reuse)
		{
			auto key = 0;

			// See if 'item' is already in the buffer
			if (might_reuse)
			{
				// Generate a key
				key = pr::hash::Hash(item);
				auto iter = m_lookup.find(key);
				if (iter != end(m_lookup))
					return iter->second;
			}

			// Add 'item' to the upload buffer
			auto alex = Alloc(sizeof(item), alignment);
			memcpy(alex.m_mem + alex.m_ofs, &item, sizeof(item));
			auto gpu_address = alex.m_buf->GetGPUVirtualAddress() + alex.m_ofs;

			// Save in the lookup if this object might be reused
			if (might_reuse)
			{
				m_lookup[key] = gpu_address;
			}

			// Return the handle to the GPU memory
			return gpu_address;
		}

		// Recycle blocks that the GPU has finished with
		void PurgeCompleted()
		{
			auto completed = m_gsync->CompletedSyncPoint();

			// Any blocks with sync points <= 'completed' are ready to be recycled.
			for (; m_used.size() > 1 && m_used.front().m_sync_point <= completed; )
			{
				// Remove from the used list
				auto block = std::move(m_used.front());
				m_used.pop_front();

				// Add to the free list
				block.m_size = 0;
				block.m_sync_point = completed;
				m_free.push_back(std::move(block));
			}
		}

	private:

		// D3D device
		ID3D12Device* device() const
		{
			return m_gsync->D3DDevice();
		}

		// Make sure there is room in the buffers for 'sz' bytes of shader constant data.
		void EnsureCapacity(int64_t size, int alignment)
		{
			// Is there space to add 'size' bytes (after alignment)?
			if (!m_used.empty() && PadTo(m_used.back().m_size, alignment) + size <= m_used.back().m_capacity)
				return;

			// Can we recycle a block from the free list
			auto iter = find_if(m_free, [=](auto& blk) { return blk.m_capacity >= size; });
			if (iter != m_free.end())
			{
				// Remove from the free list
				auto block = std::move(*iter);
				m_free.erase_fast(iter);

				// Add to the 'in-use' list
				block.m_sync_point = m_gsync->LastAddedSyncPoint();
				m_used.push_back(std::move(block));
				return;
			}

			// Create a new block
			auto blk_size = PadTo(std::max(size, m_blk_size), m_blk_align);
			Block block(device(), blk_size, m_blk_align, m_gsync->LastAddedSyncPoint());
			m_used.push_back(std::move(block));
		}
	};
}
