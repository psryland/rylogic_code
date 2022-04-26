//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/descriptor.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/gpu_sync.h"

namespace pr::rdr12
{
	struct GpuDescriptorHeap
	{
		// Notes:
		//  - Have one of these per window, per heap type (SRV, Sampler).
		//  - The heap is treated like a ring buffer, with 'sync points' interleaved.
		//  - A sync point marks a new "frame" of GPU descriptors.
		//  - The tail of the ring buffer advances as sync points are reached by the GPU.

		using Lookup = Lookup<int, D3D12_GPU_DESCRIPTOR_HANDLE>;
		using SyncPoint = struct { uint64_t m_sync_point; int m_index; };
		using SyncPoints = pr::deque<SyncPoint>;

		D3DPtr<ID3D12DescriptorHeap> m_heap;     // The shader visible heap for descriptors.
		int                          m_size;     // The total size of the heap
		GpuSync*                     m_gsync;    // The GPU fence marking GPU progress.
		SyncPoints                   m_sync;     // Positions in the ring buffer and associated sync points.
		Lookup                       m_lookup;   // A lookup for descriptors added since the last sync point.
		AutoSub                      m_eh0;      // Event subscription
		int                          m_des_size; // The size of one descriptor
		int                          m_head;     // Insert point for added descriptors

		GpuDescriptorHeap(int size, D3D12_DESCRIPTOR_HEAP_TYPE type, GpuSync* gsync)
			:m_heap()
			,m_size(size)
			,m_gsync(gsync)
			,m_sync()
			,m_lookup()
			,m_eh0()
			,m_des_size(s_cast<int>(device()->GetDescriptorHandleIncrementSize(type)))
			,m_head()
		{
			// Create the GPU heap
			D3D12_DESCRIPTOR_HEAP_DESC desc = {
				.Type = type,
				.NumDescriptors = s_cast<UINT>(m_size),
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
				.NodeMask = 0U,
			};
			Throw(device()->CreateDescriptorHeap(&desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_heap.m_ptr));
			
			// Add a dummy sync point to be the current tail.
			m_sync.push_back({0ULL, 0});

			// Sign up for notifications when a sync point is added
			m_eh0 = m_gsync->SyncPointAdded += [this](GpuSync&, EmptyArgs const&)
			{
				// This begins a new "frame" of descriptors. 
				m_sync.push_back({m_gsync->LastAddedSyncPoint(), m_head});
				m_lookup.clear();
				PurgeCompleted();
			};
		}

		// The pointer to the base of the shader visible heap
		ID3D12DescriptorHeap* get() const
		{
			return m_heap.get();
		}
		
		// Given a range of descriptors, ensure they exist in the GPU heap, and return the handle of the first one
		D3D12_GPU_DESCRIPTOR_HANDLE Add(std::span<Descriptor const> descriptors)
		{
			// Hash the CPU descriptor indices to generate a lookup key
			auto key = 0;
			for (auto& des : descriptors)
				key = pr::hash::Hash32CT(des.m_index, key);

			// See if this combination of descriptors has already been added
			auto iter = m_lookup.find(key);
			if (iter != end(m_lookup))
				return iter->second;

			// The number of descriptors to add in a contiguous block
			auto count = s_cast<int>(descriptors.size());

			// Assert capacity
			PurgeCompleted();
			EnsureCapacity(count);

			// Get the heap handles (CPU side, and GPU side) at the insert position (m_head).
			D3D12_CPU_DESCRIPTOR_HANDLE dest_cpu = {m_heap->GetCPUDescriptorHandleForHeapStart().ptr + m_head * m_des_size};
			D3D12_GPU_DESCRIPTOR_HANDLE dest_gpu = {m_heap->GetGPUDescriptorHandleForHeapStart().ptr + m_head * m_des_size};

			// Copy the descriptors to the GPU heap (one at a time, because 'descriptors' are not contiguous)
			for (auto& des : descriptors)
			{
				device()->CopyDescriptorsSimple(1U, dest_cpu, des.m_cpu, des.m_type);
				dest_cpu = {dest_cpu.ptr + m_des_size};
			}

			// Return the GPU handle for the start of the block of descriptors
			m_head = Wrap(m_head + count, 0, m_size);
			return dest_gpu;
		}
		D3D12_GPU_DESCRIPTOR_HANDLE Add(Descriptor const& descriptor)
		{
			return Add({&descriptor, 1});
		}

		// Remove sync points that the GPU has completed, effectively advancing the tail of the ring buffer
		void PurgeCompleted()
		{
			// Always leave at least one sync point, to mark the tail index.
			auto completed = m_gsync->CompletedSyncPoint();
			for (; m_sync.size() > 1 && m_sync[1].m_sync_point <= completed; )
				m_sync.pop_front();
		}

	private:

		// D3D device
		ID3D12Device* device() const
		{
			return m_gsync->D3DDevice();
		}

		// Return the tail index for the ring buffer
		int tail() const
		{
			// The sync point is the first descriptor "after" the sync point,
			// so the tail is the position before it. Anything between the last
			// completed sync point and 'm_head' is still in use by the GPU.
			return Wrap(m_sync.front().m_index - 1, 0, m_size);
		}

		// Return the amount of free space in the ring buffer
		int free() const
		{
			return (tail() - m_head + m_size) % m_size;
		}

		// Check there is room for 'sz' more descriptors.
		void EnsureCapacity(int sz)
		{
			// Is there room between [m_head : buffer_end)
			if (m_size - m_head < sz) // no, roll head back to 0
			{
				// Is tail between [m_head : buffer_end)
				if (free() < m_size - m_head) // yes, overflow
					throw std::runtime_error("The GPU descriptor heap is full. Make it bigger");

				// Roll head back to 0
				m_head = 0;
			}

			// Is there room between [m_head : tail)
			if (free() < sz) // no, overflow
				throw std::runtime_error("The GPU descriptor heap is full. Make it bigger");

			// There is enough room at 'm_head' for a contiguous block of 'sz' descriptors.
			return;
		}
	};
}
