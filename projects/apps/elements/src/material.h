#pragma once
#include "src/forward.h"
#include "src/element.h"
#include "src/bond.h"

namespace ele
{
	// The stuff that the universe has in it
	struct Material
	{
		// The elements that this material is made of
		// m_elem1 * m_count1 + m_elem2 * m_count2
		Element m_elem1;
		Element m_elem2;
		size_t m_count1;
		size_t m_count2;

		// The name of the material (derived from the elements)
		std::string m_name;          // Its full chemical name
		std::string m_name_symbolic; // Symbolic name
		std::string m_name_common;   // What the layman call it

		// The index of this material in the possible combinations
		size_t m_index;

		// The configuration of the material
		Bond m_bonds[EPerm2::NumberOf];

		// A measure of how ionic the bond is.
		// Ionic bonds tend to form strong macro structures (e.g. crystal lattices)
		double m_ionicity;

		// The measure of how strongly bonded this material is
		pr::joules_t m_enthalpy;

		double m_molar_mass;

		double m_melting_point;
		double m_boiling_point;

		pr::kilograms_p_metre³_t m_density;

		// True if this is a stable material, false otherwise
		bool m_stable;

		//// The atomic weight of the material (derived from the elements)
		//size_t AtomicWeight() const;

		// The density of the material at room temperature
		pr::kilograms_p_metre³_t Density() const { return 1.0; }

		// True if this material is known to the player
		bool m_discovered;

		Material();
		Material(Element e1, Element e2, GameConstants const& consts);

		// Call to update the name
		void UpdateName(std::string const& common_name);
	};
}
