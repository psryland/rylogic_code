#pragma once

#include "elements/forward.h"
#include "elements/element.h"

namespace ele
{
	// The stuff that the universe has in it
	struct Material
	{
		// The elements that this material is made of
		// m_elem1 * m_count1 + m_elem2 * m_count2
		Element m_elem1;
		Element m_elem2;

		// True if this is an ionic bond otherwise covalent.
		bool m_ionic;

		size_t m_count1;
		size_t m_count2;

		// The name of the material (derived from the elements)
		std::string m_name;

		// A hash code for materials of this type
		pr::hash::HashValue m_hash;

		// The configuration of the material
		size_t m_bonds[EPerm2::NumberOf];

		//// The atomic weight of the material (derived from the elements)
		//size_t AtomicWeight() const;

		// The density of the material at room temperature
		pr::kilograms_p_metre³_t Density() const { return 1.0; }

		Material();
		Material(Element e1, Element e2, GameConstants const& consts);
	};
}
