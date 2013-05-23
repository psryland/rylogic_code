#pragma once

#include "elements/forward.h"

namespace ele
{
	struct WorldState
	{
		// The generated game constants
		GameConstants const& m_consts;

		// The time remaining till the star goes nova
		pr::seconds_t m_time_till_nova;

		// The acceleration required of the ship in order to reach escape velocity in time
		pr::metres_p_sec²_t m_required_acceleration;

		WorldState(GameConstants const& consts);

		// Advance the world state by 'elapsed' seconds
		bool Step(pr::seconds_t elapsed);
	
	private:
		PR_NO_COPY(WorldState);
	};
}
