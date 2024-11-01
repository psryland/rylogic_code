#include "src/forward.h"
#include "src/material.h"
#include "src/element.h"
#include "src/game_constants.h"
#include "src/lab.h"

namespace ele
{
	template <int Idx> size_t ElementRatio(Element e1, Element e2)
	{
		if (e1.IsNobal() || e2.IsNobal()) return 0;
		if (Idx == 1) return e2.m_valence_holes     / pr::GreatestCommonFactor(e1.m_valence_electrons, e2.m_valence_holes);
		if (Idx == 2) return e1.m_valence_electrons / pr::GreatestCommonFactor(e1.m_valence_electrons, e2.m_valence_holes);
		return 0;
	}

	char const* UnknownMaterialName = "unknown compound";

	Material::Material()
		:m_elem1()
		,m_elem2()
		,m_count1()
		,m_count2()
		,m_name()
		,m_name_symbolic()
		,m_name_common()
		,m_index()
		,m_bonds()
		,m_ionicity()
		,m_stable(false)
		,m_discovered(false)
	{}
	Material::Material(Element e1, Element e2, GameConstants const& consts)
		:m_elem1(e1.m_valence_electrons < e2.m_valence_electrons ? e1 : e2)
		,m_elem2(e1.m_valence_electrons < e2.m_valence_electrons ? e2 : e1)
		,m_count1(ElementRatio<1>(m_elem1, m_elem2))
		,m_count2(ElementRatio<2>(m_elem1, m_elem2))
		,m_name()
		,m_name_symbolic()
		,m_name_common()
		,m_index(MaterialIndex(e1,e2))
		,m_bonds()
		,m_ionicity(BondIonicity(m_elem1, m_elem2))
		,m_stable(false)
		,m_discovered(false)
	{
		using namespace EPerm2;
		UpdateName(UnknownMaterialName);
		if (e1.m_valence_electrons == 0 || e2.m_valence_electrons == 0)
			return;

		// Find the bond strengths for each permutation of elem1,elem2
		Bond bonds[NumberOf];
		BondStrengths(m_elem1, m_elem2, consts, bonds);
		for (int i = 0; i != NumberOf; ++i) m_bonds[i] = bonds[i];
		OrderByStrength(bonds);

		// Define the bond configuration.
		// All structures are basically long chains with the other element hanging off
		// e.g.
		//  B - A - A - A - B
		//      |   |   |
		//      B   B   B
		// The chain is formed from the highest bond strength
		switch (bonds[0].m_perm) {
		case AA:
			m_bonds[AA].m_count = m_count1 - 1; // A - A - A - A ...
			m_bonds[AB].m_count = m_count2;     // B   B   B
			break;
		case BB:
			m_bonds[BB].m_count = m_count2 - 1; // B - B - B - B ...
			m_bonds[AB].m_count = m_count1;     // A   A   A
			break;
		case AB:
			{
				// A-B = 1
				// A-B-A-B = 3
				// A-B-A-B-A-B = 5 ...
				size_t c = std::min(m_count1, m_count2);
				m_bonds[AB].m_count = c * 2 - 1    // A - B - A - B ...
					+ (m_count1-c) + (m_count2-c); // B       B
			}
			break;
		}

		// The bond energy of the material is the sum of the individual bond energies
		m_enthalpy =
			m_bonds[AA].m_count * m_bonds[AA].m_strength +
			m_bonds[AB].m_count * m_bonds[AB].m_strength +
			m_bonds[BB].m_count * m_bonds[BB].m_strength;
	}

	// Call to update the name
	void Material::UpdateName(std::string const& common_name)
	{
		// A material can be discovered independently of its elements.
		// In this case the name is arbitrary (assigned by player?)
		// If one element is known it gets a partial name 'Something-hydride', 'Sodium-something'
		bool e1_known = pr::AllSet(m_elem1.m_known_properties, EElemProp::Existence);
		bool e2_known = pr::AllSet(m_elem2.m_known_properties, EElemProp::Existence);
		m_name_common = common_name;

		// If both elements are known, use it's standard name
		if (e1_known && e2_known)
		{
			m_name          = MaterialName(m_elem1, m_count1, m_elem2, m_count2);
			m_name_symbolic = MaterialSymName(m_elem1, m_count1, m_elem2, m_count2);
		}
		else if (e1_known || e2_known)
		{
			auto& elem = e1_known ? m_elem1 : m_elem2;
			m_name = pr::FmtS("%s-??", elem.m_name->m_fullname);
			m_name_symbolic = pr::FmtS("%s-??", elem.m_name->m_symbol);
		}
		else
		{
			m_name = UnknownMaterialName;
			m_name_symbolic = "??";
		}
	}
}
