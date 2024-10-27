//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/utility/barrier_batch.h"

namespace pr::rdr12
{
	void RestoreResourceStateDefaults(CmdList<D3D12_COMMAND_LIST_TYPE_DIRECT>& cmd_list)
	{
		// Insert resource state transitions back to the default state for
		// any resource not in its default state at the end of the command list.
		BarrierBatch bb(cmd_list);
		for (auto const& [res, state] : cmd_list.m_res_state.States())
		{
			auto def_state = DefaultResState(res);
			if (state == def_state) continue;
			bb.Transition(res, def_state);
		}
		bb.Commit();
	}
}