#pragma once

#include "elements/forward.h"
#include "elements/material.h"

namespace ele
{
	// Represents the result of a reaction between two materials
	struct Reaction
	{
		// The materials going into the reaction
		Material m_mat1;
		Material m_mat2;

		// The materials produced by the reaction.
		// If empty, then the materials don't react
		std::vector<Material> m_out;

		// The energy of the reaction (-ve = endothermic)
		double m_energy_change;

		Reaction(Material mat1, Material mat2, GameConstants const& consts);
	};

}
