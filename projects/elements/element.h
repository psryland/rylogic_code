#pragma once

#include "elements/forward.h"

namespace ele
{
	// The stuff that all materials are made of
	struct Element
	{
		// Where this element lives in the period table
		size_t m_atomic_weight;

		// The index of the element name in the element names table
		size_t m_name_idx;

		// True once this element has been discovered
		bool m_discovered;

		//PR_SQLITE_TABLE(Element,"")
		//PR_SQLITE_COLUMN(AtomicWeight ,m_atomic_weight ,integer ,"primary key not null")
		//PR_SQLITE_COLUMN(NameIdx      ,m_name_idx      ,integer ,"")
		//PR_SQLITE_COLUMN(Discovered   ,m_discovered    ,integer ,"")
		//PR_SQLITE_TABLE_END()

		Element();

		// Return the name of the element
		char const* Name(GameConstants const& consts) const;

		// Return the symbol name of the element
		char const* Symbol(GameConstants const& consts) const;
	};
}
