#pragma once

#include "elements/forward.h"
#include "elements/game_constants.h"
#include "elements/world_state.h"
#include "elements/stockpile.h"
#include "elements/lab.h"
#include "elements/ship.h"

namespace ele
{
	// A container for the game instance
	struct GameInstance
	{
		GameConstants m_consts;
		WorldState    m_world_state;
		Stockpile     m_stockpile;
		Lab           m_lab;
		Ship          m_ship;

		GameInstance(int seed);

		void Step(pr::seconds_t elapsed);

		// Generates the starting materials
		void GenerateStartingMaterials();

		// Called at the end of the game when the star goes nova
		void Supernova();

		PR_NO_COPY(GameInstance);
	};
}