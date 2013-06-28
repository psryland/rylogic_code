#pragma once

#include "elements/forward.h"
#include "elements/bond.h"
#include "elements/element.h"
#include "elements/material.h"
#include "elements/game_constants.h"

namespace ele
{
	// Used to create materials
	struct Lab
	{
		GameConstants const& m_consts;
		ElemCont m_elements;          // The elements in the world
		MatCont m_materials;          // Every possible material combination
		ElemPtrCont m_element_order;  // The display order of the elements (only includes known elements)
		MatPtrCont m_materials_order; // The display order of the materials (only includes known materials)

		// A bit mask of the properties of the elements that are known
		EElemProp m_known_properties;

		explicit Lab(GameConstants const& consts);

		// Called to 'discover' a new element
		void DiscoverElement(atomic_number_t atomic_number);

		// Called to 'discover' a new material
		// A material can be discovered independently of the elements its made of.
		void DiscoverMaterial(int index);

		// Returns a collection of the materials related to 'elem'
		MatCPtrCont RelatedMaterials(Element const& elem) const;

	private:
		PR_NO_COPY(Lab);

		// Update the display order of the elements based on what the player
		// currently knowns about them. Order is atomic number, alphabetical
		void UpdateDisplayOrder();
	};

	// Returns a unique index for a material combination
	// The order of elem1/elem2 does not effect the index
	inline size_t MaterialIndex(atomic_number_t elem1_atomic_number, atomic_number_t elem2_atomic_number)
	{
		return pr::tri_table::Index<pr::tri_table::Inclusive>(elem1_atomic_number - 1, elem2_atomic_number - 1);
	}
	inline size_t MaterialIndex(Element const& elem1, Element const& elem2)
	{
		return MaterialIndex(elem1.m_atomic_number, elem2.m_atomic_number);
	}

	// Generate the name of a material formed from the given elements
	std::string MaterialName(Element elem1, size_t count1, Element elem2, size_t count2);

	// Generate the symbollic name of a material formed from the given elements
	std::string MaterialSymName(Element elem1, size_t count1, Element elem2, size_t count2);

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
