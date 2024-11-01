#include "src/forward.h"
#include "src/stockpile.h"
#include "src/material.h"
#include "src/game_constants.h"

namespace ele
{
	// A wrapper around the store of materials
	Stockpile::Stockpile()
		:m_mats()
	{}

	// Add a new material to the stock pile
	void Stockpile::Add(Material m)
	{
		(void)m;
		//m_mats[m.m_hash] = m;
	}
		
	void Stockpile::Step(pr::seconds_t elapsed)
	{
		(void)elapsed;
	}
}

