#pragma once

#include "elements/forward.h"
#include "elements/bond.h"
#include "elements/material.h"
#include "elements/game_constants.h"

namespace ele
{
	// Used to create materials
	struct Lab
	{
		GameConstants const& m_consts;

		explicit Lab(GameConstants const& consts);

	private:
		PR_NO_COPY(Lab);
	};

	// Generate the name of a material formed from the given elements
	std::string MaterialName(Element elem1, size_t count1, Element elem2, size_t count2);

	// Calculates a bond strength between the given elements
	// Negative values mean no bond will form
	double BondStrength(Element elem1, Element elem2, GameConstants const& consts);

	// Calculates the bond strengths for all permutations of elem1,elem2
	void BondStrengths(Element elem1, Element elem2, GameConstants const& consts, Bond (&bonds)[EPerm2::NumberOf]);

	// Calculates the bond strengths for all permutations of the elements in mat1,mat2
	void BondStrengths(Material mat1, Material mat2, GameConstants const& consts, Bond (&bonds)[EPerm4::NumberOf]);

	// Returns true if 'elem1' and 'elem2' would be ionically bonded (as opposed to covalently bonded)
	// The atoms of covalent materials are bound tightly to each other in stable molecules, but those molecules are generally not very
	// strongly attracted to other molecules in the material. On the other hand, the atoms (ions) in ionic materials show strong attractions
	// to other ions in their vicinity. This generally leads to low melting points for covalent solids, and high melting points for ionic solids.
	double BondIonicity(Element elem1, Element elem2);
}
