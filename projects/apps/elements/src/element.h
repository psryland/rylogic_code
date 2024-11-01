#pragma once
#include "src/forward.h"

namespace ele
{
	// The stuff that all materials are made of
	struct Element
	{
		// Where this element lives in the period table
		atomic_number_t m_atomic_number;

		// The name of the element
		ElementName const* m_name;

		// The period within the periodic table (i.e. row)
		size_t m_period;

		// The number of free electrons this element has in its non-ionised state
		size_t m_valence_electrons;

		// The number of electrons needed to fill this electron shell (from its non-ionised state)
		size_t m_valence_holes;

		// A measure of the pull the element has on other electrons
		// In the real world, this increases from bottom left to top right of the periodic table
		// with a range from ~0.5(Francium) to 4 (Fluorine). The Ionicity of a bond between two
		// elements is determined from difference in electronegativity. On the 0.5->4.0 scale
		// any bond with a difference > ~1.8 is considered ionic.
		pr::fraction_t m_electro_negativity;

		// The melting/boiling points of the element
		pr::celsius_t m_melting_point;
		pr::celsius_t m_boiling_point;

		pr::metres_t m_atomic_radius;

		// A bit mask of the property values that are known for this element
		EElemProp m_known_properties;

		Element();
		Element(atomic_number_t atomic_number, GameConstants const& consts);

		// Returns true if this element is a nobal gas
		bool IsNobal() const { return m_valence_electrons == 0; }

		// Returns true if this element is closer to the left side of the periodic table than the right
		bool IsMetal() const { return m_atomic_number != 1 && m_valence_electrons < m_valence_holes; }
	};
}
