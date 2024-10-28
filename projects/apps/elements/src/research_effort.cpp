#include "elements/stdafx.h"
#include "elements/research_effort.h"
#include "elements/game_constants.h"
#include "elements/world_state.h"

namespace ele
{
	ResearchEffort::ResearchEffort(man_days_t remaining_effort, pr::fraction_t resources, GameConstants const& consts)
		:m_consts(consts)
		,m_remaining_effort(remaining_effort)
		,m_assigned_resources(resources)
		,m_time_till_discovery(std::numeric_limits<pr::seconds_t>::max())
	{
		Step(0.0);
	}

	// Update the remaining time till discovery based on the assigned resources
	void ResearchEffort::Step(pr::seconds_t elapsed)
	{
		// Reduce the remaining effort by the assigned man power working for 'elapsed' seconds
		man_power_t man_power = m_assigned_resources * m_consts.m_total_man_power;
		m_remaining_effort -= (man_power * elapsed) / seconds_per_day;
		if (m_remaining_effort <= std::numeric_limits<man_days_t>::epsilon())
			m_remaining_effort = 0.0;

		// Determine the estimated time till discovery
		m_time_till_discovery = std::numeric_limits<pr::seconds_t>::max();
		if (man_power > std::numeric_limits<man_power_t>::epsilon())
		{
			m_time_till_discovery -= (m_remaining_effort / man_power) * seconds_per_day;
			if (m_time_till_discovery <= std::numeric_limits<pr::seconds_t>::epsilon())
				m_time_till_discovery = 0.0;
		}
	}
}