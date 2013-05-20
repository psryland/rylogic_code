#include "elements/stdafx.h"
#include "elements/element.h"
#include "elements/game_constants.h"

namespace ele
{
	// The stuff that all materials are made of
	Element::Element()
		:m_atomic_weight(1)
		,m_name_idx(0)
		,m_discovered(false)
	{}

	// Return the name of the element
	char const* Element::Name(GameConstants const& consts) const
	{
		return consts.m_element_name[m_name_idx].m_name;
	}

	// Return the symbol name of the element
	char const* Element::Symbol(GameConstants const& consts) const
	{
		return consts.m_element_name[m_name_idx].m_symbol;
	}
}
