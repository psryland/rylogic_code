#include "elements/stdafx.h"
#include "elements/world_state.h"
#include "elements/game_constants.h"

namespace ele
{
	WorldState::WorldState(GameConstants const& consts)
		:m_consts(consts)
		,m_time_till_nova(consts.m_start_time_till_nova)
		,m_required_acceleration(0)
	{}

	// Returns false if the star has gone nova
	bool WorldState::Step(pr::seconds_t elapsed)
	{
		elapsed *= m_consts.m_time_scaler;

		// Reduce the count down
		m_time_till_nova -= elapsed;
		if (m_time_till_nova <= 0.0)
			return false;

		// Given the time remaining, this is the average acceleration that the ship needs to escape
		m_required_acceleration = m_consts.m_escape_velocity / m_time_till_nova;

		return true;
	}
}
