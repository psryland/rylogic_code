#pragma once
#include "src/forward.h"
#include "src/research_effort.h"

namespace ele
{
	// The world state as the player knows about it
	// Some variables contain unknown until they are revealed to the player
	// or the player discovers them
	struct WorldState
	{
		// The generated game constants
		GameConstants const& m_consts;

		// The time remaining till the star goes nova
		pr::seconds_t m_time_till_nova;

		// The distance to the star
		ResearchEffort m_star_distance_research;

		// The mass of the star
		ResearchEffort m_star_mass_research;

		// The acceleration required of the ship in order to reach escape velocity in time
		pr::metres_p_sec²_t m_required_acceleration;

		// The average local temperature, this increases as the star nears super nova
		pr::celsius_t m_average_local_temperature;

		WorldState(GameConstants const& consts);

		// Advance the world state by 'elapsed' seconds
		bool Step(pr::seconds_t elapsed);

		PR_NO_COPY(WorldState);
	};
}
