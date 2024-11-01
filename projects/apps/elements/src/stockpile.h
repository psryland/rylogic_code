#pragma once
#include "src/forward.h"
#include "src/material.h"

namespace ele
{
	// A wrapper around the store of materials
	struct Stockpile
	{
		typedef std::map<pr::hash::HashValue32, Material> MatCont;
		MatCont m_mats;

		Stockpile();

		// Add a new material to the stock pile
		void Add(Material m);
		void Step(pr::seconds_t elapsed);
	};
}
