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
		using Ref = struct { D3DPtr<ID3D12Object> ptr; uint64_t sync_point; };
		using Objects = pr::vector<Ref>;

		Objects m_objects;
		GpuSync& m_gsync;
		AutoSub m_ev_sweep;

		explicit KeepAlive(GpuSync& gsync)
			:m_objects()
			,m_gsync(gsync)
			,m_ev_sweep(m_gsync.SyncPointCompleted += std::bind(&KeepAlive::Sweep, this, _1, _2))
		{}
		KeepAlive(KeepAlive const&) = delete;
		KeepAlive& operator = (KeepAlive const&) = delete;

		// Add an object to be kept alive until 'sync_point' is reached
		void Add(ID3D12Object* obj, uint64_t sync_point)
		{
			m_objects.push_back(Ref{
				.ptr = D3DPtr<ID3D12Object>(obj, true),
				.sync_point = sync_point,
				});
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
