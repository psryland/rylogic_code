#include "elements/stdafx.h"
#include "elements/material.h"

namespace ele
{
	// The stuff that the universe has in it
	Material::Material()
		:m_metal()
		,m_nonmetal()
		,m_metal_count(0)
		,m_nonmetal_count(0)
	{}

	// The name of the material (derived from the elements)
	std::string Material::Name(GameConstants const& consts) const
	{
		return pr::FmtS("%s%d%s%d",m_metal.Name(consts),m_metal_count,m_nonmetal.Name(consts),m_nonmetal_count);
	}

	// The atomic weight of the material (derived from the elements)
	size_t Material::AtomicWeight() const
	{
		return m_metal_count * m_metal.m_atomic_weight + m_nonmetal_count * m_nonmetal.m_atomic_weight;
	}

}
