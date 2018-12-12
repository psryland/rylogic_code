//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/material/material.h"

namespace pr::physics
{
	struct MaterialMap
	{
	private:
		std::vector<Material> m_mats;

	public:
		MaterialMap()
			:m_mats()
		{
			// Add a default material
			m_mats.push_back(Material{});
		}

		// Add a material to the collection
		void Add(Material const& mat)
		{
			assert("Material id already exists" && (*this)(mat.m_id).m_id == Material::DefaultID);
			m_mats.push_back(mat);
		}

		// Remove a material by id
		void Remove(int id)
		{
			auto at = std::find_if(std::begin(m_mats), std::end(m_mats), [=](Material const& m){ return m.m_id == id; });
			if (at == std::end(m_mats)) return;
			m_mats.erase(at);
		}

		// Access a material by ID
		Material const& operator()(int id) const
		{
			// todo: sort 'm_mats' and binary search
			for (auto& mat : m_mats)
			{
				if (mat.m_id != id) continue;
				return mat;
			}
			return m_mats[0];
		}

		// Return the material that represents the properties of two materials in contact
		Material operator()(int id0, int id1) const
		{
			auto& mat0 = (*this)(id0);
			auto& mat1 = (*this)(id1);

			return Material(
				Material::NoID,
				(mat0.m_density + mat1.m_density) * 0.5f,
				Sqrt(mat0.m_friction_static * mat1.m_friction_static),
				Sqrt(mat0.m_friction_dynamic * mat1.m_friction_dynamic),
				(mat0.m_elasticity_norm + mat1.m_elasticity_norm) * 0.5f,
				(mat0.m_elasticity_tang + mat1.m_elasticity_tang) * 0.5f);
		}
	};
}
