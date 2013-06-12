#pragma once

#include "elements/forward.h"
#include "elements/material.h"

namespace ele
{
	// Represents the result of a reaction between two materials
	struct Reaction
	{
		// The materials going into the reaction
		Material const* m_mat1;
		Material const* m_mat2;

		// The energy input to the reaction
		// Needed for endothermic reactions to do anything
		// Could be things like heating, laser light, etc
		double m_input_energy;

		// The materials produced by the reaction.
		// If empty, then the materials don't react
		std::vector<Material> m_out;

		// The energy of the reaction (-ve = endothermic)
		double m_energy_change;

		Reaction();
		Reaction(Material const& mat1, Material const& mat2);

		void Do(GameConstants const& consts);
	};

}
