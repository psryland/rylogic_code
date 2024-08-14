//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/resource/gpu_transfer_buffer.h"
#include "pr/view3d-12/utility/barrier_batch.h"

namespace pr::rdr12
{
	template <D3D12_COMMAND_LIST_TYPE QueueType>
	struct GpuJob
	{
		using GpuViewHeap = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		using CmdListCollection = CmdListCollection<QueueType>;
		using BarrierBatch = BarrierBatch<QueueType>;
		using CmdAllocPool = CmdAllocPool<QueueType>;
		using CmdList = CmdList<QueueType>;

		D3DPtr<ID3D12Device4>      m_device;    // The device to use for the GPU operations
		D3DPtr<ID3D12CommandQueue> m_queue;     // The command queue to use for the GPU operations
		GpuSync                    m_gsync;     // The GPU fence
		GpuViewHeap                m_view_heap; // A GPU visible descriptor heap
		CmdAllocPool               m_cmd_pool;  // Command allocator pool for the compute shader
		CmdList                    m_cmd_list;  // Command list for the compute shader
		BarrierBatch               m_barriers;  // Barrier batch for the compute shader
		GpuUploadBuffer            m_upload;    // Upload buffer for the compute shader
		GpuReadbackBuffer          m_readback;  // Read back buffer for the compute shader

		// Get a pointer to the queue
		static D3DPtr<ID3D12CommandQueue> GetQueue(ID3D12Device4* device)
		{
			D3D12_COMMAND_QUEUE_DESC queue_desc = {
				.Type = QueueType,
				.Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
				.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE,
				.NodeMask = 0,
			};
			D3DPtr<ID3D12CommandQueue> m_queue;
			Check(device->CreateCommandQueue(&queue_desc, __uuidof(ID3D12CommandQueue), (void**)&m_queue.m_ptr));
			return m_queue;
		}

		GpuJob(ID3D12Device4* device, char const* name, uint32_t pix_colour, int view_heap_capacity = 1)
			: m_device(device, true)
			, m_queue(GetQueue(device))
			, m_gsync(device)
			, m_view_heap(view_heap_capacity, m_gsync)
			, m_cmd_pool(m_gsync)
			, m_cmd_list(device, m_cmd_pool.Get(), nullptr, name, pix_colour)
			, m_barriers(m_cmd_list)
			, m_upload(m_gsync, 0)
			, m_readback(m_gsync, 0)
		{
			auto heaps = { m_view_heap.get() };
			m_cmd_list.SetDescriptorHeaps({ heaps.begin(), heaps.size() });
		}

		// Run the job and block till complete
		void Run()
		{
			// Job complete
			m_cmd_list.Close();

			// Run the sort job
			CmdListCollection cmd_lists = { m_cmd_list.get() };
			m_queue->ExecuteCommandLists(cmd_lists.count(), cmd_lists.data());

			// Record the sync point for when the command will be finished
			auto sync_point = m_gsync.AddSyncPoint(m_queue.get());
			m_cmd_list.SyncPoint(sync_point);

			// Reset for the next job
			m_cmd_list.Reset(m_cmd_pool.Get());

			// Rebind the view heap after reset
			auto heaps = { m_view_heap.get() };
			m_cmd_list.SetDescriptorHeaps({ heaps.begin(), heaps.size() });

			// Wait for the GPU to finish
			m_gsync.Wait();
		}
	};

	using GraphicsJob = GpuJob<D3D12_COMMAND_LIST_TYPE_DIRECT>;
	using ComputeJob = GpuJob<D3D12_COMMAND_LIST_TYPE_COMPUTE>;
	
	// Calculate the number of dispatches needed to process 'total' items in groups of 'group_size'
	inline int DispatchCount(int total, int group_size)
	{
		return (total + group_size - int(1)) / group_size;
	}
	inline iv3 DispatchCount(iv3 total, iv3 group_size)
	{
		return (total + group_size - iv3(1)) / group_size;
	}
}