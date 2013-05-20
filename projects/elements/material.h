#pragma once

#include "elements/forward.h"
#include "elements/element.h"

namespace ele
{
	// The stuff that the universe has in it
	struct Material
	{
		// The elements that this material is made of
		// m_metal * m_metal_count + m_nonmetal * m_nonmetal_count
		Element m_metal;
		Element m_nonmetal;
		size_t m_metal_count;
		size_t m_nonmetal_count;

		// The name of the material (derived from the elements)
		std::string Name(GameConstants const& consts) const;

		// The atomic weight of the material (derived from the elements)
		size_t AtomicWeight() const;

		// The density of the material at room temperature
		pr::kilograms_p_metre³_t Density() const { return 1.0; }

		//PR_SQLITE_TABLE(Material,"")
		//PR_SQLITE_COLUMN(Id           ,m_id            ,integer ,"primary key not null")
		//PR_SQLITE_COLUMN(Name         ,m_name          ,text    ,"")
		//PR_SQLITE_COLUMN(AtomicWeight ,m_atomic_weight ,real    ,"")
		//PR_SQLITE_COLUMN(Discovered   ,m_discovered    ,integer ,"")
		//PR_SQLITE_TABLE_END()

		Material();
	};
}
