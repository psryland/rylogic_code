#pragma once

#include "elements/forward.h"
#include "elements/material.h"

namespace ele
{
	// A wrapper around the store of materials
	struct Stockpile
	{
		typedef std::map<pr::hash::HashValue, Material> MatCont;
		MatCont m_mats;

		Stockpile();

		// Add a new material to the stock pile
		void Add(Material m);
		void Step(pr::seconds_t elapsed);
	};
}
