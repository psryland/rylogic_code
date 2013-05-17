#pragma once

#include "elements/forward.h"

namespace ele
{
	// The stuff that the universe has in it
	struct Material
	{
		// The name of the material
		std::string m_name;

		// The density of the material
		pr::kilograms_p_metre�_t m_density;

		// Controls the field strength of this material.
		// field_strength = 1 / (m_field_falloff * r� + 1)
		double m_field_falloff;

		// The fraction of the material that is converted to energy when reacted
		// e.g
		//  if we have 1kg of fuel, and a reaction_ratio of 0.1
		//  0.1kg of fuel is converted to the energy that accelerates the remaining 0.9kg to a velocity Ve (exhaust velocity)
		//  E = mc� so E = 0.1 * c�
		// Relativistic kinectic energy, E = mc�(gamma - 1), gamma = 1/sqrt(1 - (v/c)�)
		//  v = c * sqrt(1 - 1/(E/mc� + 1)�)
		double m_reaction_ratio;

		// Unique id generator for the materials
		static size_t Id() { static int s_id = 0; return ++s_id; }
		
		Material()
			:m_name(pr::FmtS("material%d", Id()))
			,m_density(1.0)
			,m_field_falloff(1.0)
			,m_reaction_ratio(0.0001)
		{}
	};
}
