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
		D3DPtr<ID3D12Fence> m_fence; // For signalling completed execution of commands.
		Handle              m_event; // The event that is signalled by Dx12 when a command list is complete
		uint64_t            m_issue; // The issue number of the last queued job

		GpuSync()
			:m_fence()
			, m_event()
			, m_issue()
		{}
		explicit GpuSync(ID3D12Device* device)
			: GpuSync()
		{
			Init(device);
		}
		GpuSync(GpuSync&& rhs)
			:m_fence(std::move(rhs.m_fence))
			,m_event(std::move(rhs.m_event))
			,m_issue(rhs.m_issue)
		{}
		GpuSync& operator=(GpuSync&& rhs)
		{
			if (this == &rhs) return *this;
			std::swap(m_fence, rhs.m_fence);
			std::swap(m_event, rhs.m_event);
			std::swap(m_issue, rhs.m_issue);
		}
		GpuSync(GpuSync const&) = delete;
		GpuSync& operator=(GpuSync const&) = delete;
		~GpuSync()
		{
			Release();
		}

		// Create sync objects
		void Init(ID3D12Device* device)
		{
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
		}

		// Add a synchronisation point to 'queue'
		void AddSyncPoint(ID3D12CommandQueue* queue)
		{
			Throw(queue->Signal(m_fence.get(), ++m_issue));
		}

		// Wait till the last sync point is reached
		void Wait()
		{
			for (;m_fence->GetCompletedValue() != m_issue;)
			{
				if (m_fence->GetCompletedValue() > m_issue)
					throw std::runtime_error("GPU has signalled an issue number higher than the latest");

				Throw(m_fence->SetEventOnCompletion(m_issue, m_event));
				WaitForSingleObject(m_event, INFINITE);
			}
		}
	};
}
