#pragma once
#include "src/forward.h"
#include "src/game_constants.h"
#include "src/world_state.h"
#include "src/stockpile.h"
#include "src/lab.h"
#include "src/ship.h"

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