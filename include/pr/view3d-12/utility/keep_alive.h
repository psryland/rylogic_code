//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/gpu_sync.h"

namespace pr::rdr12
{
	struct KeepAlive
	{
		// Notes:
		//  - This class simply keeps a ref ptr to an object until the 'gsync' notifies that the 
		//    GPU has reached a certain sync point. The 'SyncPointCompleted' event should be 
		//    called automatically by the Timer on the dummy window in the Renderer. This relies
		//    on the GpuSync object registering it's Poll function with the Renderer.

		using Ref = struct
		{
			D3DPtr<ID3D12Object> ptr;
			D3DPtr<IRefCounted> ptr2;
			uint64_t sync_point;
		};
		using Objects = pr::vector<Ref, 4>;

		Objects m_objects;
		GpuSync& m_gsync;
		AutoSub m_ev_sweep;

		explicit KeepAlive(GpuSync& gsync)
			:m_objects()
			,m_gsync(gsync)
			,m_ev_sweep(m_gsync.SyncPointCompleted += std::bind(&KeepAlive::Sweep, this_ptr(), _1, _2))
		{}
		KeepAlive(KeepAlive const&) = delete;
		KeepAlive& operator = (KeepAlive const&) = delete;
		KeepAlive* this_ptr() { return this; }

		// Add an object to be kept alive until 'sync_point' is reached
		template <RefCountedType T> void Add(D3DPtr<T> obj, uint64_t sync_point)
		{
			m_objects.push_back(Ref{
				.ptr = nullptr,
				.ptr2 = obj,
				.sync_point = sync_point,
				});
		}
		template <RefCountedType T> void Add(D3DPtr<T> obj)
		{
			Add(obj, m_gsync.NextSyncPoint());
		}
		void Add(ID3D12Object* obj, uint64_t sync_point)
		{
			m_objects.push_back(Ref{
				.ptr = D3DPtr<ID3D12Object>(obj, true),
				.ptr2 = nullptr,
				.sync_point = sync_point,
				});
		}
		void Add(ID3D12Object* obj)
		{
			Add(obj, m_gsync.NextSyncPoint());
		}

	private:

		// Remove objects whose sync point has been reached
		void Sweep(GpuSync&, EmptyArgs const&)
		{
			auto sync_point = m_gsync.CompletedSyncPoint();
			pr::erase_if_unstable(m_objects, [=](auto obj) { return obj.sync_point <= sync_point; });
		}
	};
}
