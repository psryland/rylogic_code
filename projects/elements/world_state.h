#pragma once

#include "elements/forward.h"

namespace ele
{
	struct WorldState
	{
		// The generated game constants
		GameConstants& m_constants;

		// The time remaining till the star goes nova
		pr::seconds_t m_time_till_nova;

		// The acceleration required of the ship in order to reach escape velocity in time
		pr::metres_p_sec²_t m_required_acceleration;

		WorldState(GameConstants& constants);

		// Advance the world state by 'elapsed' seconds
		bool Step(pr::seconds_t elapsed);
	
	private:
		WorldState(WorldState const&);
		WorldState& operator =(WorldState const&);
	};
}
