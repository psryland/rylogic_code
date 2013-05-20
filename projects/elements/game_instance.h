#pragma once

#include "elements/forward.h"
#include "elements/game_constants.h"
#include "elements/world_state.h"
#include "elements/stockpile.h"
#include "elements/ship.h"

namespace ele
{
	// A container for the game instance
	struct GameInstance
	{
		GameConstants m_constants;
		WorldState    m_world_state;
		Stockpile     m_stockpile;
		Ship          m_ship;
		EView         m_view;          // The current active game view

		GameInstance(int seed);

		void Step(pr::seconds_t elapsed);

		// Called at the end of the game when the star goes nova
		void Supernova();

		PR_NO_COPY(GameInstance);
	};
}