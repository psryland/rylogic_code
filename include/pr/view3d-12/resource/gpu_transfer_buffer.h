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
	// An allocation in a GpuTransferBuffer
	struct GpuTransferAllocation
	{
		// Notes:
		//  - The allocation is a linear block of memory, but for images can be interpreted as an array of mips.
		ID3D12Resource* m_res; // The upload resource that contains this allocation.
		uint8_t* m_mem;        // The system memory address, mapped to m_res->GetGPUAddress().
		int64_t m_ofs;         // The offset from 'm_mem' (aka 'm_buf->GetGPUAddress()') to the start of the allocation.
		int64_t m_size;        // The size of the allocation (in bytes).

		template <typename T> T const* ptr() const { return reinterpret_cast<T*>(m_mem + m_ofs); }
		template <typename T> T const* end() const { return reinterpret_cast<T*>(m_mem + m_ofs + m_size); }
		template <typename T> T* ptr() { return reinterpret_cast<T*>(m_mem + m_ofs); }
		template <typename T> T* end() { return reinterpret_cast<T*>(m_mem + m_ofs + m_size); }
	};

	template <D3D12_HEAP_TYPE HeapType>
	struct GpuTransferBuffer :RefCounted<GpuTransferBuffer<HeapType>>
	{
		// Notes:
		//  - In Dx11, setting the shader constants would copy to a new area of memory, behind the scenes, for each Map/Unmap.
		//    In Dx12, we have to do this ourselves, you can't use the same bit of memory in 'SetGraphicsRootConstantBufferView'
		//    calls (for e.g.). It's not making any copies.
		//  - This class is a deque of ID3D12Resource buffers (blocks) used to store data until the GPU has finished with it.
		//    It's a bit like the GpuDescriptorHeap, except that it is a container of Upload resource memory.
		//  - This type is used for uploading constant buffers for shaders, initialising textures, initialising V/I buffers, etc.
		//  - The 'block_size' parameter only controls the default size of each block. Larger blocks are created as needed.

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
				HeapProps heap_props(HeapType);
				ResDesc desc = ResDesc::Buf(size, 1, {}, alignment).def_state(D3D12_RESOURCE_STATE_GENERIC_READ);
				assert(desc.Check());
				Check(device->CreateCommittedResource(
					&heap_props, D3D12_HEAP_FLAG_NONE,
					&desc, D3D12_RESOURCE_STATE_COMMON, nullptr,
					__uuidof(ID3D12Resource), (void**)m_res.address_of()));
				Check(m_res->SetName(L"GpuTransferBuffer:Block"));

				// Upload buffers can live mapped
				Check(m_res->Map(0U, nullptr, (void**)&m_mem));
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

		using Lookup = Lookup<int, D3D12_GPU_VIRTUAL_ADDRESS>;
		using SyncPoint = struct { Block const* m_block; int64_t m_offset; };
		using SyncPoints = pr::deque<SyncPoint, 4>;
		using UsedBlocks = pr::deque<Block, 4>;
		using FreeBlocks = pr::vector<Block, 4>;
		using Allocation = GpuTransferAllocation;

		UsedBlocks m_used;      // The set of blocks in use by the GPU (or currently being added to).
		FreeBlocks m_free;      // Blocks that the GPU has finished with and can be recycled.
		int64_t    m_blk_size;  // The default size of each block. (Will auto create large blocks if needed).
		int32_t    m_blk_align; // The alignment to create blocks with.
		GpuSync*   m_gsync;     // The GPU fence marking GPU progress.
		Lookup     m_lookup;    // A lookup for buffer reuse (since the last sync point).
		AutoSub    m_eh0;       // Event subscription

		GpuTransferBuffer(GpuSync& gsync, int64_t block_size, int block_alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT)
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
		GpuTransferBuffer(GpuTransferBuffer&& rhs) noexcept
			: m_used(std::move(rhs.m_used))
			, m_free(std::move(rhs.m_free))
			, m_blk_size(rhs.m_blk_size)
			, m_blk_align(rhs.m_blk_align)
			, m_gsync(rhs.m_gsync)
			, m_lookup(std::move(rhs.m_lookup))
			, m_eh0(std::move(rhs.m_eh0))
		{
			rhs.m_blk_size = 0;
			rhs.m_blk_align = 0;
			rhs.m_gsync = nullptr;
		}
		GpuTransferBuffer(GpuTransferBuffer const&) = delete;
		GpuTransferBuffer& operator=(GpuTransferBuffer&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_used, rhs.m_used);
			std::swap(m_free, rhs.m_free);
			std::swap(m_blk_size, rhs.m_blk_size);
			std::swap(m_blk_align, rhs.m_blk_align);
			std::swap(m_gsync, rhs.m_gsync);
			std::swap(m_lookup, rhs.m_lookup);
			std::swap(m_eh0, rhs.m_eh0);
			return *this;
		}
		GpuTransferBuffer& operator=(GpuTransferBuffer const&) = delete;
		~GpuTransferBuffer()
		{
			for (; !m_used.empty();)
			{
				auto& used = m_used.front();
				m_gsync->Wait(used.m_sync_point);
				PurgeCompleted(false);
			}
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
				.m_res = block.m_res.get(),
				.m_mem = block.m_mem,
				.m_ofs = PadTo(block.m_size, alignment),
				.m_size = size,
			};

			// Consume from the block
			block.m_size = PadTo(block.m_size, alignment) + size;

			// Return the allocation
			return alex;
		}
		template <typename T> Allocation Alloc(int count)
		{
			return Alloc(count * sizeof(T), alignof(T));
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
				key = pr::hash::HashBytes32(&item, &item + 1);
				auto iter = m_lookup.find(key);
				if (iter != end(m_lookup))
					return iter->second;
			}

			// Add 'item' to the upload buffer
			auto alex = Alloc(sizeof(item), alignment);
			memcpy(alex.m_mem + alex.m_ofs, &item, sizeof(item));
			auto gpu_address = alex.m_res->GetGPUVirtualAddress() + alex.m_ofs;

			// Save in the lookup if this object might be reused
			if (might_reuse)
			{
				m_lookup[key] = gpu_address;
			}

			// Return the handle to the GPU memory
			return gpu_address;
		}

		// Recycle blocks that the GPU has finished with
		void PurgeCompleted(bool keep_one = true)
		{
			auto completed = m_gsync->CompletedSyncPoint();

			// Any blocks with sync points <= 'completed' are ready to be recycled.
			for (; !m_used.empty() && m_used.front().m_sync_point <= completed; )
			{
				// To reduce allocations, keep the last used block active (unless destructing)
				if (keep_one && m_used.size() == 1)
					break;

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

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<GpuTransferBuffer>* doomed)
		{
			auto upload_buffer = static_cast<GpuTransferBuffer*>(doomed);
			rdr12::Delete(upload_buffer);
		}
		friend struct RefCount<GpuTransferBuffer>;
	};

	using GpuUploadBuffer = GpuTransferBuffer<D3D12_HEAP_TYPE_UPLOAD>;
	using GpuReadbackBuffer = GpuTransferBuffer<D3D12_HEAP_TYPE_READBACK>;
}
