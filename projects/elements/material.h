#pragma once

#include "elements/forward.h"
#include "elements/element.h"
#include "elements/bond.h"

namespace ele
{
	// The stuff that the universe has in it
	struct Material
	{
		// The elements that this material is made of
		// m_elem1 * m_count1 + m_elem2 * m_count2
		Element m_elem1;
		Element m_elem2;

		// A measure of how ionic the bond is.
		// Ionic bonds tend to form strong macro structures (e.g. crystal lattices)
		double m_ionicity;

		size_t m_count1;
		size_t m_count2;

		// The name of the material (derived from the elements)
		std::string m_name;

		// A hash code for materials of this type
		pr::hash::HashValue m_hash;

		// The configuration of the material
		Bond m_bonds[EPerm2::NumberOf];

		// True if this is a stable material, false otherwise
		bool m_stable;

		//// The atomic weight of the material (derived from the elements)
		//size_t AtomicWeight() const;

		// The density of the material at room temperature
		pr::kilograms_p_metre³_t Density() const { return 1.0; }

		Material();
		Material(Element e1, Element e2, GameConstants const& consts);
	};
}
