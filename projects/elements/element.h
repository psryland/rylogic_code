#pragma once

#include "elements/forward.h"

namespace ele
{
	// The stuff that all materials are made of
	struct Element
	{
		// Where this element lives in the period table
		size_t m_atomic_number;

		// The name of the element
		ElementName const* m_name;

		// The period within the periodic table (i.e. row)
		size_t m_period;

		// The number of free electrons this element has in its non-ionised state
		size_t m_valence_electrons;

		// The number of electrons needed to fill this electron shell (from its non-ionised state)
		size_t m_valence_holes;

		// True once this element has been discovered
		bool m_discovered;

		Element();
		Element(size_t atomic_number, GameConstants const& consts);

		// Returns true if this element is a nobal gas
		bool IsNobal() const { return m_valence_electrons == 0; }

		// Returns true if this element is closer to the left side of the periodic table than the right
		bool IsMetal() const { return m_atomic_number != 1 && m_valence_electrons < m_valence_holes; }
	};
}
