#include "elements/stdafx.h"
#include "elements/element.h"
#include "elements/game_constants.h"

namespace ele
{
	// The row of the periodic table that an element is in
	inline size_t Period(size_t atomic_number, GameConstants const& consts)
	{
		size_t i;
		for (i = 1; i != PR_COUNTOF(consts.m_valence_levels) && atomic_number > consts.m_valence_levels[i]; ++i) {}
		return i - 1;
	}

	// The stuff that all materials are made of
	Element::Element()
		:m_atomic_number()
		,m_name()
		,m_period()
		,m_valence_electrons()
		,m_valence_holes()
		,m_discovered(false)
	{}
	Element::Element(size_t atomic_number, GameConstants const& consts)
		:m_atomic_number()
		,m_name()
		,m_period()
		,m_valence_electrons()
		,m_valence_holes()
		,m_discovered(false)
	{
		PR_ASSERT(PR_DBG, atomic_number > 0 && atomic_number <= consts.m_element_count, "");

		m_atomic_number  = atomic_number;
		m_name           = &consts.m_element_name[m_atomic_number];
		m_period         = Period(m_atomic_number, consts);
		m_valence_electrons = m_atomic_number != consts.m_valence_levels[m_period+1] ? m_atomic_number - consts.m_valence_levels[m_period] : 0;
		m_valence_holes     = m_atomic_number != consts.m_valence_levels[m_period+1] ? consts.m_valence_levels[m_period+1] - m_atomic_number : 0;

		PR_ASSERT(PR_DBG, m_valence_electrons >= 0 && m_valence_electrons <= consts.m_valence_levels[m_period+1] - consts.m_valence_levels[m_period], "");
		PR_ASSERT(PR_DBG, m_valence_holes     >= 0 && m_valence_holes     <= consts.m_valence_levels[m_period+1] - consts.m_valence_levels[m_period], "");
	}
}
