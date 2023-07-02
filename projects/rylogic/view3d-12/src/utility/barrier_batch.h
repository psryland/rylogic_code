//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/resource/resource_state.h"

namespace pr::rdr12
{
	struct BarrierBatch
	{
		// Notes:
		//  - This type batches barriers eliminating unnecessary transitions.
		//  - Barriers should be submitted to the command list in batches when possible (for performance).

		using Barriers = pr::vector<D3D12_RESOURCE_BARRIER>;
		Barriers m_barriers;

		// Resource usage barrier
		void Transition(ID3D12Resource* resource, D3D12_RESOURCE_STATES state, uint32_t sub = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, D3D12_RESOURCE_BARRIER_FLAGS flags = D3D12_RESOURCE_BARRIER_FLAG_NONE)
		{
			// If the new transition takes all subresources to 'state'...
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
			auto res_state = ResState(resource);
			if (sub == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES && res_state != state) // Tests all subresource states for any != 'state'
			{
				// Transition all mips to the default, then default to 'state'
				auto def_state = res_state.DefaultState();
				res_state.EnumMipSpecificStates([&](int sub, D3D12_RESOURCE_STATES state_before)
				{
					// The ResState type should prevent mip-specific states that are the same as the default state.
					assert(state_before != def_state);
					m_barriers.push_back(D3D12_RESOURCE_BARRIER {
						.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
						.Flags = flags,
						.Transition = {
							.pResource = resource,
							.Subresource = s_cast<UINT>(sub),
							.StateBefore = state_before,
							.StateAfter = def_state,
						},
					});
				});

				// Transition everything to 'state'
				if (state != def_state)
				{
					m_barriers.push_back(D3D12_RESOURCE_BARRIER {
						.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
						.Flags = flags,
						.Transition = {
							.pResource = resource,
							.Subresource = sub,
							.StateBefore = def_state,
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
						.pResource = resource,
						.Subresource = sub,
						.StateBefore = res_state[sub],
						.StateAfter = state,
					},
				});
			}
		}

		// Aliased memory resource barrier
		void Aliasing(ID3D12Resource* pResourceBefore, ID3D12Resource* pResourceAfter)
		{
			D3D12_RESOURCE_BARRIER barrier = {
				.Type = D3D12_RESOURCE_BARRIER_TYPE_ALIASING,
				.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
				.Aliasing = {
					.pResourceBefore = pResourceBefore,
					.pResourceAfter = pResourceAfter,
				},
			};
			m_barriers.push_back(barrier);
		}

		// UAV resource barrier
		void UAV(ID3D12Resource* pResource)
		{
			D3D12_RESOURCE_BARRIER barrier = {
				.Type = D3D12_RESOURCE_BARRIER_TYPE_UAV,
				.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
				.UAV = {
					.pResource = pResource,
				}
			};
			m_barriers.push_back(barrier);
		}

		// Send the barriers to the command list
		void Commit(ID3D12GraphicsCommandList* cmd_list)
		{
			// todo.. Could remove redundant barriers here
			if (m_barriers.empty())
				return;

			// Send the barriers to the command list
			cmd_list->ResourceBarrier(s_cast<UINT>(m_barriers.size()), m_barriers.data());

			// Apply the resource states from the transitions
			for (auto& barrier : m_barriers)
			{
				if (barrier.Type != D3D12_RESOURCE_BARRIER_TYPE_TRANSITION) continue;
				auto res_state = ResState(barrier.Transition.pResource);
				res_state.Apply(barrier.Transition.StateAfter, barrier.Transition.Subresource);
			}

			// Reset the batch
			m_barriers.resize(0);
		}
	};
}
