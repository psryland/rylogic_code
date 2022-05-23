//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct GpuSync
	{
		// Notes:
		//  - There can be any number of 'GpuSync' (i.e. fences) in use at any time.
		//  - m_fence->Signal() sets the fence value from the CPU side (i.e. immediately)
		//  - m_queue->Signal() gets the GPU to set the fence value when it encounters it in the command queue.

		ID3D12Device4* m_d3d_device; // The device used to create the fence
		D3DPtr<ID3D12Fence> m_fence; // For signalling completed execution of commands.
		Handle              m_event; // The event that is signalled by Dx12 when a command list is complete
		uint64_t            m_sync;  // The issue number of the last queued job.

		GpuSync()
			: m_d3d_device()
			, m_fence()
			, m_event()
			, m_sync()
		{}
		explicit GpuSync(ID3D12Device4* device)
			: GpuSync()
		{
			Init(device);
		}
		GpuSync(GpuSync&&) = default;
		GpuSync& operator=(GpuSync&&) = default;
		GpuSync(GpuSync const&) = delete;
		GpuSync& operator=(GpuSync const&) = delete;
		~GpuSync()
		{
			Release();
		}

		// Return the device used to create this gpu fence
		ID3D12Device4* D3DDevice()
		{
			return m_d3d_device;
		}

		// Create sync objects
		void Init(ID3D12Device4* device)
		{
			Release();

			m_d3d_device = device;
			Throw(device->CreateFence(0, D3D12_FENCE_FLAG_NONE, __uuidof(ID3D12Fence), (void**)&m_fence.m_ptr));
			m_event = CreateEventExW(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
			Throw(m_event != nullptr, "Creating an event for the thread fence failed");
		}

		// Release COM pointers
		void Release()
		{
			// Ensure there are no outstanding tasks on the GPU
			if (m_fence != nullptr)
				Wait();

			// Release
			m_event.close();
			m_fence = nullptr;
			m_d3d_device = nullptr;
		}

		// The sync point last added by this GpuSync instance.
		uint64_t LastAddedSyncPoint() const
		{
			// This is used to know when a specific GpuSync 
			return m_sync;
		}

		// The sync point that this GpuSync (a.k.a fence) has reached so far.
		uint64_t CompletedSyncPoint() const
		{
			return m_fence->GetCompletedValue();
		}

		// Add a synchronisation point to 'queue'. Returns the sync point number to wait for.
		uint64_t AddSyncPoint(ID3D12CommandQueue* queue)
		{
			Throw(queue->Signal(m_fence.get(), ++m_sync));
			SyncPointAdded(*this);
			return m_sync;
		}

		// Wait until the given sync point value is reached
		void Wait(uint64_t sync_point) const
		{
			// Wait until the fence reports a completed sync point >= 'sync_point'
			for (auto sp = CompletedSyncPoint(); sp < sync_point; sp = CompletedSyncPoint())
			{
				Throw(m_fence->SetEventOnCompletion(m_sync, m_event));
				WaitForSingleObject(m_event, INFINITE);
			}
		}
		
		// Wait till the last sync point is reached
		void Wait() const
		{
			Wait(m_sync);
		}
		
		// Raised when AddSyncPoint is called
		EventHandler<GpuSync&, EmptyArgs const&, true> SyncPointAdded;
	};
}
