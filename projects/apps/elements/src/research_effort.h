#pragma once
#include "src/forward.h"

namespace ele
{
	struct ResearchEffort
	{
		// Global game constants
		GameConstants const& m_consts;

		// The work left to be done for this discovery
		man_days_t m_remaining_effort;

		// The fraction of total research resources assigned to this research effort
		pr::fraction_t m_assigned_resources;

		// The time remaining until this research effort results in a discovery
		pr::seconds_t m_time_till_discovery;

		ResearchEffort(man_days_t remaining_effort, pr::fraction_t resources, GameConstants const& consts);

		// Update the remaining time till discovery based on the assigned resources
		void Step(pr::seconds_t elapsed);

		// True if the research is complete
		bool Complete() const { return m_remaining_effort == 0.0; }
	};
}