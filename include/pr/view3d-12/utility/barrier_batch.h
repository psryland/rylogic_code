//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/utility/conversion.h"
#include "pr/view3d-12/resource/resource_state.h"

namespace pr::rdr12
{
	template <D3D12_COMMAND_LIST_TYPE ListType>
	struct BarrierBatch
	{
		// Notes:
		//  - This type batches barriers eliminating unnecessary transitions.
		//  - Barriers should be submitted to the command list in batches when possible (for performance).
		//  - Barriers are per-command list because resource states are per-command list.
		using Barriers = pr::vector<D3D12_RESOURCE_BARRIER, 4>;
		using CmdList = CmdList<ListType>;

		Barriers m_barriers;
		CmdList& m_cmd_list;
		
		BarrierBatch(CmdList& cmd_list)
			: m_barriers()
			, m_cmd_list(cmd_list)
		{}
		BarrierBatch(BarrierBatch&&) = delete;
		BarrierBatch(BarrierBatch const&) = delete;
		BarrierBatch& operator =(BarrierBatch&&) = delete;
		BarrierBatch& operator =(BarrierBatch const&) = delete;

		// Resource usage barrier
		BarrierBatch& Transition(ID3D12Resource const* resource, D3D12_RESOURCE_STATES state, uint32_t sub = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, D3D12_RESOURCE_BARRIER_FLAGS flags = D3D12_RESOURCE_BARRIER_FLAG_NONE)
		{
			assert(resource != nullptr);

			#if PR_DBG_RDR
			//auto res_name = DebugName(resource);
			//if (std::string_view(res_name) == "SpatialPartition:IdxCount")
			//	PR_INFO(1, std::format("Resource '{}' -> {}", res_name, To<std::string>(state)).c_str());
			#endif

			// If the new transition takes all sub resources to 'state'...
			if (sub == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
			{
				// Remove all transitions for 'resource' (even sub-resource only transitions)
				pr::erase_if(m_barriers, [=](auto& b) { return b.Type == D3D12_RESOURCE_BARRIER_TYPE_TRANSITION && b.Transition.pResource == resource; });
			}

			// Otherwise, if the transition only takes a sub resource to 'state' but not the others..
			else
			{
				// Remove any existing transitions for 'sub' only
				pr::erase_if(m_barriers, [=](auto& b) { return b.Type == D3D12_RESOURCE_BARRIER_TYPE_TRANSITION && b.Transition.pResource == resource && b.Transition.Subresource == sub; });
			}

			// If all of the sub resources of 'resource' are in the same state, then we can simply transition from that one state to 'state'.
			// If the sub resources are in different states, we need to transition each back to the default state first.
			auto& res_state = m_cmd_list.ResState(resource);
			if (sub == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES && res_state != state) // Tests all subresource states for any != 'state'
			{
				// Transition all mips to the mip0 state, then transition all mips to 'state' in one command
				auto mip0_state = res_state.Mip0State();
				res_state.EnumMipSpecificStates([&](int sub, D3D12_RESOURCE_STATES state_before)
				{
					// The ResState type should prevent mip-specific states that are the same as the default state.
					assert(state_before != mip0_state);
					m_barriers.push_back(D3D12_RESOURCE_BARRIER {
						.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
						.Flags = flags,
						.Transition = {
							.pResource = const_cast<ID3D12Resource*>(resource),
							.Subresource = s_cast<UINT>(sub),
							.StateBefore = state_before,
							.StateAfter = mip0_state,
						},
					});
				});

				// Now, transition everything to 'state'
				if (state != mip0_state)
				{
					m_barriers.push_back(D3D12_RESOURCE_BARRIER {
						.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
						.Flags = flags,
						.Transition = {
							.pResource = const_cast<ID3D12Resource*>(resource),
							.Subresource = sub,
							.StateBefore = mip0_state,
							.StateAfter = state,
						},
					});
				}
			}

			// Transition 'sub' to 'state'
			else if (res_state[sub] != state)
			{
				// Transition subresource 'sub' to 'state' only
				m_barriers.push_back(D3D12_RESOURCE_BARRIER {
					.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
					.Flags = flags,
					.Transition = {
						.pResource = const_cast<ID3D12Resource*>(resource),
						.Subresource = sub,
						.StateBefore = res_state[sub],
						.StateAfter = state,
					},
				});
			}

			// Only record the new states for 'resource' when they've been commited
			return *this;
		}

		// Aliased memory resource barrier
		BarrierBatch& Aliasing(ID3D12Resource const* pResourceBefore, ID3D12Resource const* pResourceAfter)
		{
			D3D12_RESOURCE_BARRIER barrier = {
				.Type = D3D12_RESOURCE_BARRIER_TYPE_ALIASING,
				.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
				.Aliasing = {
					.pResourceBefore = const_cast<ID3D12Resource*>(pResourceBefore),
					.pResourceAfter = const_cast<ID3D12Resource*>(pResourceAfter),
				},
			};
			m_barriers.push_back(barrier);
			return *this;
		}

		// UAV resource barrier
		BarrierBatch& UAV(ID3D12Resource const* pResource)
		{
			D3D12_RESOURCE_BARRIER barrier = {
				.Type = D3D12_RESOURCE_BARRIER_TYPE_UAV,
				.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
				.UAV = {
					.pResource = const_cast<ID3D12Resource*>(pResource),
				}
			};
			m_barriers.push_back(barrier);
			return *this;
		}

		// Send the barriers to 'cmd_list'
		void Commit()
		{
			// Note:
			//  - The ideal way to use a barrier batch is to call 'Commit()' before adding
			//    commands and UAV/Transition/etc after adding commands.

			//TODO.. Could remove redundant barriers here
			if (m_barriers.empty())
				return;

			// Send the barriers to the command list
			m_cmd_list.ResourceBarrier(m_barriers);

			// Apply the resource states from the transitions
			for (auto& barrier : m_barriers)
			{
				if (barrier.Type != D3D12_RESOURCE_BARRIER_TYPE_TRANSITION) continue;
				auto& res_state = m_cmd_list.ResState(barrier.Transition.pResource);
				res_state.Apply(barrier.Transition.StateAfter, barrier.Transition.Subresource);
			}

			// Reset the batch
			m_barriers.resize(0);
		}
	};
}
