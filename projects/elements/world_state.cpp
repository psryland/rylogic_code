#include "elements/stdafx.h"
#include "elements/world_state.h"
#include "elements/game_constants.h"

namespace ele
{
	const pr::seconds_t TimeTillNova = 365 * 24 * 60 * 60;
	WorldState::WorldState(GameConstants& constants)
		:m_constants(constants)
		,m_time_till_nova(TimeTillNova)
		,m_required_acceleration(0)
	{}

	// Returns false if the star has gone nova
	bool WorldState::Step(pr::seconds_t elapsed)
	{
		// Reduce the count down
		m_time_till_nova -= elapsed;
		if (m_time_till_nova <= 0.0)
			return false;

		// Given the time remaining, this is the average acceleration that the ship needs to escape
		m_required_acceleration = m_constants.m_escape_velocity / m_time_till_nova;

		return true;
	}
}
