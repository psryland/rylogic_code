//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/utility/pipe_states.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/nugget.h"

namespace pr::rdr12
{
	// Constructor
	PipeState::PipeState()
		:m_desc()
		,m_states()
	{}

	// Reset the state
	void PipeState::Reset(PipeStateDesc const& desc)
	{
		m_desc = desc;
		m_states.resize(0);
	}

	// Clear a pipe state
	void PipeState::Clear(EPipeState ps)
	{
		pr::erase_unstable(m_states, ps);
	}

	// Set the value of a pipeline state
	void PipeState::Set(EPipeState ps, void const* data)
	{
		field_t field = {ps};
		pr::insert_unique(m_states, ps);
		memcpy(field.ptr(m_desc), data, field.range.size);
	}

	// Copy the pipeline states from 'rhs' into this object
	void PipeState::Set(PipeState const& rhs)
	{
		for (auto ps : rhs.m_states)
			Set(ps, field_t(ps).ptr(rhs.m_desc));
	}

	// Set the pipeline states related to a nugget
	void PipeState::Set(Nugget const& nugget)
	{
		// Nugget topology type
		Set<EPipeState::PrimitiveTopologyType>(To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(nugget.m_topo));

		// Nugget pipe state overrides
		Set(nugget.m_pipe_state);
	}

	// Find the pipeline state (if it exists)
	void const* PipeState::Find(EPipeState ps) const
	{
		return contains(m_states, ps) ? field_t{ps}.ptr(m_desc) : nullptr;
	}
}
